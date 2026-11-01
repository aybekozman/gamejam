using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Abyss.Story;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>Opt-in end-to-end check; teleports test actors and accelerates only the test-copy purifier.</summary>
[InitializeOnLoad]
public static class StoryJourneyPlayCheck
{
    const string Session="StoryJourneyCheck";
    static StringBuilder report;
    static int phase, failures, originalEnemies;
    static double started, since;
    static float progress, expectedSpeed;
    static bool initialized;
    static ShipController ship;
    static ShipUIManager ui;
    static ShipWaterPurification water;
    static Rigidbody2D body;
    static Keyboard keyboard;
    static StoryJourneyPlayCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Session+"Restore",false))return;
            var path=SessionState.GetString(Session+"Start","");
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            bool background=SessionState.GetBool(Session+"Background",false);
            EditorApplication.delayCall+=()=>PlayerSettings.runInBackground=background;
            SessionState.SetBool(Session,false);SessionState.SetBool(Session+"Restore",false);
        };
    }
    [MenuItem("Tools/Story Journey/Run two-scene Play Mode check")]
    public static void StartCheck()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");
        SessionState.SetString(Session+"Start",AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene));
        SessionState.SetBool(Session+"Background",PlayerSettings.runInBackground);
        SessionState.SetBool(Session,true);SessionState.SetBool(Session+"Restore",true);
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(StorySceneJourney.UnderwaterPath);
        Selection.activeObject=null;EditorApplication.isPlaying=true;
    }
    static void Check(bool good,string message){report.AppendLine((good?"PASS ":"FAIL ")+message);if(!good)failures++;}
    static void Next(int value){phase=value;since=EditorApplication.timeSinceStartup;}
    static void Move(UnderwaterDayStory story,Vector2 at){story.player.transform.position=at;story.player.GetComponent<Rigidbody2D>().position=at;Physics2D.SyncTransforms();}
    static void ClickMode(OrbRewardToggleView view,bool xp)
    {
        Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)view.transform;
        var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(rect.rect.width*(xp?.75f:.25f),-rect.rect.height*.5f))),button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Check(hits.Count>0&&hits[0].gameObject==view.gameObject,"Selector receives real EventSystem raycast");
        if(hits.Count>0)ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Session,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        Application.runInBackground=true;EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if(!initialized)
            {
                report=new StringBuilder("Two-scene journey Play Mode integration check\nTest-copy actor teleports and dialogue Advance calls; no authored tuning changes.\n");
                failures=0;phase=0;started=since=EditorApplication.timeSinceStartup;initialized=true;
            }
            if(EditorApplication.timeSinceStartup-started>240)throw new Exception("Timeout at test phase "+phase);
            double elapsed=EditorApplication.timeSinceStartup-since;
            var flow=StorySceneJourney.Instance;if(!flow)return;
            var scene=SceneManager.GetActiveScene();
            var story=scene.path==StorySceneJourney.UnderwaterPath?StorySceneJourney.FindIn<UnderwaterDayStory>(scene):null;
            if(story&&story.ui&&story.ui.Active)story.ui.Advance();
            if(phase==0&&story&&Time.frameCount>3)
            {
                Check(flow.Stage==JourneyStage.FirstUnderwater&&story.Stage==UnderwaterDayStage.Beginning,"New game starts in authored first underwater chapter");
                Check(!story.secondChapter.skipShipForDevelopment&&!story.secondChapter.Started&&!story.thirdChapter.Started,"Normal route disables development chapter skips");
                Check(EditorBuildSettings.scenes.First(s=>s.enabled).path==StorySceneJourney.UnderwaterPath&&EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==StorySceneJourney.ShipPath),"Both actual scene paths are enabled; underwater is first");
                Move(story,(Vector2)story.player.transform.position+Vector2.right*(story.triggerDistance+1));Next(1);
            }
            else if(phase==1&&story)
            {
                if(story.Stage==UnderwaterDayStage.FollowToSurface)Move(story,story.Discovery);
                if(story.Stage==UnderwaterDayStage.InspectBodies){Check(true,"Existing first death and rise-to-surface sequence reached inspections");Next(2);}
            }
            else if(phase==2&&story&&!story.ui.Active)
            {
                for(int i=0;i<3;i++)if(!story.HasInspected(i)){Move(story,story.bodies[i].position);story.TryInspect(i);break;}
                if(story.Stage==UnderwaterDayStage.ReportToMayor){Check(story.InspectionCount==3,"Three original inspection conditions completed");Next(3);}
            }
            else if(phase==3&&story&&!story.ui.Active)
            {
                if(story.Stage==UnderwaterDayStage.EscortMayor)Next(4);
                else {Move(story,story.mayor.Position);story.TalkToMayor();}
            }
            else if(phase==4&&story)
            {
                story.mayor.Place(story.Discovery);Move(story,story.Discovery);
                if(story.Complete&&!story.ui.Active){Check(true,"Original mayor final dialogue completion reached");Next(5);}
            }
            else if(phase==5&&story&&!story.ui.Active)
            {
                Move(story,story.playerHome.bounds.center);story.TryInteractNearest();
                story.secondChapter.firstChapterDepartureRequested.Invoke();
                Check(flow.Stage==JourneyStage.FirstShip&&flow.IsTransitioning&&flow.TransitionCount==1,"Real home continue loads first ship exactly once, even with duplicate event");Next(6);
            }
            else if(phase==6&&scene.path==StorySceneJourney.ShipPath&&!flow.IsTransitioning)
            {
                ship=StorySceneJourney.FindIn<ShipController>(scene);ui=StorySceneJourney.FindIn<ShipUIManager>(scene);water=ship.GetComponent<ShipWaterPurification>();body=ship.GetComponent<Rigidbody2D>();
                Check(water&&water.fill&&water.percentage&&water.NormalizedProgress==0&&water.forwardDistanceForFull==1000,"Purifier starts at zero and binds real 0–1 progress to HUD");
                var view=StorySceneJourney.FindIn<OrbRewardToggleView>(scene);
                Check(view.lootMode==ui&&view.accent.sprite==view.xpFill,"Selector binds to actual loot manager and initial XP sprite");
                originalEnemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
                foreach(var weapons in UnityEngine.Object.FindObjectsByType<ShipWeapons>(FindObjectsSortMode.None))weapons.enabled=false;
                typeof(PlayerHealth).GetProperty("IsInvulnerable").SetValue(ship.GetComponent<PlayerHealth>(),true);
                keyboard=InputSystem.AddDevice<Keyboard>("JourneyCheckKeyboard");InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Space));Next(7);
            }
            else if(phase==7&&elapsed>.3)
            {
                var view=StorySceneJourney.FindIn<OrbRewardToggleView>(scene);
                Check(!ui.isLootingXP&&view.accent.sprite==view.energyFill,"Existing Space key switches to Energy and green sprite");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                ship.currentEnergy=50;float xp=ui.currentXP;ui.CollectLoot(1,15);
                Check(ship.currentEnergy==65&&ui.currentXP==xp,"Energy mode awards only energy through existing loot API");
                ClickMode(view,true);ui.CollectLoot(1,15);
                Check(ui.isLootingXP&&view.accent.sprite==view.xpFill&&ui.currentXP>xp,"Right click selects XP and awards existing XP");
                ClickMode(view,false);Next(8);
            }
            else if(phase==8&&elapsed>6)
            {
                var spawner=StorySceneJourney.FindIn<EnemySpawner>(scene);
                Check(spawner.enabled&&UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length>originalEnemies,"First ship keeps existing spawning beyond initial enemies");
                foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
                ship.enabled=false;body.rotation=0;body.linearVelocity=Vector2.zero;progress=water.NormalizedProgress;Next(9);
            }
            else if(phase==9&&elapsed>.3){Check(water.NormalizedProgress==progress,"Standing still does not purify");body.linearVelocity=Vector2.right*4;Next(10);}
            else if(phase==10&&elapsed>.3){Check(water.NormalizedProgress==progress,"Sideways movement does not purify");body.linearVelocity=Vector2.down*4;Next(11);}
            else if(phase==11&&elapsed>.3){Check(water.NormalizedProgress==progress,"Reverse movement does not purify");body.linearVelocity=Vector2.up*8;Time.timeScale=0;Next(12);}
            else if(phase==12&&elapsed>.3){Check(water.NormalizedProgress==progress,"Pause freezes purification");Time.timeScale=1;Next(13);}
            else if(phase==13&&elapsed>.3)
            {
                Check(water.NormalizedProgress>progress&&Mathf.Abs(water.fill.fillAmount-water.NormalizedProgress)<.00001f&&water.percentage.text=="%"+Mathf.FloorToInt(water.NormalizedProgress*100),"Forward physics motion increases real progress and both HUD values");
                expectedSpeed=ship.maxSpeed+2;ship.maxSpeed=expectedSpeed;ship.maxEnergy=135;ship.currentEnergy=70;
                ui.currentLevel=7;ui.currentXP=3.25f;ui.nextLevelXP=85;ui.xpGainMultiplier=1.8f;ui.dashRegenRate=.8f;
                var weapons=ship.GetComponent<ShipWeapons>();weapons.fireRate=.37f;weapons.attackRange=17;
                weapons.allTurrets[0].recoilForce=1.25f;
                var health=ship.GetComponent<PlayerHealth>();health.SetMaxHealth(150,true);health.damageResistPercent=12;
                Bullet.bonusDamage=8;Bullet.bonusSpeed=5;Bullet.bonusKnockback=2;
                water.forwardDistanceForFull=2;body.linearVelocity=Vector2.up*8;Next(14);
            }
            else if(phase==14&&story&&flow.Stage==JourneyStage.SecondUnderwater&&!flow.IsTransitioning&&story.secondChapter.Stage==SecondChapterStage.AttendAnnouncement)
            {
                Check(story.Complete&&story.InspectionCount==3&&!story.thirdChapter.Started,"Return starts second chapter without replaying the first or starting third");
                Check(story.secondChapter.participantRoot.gameObject.activeSelf&&story.secondChapter.scientists.All(s=>s.gameObject.activeInHierarchy),"Existing second-chapter API initializes announcement actors");
                Check(Vector2.Distance(story.player.transform.position,story.secondChapter.homeArrival)<1,"Second chapter uses authored player arrival");Next(15);
            }
            else if(phase==15&&story&&!story.ui.Active)
            {
                if(story.secondChapter.Stage==SecondChapterStage.AttendAnnouncement){Move(story,story.secondChapter.meetingPoint);story.secondChapter.Interact();}
                if(story.secondChapter.Stage==SecondChapterStage.LeaveForSharks)Next(16);
            }
            else if(phase==16&&story&&!story.ui.Active)
            {
                Move(story,story.secondChapter.departurePoint);story.secondChapter.Tick();story.secondChapter.secondChapterCompleted.Invoke();
                Check(flow.Stage==JourneyStage.ShipBattle&&flow.TransitionCount==3,"Authored second chapter departure loads battle ship once");Next(17);
            }
            else if(phase==17&&scene.path==StorySceneJourney.ShipPath&&!flow.IsTransitioning)
            {
                ship=StorySceneJourney.FindIn<ShipController>(scene);ui=StorySceneJourney.FindIn<ShipUIManager>(scene);water=ship.GetComponent<ShipWaterPurification>();
                Check(ui.currentLevel==7&&ui.currentXP==3.25f&&ui.nextLevelXP==85&&ui.xpGainMultiplier==1.8f,"Real level, XP and XP upgrade multiplier survive scene round trip");
                Check(ship.maxSpeed==expectedSpeed&&ship.maxEnergy==135&&ui.dashRegenRate==.8f,"Applied movement, energy and dash upgrades survive");
                var weapons=ship.GetComponent<ShipWeapons>();var health=ship.GetComponent<PlayerHealth>();
                Check(weapons.fireRate==.37f&&weapons.attackRange==17&&weapons.allTurrets[0].recoilForce==1.25f&&health.maxHealth==150&&health.damageResistPercent==12,"Applied weapon, recoil, health and resistance upgrades survive");
                Check(Bullet.bonusDamage==8&&Bullet.bonusSpeed==5&&Bullet.bonusKnockback==2,"Static projectile upgrades restored after destination Awake reset");
                Check(water.NormalizedProgress==1&&water.percentage.text=="%100","Water remains full on second ship visit");
                Check(!ui.isLootingXP&&StorySceneJourney.FindIn<OrbRewardToggleView>(scene).lootMode==ui,"Loot mode survives and remains connected");
                Check(StorySceneJourney.FindIn<EnemySpawner>(scene).enabled&&weapons.enabled&&ship.enabled,"Second ship uses normal controller, weapons and infinite spawner");Next(18);
            }
            else if(phase==18&&elapsed>2)
            {
                Check(flow.Stage==JourneyStage.ShipBattle&&flow.TransitionCount==3&&scene.path==StorySceneJourney.ShipPath,"Full progress on second visit does not cause a return loop");
                Check(UnityEngine.Object.FindObjectsByType<StorySceneJourney>(FindObjectsSortMode.None).Length==1&&UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1&&UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length<=1,"No duplicate journey manager, listener or EventSystem");
                flow.NewGame();Next(19);
            }
            else if(phase==19&&story&&!flow.IsTransitioning)
            {
                Check(flow.Stage==JourneyStage.FirstUnderwater&&story.Stage==UnderwaterDayStage.Beginning&&!story.secondChapter.Started,"New Game resets the session to the first underwater chapter");Finish();
            }
        }
        catch(Exception error){if(report==null)report=new StringBuilder();Check(false,error.ToString());Finish();}
    }
    static void Finish()
    {
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);keyboard=null;
        Time.timeScale=1;report.AppendLine("Failures: "+failures);
        Directory.CreateDirectory("Logs/StoryJourney");File.WriteAllText("Logs/StoryJourney/play-check.txt",report.ToString());Debug.Log(report.ToString());
        initialized=false;SessionState.SetBool(Session,false);EditorApplication.isPlaying=false;
    }
}
