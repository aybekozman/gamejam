using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Abyss.EditorTools
{
    [InitializeOnLoad]
    public static class HabitatUpgrade
    {
        static string Command => Path.GetFullPath("Temp/habitat-command.txt");
        static HabitatUpgrade() {
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("HabitatVerify",false)) return;
                SessionState.SetBool("HabitatVerify",false);
                EditorApplication.isPaused=false;
                Application.runInBackground=true;
                Camera.main.gameObject.AddComponent<HabitatVerification>();
            };
        }

        static void Poll()
        {
            if (!File.Exists(Command) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string command = File.ReadAllText(Command).Trim();
            File.Delete(Command);
            try
            {
                if (command == "inspect") Inspect();
                if (command == "refresh") AssetDatabase.Refresh();
                if (command == "upgrade") Upgrade();
                if (command == "seams") RepairExtensionSeams();
                if (command == "verify") { EditorApplication.isPaused=false;SessionState.SetBool("HabitatVerify", true); if(EditorApplication.isPlaying){SessionState.SetBool("HabitatVerify",false);Application.runInBackground=true;Camera.main.gameObject.AddComponent<HabitatVerification>();}else EditorApplication.isPlaying = true; }
                if (command == "stop") EditorApplication.isPlaying = false;
            }
            catch (System.Exception e) { Debug.LogException(e); }
        }

        public static void Inspect()
        {
            var scene = SceneManager.GetActiveScene();
            var lines = new List<string> { $"Scene={scene.path}; dirty={scene.isDirty}; playing={EditorApplication.isPlaying}; paused={EditorApplication.isPaused}; gameTime={Time.time}; timeScale={Time.timeScale}; testComponents={Object.FindObjectsByType<HabitatVerification>(FindObjectsSortMode.None).Length}" };
            foreach (var go in scene.GetRootGameObjects())
                lines.Add($"{go.name} | pos={go.transform.position} scale={go.transform.localScale} active={go.activeSelf} | " + string.Join(",", go.GetComponents<Collider2D>().Select(c => c.GetType().Name + ":" + c.enabled)));
            Directory.CreateDirectory("Tools/Revision2");
            File.WriteAllLines("Tools/Revision2/live-scene.txt", lines);
        }

        static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Underwater/Resources/Abyss/" + name + ".png");
        static Sprite[] Frames(string name, int row) => Enumerable.Range(0, 4).Select(i => Sprite($"{name}_{row}_{i}")).ToArray();
        static OceanSpecies Species(string name, HabitatFamily family, string sheet, int row, float width, float speed, float radius, float rate = 4)
            => new OceanSpecies { name = name, family = family, frames = Frames(sheet, row), width = width, speed = speed, roamingRadius = radius, animationRate = rate };

        static void RepairExtensionSeams()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first.");
            var group=GameObject.Find("Ocean expansion • preserved layout");
            foreach(var renderer in group.GetComponentsInChildren<SpriteRenderer>()){
                if(renderer.name!="World anchored water gradient • extended" && renderer.name!="Sky • extended" && renderer.name!="Surface • extended")continue;
                int x=Mathf.RoundToInt(Mathf.Abs(renderer.transform.position.x));
                renderer.flipX=((((x-8)/16&1)==1)!=(renderer.transform.position.x<0));
            }
            EditorSceneManager.MarkSceneDirty(group.scene);
            EditorSceneManager.SaveScene(group.scene);
        }

        class Preserved
        {
            public Transform transform;
            public Vector3 position, scale;
            public Quaternion rotation;
            public Collider2D[] colliders;
            public bool[] enabled;
        }

        [MenuItem("Underwater/Extend current scene (preserve authored layout)")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode before extending the scene.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/UnderwaterWorld.unity") throw new System.InvalidOperationException("Open UnderwaterWorld before extending it.");
            var roots = scene.GetRootGameObjects();
            if (roots.Any(g => g.name == "Ocean expansion • preserved layout"))
            {
                File.WriteAllText("Tools/Revision2/upgrade-result.txt", "Expansion already present; existing work preserved.");
                return;
            }
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Tools/Revision2");
            File.Copy(scene.path, "Tools/Revision2/UnderwaterWorld.before-expansion.unity", true);
            var preserved = roots.Where(g => g.name != "West boundary" && g.name != "East boundary").Select(g => new Preserved
            {
                transform = g.transform, position = g.transform.position, rotation = g.transform.rotation, scale = g.transform.localScale,
                colliders = g.GetComponents<Collider2D>(), enabled = g.GetComponents<Collider2D>().Select(c => c.enabled).ToArray()
            }).ToArray();
            var group = new GameObject("Ocean expansion • preserved layout");
            string[] repeatNames = { "Seabed", "Reef rock", "Rooted kelp", "Middle reef", "Far rock silhouette", "Submerged sun shaft" };
            var templates = roots.Where(g => repeatNames.Contains(g.name) && g.transform.position.x >= -48 && g.transform.position.x < 48).ToArray();
            foreach (int offset in new[] { -192, -96, 96, 192 })
            {
                var segment = new GameObject("Repeated coast " + offset);
                segment.transform.SetParent(group.transform, false);
                foreach (var template in templates)
                {
                    var clone = Object.Instantiate(template, segment.transform);
                    clone.name = template.name + " • extended";
                    clone.transform.position = template.transform.position + Vector3.right * offset;
                    if (template.name == "Reef rock") foreach (var collider in clone.GetComponents<Collider2D>()) collider.enabled = false;
                    var drift = clone.GetComponent<Drift>();
                    if (drift && drift.plant) drift.phase += Mathf.Abs(offset) * .037f;
                }
            }
            foreach (string name in new[] { "World anchored water gradient", "Sky", "Surface" })
            {
                var template = roots.First(g => g.name == name);
                for (int x = 72; x <= 248; x += 16)
                foreach (int direction in new[] { -1, 1 })
                {
                    var clone = Object.Instantiate(template, group.transform);
                    clone.name = name + " • extended";
                    clone.transform.position = new Vector3(x * direction, template.transform.position.y, template.transform.position.z);
                    clone.GetComponent<SpriteRenderer>().flipX = (((x - 8) / 16 & 1) == 1) != (direction < 0);
                    var ripple = clone.GetComponent<SurfaceRipple>();
                    if (ripple) ripple.phase = x * .1f;
                }
            }
            var deepTemplate = roots.First(g => g.name == "Deep continuation");
            var deep = Object.Instantiate(deepTemplate, group.transform);
            deep.name = "Extended deep continuation";
            deep.transform.localScale = new Vector3(512 / deep.GetComponent<SpriteRenderer>().sprite.bounds.size.x, deepTemplate.transform.localScale.y, 1);

            var pilot = Object.FindFirstObjectByType<FishPilot>();
            pilot.tilt = 58;
            pilot.swimMin = new Vector2(-239.3f, pilot.swimMin.y);
            pilot.swimMax = new Vector2(239.3f, pilot.surfaceY);
            EditorUtility.SetDirty(pilot);
            var camera = Object.FindFirstObjectByType<FollowCamera>();
            camera.worldMin.x = -240;
            camera.worldMax.x = 240;
            EditorUtility.SetDirty(camera);
            foreach (var boundary in roots.Where(g => g.name == "West boundary" || g.name == "East boundary"))
                boundary.transform.position = new Vector3(boundary.name == "West boundary" ? -241 : 241, boundary.transform.position.y, boundary.transform.position.z);

            var habitat = group.AddComponent<OceanHabitat>();
            habitat.player = pilot.transform;
            var homes = roots.Where(g => g.name.StartsWith("Coral village home")).ToArray();
            habitat.villageCenter = homes.Length > 0 ? (Vector2)(homes.Aggregate(Vector3.zero, (sum, home) => sum + home.transform.position) / homes.Length) : new Vector2(-30, -29);
            habitat.species = new[]
            {
                Species("Purple reef fish", HabitatFamily.ReefFish,"reef",0,1.25f,1.1f,9),
                Species("Turquoise reef fish", HabitatFamily.ReefFish,"reef",1,1.15f,1.25f,10),
                Species("Spotted reef fish", HabitatFamily.ReefFish,"reef",2,1.2f,.95f,9),
                Species("Yellow fish", HabitatFamily.ReefFish,"fish",0,1,1.1f,9),
                Species("Striped fish", HabitatFamily.ReefFish,"fish",1,1.1f,1.2f,10),
                Species("Pink fish", HabitatFamily.ReefFish,"fish",2,1.15f,1.3f,10),
                Species("Blue fish", HabitatFamily.ReefFish,"fish",3,1.35f,1.5f,12),
                Species("Turtle", HabitatFamily.GentleLife,"animal",0,2.2f,.65f,10,3),
                Species("Jellyfish", HabitatFamily.GentleLife,"animal",1,1.1f,.4f,6,3),
                Species("Ray", HabitatFamily.GentleLife,"animal",2,2.6f,.85f,12,3),
                Species("Seahorse", HabitatFamily.GentleLife,"animal",3,.85f,.3f,4,3),
                Species("Reef shark", HabitatFamily.Shark,"shark",0,4,1.5f,20,4),
                Species("Deep shark", HabitatFamily.Shark,"shark",1,4.6f,1.35f,20,4),
                Species("Blue whale", HabitatFamily.Whale,"whale",0,7,.7f,22,3),
                Species("Humpback whale", HabitatFamily.Whale,"whale",1,7.5f,.65f,22,3)
            };
            foreach (var saved in preserved)
            {
                if (saved.transform.position != saved.position || saved.transform.rotation != saved.rotation || saved.transform.localScale != saved.scale)
                    throw new System.InvalidOperationException("Authored transform changed: " + saved.transform.name);
                for (int i = 0; i < saved.colliders.Length; i++)
                    if (saved.colliders[i].enabled != saved.enabled[i]) throw new System.InvalidOperationException("Authored collider changed: " + saved.transform.name);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Tools/Revision2/upgrade-result.txt", $"PASS {preserved.Length} authored transforms and all their collider enabled states preserved.\nWorld extended horizontally to [-240,240]. Village center={habitat.villageCenter}.\nOnly camera/swim limits, player pitch and outer boundary positions updated.");
            Debug.Log("Habitat expansion applied without rebuilding the authored scene.");
        }
    }
}
