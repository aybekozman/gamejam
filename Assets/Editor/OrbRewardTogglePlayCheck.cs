using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Opt-in checks against the real HUD and loot in a disposable Play Mode session.</summary>
[InitializeOnLoad]
public static class OrbRewardTogglePlayCheck
{
    const string Session="OrbRewardCheck";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static ShipHUDController hud;
    static OrbRewardMode mode;
    static OrbRewardToggleView view;
    static Keyboard keyboard;
    static EditorWindow game;
    static Type gameType;
    static StringBuilder report;
    static double start;
    static int stage,failures,changes;
    static bool initialized;
    static OrbRewardTogglePlayCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Session+"Restore",false)) return;
            string path=SessionState.GetString(Session+"Scene","");
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            bool background=SessionState.GetBool(Session+"Background",false);
            EditorApplication.delayCall+=()=>PlayerSettings.runInBackground=background;
            FindGame(); gameType.GetProperty("selectedSizeIndex",Flags).SetValue(game,SessionState.GetInt(Session+"Size",0));
            SessionState.SetBool(Session,false); SessionState.SetBool(Session+"Restore",false);
        };
    }
    [MenuItem("Tools/Ship HUD/Run orb selector checks")]
    public static void StartCheck()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop Play Mode first.");
        FindGame();
        SessionState.SetInt(Session+"Size",(int)gameType.GetProperty("selectedSizeIndex",Flags).GetValue(game));
        SessionState.SetString(Session+"Scene",AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene));
        SessionState.SetBool(Session+"Background",PlayerSettings.runInBackground);
        SessionState.SetBool(Session,true); SessionState.SetBool(Session+"Restore",true);
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ShipHUDSetup.ScenePath);
        Resolution(1920,1080); EditorApplication.isPlaying=true;
    }
    static void FindGame()
    {
        gameType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        game=Resources.FindObjectsOfTypeAll(gameType).OfType<EditorWindow>().FirstOrDefault();
        if(!game) game=EditorWindow.GetWindow(gameType);
    }
    static void Resolution(int w,int h)
    {
        gameType.GetMethod("SetCustomResolution",Flags).Invoke(game,new object[]{new Vector2(w,h),"Orb selector check"});
        game.Repaint(); EditorApplication.QueuePlayerLoopUpdate();
    }
    static void Check(bool good,string message) { report.AppendLine((good?"PASS ":"FAIL ")+message); if(!good) failures++; }
    static void Press(bool down)=>InputSystem.QueueStateEvent(keyboard,down?new KeyboardState(UnityEngine.InputSystem.Key.Q):new KeyboardState());
    static void Award(int amount)
    {
        var loot=new GameObject("TEST orb").AddComponent<Loot>(); loot.expValue=amount;
        Check(loot.TryCollect(hud.experience) && !loot.TryCollect(hud.experience),"Orb awards once: "+amount);
    }
    static void Click(bool energy)
    {
        var position=RectTransformUtility.WorldToScreenPoint(null,view.transform.TransformPoint(new Vector3(energy?75:225,-22,0)));
        var data=new PointerEventData(EventSystem.current){position=position,button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
        Check(hits.Count>0 && hits[0].gameObject==view.gameObject,"Selector receives actual UI raycast");
        if(hits.Count>0) ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Session,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if(!initialized)
            {
                FindGame();
                hud=UnityEngine.Object.FindFirstObjectByType<ShipHUDController>(); if(!hud) return;
                mode=hud.ship.GetComponent<OrbRewardMode>(); view=hud.GetComponentInChildren<OrbRewardToggleView>();
                report=new StringBuilder("HudFirstTry orb selector Play Mode checks\n"); failures=0; changes=0; stage=0;
                foreach(var spawner in UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) {spawner.StopAllCoroutines();spawner.enabled=false;}
                foreach(var weapons in UnityEngine.Object.FindObjectsByType<ShipWeapons>(FindObjectsSortMode.None)) weapons.enabled=false;
                foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                hud.energy.RegenerationPerSecond=0; hud.energy.TryConsumeEnergy(60);
                keyboard=InputSystem.AddDevice<Keyboard>("OrbSelectorCheckKeyboard");
                Application.runInBackground=true; start=EditorApplication.timeSinceStartup; initialized=true;
                Check(mode && view.owner==mode,"Scene references bind UI to ship reward owner");
                Check(mode.CurrentMode==OrbRewardMode.RewardKind.Experience,"Initial mode preserves XP rewards");
            }
            double t=EditorApplication.timeSinceStartup-start;
            if(stage==0 && t>.5)
            {
                Award(7); Check(hud.experience.CurrentXP==7 && hud.energy.CurrentEnergy==40,"XP mode leaves energy unchanged");
                Press(true); stage++;
            }
            if(stage==1 && t>1)
            {
                Check(mode.CurrentMode==OrbRewardMode.RewardKind.Energy && Mathf.Abs(view.thumb.anchoredPosition.x-8)<.1f,"Q selects Energy and slides thumb left");
                Press(false); Award(9); Check(hud.energy.CurrentEnergy==49 && hud.experience.CurrentXP==7,"Energy mode restores energy without XP");
                Capture("orb-energy"); hud.energy.RestoreEnergy(999); Award(10);
                Check(hud.energy.CurrentEnergy==100 && hud.experience.CurrentXP==7,"Energy caps without XP overflow");
                Click(false); stage++;
            }
            if(stage==2 && t>1.5)
            {
                Check(mode.CurrentMode==OrbRewardMode.RewardKind.Experience && Mathf.Abs(view.thumb.anchoredPosition.x-152)<.1f,"Right click target selects XP and slides right");
                Award(5); Check(hud.experience.CurrentXP==12,"Click selection controls real reward");
                hud.energy.TryConsumeEnergy(20);
                var loot=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Loot.prefab"),hud.ship.transform.position+Vector3.right*2,Quaternion.identity).GetComponent<Loot>();
                loot.expValue=11; loot.Magnetize(hud.ship.transform);
                mode.TrySelectMode(OrbRewardMode.RewardKind.Energy); stage++;
            }
            if(stage==3 && t>2.5)
            {
                Check(hud.energy.CurrentEnergy==91 && hud.experience.CurrentXP==12,"In-flight magnetized orb uses selection at collection time");
                mode.enabled=false; mode.enabled=true; mode.enabled=false; mode.enabled=true;
                mode.ModeChanged+=CountChange; Press(true); stage++;
            }
            if(stage==4 && t>3)
            {
                Check(changes==1 && mode.CurrentMode==OrbRewardMode.RewardKind.Experience,"Re-enable retains exactly one keyboard callback"); stage++;
            }
            if(stage==5 && t>3.5)
            {
                Check(changes==1,"Holding Q does not repeat toggles"); Press(false); Time.timeScale=0; stage++;
            }
            if(stage==6 && t>4) {Press(true); stage++;}
            if(stage==7 && t>4.5)
            {
                Check(mode.CurrentMode==OrbRewardMode.RewardKind.Experience && changes==1,"Paused game ignores Q");
                Press(false); Time.timeScale=1; Resolution(1280,720); stage++;
            }
            if(stage==8 && t>5)
            {
                Capture("orb-xp-1280"); Click(true); stage++;
            }
            if(stage==9 && t>5.5)
            {
                Check(mode.CurrentMode==OrbRewardMode.RewardKind.Energy && view.accent.sprite==view.energyFill,"Left click selects green Energy state");
                view.enabled=false; view.enabled=true; mode.TrySelectMode(OrbRewardMode.RewardKind.Experience); stage++;
            }
            if(stage==10 && t>6)
            {
                Check(view.accent.sprite==view.xpFill && Mathf.Abs(view.thumb.anchoredPosition.x-152)<.1f,"View resubscribes after re-enable"); Finish();
            }
        }
        catch(Exception error) { if(report==null) report=new StringBuilder(); Check(false,error.ToString()); Finish(); }
    }
    static void CountChange(OrbRewardMode.RewardKind kind) {changes++;}
    static void Capture(string name)
    {
        Directory.CreateDirectory("Logs/ShipHUD"); Canvas.ForceUpdateCanvases();
        var corners=new Vector3[4]; ((RectTransform)view.transform).GetWorldCorners(corners);
        Check(corners.All(p=>p.x>=0 && p.y>=0 && p.x<=Screen.width && p.y<=Screen.height),"Selector inside screen: "+Screen.width+"x"+Screen.height);
        var target=(RenderTexture)gameType.GetMethod("RenderView",Flags).Invoke(game,new object[]{new Vector2(Screen.width,Screen.height),false});
        var old=RenderTexture.active;
        try
        {
            RenderTexture.active=target; var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
            if(SystemInfo.graphicsUVStartsAtTop)
            {
                var rows=image.GetPixels32();
                for(int y=0;y<image.height/2;y++) for(int x=0;x<image.width;x++)
                {int a=y*image.width+x,b=(image.height-1-y)*image.width+x;var value=rows[a];rows[a]=rows[b];rows[b]=value;}
                image.SetPixels32(rows); image.Apply();
            }
            File.WriteAllBytes("Logs/ShipHUD/"+name+".png",image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
        }
        finally {RenderTexture.active=old;}
    }
    static void Finish()
    {
        if(mode) mode.ModeChanged-=CountChange;
        if(keyboard!=null) InputSystem.RemoveDevice(keyboard);
        Time.timeScale=1; report.AppendLine("Failures: "+failures);
        Directory.CreateDirectory("Logs/ShipHUD"); File.WriteAllText("Logs/ShipHUD/orb-toggle-check.txt",report.ToString()); Debug.Log(report.ToString());
        initialized=false; SessionState.SetBool(Session,false); EditorApplication.isPlaying=false;
    }
}
