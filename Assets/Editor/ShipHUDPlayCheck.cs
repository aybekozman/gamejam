using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>Opt-in integration tests. Gameplay changes and test objects exist only in Play Mode.</summary>
[InitializeOnLoad]
public static class ShipHUDPlayCheck
{
    const string Key="ShipHUDCheck";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static ShipHUDController hud;
    static ShipController ship;
    static Rigidbody2D body;
    static bool initialized;
    static bool[] steps=new bool[40];
    static float start;
    static float cooldown, completion;
    static int initialWeaponLevel;
    static int firstFrame, failures;
    static double pauseUntil;
    static float[] pauseValues;
    static StringBuilder report;
    static Keyboard testKeyboard;
    static int keyboardCharges;
    static long magnetExpectedXP;
    static bool baselineCollision;
    static EditorWindow gameView;
    static Type gameViewType;
    static ShipHUDPlayCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key+"Restore",false)) return;
            string path=SessionState.GetString(Key+"Scene","");
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            bool background=SessionState.GetBool(Key+"Background",false);
            EditorApplication.delayCall+=()=>PlayerSettings.runInBackground=background;
            SessionState.SetBool(Key,false); SessionState.SetBool(Key+"Restore",false);
            FindGameView();
            gameViewType.GetProperty("selectedSizeIndex",Flags)?.SetValue(gameView,SessionState.GetInt(Key+"Size",0));
        };
    }
    [MenuItem("Tools/Ship HUD/Run Play Mode checks")]
    public static void StartCheck()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop Play Mode before running HUD checks.");
        FindGameView();
        SessionState.SetInt(Key+"Size",(int)gameViewType.GetProperty("selectedSizeIndex",Flags).GetValue(gameView));
        SessionState.SetString(Key+"Scene",AssetDatabase.GetAssetPath(EditorSceneManagerStart()));
        SessionState.SetBool(Key+"Background",PlayerSettings.runInBackground);
        SessionState.SetBool(Key,true); SessionState.SetBool(Key+"Restore",true);
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ShipHUDSetup.ScenePath);
        Selection.activeObject=null;
        SetResolution(1920,1080);
        EditorApplication.isPlaying=true;
    }
    static SceneAsset EditorSceneManagerStart()=>UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene;
    static void FindGameView()
    {
        gameViewType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        gameView=Resources.FindObjectsOfTypeAll(gameViewType).OfType<EditorWindow>().FirstOrDefault();
        if(!gameView) gameView=EditorWindow.GetWindow(gameViewType);
    }
    static void SetResolution(int width,int height)
    {
        if(!gameView) FindGameView();
        gameViewType.GetMethod("SetCustomResolution",Flags).Invoke(gameView,new object[]{new Vector2(width,height),"HUD check"});
        gameView.Repaint();
        EditorApplication.QueuePlayerLoopUpdate();
    }
    static bool Once(int step,float t,float at)
    {
        if(steps[step] || t<at) return false;
        steps[step]=true; return true;
    }
    static void Check(bool condition,string message)
    {
        report.AppendLine((condition?"PASS ":"FAIL ")+message);
        if(!condition) failures++;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        EditorApplication.QueuePlayerLoopUpdate();
        if(!initialized)
        {
            var scene=SceneManager.GetSceneByPath(ShipHUDSetup.ScenePath); if(!scene.isLoaded) return;
            hud=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ShipHUDController>()).Single();
            ship=hud.ship; body=ship.GetComponent<Rigidbody2D>();
            cooldown=ship.dashCooldown; completion=2*cooldown+1.1f;
            initialWeaponLevel=ship.GetComponent<ShipWeapons>().shipLevel;
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var spawner in root.GetComponentsInChildren<EnemySpawner>()) { spawner.StopAllCoroutines(); spawner.enabled=false; }
                foreach(var weapons in root.GetComponentsInChildren<ShipWeapons>()) weapons.enabled=false;
                foreach(var enemy in root.GetComponentsInChildren<Enemy>()) enemy.gameObject.SetActive(false);
            }
            baselineCollision=Physics2D.GetIgnoreLayerCollision(ship.gameObject.layer,LayerMask.NameToLayer("Enemy"));
            testKeyboard=InputSystem.AddDevice<Keyboard>("HUDCheckKeyboard");
            start=Time.time; firstFrame=Time.frameCount; initialized=true; steps=new bool[40]; failures=0; pauseUntil=0;
            report=new StringBuilder("HudFirstTry Play Mode integration checks\nOnly test-copy gameplay is manipulated. Provided font intentionally omitted at user's request.\n");
            Application.runInBackground=true;
            Check(ship.AvailableDashCharges==3 && Enumerable.Range(0,3).All(i=>ship.GetDashProgress(i)==1),"A: starts with three ready dash charges");
            Check(hud.dashFills.All(i=>i.fillMethod==UnityEngine.UI.Image.FillMethod.Vertical && i.fillOrigin==(int)UnityEngine.UI.Image.OriginVertical.Top),"Dash recharge fills vertically from top inside the icon alpha");
            Check(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.UIElements.UIDocument>(true)).All(d=>!d.enabled),"Old UI Toolkit document disabled; only one HUD");
            Check(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>()).Count()==1,"Exactly one active EventSystem");
            Check(hud.GetComponentsInChildren<TMPro.TMP_Text>().All(text=>text.font==hud.hudFont),"All labels share the replaceable temporary project font");
        }
        float t=Time.time-start;
        if(Once(0,t,.2f))
        {
            hud.energy.TryConsumeEnergy(25);
            Check(Mathf.Abs(hud.energyFill.fillAmount-.75f)<.01f,"Energy consumption updates the real HUD");
            float energy=hud.energy.CurrentEnergy;
            Check(ship.TryDash(Vector2.one) && ship.AvailableDashCharges==2 && ship.GetDashProgress(0)==0,"B: one dash consumes only one charge");
            Check(hud.energy.CurrentEnergy==energy,"Dash has no energy cost");
            Check(!ship.TryDash(Vector2.up) && ship.AvailableDashCharges==2,"K: cannot start a second movement while dashing");
            Check(!Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer("Default"),LayerMask.NameToLayer("Enemy")),
                "Dash invulnerability does not disable Default-layer projectile collisions");
        }
        if(Once(1,t,.45f))
        {
            Check(body.linearVelocity.magnitude<=ship.dashSpeed+.1f && body.linearVelocity.magnitude>=ship.dashSpeed*.9f && Mathf.Abs(body.linearVelocity.x-body.linearVelocity.y)<.1f,
                "I: diagonal dash is normalized; existing physics damping is preserved");
            Check(ship.GetDashProgress(0)>0 && hud.dashFills[1].fillAmount==1 && hud.dashFills[2].fillAmount==1,"Unused icons stay ready while the used slot recharges");
        }
        if(Once(2,t,1.3f))
        {
            float before=ship.GetDashRemaining(0);
            Check(ship.TryDash(Vector2.up) && ship.GetDashRemaining(0)==before && ship.AvailableDashCharges==1,"C: second dash preserves first slot's remaining cooldown");
        }
        if(Once(3,t,2f)) { LayoutCheck(1920,1080); Capture("hud-1920x1080-partial"); }
        if(Once(4,t,2.4f))
        {
            float a=ship.GetDashRemaining(0),b=ship.GetDashRemaining(1);
            Check(ship.TryDash(Vector2.up) && ship.AvailableDashCharges==0 && ship.GetDashRemaining(0)==a && ship.GetDashRemaining(1)==b,"D: all three slots keep independent cooldowns");
        }
        if(Once(5,t,2.65f))
        {
            Check(ship.GetDashProgress(0)>ship.GetDashProgress(1) && ship.GetDashProgress(1)>ship.GetDashProgress(2),"Three distinct progress values visible simultaneously");
            Capture("hud-three-cooldowns");
            pauseValues=Enumerable.Range(0,3).Select(ship.GetDashRemaining).ToArray();
            Time.timeScale=0; pauseUntil=EditorApplication.timeSinceStartup+.4;
        }
        if(Time.timeScale==0 && pauseUntil>0 && EditorApplication.timeSinceStartup>=pauseUntil)
        {
            Check(Enumerable.Range(0,3).All(i=>ship.GetDashRemaining(i)==pauseValues[i]),"H: pause freezes all cooldown timers");
            Time.timeScale=1; pauseUntil=0;
        }
        if(Once(6,t,3.55f))
        {
            var before=Enumerable.Range(0,3).Select(ship.GetDashRemaining).ToArray();
            Check(!ship.TryDash(Vector2.left) && Enumerable.Range(0,3).All(i=>before[i]==ship.GetDashRemaining(i)),"G: no-charge attempt fails without changing timers");
        }
        if(Once(7,t,cooldown+.3f))
        {
            Check(ship.GetDashProgress(0)==1 && ship.GetDashProgress(1)<1,"F: first slot becomes ready before the others");
            float b=ship.GetDashRemaining(1),c=ship.GetDashRemaining(2);
            Check(ship.TryDash(Vector2.right) && ship.GetDashProgress(0)==0 && b==ship.GetDashRemaining(1) && c==ship.GetDashRemaining(2),"E: reusing the recovered slot resets only that slot");
        }
        if(Once(8,t,cooldown+.6f))
            Check(Enumerable.Range(0,3).All(i=>Mathf.Abs(hud.dashFills[i].fillAmount-ship.GetDashProgress(i))<.03f),"J: HUD progress equals actual charge state");
        if(Once(9,t,6.2f)) SetResolution(1280,720);
        if(Once(10,t,6.6f)) { LayoutCheck(1280,720); Capture("hud-1280x720"); SetResolution(1280,1024); }
        if(Once(11,t,7.1f)) { LayoutCheck(1280,1024); Capture("hud-1280x1024"); SetResolution(2560,1080); }
        if(Once(12,t,7.6f)) { LayoutCheck(2560,1080); Capture("hud-2560x1080"); SetResolution(1920,1080); }
        if(Once(13,t,completion))
        {
            Check(ship.AvailableDashCharges==3 && hud.dashFills.All(i=>i.fillAmount==1),"All charges finish independently and become immediately ready");
            Check(hud.energy.CurrentEnergy>90,"Energy regenerates over scaled game time");
            hud.health.TakeDamage(25);
            Check(Mathf.Abs(hud.healthFill.fillAmount-.75f)<.01f,"Health damage updates the live bar and value");
            hud.health.Heal(10); hud.health.SetMaxHealth(120);
            Check(Mathf.Abs(hud.healthFill.fillAmount-85f/120)<.01f,"Healing and maximum-health changes update the HUD");
            hud.health.SetMaxHealth(100);
            var loot=new GameObject("TEST direct XP reward").AddComponent<Loot>(); loot.expValue=350;
            Check(loot.TryCollect(hud.experience) && !loot.TryCollect(hud.experience),"The same loot cannot award XP twice");
            Check(hud.experience.PlayerLevel==3 && hud.experience.CurrentXP==115 && hud.levelText.text=="3","XP carries over through multiple player levels; badge updates");
            Check(ship.GetComponent<ShipWeapons>().shipLevel==initialWeaponLevel,"Player XP does not change weapon or expedition levels");
            var actualLoot=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Loot.prefab"),ship.transform.position+Vector3.right*.2f,Quaternion.identity);
            actualLoot.GetComponent<Loot>().expValue=25;
            magnetExpectedXP=hud.experience.CurrentXP+25;
        }
        if(Once(14,t,completion+.7f))
        {
            Check(hud.experience.CurrentXP==magnetExpectedXP,"Real LootMagnet collection awards XP without manual reward calls");
            Quaternion oldRotation=ship.transform.rotation;
            bool compassCorrect=true;
            foreach(float angle in new[]{0f,90f,180f,270f})
            {
                ship.transform.rotation=Quaternion.Euler(0,0,angle); hud.RefreshAll();
                compassCorrect&=Mathf.Abs(Mathf.DeltaAngle(hud.compassNeedle.eulerAngles.z,angle))<.1f && hud.compassNeedle.parent.localRotation==Quaternion.identity;
            }
            ship.transform.rotation=oldRotation; hud.RefreshAll();
            Check(compassCorrect,"Compass needle verified at north, west, south, east; base stays fixed");
            Capture("hud-final");
            ship.TryDash(Vector2.right);
            ship.enabled=false;
            Check(!ship.IsDashing && Physics2D.GetIgnoreLayerCollision(ship.gameObject.layer,LayerMask.NameToLayer("Enemy"))==baselineCollision,"Disabling the controller cancels dash and restores collision state");
            ship.enabled=true; ship.enabled=false; ship.enabled=true;
            keyboardCharges=ship.AvailableDashCharges;
            InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.LeftShift));
        }
        if(Once(15,t,completion+1f))
        {
            Check(ship.AvailableDashCharges==keyboardCharges-1,"Named input callbacks still consume exactly one charge after re-enable");
            InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());
            uint[] missing;
            bool turkish=hud.hudFont.HasCharacters("GEMİ GELİŞTİRMELERİ Sağlık Enerji Üçüncü Sefer Düşman Yaklaşıyor! Pusula Seviye",out missing,true,true);
            report.AppendLine("INFO Temporary project-font Turkish coverage: "+turkish+" (supplied font intentionally not installed)");
        }
        if(Once(16,t,completion+2.3f))
        {
            Check(Time.frameCount-firstFrame>100,"Play Mode frames genuinely advance during checks");
            hud.health.TakeDamage(1000);
            Check(hud.health.IsDead && hud.health.CurrentHealth==0 && !ship.enabled && Time.timeScale==0,"Existing death/pause behaviour preserved and health clamps at zero");
            Finish();
        }
    }
    static void LayoutCheck(int width,int height)
    {
        Canvas.ForceUpdateCanvases();
        bool inside=true; var corners=new Vector3[4];
        foreach(string name in new[]{"LeftHUD","RightHUD"})
        {
            var rect=(RectTransform)hud.safeArea.Find(name); rect.GetWorldCorners(corners);
            foreach(var corner in corners) if(corner.x<-.5f || corner.y<-.5f || corner.x>Screen.width+.5f || corner.y>Screen.height+.5f) inside=false;
        }
        Check(Screen.width==width && Screen.height==height && inside,$"Responsive layout stays inside actual {width}x{height} Game View");
    }
    static void Capture(string name)
    {
        Directory.CreateDirectory("Logs/ShipHUD");
        Canvas.ForceUpdateCanvases();
        if(!gameView) FindGameView();
        var target=(RenderTexture)gameViewType.GetMethod("RenderView",Flags).Invoke(gameView,new object[]{new Vector2(Screen.width,Screen.height),false});
        if(!target) throw new Exception("Game View render target unavailable");
        var old=RenderTexture.active;
        try
        {
            RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
            // Game View render targets on Direct3D are stored top-down, unlike PNG readback.
            if(SystemInfo.graphicsUVStartsAtTop)
            {
                var rows=image.GetPixels32();
                for(int y=0;y<image.height/2;y++) for(int x=0;x<image.width;x++)
                {
                    int a=y*image.width+x,b=(image.height-1-y)*image.width+x;
                    var value=rows[a]; rows[a]=rows[b]; rows[b]=value;
                }
                image.SetPixels32(rows); image.Apply();
            }
            File.WriteAllBytes("Logs/ShipHUD/"+name+".png",image.EncodeToPNG());
            int visible=0;
            foreach(var pixel in image.GetPixels32()) if(pixel.r>20 || pixel.g>20 || pixel.b>20) visible++;
            Check(visible>image.width*image.height/100,"Rendered screenshot is non-empty: "+name);
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally {RenderTexture.active=old;}
    }
    static void Finish()
    {
        if(testKeyboard!=null) InputSystem.RemoveDevice(testKeyboard);
        Time.timeScale=1;
        report.AppendLine("Failures: "+failures);
        Directory.CreateDirectory("Logs/ShipHUD"); File.WriteAllText("Logs/ShipHUD/play-check.txt",report.ToString());
        Debug.Log(report.ToString());
        initialized=false; SessionState.SetBool(Key,false);
        EditorApplication.isPlaying=false;
    }
}
