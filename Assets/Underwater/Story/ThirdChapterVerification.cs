#if UNITY_EDITOR

using System.Collections;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using UnityEngine;

using UnityEditor;

using UnityEngine.InputSystem;

using UnityEngine.InputSystem.LowLevel;

using UnityEngine.UI;

namespace Abyss.Story {

public class ThirdChapterVerification:MonoBehaviour {

 readonly List<string> results=new();UnderwaterDayStory story;UnderwaterThirdChapter third;FishPilot player;Rigidbody2D body;InputSettings originalSettings,testSettings;int errors,completed,maxBalls,routeHits;bool routeClear=true;

 void OnEnable()=>Application.logMessageReceived+=Log;

 void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;File.AppendAllText("Tools/Revision10/errors.txt",text+"\n"+stack+"\n");}}

 void OnDisable(){Application.logMessageReceived-=Log;if(originalSettings)InputSystem.settings=originalSettings;if(testSettings)Destroy(testSettings);}

 void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines("Tools/Revision10/playmode-results.txt",results);}

 void Keys(Vector2 direction){var keys=new List<Key>();if(direction.x>.1f)keys.Add(Key.D);if(direction.x<-.1f)keys.Add(Key.A);if(direction.y>.1f)keys.Add(Key.W);if(direction.y<-.1f)keys.Add(Key.S);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys.ToArray()));}

 IEnumerator PressE(){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));yield return null;yield return null;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;}

 IEnumerator Swim(Vector2 goal,float timeout=90,bool dodge=false){float start=Time.time;int previousHealth=third.Health;bool captured=false;

  while(Vector2.Distance(body.position,goal)>.55f&&Time.time-start<timeout){

   if(third.Stage==ThirdChapterStage.Respawning){Keys(Vector2.zero);yield return null;continue;}

   Vector2 direction=goal-body.position;if(Mathf.Abs(direction.y)<.5f)direction.y=0;if(Mathf.Abs(direction.x)>.7f)direction.x=Mathf.Sign(direction.x);if(Mathf.Abs(direction.y)>.2f)direction.y=Mathf.Sign(direction.y);

   if(dodge)foreach(var ball in third.hazards.Balls)if(ball.gameObject.activeSelf&&ball.Dangerous){Vector2 offset=(Vector2)ball.transform.position-body.position;float t=Mathf.Max(.1f,offset.x/player.speed);float predicted=offset.y-third.hazards.terminalSpeed*t;if(offset.x>-.5f&&offset.x<7&&Mathf.Abs(predicted)<4){direction.y=body.position.y<-17?1:-1;break;}}

   if(!WaterRoute.Segment(body.position,body.position+direction.normalized*.8f,.4f)){direction=Vector2.up;}

   if(dodge&&!captured&&third.hazards.Balls.Any(b=>b.gameObject.activeSelf&&Mathf.Abs(b.transform.position.x-body.position.x)<10&&b.transform.position.y>body.position.y-5&&b.transform.position.y<body.position.y+6)){captured=true;Capture("live-cannon-section");}Keys(direction);maxBalls=Mathf.Max(maxBalls,third.hazards.ActiveCount);routeClear&=WaterRoute.Clear(body.position,.32f);if(third.Health<previousHealth)routeHits++;previousHealth=third.Health;yield return new WaitForFixedUpdate();

  }Keys(Vector2.zero);body.linearVelocity=Vector2.zero;Check(Vector2.Distance(body.position,goal)<1,"Normal WASD swimming reaches "+goal+" in "+(Time.time-start).ToString("F1")+"s");

 }

 void Capture(string name){var cam=Camera.main;var rt=RenderTexture.GetTemporary(1280,720,24);var old=cam.targetTexture;var active=RenderTexture.active;cam.targetTexture=rt;var canvas=story.ui.GetComponentInChildren<Canvas>();float scale=canvas.scaleFactor;canvas.scaleFactor=720f/1080;foreach(var t in canvas.GetComponentsInChildren<Text>())t.SetAllDirty();Canvas.ForceUpdateCanvases();if(third.Started)third.Tick();cam.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Tools/Revision10/"+name+".png",image.EncodeToPNG());Destroy(image);cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);canvas.scaleFactor=scale;foreach(var t in canvas.GetComponentsInChildren<Text>())t.SetAllDirty();}

 IEnumerator Start(){Directory.CreateDirectory("Tools/Revision10");Application.runInBackground=true;originalSettings=InputSystem.settings;testSettings=Instantiate(originalSettings);testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=testSettings;InputSystem.EnableDevice(Keyboard.current);yield return new WaitForSeconds(1);

  story=FindFirstObjectByType<UnderwaterDayStory>();third=story.thirdChapter;player=story.player;body=player.GetComponent<Rigidbody2D>();story.ui.Clear();third.chapterCompleted.AddListener(()=>completed++);

  third.TryContinue();Check(!third.Started,"Third chapter cannot bypass unfinished second chapter");

  story.PrepareForSecondChapter();story.secondChapter.PrepareForThirdChapter();body.position=story.secondChapter.departurePoint;yield return new WaitForFixedUpdate();yield return PressE();Check(third.Stage==ThirdChapterStage.Preparing&&player.ControlsLocked,"Real E at second-chapter endpoint begins same-scene transition");yield return new WaitForSeconds(1.5f);

  Check(third.Stage==ThirdChapterStage.Travel&&story.Complete&&story.secondChapter.Complete&&!story.escort.Following,"Previous chapters remain complete without replay or escort");Check(Vector2.Distance(body.position,third.roadStart)<.2f&&!player.ControlsLocked,"Safe road start restores control");Check(story.ui.ActorTarget==third.leader.transform&&story.ui.ActorTargetOffscreen,"Offscreen marker directly tracks leader Transform");Capture("road-start");

  float aspect=Camera.main.aspect;bool inside=true;foreach(float ratio in new[]{.6f,1.777f,2.333f}){Camera.main.aspect=ratio;story.ui.SetActorTarget(third.leader.transform,third.leader.visual,"Köpekbalığı Lideri");var marker=story.ui.transform.Find("Underwater story HUD/Current story target").GetComponent<RectTransform>();var canvas=marker.parent.GetComponent<RectTransform>();inside&=Mathf.Abs(marker.anchoredPosition.x)+marker.rect.width*.5f<=canvas.rect.width*.5f+.1f&&Mathf.Abs(marker.anchoredPosition.y)+marker.rect.height*.5f<=canvas.rect.height*.5f+.1f;}Camera.main.aspect=aspect;Check(inside,"Leader edge marker remains inside UI at narrow, normal and wide aspect ratios");

  Check(third.hazards.terminalSpeed/player.speed>=1.2f&&third.hazards.terminalSpeed/player.speed<=1.5f&&player.speed==6,"Cannon speed 1.35x unchanged player speed");Check(third.checkpoints.All(p=>third.IsSafe(p)&&WaterRoute.Clear(p,.4f)),"All checkpoints clear of terrain and danger sections");

  var sign=third.warningSign;var ground=Physics2D.RaycastAll(new Vector2(sign.transform.position.x,player.surfaceY),Vector2.down,60).Where(h=>h.collider&&!h.collider.isTrigger&&!h.collider.attachedRigidbody).OrderBy(h=>h.distance).First();Check(Mathf.Abs(sign.bounds.min.y-ground.point.y+.12f)<.06f&&!sign.GetComponent<Collider2D>(),"Warning sign base embeds slightly in real ground without blocking route");

  Check(third.leader.visual.sprite.bounds.size==third.leaderTalk[0].bounds.size&&third.leaderTalk.All(s=>s.pivot==third.leaderSwim[0].pivot),"Leader poses share aligned canvas and pivot");Check(!third.TakeHit()&&third.Health==3,"Starting safe checkpoint rejects damage");

  var audio=story.GetComponent<UnderwaterSceneAudio>();Check(audio.ambience&&audio.bubble&&audio.SourceCount==5&&audio.bubbleVolume<audio.ambienceVolume,"One audio controller with fixed source count and quieter bubbles");

  var beds=audio.GetComponents<AudioSource>();beds[0].time=audio.ambience.length-.25f;yield return new WaitForSeconds(.9f);Check(beds[1].isPlaying&&beds[1].time<2&&audio.SourceCount==5,"Ambience crosses loop boundary with fixed overlapping sources");

  third.hazards.enabled=false;third.hazards.Begin();body.position=new Vector2(-126,-21);yield return new WaitForSeconds(1.4f);var ball=third.hazards.Spawn(-126);Check(ball&&Mathf.Abs(ball.transform.position.y-(player.surfaceY+.15f))<.01f,"Cannonball originates at true water surface, not camera top");

  ball.transform.position=body.position+Vector2.up*1.2f;ball.velocity=Vector2.down*25;yield return new WaitForSeconds(.12f);Check(third.Health==2&&!ball.Dangerous&&!ball.gameObject.activeSelf,"Swept cannon collision causes one hit and retires ball");Check(!third.TakeHit()&&third.Health==2,"Post-hit invulnerability blocks immediate repeat damage");

  yield return new WaitForSeconds(1.4f);Check(third.TakeHit()&&third.Health==1,"Damage resumes after 1.3s invulnerability");yield return new WaitForSeconds(1.4f);third.TakeHit();yield return new WaitForSeconds(1.2f);

  Check(third.Health==3&&Vector2.Distance(body.position,third.checkpoints[0])<.2f&&!player.ControlsLocked&&third.hazards.ActiveCount==0,"Three hits fade-respawn at safe checkpoint with health and hazards reset");Check(story.ui.ActorTarget==third.leader.transform&&story.Complete&&story.secondChapter.Complete,"Respawn retains leader goal and earlier chapter completion");

  third.hazards.enabled=false;third.hazards.Clear();var floor=Physics2D.RaycastAll(new Vector2(-126,0),Vector2.down,50).Where(h=>h.collider&&!h.collider.isTrigger&&!h.collider.attachedRigidbody).OrderBy(h=>h.distance).First();var wallBall=third.hazards.Spawn(-126);wallBall.transform.position=new Vector2(-126,floor.point.y+1);wallBall.velocity=Vector2.down*100;yield return new WaitForSeconds(.08f);Check(!wallBall.Dangerous&&!wallBall.gameObject.activeSelf,"High-speed swept circle stops at solid floor without tunneling");

  third.hazards.Clear();Check(third.hazards.Spawn(third.leaderRest.x)==null&&third.hazards.Spawn(sign.transform.position.x)==null,"Leader and sign safe areas refuse cannon spawns");third.hazards.enabled=true;third.hazards.Begin();

  yield return Swim(new Vector2(155,-27),95,true);Check(maxBalls<=third.hazards.maximumActive&&routeClear,"Normal-input full journey respects configured hazard cap and terrain");Check(third.Health>0&&maxBalls>0,"Normal-input dodge run reaches safe region with checkpoint recovery (hits="+routeHits+")");Check(third.CheckpointIndex==4,"Safe checkpoints advance along the route");Capture("last-checkpoint");

  yield return Swim(new Vector2(173,-27),12,true);yield return new WaitForSeconds(.3f);Check(story.ui.Active&&!story.ui.Blocking,"Sign approach shows chapter-local nonblocking warning");Capture("grounded-warning-sign");yield return new WaitForSeconds(3);Check(!player.ControlsLocked,"Sign and journey thoughts never lock movement");

  yield return Swim(third.leaderRest+Vector2.left*3,15);yield return new WaitForSeconds(.6f);Check(third.Stage==ThirdChapterStage.LeaderReady&&!story.ui.ActorTargetOffscreen&&story.ui.ActorTarget==third.leader.transform,"Visible leader gets overhead goal and talk objective");Capture("leader-approach");

  Check(!third.TakeHit()&&third.Health>0,"Leader safe area blocks damage");int drain=0;while(story.ui.Active&&drain++<40){yield return null;story.ui.Advance();yield return null;}yield return PressE();Check(third.Stage==ThirdChapterStage.Dialogue&&player.ControlsLocked&&third.hazards.ActiveCount==0&&!third.hazards.Running,"Dialogue stops local hazards and hides mission marker");

  var lines=third.DialogueLines();for(int i=0;i<lines.Length;i++){Check(story.ui.CurrentText==lines[i].text&&story.ui.SpeakerName==lines[i].speakerName&&story.ui.SpeakerPortrait==lines[i].portrait,"Leader dialogue sentence "+(i+1)+" has correct speaker/text/portrait");yield return PressE();Check(story.ui.TextReady&&story.ui.CurrentText==lines[i].text,"Typewriter completes before advancing sentence "+(i+1));if(i==0||i==7)Capture("leader-dialogue-"+i);yield return PressE();}

  Check(!player.ControlsLocked&&third.Stage==ThirdChapterStage.Rally,"Dialogue releases controls for short safe rally");float wait=Time.time;while(!third.Complete&&Time.time-wait<15)yield return null;

  Check(Vector2.Distance(third.leader.Position,third.rallyPositions[0])<.4f&&Vector2.Distance(third.guards[0].Position,third.rallyPositions[1])<.4f&&Vector2.Distance(third.guards[1].Position,third.rallyPositions[2])<.4f,"All three allies physically reach distinct rally positions");Check(third.Complete&&completed==1&&third.hazards.ActiveCount==0&&!third.hazards.Running,"Leader and guards rally; completion fires once and clears hazards");Check(story.ui.ActorTarget==null&&story.ui.Objective=="Köpekbalıklarının yardımı sağlandı.","Completion clears goal marker and records help secured");Capture("chapter-complete");

  third.Interact();third.BeginThirdChapter();Check(completed==1&&!story.ui.Active&&third.Complete,"Completed dialogue and chapter cannot replay");Check(audio.SourceCount==5&&audio.BubblePlays>0,"Audio sources do not multiply across respawn and chapter changes");Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="UnderwaterWorld"&&errors==0,"No scene transitions or new runtime errors");

  File.WriteAllText("Tools/Revision10/test-complete.txt","Finished normal-control Play Mode run");EditorApplication.isPlaying=false;

 }

}}

#endif

