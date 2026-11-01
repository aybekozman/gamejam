using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Opt-in GPU image comparisons and scaled-clock checks; modifies only Play Mode copies.</summary>
[InitializeOnLoad]
public static class OceanCrestPlayCheck
{
    const string Session="OceanCrestCheck";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static InfiniteOcean water;
    static SpriteRenderer surface;
    static MaterialPropertyBlock properties;
    static EditorWindow game;
    static Type gameType;
    static StringBuilder report;
    static double start;
    static int stage,failures,firstFrame;
    static bool initialized;
    static float pausedClock;
    static Color32[] live;
    static Color32[] original, a, sameStep, b;
    static string label;
    static OceanCrestPlayCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Session+"Restore",false)) return;
            var path=SessionState.GetString(Session+"Start","");
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            bool background=SessionState.GetBool(Session+"Background",false);
            EditorApplication.delayCall+=()=>PlayerSettings.runInBackground=background;
            FindGame();gameType.GetProperty("selectedSizeIndex",Flags).SetValue(game,SessionState.GetInt(Session+"Size",0));
            SessionState.SetBool(Session,false);SessionState.SetBool(Session+"Restore",false);
        };
    }
    [MenuItem("Tools/Ship Water/Check HUD crest animation")]
    public static void CheckHUD()=>StartCheck("HudFirstTry");
    [MenuItem("Tools/Ship Water/Check world-space crest animation")]
    public static void CheckWorld()=>StartCheck("ShipScene");
    public static void StartCheck(string sceneName)
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop Play Mode first.");
        FindGame(); SessionState.SetInt(Session+"Size",(int)gameType.GetProperty("selectedSizeIndex",Flags).GetValue(game));
        SessionState.SetString(Session+"Start",AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene));
        SessionState.SetBool(Session+"Background",PlayerSettings.runInBackground);
        SessionState.SetString(Session+"Scene",sceneName);SessionState.SetBool(Session,true);SessionState.SetBool(Session+"Restore",true);
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/"+sceneName+".unity");
        gameType.GetMethod("SetCustomResolution",Flags).Invoke(game,new object[]{new Vector2(1280,720),"Crest check"});
        EditorApplication.isPlaying=true;
    }
    static void FindGame()
    {
        gameType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        game=Resources.FindObjectsOfTypeAll(gameType).OfType<EditorWindow>().FirstOrDefault();
        if(!game) game=EditorWindow.GetWindow(gameType);
    }
    static void Check(bool good,string message) {report.AppendLine((good?"PASS ":"FAIL ")+message);if(!good) failures++;}
    static void Tick()
    {
        if(!SessionState.GetBool(Session,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if(!initialized)
            {
                label=SessionState.GetString(Session+"Scene","");
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/"+label+".unity");if(!scene.isLoaded) return;
                water=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<InfiniteOcean>()).Single();
                for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                {var other=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);if(other!=scene) foreach(var root in other.GetRootGameObjects()) root.SetActive(false);}
                foreach(var root in scene.GetRootGameObjects())
                {
                    foreach(var spawner in root.GetComponentsInChildren<EnemySpawner>()) {spawner.StopAllCoroutines();spawner.enabled=false;}
                    foreach(var weapons in root.GetComponentsInChildren<ShipWeapons>()) weapons.enabled=false;
                    foreach(var enemy in root.GetComponentsInChildren<Enemy>()) enemy.gameObject.SetActive(false);
                }
                FindGame(); surface=water.GetComponent<SpriteRenderer>();properties=new MaterialPropertyBlock();
                report=new StringBuilder(label+" crest shader Play Mode / GPU checks\n");failures=0;stage=0;
                Check(surface.sharedMaterial.shader.name=="Ship/Calm Pixel Ocean"&&!ShaderUtil.ShaderHasError(surface.sharedMaterial.shader),"Correct shader bound; shader compiles");
                Check(water.crestDisplacementPixels>0&&water.crestDisplacementPixels<=2,"Subtle pixel displacement configured");
                if(!water.worldSpaceWater) Check(water.GetComponent<Animator>().enabled,"Original sprite Animator retained");
                Application.runInBackground=true;start=EditorApplication.timeSinceStartup;firstFrame=Time.frameCount;initialized=true;
            }
            double t=EditorApplication.timeSinceStartup-start;
            if(stage==0&&t>.7)
            {
                live=Capture(label+"-live-a");surface.GetPropertyBlock(properties);pausedClock=properties.GetFloat("_OceanTime");
                Check(pausedClock>0,"Shader receives advancing scaled clock");
                Check(properties.GetFloat("_UseSpriteUV")== (water.worldSpaceWater?0:1),"Correct world-space / sprite-atlas sampling path");
                Time.timeScale=0;stage++;
            }
            if(stage==1&&t>1.2)
            {
                surface.GetPropertyBlock(properties);Check(properties.GetFloat("_OceanTime")==pausedClock,"Pause freezes crest clock");Time.timeScale=1;stage++;
            }
            if(stage==2&&t>2)
            {
                Capture(label+"-live-b");surface.GetPropertyBlock(properties);
                Check(properties.GetFloat("_OceanTime")>pausedClock&&Time.frameCount>firstFrame+8,"Resume advances shader clock and real game frames");
                // Hold all existing water motion still to isolate the new effect on the GPU.
                water.enabled=false; var animator=water.GetComponent<Animator>();if(animator) animator.enabled=false;
                Time.timeScale=0;
                properties.SetVector("_Flow",Vector4.zero);properties.SetFloat("_OceanTime",0);
                properties.SetVector("_CrestMotion",Vector4.zero);surface.SetPropertyBlock(properties);
                stage++;
            }
            if(stage==3&&t>2.3)
            {
                original=Capture(null);
                properties.SetVector("_CrestMotion",new Vector4(1.25f,1.1f,.14f,6));surface.SetPropertyBlock(properties);
                stage++;
            }
            if(stage==4&&t>2.6)
            {
                a=Capture(label+"-isolated-a");
                properties.SetFloat("_OceanTime",.01f);surface.SetPropertyBlock(properties);stage++;
            }
            if(stage==5&&t>2.9)
            {
                sameStep=Capture(null);properties.SetFloat("_OceanTime",2);surface.SetPropertyBlock(properties);stage++;
            }
            if(stage==6&&t>3.2)
            {
                b=Capture(label+"-isolated-b");
                int changed=0,touched=0,quantizedChanges=0,pixels=0,magenta=0;double delta=0;
                // Water-only rectangle, clear of the ship and corner HUDs.
                for(int y=180;y<540;y++) for(int x=80;x<470;x++)
                {
                    int p=y*Screen.width+x;pixels++;
                    int difference=Math.Abs(a[p].r-b[p].r)+Math.Abs(a[p].g-b[p].g)+Math.Abs(a[p].b-b[p].b);
                    if(difference>3) changed++;delta+=difference/3.0;
                    if(!a[p].Equals(sameStep[p])) quantizedChanges++;
                    if(!original[p].Equals(a[p])) touched++;
                    if(b[p].r>200&&b[p].b>200&&b[p].g<60) magenta++;
                }
                Check(changed>20,"Crests animate with original current and Animator held still: "+changed+" changed pixels");
                Check(quantizedChanges==0,"6 FPS time steps do not crawl between frames");
                Check(touched<pixels*.4&&delta/pixels<6,"Most sea pixels retained; restrained change (touched "+touched+"/"+pixels+", mean delta "+(delta/pixels).ToString("F3")+")");
                Check(magenta==0,"No shader-error magenta pixels");
                properties.SetVector("_CrestMotion",Vector4.zero);properties.SetFloat("_OceanTime",0);surface.SetPropertyBlock(properties);
                stage++;
            }
            if(stage==7&&t>3.5)
            {
                var disabled=Capture(null);Check(original.SequenceEqual(disabled),"Zero displacement exactly restores unmodified water sampling");
                Finish();
            }
        }
        catch(Exception error){if(report==null) report=new StringBuilder();Check(false,error.ToString());Finish();}
    }
    static Color32[] Capture(string name)
    {
        Canvas.ForceUpdateCanvases();
        var target=(RenderTexture)gameType.GetMethod("RenderView",Flags).Invoke(game,new object[]{new Vector2(Screen.width,Screen.height),false});
        var old=RenderTexture.active;
        try
        {
            RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            var rows=image.GetPixels32();
            if(SystemInfo.graphicsUVStartsAtTop) for(int y=0;y<image.height/2;y++) for(int x=0;x<image.width;x++)
            {int a=y*image.width+x,b=(image.height-1-y)*image.width+x;var pixel=rows[a];rows[a]=rows[b];rows[b]=pixel;}
            if(name!=null){image.SetPixels32(rows);image.Apply();Directory.CreateDirectory("Logs/OceanCrests");File.WriteAllBytes("Logs/OceanCrests/"+name+".png",image.EncodeToPNG());}
            UnityEngine.Object.DestroyImmediate(image);return rows;
        }
        finally{RenderTexture.active=old;}
    }
    static void Finish()
    {
        Time.timeScale=1;report.AppendLine("Failures: "+failures);
        Directory.CreateDirectory("Logs/OceanCrests");File.WriteAllText("Logs/OceanCrests/"+label+"-check.txt",report.ToString());Debug.Log(report.ToString());
        initialized=false;SessionState.SetBool(Session,false);EditorApplication.isPlaying=false;
    }
}
