using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in integration check. All movement overrides exist only in Play Mode.</summary>
[InitializeOnLoad]
public static class ShipWaterPlayCheck
{
    const string Key = "ShipWaterCheck";
    static ShipMovementFoam foam;
    static ShipController movement;
    static Rigidbody2D body;
    static Camera camera;
    static float start;
    static int initialFrame;
    static bool[] sampled = new bool[12];
    static bool teleported, disabled, initialized;
    static double pauseUntil;
    static float pauseIntensity;
    static StringBuilder report = new StringBuilder();
    static int failures;
    static Color32[] waveSample;
    static OceanSurfaceWavelets wavelets;
    static SpriteRenderer waveProbe;
    static Vector3 waveProbePosition;
    static float waveProbeTime;
    static ShipWaterPlayCheck()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + "Restore", false))
            {
                string previous = SessionState.GetString(Key + "StartScene", "");
                UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                bool background = SessionState.GetBool(Key + "Background", false);
                EditorApplication.delayCall += () => PlayerSettings.runInBackground = background;
                SessionState.SetBool(Key + "Restore", false);
                SessionState.SetBool(Key, false);
            }
        };
    }
    [MenuItem("Tools/Ship Water/Run Play Mode integration check")]
    public static void StartCheck()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode before starting the check.");
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + "Restore", true);
        SessionState.SetString(Key + "StartScene", AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene));
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/ShipScene.unity");
        SessionState.SetBool(Key + "Background", PlayerSettings.runInBackground);
        Selection.activeObject = null;
        EditorApplication.isPlaying = true;
    }
    static void Check(bool condition, string message)
    {
        report.AppendLine((condition ? "PASS " : "FAIL ") + message);
        if (!condition) failures++;
    }
    static void Sample(int index, bool condition, string message, string picture = null)
    {
        if (sampled[index]) return;
        sampled[index] = true;
        Check(condition, message);
        if (picture != null) Capture(picture);
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        EditorApplication.QueuePlayerLoopUpdate();
        if (!initialized)
        {
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/ShipScene.unity");
            if (!scene.isLoaded) return;
            // Preserve the user's other loaded/dirty scenes; only their runtime copies are hidden.
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var other = SceneManager.GetSceneAt(s);
                if (other == scene) continue;
                foreach (var root in other.GetRootGameObjects()) root.SetActive(false);
            }
            foam = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShipMovementFoam>()).First();
            movement = foam.movement; body = foam.body;
            movement.enabled = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var spawner in root.GetComponentsInChildren<EnemySpawner>()) { spawner.CancelInvoke(); spawner.StopAllCoroutines(); spawner.enabled = false; }
                foreach (var weapons in root.GetComponentsInChildren<ShipWeapons>()) weapons.enabled = false;
            }
            foreach (var enemy in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Enemy>())) enemy.gameObject.SetActive(false);
            camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).First(c => c.CompareTag("MainCamera"));
            start = Time.time; initialFrame = Time.frameCount;
            failures = 0; initialized = true;
            Application.runInBackground = true;
            sampled = new bool[12]; teleported = disabled = false; pauseUntil = 0;
            waveSample = null;
            waveProbe = null;
            report = new StringBuilder("Ship water Play Mode integration check\nVelocity-driven checks; keyboard input and combat are not exercised.\n");
            Check(foam.bowAnchor && foam.bowAnchor.localPosition.y > 7 && foam.bowFrames.Length == 4,
                "Bow cutting foam attached flush at front, with four animation frames");
            Check(foam.wakeFrames.Length == 4 && foam.wakeFrames[0] != foam.bowFrames[0],
                "Stern turbulence uses its own generated sprites");
            wavelets = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OceanSurfaceWavelets>()).Single();
            Check(foam.bowFrames[0].name.StartsWith("Bow_") && foam.transform.Find("Foam Bow").GetChild(0).localRotation == Quaternion.identity,
                "Supplied bow V points forward, with arms trailing down the hull");
            Check(foam.sternArcFrames.Length == 4 && foam.sternArcFrames[0].name.StartsWith("Arc_"),
                "Supplied stern wave arcs overlay the separate turbulence foam");
            Check(!wavelets.CanPlace(wavelets.shipHull.bounds.center, Vector2.one*2), "Surface waves reject the ship hull");
            // The current scene has no coast or harbour. Exercise the same exclusion path
            // with temporary coast/pier colliders in the test copy, never in the saved scene.
            var coast = new GameObject("TEST ONLY coast collider");
            SceneManager.MoveGameObjectToScene(coast,scene);
            coast.transform.position = new Vector3(18,0,0);
            coast.AddComponent<BoxCollider2D>().size = new Vector2(8,12);
            var pier = new GameObject("TEST ONLY pier collider");
            SceneManager.MoveGameObjectToScene(pier,scene);
            pier.transform.position = new Vector3(-18,0,0);
            pier.AddComponent<BoxCollider2D>().size = new Vector2(3,18);
            Physics2D.SyncTransforms();
            Check(!wavelets.CanPlace(coast.transform.position,Vector2.one*2) && !wavelets.CanPlace(pier.transform.position,Vector2.one*2),
                "Surface waves reject test coast and pier colliders");
            bool oldBounds = wavelets.useWaterBounds;
            Rect originalBounds = wavelets.waterBounds;
            wavelets.useWaterBounds = true;
            wavelets.waterBounds = new Rect(-30,-30,60,60);
            Check(!wavelets.CanPlace(new Vector2(31,0),Vector2.one*2), "Surface waves respect optional water bounds");
            wavelets.useWaterBounds = oldBounds; wavelets.waterBounds = originalBounds;
        }
        if (Time.frameCount <= initialFrame + 2) return;
        float t = Time.time - start;
        if (t < 1) body.linearVelocity = Vector2.zero;
        else if (t < 3) body.linearVelocity = movement.transform.up * 3;
        else if (t < 7) body.linearVelocity = movement.transform.up * 15;
        else if (t < 8.5f) body.linearVelocity = -movement.transform.up * 7;
        else if (t < 10.5f) body.linearVelocity = Vector2.zero;
        else body.linearVelocity = movement.transform.up * 15;
        if (t > 5 && t < 7) movement.transform.Rotate(0, 0, -160 * Time.deltaTime);
        if (t > 2 && t < 2.5f && !waveProbe)
        {
            waveProbe = wavelets.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r => r.enabled && r.color.a > wavelets.opacity*.95f && wavelets.types.Any(type => type.frames[1] == r.sprite));
            if (waveProbe) { waveProbePosition=waveProbe.transform.position; waveProbeTime=t; }
        }
        if (waveProbe && t > waveProbeTime+.06f && !sampled[10])
            Sample(10, waveProbe.enabled && waveProbe.transform.position == waveProbePosition, "Camera tracking does not move an existing surface wave");
        if (t > .6f && t < .85f && waveSample == null) waveSample = Capture("waves-a");
        if (t > .9f && t < 1 && waveSample != null && !sampled[9])
        {
            var nextWave = Capture("waves-b");
            int changed = 0;
            for (int y = 60; y < 740; y += 8) for (int x = 40; x < 300; x += 8)
            {
                int p = y * 1200 + x;
                if (Math.Abs(nextWave[p].g - waveSample[p].g) > 1) changed++;
            }
            Sample(9, changed > 30, "Wave shading changes while the ship and camera are stationary");
        }
        if (t > .7f && t < 1) Sample(0, foam.Intensity < .01f && foam.ActiveTrailCount == 0, "Idle: no foam", "idle");
        if (t > 2.5f && t < 3) Sample(1, foam.Intensity > .01f && foam.Intensity < .2f, "Slow: subdued foam");
        if (t > 4.5f && t < 5) Sample(2, foam.Intensity > .9f && foam.ActiveTrailCount > 0, "Full speed: visible wake", "full-speed");
        if (t > 4 && !sampled[11])
        {
            var active = wavelets.GetComponentsInChildren<SpriteRenderer>().Where(r => r.enabled).ToArray();
            bool separated = true;
            for (int i=0; i<active.Length; i++) for(int j=i+1; j<active.Length; j++)
                if (Vector2.Distance(active[i].transform.position,active[j].transform.position) < wavelets.minimumDistance-.01f) separated=false;
            Sample(11, active.Length>0 && active.Length<=wavelets.activeLimit && wavelets.PoolCount==16 && separated,
                "Surface waves remain sparse, separated and bounded by their pool");
        }
        if (t > 6.5f && t < 7)
        {
            var trail = SceneManager.GetSceneByPath("Assets/Scenes/ShipScene.unity").GetRootGameObjects().First(r => r.name == "Ship foam trail (pooled)");
            var segments = trail.GetComponentsInChildren<SpriteRenderer>().Where(r => r.enabled).ToArray();
            bool independent = segments.Any(r => Quaternion.Angle(r.transform.rotation, movement.transform.rotation) > 8);
            Sample(3, independent, "Turning: previous segments retain world rotation", "turn");
        }
        if (t > 8.2f && t < 8.5f) Sample(4, foam.Intensity < .01f && foam.ActiveTrailCount == 0, "Reverse: no strong forward foam");
        if (t > 10 && t < 10.5f) Sample(5, foam.Intensity < .01f && foam.ActiveTrailCount == 0, "Stopped: foam fades and trail expires", "stopped");
        if (t > 12 && t < 12.5f) Sample(6, foam.Intensity > .9f && foam.ActiveTrailCount > 0, "Restarting movement: foam resumes");
        if (t > 12.5f && !teleported) { body.position += Vector2.one * 100; teleported = true; }
        if (t > 12.5f && t < 12.7f && foam.ActiveTrailCount == 0) Sample(7, true, "Teleport: old wake cleared");
        if (t > 13.5f && !disabled) { foam.enabled = false; disabled = true; Check(foam.ActiveTrailCount == 0, "Disable clears trail"); }
        if (t > 14 && !foam.enabled) { foam.enabled = true; Check(foam.ActiveTrailCount == 0, "Enable starts clean"); }
        if (t > 15 && pauseUntil == 0)
        { pauseUntil = EditorApplication.timeSinceStartup + .7; pauseIntensity = foam.Intensity; Time.timeScale = 0; }
        if (Time.timeScale == 0 && EditorApplication.timeSinceStartup >= pauseUntil)
        { Check(Mathf.Approximately(pauseIntensity, foam.Intensity), "Pause freezes visual simulation"); Time.timeScale = 1; }
        if (t > 20 && !sampled[8])
        {
            Sample(8, Time.frameCount - initialFrame > 100, "Player frames advance during check");
            var trail = SceneManager.GetSceneByPath("Assets/Scenes/ShipScene.unity").GetRootGameObjects().First(r => r.name == "Ship foam trail (pooled)");
            Check(trail.GetComponentsInChildren<SpriteRenderer>(true).Length == foam.poolSize && foam.ActiveTrailCount <= foam.poolSize, "Trail object count remains bounded after sustained movement");
            Check(sampled.Take(8).All(v => v), "All movement cases sampled");
            var water = SceneManager.GetSceneByPath("Assets/Scenes/ShipScene.unity").GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<InfiniteOcean>()).First();
            var bounds = water.GetComponent<SpriteRenderer>().bounds;
            float h = camera.orthographicSize;
            Check(bounds.Contains(new Vector3(camera.transform.position.x - h * camera.aspect, camera.transform.position.y - h, bounds.center.z)) && bounds.Contains(new Vector3(camera.transform.position.x + h * camera.aspect, camera.transform.position.y + h, bounds.center.z)), "Ocean covers camera after movement and teleport");
            Finish();
        }
    }
    static Color32[] Capture(string name)
    {
        Directory.CreateDirectory("Logs/ShipWater");
        var target = RenderTexture.GetTemporary(1200, 800, 24);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1200, 800, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1200, 800), 0, 0); image.Apply();
            File.WriteAllBytes("Logs/ShipWater/" + name + ".png", image.EncodeToPNG());
            var pixels = image.GetPixels32();
            UnityEngine.Object.DestroyImmediate(image);
            return pixels;
        }
        finally { camera.targetTexture = oldTarget; RenderTexture.active = oldActive; RenderTexture.ReleaseTemporary(target); }
    }
    static void Finish()
    {
        report.AppendLine("Failures: " + failures);
        Directory.CreateDirectory("Logs/ShipWater");
        File.WriteAllText("Logs/ShipWater/play-check.txt", report.ToString());
        Debug.Log(report.ToString());
        Application.runInBackground = SessionState.GetBool(Key + "Background", false);
        SessionState.SetBool(Key, false);
        initialized = false;
        Time.timeScale = 1;
        EditorApplication.isPlaying = false;
    }
}
