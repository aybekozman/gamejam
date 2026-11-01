using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Abyss.EditorTools
{
    [InitializeOnLoad]
    public static class CoastalLayoutFinish
    {
        static CoastalLayoutFinish() { EditorApplication.update += Poll; }
        static void Poll()
        {
            const string path = "Temp/coastal-command.txt";
            if (!File.Exists(path) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string command = File.ReadAllText(path).Trim();
            File.Delete(path);
            try { if (command == "apply") Apply(); }
            catch (System.Exception error) { Debug.LogException(error); }
        }

        static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Underwater/Resources/Abyss/" + name + ".png");
        static Sprite[] Frames(string sheet, int row) => Enumerable.Range(0, 4).Select(i => S($"{sheet}_{row}_{i}")).ToArray();

        [MenuItem("Underwater/Update village start and distant life (preserve layout)")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode before updating the scene.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/UnderwaterWorld.unity") throw new System.InvalidOperationException("Open UnderwaterWorld first.");
            var pilot = Object.FindFirstObjectByType<FishPilot>();
            var camera = Camera.main;
            var habitat = Object.FindFirstObjectByType<OceanHabitat>();
            var homes = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r => r.gameObject.scene == scene && r.name.StartsWith("Coral village home")).ToArray();
            if (homes.Length == 0) throw new System.InvalidOperationException("No village homes found.");
            Directory.CreateDirectory("Tools/Revision3");
            EditorSceneManager.SaveScene(scene);
            if (!File.Exists("Tools/Revision3/user-layout-before.unity")) File.Copy(scene.path, "Tools/Revision3/user-layout-before.unity");
            var saved = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.gameObject.scene == scene && t != pilot.transform && t != camera.transform)
                .Select(t => new { transform = t, position = t.localPosition, scale = t.localScale, rotation = t.localRotation }).ToArray();
            var colliders = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Where(c => c.gameObject.scene == scene).Select(c => new { collider = c, json = EditorJsonUtility.ToJson(c) }).ToArray();

            habitat.villageCenter = homes.Aggregate(Vector2.zero, (sum, home) => sum + (Vector2)home.transform.position) / homes.Length;
            habitat.sharkRegionStartX = habitat.worldMax.x - 60;
            habitat.sharkRegionReefFishCount = 3;
            habitat.sharksPerArea = 3;
            Physics2D.SyncTransforms();
            var home = homes.OrderBy(r => Vector2.Distance(r.transform.position, habitat.villageCenter)).First();
            Bounds bounds = home.bounds;
            foreach (var collider in home.GetComponents<Collider2D>()) if (collider.enabled) bounds.Encapsulate(collider.bounds);
            var capsule = pilot.GetComponent<CapsuleCollider2D>();
            Vector2 spawn = default;
            bool found = false;
            var preferred = new[] {
                new Vector2(bounds.max.x + 1, bounds.center.y + .7f),
                new Vector2(bounds.min.x - 1, bounds.center.y + .7f),
                new Vector2(bounds.center.x, bounds.max.y + .7f)
            };
            foreach (var point in preferred)
            {
                for (int step = 0; step < 12 && !found; step++)
                {
                    var candidate = point + Vector2.up * step * .35f;
                    if (candidate.x < pilot.swimMin.x + 1 || candidate.x > pilot.swimMax.x - 1 || candidate.y < pilot.swimMin.y + .5f || candidate.y > pilot.surfaceY - 1) continue;
                    var hits = Physics2D.OverlapCapsuleAll(candidate + capsule.offset, capsule.size * 1.15f, capsule.direction, 0);
                    if (hits.Any(c => c.enabled && !c.isTrigger && !c.transform.IsChildOf(pilot.transform))) continue;
                    spawn = candidate;
                    found = true;
                }
                if (found) break;
            }
            if (!found) throw new System.InvalidOperationException("No clear start next to the central village home.");
            pilot.transform.position = new Vector3(spawn.x, spawn.y, pilot.transform.position.z);
            var follow = camera.GetComponent<FollowCamera>();
            float halfWidth = camera.orthographicSize * camera.aspect;
            camera.transform.position = new Vector3(Mathf.Clamp(spawn.x, follow.worldMin.x + halfWidth, follow.worldMax.x - halfWidth), Mathf.Clamp(spawn.y + .7f, follow.worldMin.y + follow.orthographicSize, follow.worldMax.y - follow.orthographicSize), -10);
            var background = Object.FindFirstObjectByType<DistantOceanLife>();
            if (!background) background = new GameObject("Distant ocean life • schools and whales").AddComponent<DistantOceanLife>();
            background.singleFish = Enumerable.Range(0, 3).SelectMany(row => Frames("shadow", row)).ToArray();
            background.schoolFrames = Frames("shadow", 3);
            background.blueWhaleFrames = Frames("whale", 0);
            background.humpbackFrames = Frames("whale", 1);
            background.surfaceY = pilot.surfaceY;
            foreach (var item in saved)
                if (item.transform.localPosition != item.position || item.transform.localRotation != item.rotation || item.transform.localScale != item.scale)
                    throw new System.InvalidOperationException("Existing layout was changed: " + item.transform.name);
            foreach (var item in colliders)
                if (EditorJsonUtility.ToJson(item.collider) != item.json) throw new System.InvalidOperationException("Existing collider changed: " + item.collider.name);
            EditorUtility.SetDirty(habitat);
            EditorUtility.SetDirty(pilot);
            EditorUtility.SetDirty(background);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Tools/Revision3/layout-result.txt", $"PASS {saved.Length} existing transforms preserved; {colliders.Length} collider definitions preserved exactly.\nHomes={homes.Length}; village center={habitat.villageCenter}; player start={spawn}.\nSharks only at X >= {habitat.sharkRegionStartX}; sparse colorful fish there; whales background only.");
            Debug.Log("Village start, right-edge sharks and distant schools/whales applied while preserving the authored map.");
        }
    }
}
