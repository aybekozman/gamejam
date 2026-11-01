#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Abyss.Story
{
    public class SecondChapterVerification:MonoBehaviour
    {
        readonly List<string> results=new();UnderwaterDayStory story;UnderwaterSecondChapter chapter;FishPilot player;Rigidbody2D body;
        InputSettings originalSettings,testSettings;int errors,completeEvents,exitEvents;
        void OnEnable()=>Application.logMessageReceived+=Log;
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors++;}
        void OnDisable(){Application.logMessageReceived-=Log;if(originalSettings)InputSystem.settings=originalSettings;if(testSettings)Destroy(testSettings);}
        void Check(bool condition,string label){results.Add((condition?"PASS ":"FAIL ")+label);File.WriteAllLines("Tools/Revision9/playmode-results.txt",results);}
        IEnumerator PressE(){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));yield return null;yield return null;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;}
        IEnumerator Drain(){int guard=0;while(story.ui.Active&&guard++<100){yield return null;story.ui.Advance();yield return null;}Check(!story.ui.Active,"Dialogue drains safely");}
        IEnumerator Travel(Vector2 goal,float timeout=24)
        {
            var path=WaterRoute.Find(body.position,goal,.4f);Check(path.Count>0,"Clear player route to "+goal);float start=Time.time;
            foreach(var point in path)while(Vector2.Distance(body.position,point)>.25f&&Time.time-start<timeout){player.testInput=(point-body.position).normalized;yield return new WaitForFixedUpdate();}
            player.testInput=Vector2.zero;body.linearVelocity=Vector2.zero;Check(Vector2.Distance(body.position,goal)<.8f,"Actual player swim reaches "+goal);
        }
        void Capture(string name)
        {
            var cam=Camera.main;var rt=RenderTexture.GetTemporary(1280,720,24);var previous=cam.targetTexture;var active=RenderTexture.active;cam.targetTexture=rt;
            var canvas=story.ui.GetComponentInChildren<Canvas>();float scale=canvas.scaleFactor;canvas.scaleFactor=720f/1080;
            foreach(var text in canvas.GetComponentsInChildren<Text>())text.SetAllDirty();Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
            var capture=new Texture2D(1280,720,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,1280,720),0,0);capture.Apply();File.WriteAllBytes("Tools/Revision9/"+name+".png",capture.EncodeToPNG());Destroy(capture);cam.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);canvas.scaleFactor=scale;foreach(var text in canvas.GetComponentsInChildren<Text>())text.SetAllDirty();
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("Tools/Revision9");Application.runInBackground=true;
            originalSettings=InputSystem.settings;testSettings=Instantiate(originalSettings);testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=testSettings;InputSystem.EnableDevice(Keyboard.current);
            yield return new WaitForSeconds(1);story=FindFirstObjectByType<UnderwaterDayStory>();chapter=story.secondChapter;player=story.player;body=player.GetComponent<Rigidbody2D>();story.ui.Clear();
            var mayor=story.mayor;chapter.firstChapterDepartureRequested.AddListener(()=>exitEvents++);chapter.secondChapterCompleted.AddListener(()=>completeEvents++);
            chapter.RequestHomeEntry();Check(!chapter.Started,"Home entry cannot skip unfinished first chapter");
            story.PrepareForSecondChapter();yield return Travel(story.Discovery+Vector2.down);yield return new WaitForSeconds(.5f);Capture("corrected-surface-bodies");
            for(int i=0;i<3;i++)Check(Mathf.Abs(Mathf.DeltaAngle(story.bodies[i].eulerAngles.z,UnderwaterDayStory.CorpseAngle(i)))<3&&!story.bodies[i].GetComponent<SpriteRenderer>().flipY,"Source corpse retains pose without extra 180-degree inversion");
            yield return Travel(chapter.homeArrival);chapter.skipShipForDevelopment=false;yield return PressE();yield return PressE();
            Check(exitEvents==1&&!chapter.Started&&!player.ControlsLocked,"Development skip disabled invokes exactly one connection event, no scene loading or lock");
            story.ResetStory();story.ui.Clear();story.PrepareForSecondChapter();chapter.skipShipForDevelopment=true;yield return Travel(chapter.homeArrival);yield return PressE();
            Check(chapter.Stage==SecondChapterStage.Preparing&&player.ControlsLocked,"Real E at home begins fade and preparation");
            float until=Time.time+8;while(chapter.Stage==SecondChapterStage.Preparing&&Time.time<until)yield return null;
            Check(chapter.Stage==SecondChapterStage.AttendAnnouncement&&story.Complete&&story.InspectionCount==3,"Same-scene entry preserves chapter-one completion and starts chapter two");
            Check(Vector2.Distance(body.position,chapter.homeArrival)<.2f&&WaterRoute.Clear(body.position,.4f),"Chapter-two arrival is safely in front of House");
            Check(!player.ControlsLocked&&!story.escort.Following&&story.mayor==mayor,"Controls restored, old escort stopped, existing mayor reused");
            Check(chapter.scientists.Length==2&&chapter.scientists[0].visual.sprite.name.StartsWith("reef_1")&&chapter.scientists[1].visual.sprite.name.StartsWith("reef_2"),"Selected lower reef fish used as two scientists");
            Check(chapter.townsfolk.Length==4&&chapter.participantRoot.childCount==6,"Exactly two scientists and four citizens, no duplicate NPCs");
            var actors=chapter.scientists.Concat(chapter.townsfolk).Append(mayor).ToArray();Check(actors.All(a=>WaterRoute.Clear(a.Position,.4f)),"All meeting actors stand clear of original buildings and terrain");
            bool separate=true;for(int i=0;i<actors.Length;i++)for(int j=i+1;j<actors.Length;j++)separate&=Vector2.Distance(actors[i].Position,actors[j].Position)>1.25f;Check(separate,"Meeting actors have separate resting positions");
            yield return Drain();chapter.RequestHomeEntry();chapter.BeginSecondChapter();Check(chapter.participantRoot.childCount==6&&chapter.Stage==SecondChapterStage.AttendAnnouncement,"Repeated home entry/start cannot duplicate or restart chapter");
            yield return Travel(chapter.departurePoint);chapter.Interact();Check(chapter.Stage==SecondChapterStage.AttendAnnouncement&&completeEvents==0&&!story.ui.Active,"Exit point and interaction at distance cannot bypass announcement");
            yield return Travel(chapter.meetingPoint+Vector2.down*1.25f);yield return new WaitForSeconds(.5f);Capture("announcement-gathering");yield return PressE();
            Check(chapter.Stage==SecondChapterStage.Announcement&&player.ControlsLocked&&story.ui.CurrentText==chapter.AnnouncementLines()[0].text&&!story.ui.TextReady,"Opening E starts first sentence without skipping it");
            var expected=chapter.AnnouncementLines();
            for(int i=0;i<expected.Length;i++)
            {
                Check(story.ui.CurrentText==expected[i].text&&story.ui.SpeakerName==expected[i].speakerName&&story.ui.SpeakerPortrait==expected[i].portrait,"Sentence "+(i+1)+": correct text, speaker and portrait");
                yield return PressE();Check(story.ui.TextReady&&story.ui.CurrentText==expected[i].text,"First E completes sentence "+(i+1));
                if(i==2||i==4||i==10||i==17)Capture("dialogue-speaker-"+i);
                yield return PressE();
            }
            Check(chapter.Stage==SecondChapterStage.LeaveForSharks&&!player.ControlsLocked&&!story.escort.Following,"Announcement releases controls and enables travel objective only once");
            Check(story.ui.Objective=="Köpekbalıklarına haber vermek için yola çık.","Requested travel objective is displayed");
            var starts=chapter.townsfolk.Select(a=>a.Position).ToArray();yield return new WaitForSeconds(1.1f);
            Check(chapter.townsfolk[0].Position.y>starts[0].y+.2f&&Vector2.Distance(chapter.townsfolk[2].Position,starts[2])<.1f,"Citizen departures start in staggered small groups");
            Check(Vector2.Distance(mayor.Position,chapter.meetingPoint)<.1f&&chapter.scientists.All(a=>Vector2.Distance(a.Position,a.GetComponent<MeetingFishIdle>().restPosition)<.1f),"Mayor and scientists remain at the meeting");
            yield return Drain();yield return new WaitForSeconds(2);
            var previous=chapter.townsfolk.Select(a=>a.Position).ToArray();bool clear=true;float deadline=Time.time+18;
            while(Time.time<deadline)
            {
                for(int i=0;i<3;i++){var position=chapter.townsfolk[i].Position;clear&=WaterRoute.Clear(position,.29f)&&WaterRoute.Segment(previous[i],position,.29f);previous[i]=position;}
                if(chapter.townsfolk.Take(3).All(a=>a.Arrived))break;yield return new WaitForSeconds(.1f);
            }
            Check(clear&&chapter.townsfolk.Take(3).All(a=>a.Position.y>player.surfaceY-3),"Departing fish reach surface on obstacle-free physical routes");
            Check(Vector2.Distance(chapter.townsfolk[3].Position,starts[3])<.1f,"Some citizens remain in the town");
            yield return Travel(chapter.departurePoint);yield return null;
            Check(chapter.Complete&&completeEvents==1&&!player.ControlsLocked,"Exit after announcement completes chapter once and keeps controls free");
            Check(story.ui.Objective=="İkinci bölüm tamamlandı.","Short chapter completion message appears");
            yield return new WaitForSeconds(3.2f);Check(story.ui.Objective=="","Active objective clears after completion");
            chapter.BeginSecondChapter();chapter.Interact();chapter.RequestHomeEntry();Check(completeEvents==1&&chapter.Complete,"Repeated entry and interaction cannot replay completed chapter");
            float x=body.position.x;player.testInput=Vector2.right;yield return new WaitForSeconds(.5f);player.testInput=Vector2.zero;Check(body.position.x>x+1,"Free swimming remains available after completion");
            Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="UnderwaterWorld"&&errors==0,"No scene transition, combat or runtime errors");
            story.ResetStory();story.ui.Clear();chapter.BeginSecondChapter();yield return new WaitForSeconds(2);
            Check(chapter.Stage==SecondChapterStage.AttendAnnouncement&&chapter.participantRoot.childCount==6,"Developer direct entry resets and reuses actors");
            if(story.thirdChapter){story.thirdChapter.BeginThirdChapter();yield return new WaitForSeconds(2);Check(chapter.scientists.All(a=>a.gameObject.activeInHierarchy),"Chapter-three handoff preserves scientists already waiting in town");Check(story.thirdChapter.Stage==ThirdChapterStage.Travel&&chapter.Complete&&story.Complete&&!player.ControlsLocked&&errors==0,"Second-to-third handoff keeps completed chapters and free controls without errors");}
            File.WriteAllText("Tools/Revision9/test-complete.txt","Finished");EditorApplication.isPlaying=false;
        }
    }
}
#endif
