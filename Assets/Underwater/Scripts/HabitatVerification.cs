#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Abyss
{
    public class HabitatVerification : MonoBehaviour
    {
        readonly List<string> results = new();
        int errors;
        void OnEnable() => Application.logMessageReceived += Log;
        void OnDisable() => Application.logMessageReceived -= Log;
        void Log(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) errors++; }
        void Check(bool condition, string label) => results.Add((condition ? "PASS " : "FAIL ") + label);
        void Capture(string name)
        {
            var camera = Camera.main;
            var rt = RenderTexture.GetTemporary(1280, 720, 24);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes("Tools/Revision3/" + name + ".png", texture.EncodeToPNG());
            Destroy(texture);
            camera.targetTexture = previous;
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory("Tools/Revision3");
            Application.runInBackground = true;
            Time.timeScale = 1;
            yield return new WaitForSeconds(2);
            var habitat = FindFirstObjectByType<OceanHabitat>();
            var player = FindFirstObjectByType<FishPilot>();
            var background = FindFirstObjectByType<DistantOceanLife>();
            var body = player.GetComponent<Rigidbody2D>();
            var start = body.position;
            var houses = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Coral village home")).ToArray();
            Check(houses.Min(h => Vector2.Distance(body.position, h.bounds.ClosestPoint(body.position))) < 2, "Player starts immediately beside the moved village homes");
            var capsule = player.GetComponent<CapsuleCollider2D>();
            Check(!Physics2D.OverlapCapsuleAll(body.position + capsule.offset, capsule.size, capsule.direction, 0).Any(c => c.enabled && !c.isTrigger && !c.transform.IsChildOf(player.transform)), "Player start is clear of all house/terrain colliders");
            Check(Vector2.Distance(start, habitat.villageCenter) < 15, "Habitat center tracks the moved left-side village");
            Check(habitat.TargetCount(HabitatFamily.Shark, start) == 0 && habitat.Population.All(c => !c || c.definition.family != HabitatFamily.Shark), "No sharks at the village");
            Check(habitat.TargetCount(HabitatFamily.Shark, new Vector2(100, -15)) == 0 && habitat.TargetCount(HabitatFamily.Shark, new Vector2(-220, -15)) == 0, "No sharks in middle or left map regardless of village distance");
            Check(habitat.TargetCount(HabitatFamily.Whale, new Vector2(210, -20)) == 0 && habitat.Population.All(c => !c || c.definition.family != HabitatFamily.Whale), "Whales removed from gameplay population");
            Check(background && background.ActiveFishGroups >= 6 && background.ActiveWhales >= 1, "Background fish groups and distant whale are active");
            Check(FindObjectsByType<DistantSwimmer>(FindObjectsSortMode.None).All(c => c.GetComponent<SpriteRenderer>().sortingOrder < -30 && c.GetComponent<SpriteRenderer>().color.a <= .4f && !c.GetComponent<Collider2D>()), "Background life is muted, behind gameplay and non-colliding");
            Check(FindObjectsByType<DistantSwimmer>(FindObjectsSortMode.None).Any(c => c.GetComponent<SpriteRenderer>().sprite.name.StartsWith("shadow_3_")), "Original school silhouette sprites are used");
            Capture("village-start");
            player.testInput = Vector2.zero;
            body.position = new Vector2(207, -17);
            body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(12);
            var sharks = habitat.Population.Where(c => c && c.definition.family == HabitatFamily.Shark).ToArray();
            Check(sharks.Length > 0 && sharks.Length <= 3 && sharks.All(c => c.transform.position.x >= habitat.sharkRegionStartX), "Only a few sharks spawn, all inside rightmost region");
            int reefCount = habitat.Population.Count(c => c && c.definition.family == HabitatFamily.ReefFish);
            Check(reefCount > 0 && reefCount <= habitat.sharkRegionReefFishCount, "Sparse colorful fish coexist with right-edge sharks");
            Check(habitat.Population.All(c => !c || c.definition.family != HabitatFamily.Whale), "No foreground whales after entering the far-right region");
            var ray = habitat.species.First(s => s.family == HabitatFamily.Shark);
            Check(!habitat.Allowed(ray, new Vector2(habitat.sharkRegionStartX - .1f, -15)), "Shark wandering cannot cross out of its right-edge habitat");
            if (sharks.Length > 0)
            {
                body.position = (Vector2)sharks[0].transform.position + Vector2.left * 4;
                yield return new WaitForSeconds(1);
            }
            Capture("right-edge-life");
            Check(errors == 0, "No runtime errors during verification");
            player.testInput = null;
            body.position = start;
            body.linearVelocity = Vector2.zero;
            File.WriteAllLines("Tools/Revision3/playmode-results.txt", results);
            Debug.Log(string.Join("\n", results));
        }
    }
}
#endif
