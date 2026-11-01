#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Abyss.Story;

namespace Abyss
{
    public class DayStoryVerification:MonoBehaviour
    {
        readonly List<string> results=new();UnderwaterDayStory story;FishPilot player;Rigidbody2D body;int errors;InputSettings originalSettings,testSettings;
        void OnEnable()=>Application.logMessageReceived+=Log;
        void OnDisable(){Application.logMessageReceived-=Log;if(originalSettings)InputSystem.settings=originalSettings;if(testSettings)Destroy(testSettings);}
        void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception)errors++;}
        void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines("Tools/Revision6/playmode-results.txt",results);}
        IEnumerator Drain()
        {
            int limit=0;while(story.ui.Active&&limit++<50){yield return null;story.ui.Advance();yield return null;}
            Check(!story.ui.Active,"Dialogue queue drains and releases its input frame");
        }
        IEnumerator Travel(Vector2 goal,float timeout=18)
        {
            var route=WaterRoute.Find(body.position,goal,.4f);Check(route.Count>0,"Player route exists to "+goal);float start=Time.time;
            foreach(var point in route){while(Vector2.Distance(body.position,point)>.25f&&Time.time-start<timeout){player.testInput=(point-body.position).normalized;yield return new WaitForFixedUpdate();}}
            player.testInput=Vector2.zero;body.linearVelocity=Vector2.zero;Check(Vector2.Distance(body.position,goal)<.8f,"Player swims to "+goal+" without teleporting");
        }
        IEnumerator PressE(){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));yield return null;yield return null;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;}
        void Capture(string name)
        {
            var camera=Camera.main;var rt=RenderTexture.GetTemporary(1280,720,24);var old=camera.targetTexture;var active=RenderTexture.active;camera.targetTexture=rt;
            var canvas=story.ui.GetComponentInChildren<Canvas>();float scale=canvas.scaleFactor;canvas.scaleFactor=720f/1080;
            foreach(var text in canvas.GetComponentsInChildren<Text>())text.SetAllDirty();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Tools/Revision6/"+name+".png",image.EncodeToPNG());Destroy(image);camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);canvas.scaleFactor=scale;foreach(var text in canvas.GetComponentsInChildren<Text>())text.SetAllDirty();
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("Tools/Revision6");Application.runInBackground=true;
            originalSettings=InputSystem.settings;testSettings=Instantiate(originalSettings);testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=testSettings;InputSystem.EnableDevice(Keyboard.current);yield return new WaitForSeconds(1);
            story=FindFirstObjectByType<UnderwaterDayStory>();player=story.player;body=player.GetComponent<Rigidbody2D>();
            Check(story.Stage==UnderwaterDayStage.Beginning&&!player.ControlsLocked,"Start near home with free controls");Capture("initial-dialogue");
            story.ui.Font.RequestCharactersInTexture("şŞıİğĞçÇöÖüÜ");Check("şŞıİğĞçÇöÖüÜ".All(c=>story.ui.Font.HasCharacter(c)),"Dialogue font supports Turkish characters");
            Check(story.inputActions.FindAction("Player/Interact").bindings.Any(b=>b.path=="<Keyboard>/e"),"Existing Interact/E action reused");
            Check(story.openEyes.rect.size==story.closedEyes.rect.size&&story.openEyes.pivot==story.closedEyes.pivot,"Open/closed fish share canvas and pivot");
            Check(story.mayor.visual.sprite.name.StartsWith("reef_0"),"Purple top reef fish is the mayor");
            Check(FindFirstObjectByType<OceanHabitat>().species.All(s=>!s.frames.Any(f=>f.name.StartsWith("reef_"))),"Mayor and scientist appearances reserved from ambient spawning");
            yield return Drain();
            yield return Travel(story.mayor.Position+Vector2.left*1.4f);yield return Drain();
            var early=story.Stage;story.TalkToMayor();Check(story.Stage==early&&story.Stage!=UnderwaterDayStage.EscortMayor,"Early mayor conversation cannot advance the quest");yield return Drain();
            float wait=Time.time;while(story.Stage<UnderwaterDayStage.FollowToSurface&&Time.time-wait<15)yield return null;
            Check(story.Stage==UnderwaterDayStage.FollowToSurface,"One-time passing fish reaches death/ascent stage");
            Check(story.eventFish.visual.sprite==story.closedEyes&&Mathf.Abs(Mathf.DeltaAngle(story.eventFish.visual.transform.localEulerAngles.z,180))<8,"Closed eyes and genuine belly-up rotation");
            float initialY=story.eventFish.Position.y;yield return new WaitForSeconds(2);Check(story.eventFish.Position.y>initialY+1,"Corpse rises while player stays behind");Capture("fish-rising");
            wait=Time.time;while(story.eventFish.Position.y<story.Discovery.y-.25f&&Time.time-wait<42)yield return new WaitForSeconds(.25f);
            Check(story.eventFish.gameObject.activeSelf&&story.eventFish.Position.y<=player.surfaceY-.4f,"Story fish persists and stops underwater at the surface");
            Check(story.Stage==UnderwaterDayStage.FollowToSurface,"Slow player does not lose or auto-complete discovery");
            yield return Drain();yield return Travel(story.Discovery+Vector2.down);yield return new WaitForSeconds(.4f);yield return Drain();
            Check(story.Stage==UnderwaterDayStage.InspectBodies&&story.InspectionCount==0,"Surface discovery starts three independent inspections");
            var corpses=story.bodies.Select(b=>b.GetComponent<SpriteRenderer>()).Append(story.eventFish.visual).ToArray();bool separated=true;for(int i=0;i<corpses.Length;i++)for(int j=i+1;j<corpses.Length;j++)separated&=!corpses[i].bounds.Intersects(corpses[j].bounds);Check(separated,"Each surface body remains visibly separate and inspectable");Capture("surface-discovery");
            yield return Travel((Vector2)story.bodies[2].position+Vector2.down*.6f);yield return PressE();
            Check(story.InspectionCount==1&&story.ui.Active,"Actual E input opens a single inspection");string sentence=story.ui.CurrentText;yield return PressE();Check(story.ui.CurrentText==sentence&&story.ui.TextReady,"First advance completes typewriter without skipping sentence");yield return Drain();
            Check(!story.TryInspect(2)&&story.InspectionCount==1,"Repeated target never increments count");
            yield return Travel(story.Discovery+new Vector2(-8,-3));Check(story.InspectionCount==1,"Leaving investigation area preserves progress");
            yield return Travel((Vector2)story.bodies[0].position+Vector2.down*.6f);story.TryInspect(0);yield return Drain();
            yield return Travel((Vector2)story.bodies[1].position+Vector2.down*.6f);story.TryInspect(1);yield return Drain();
            Check(story.Stage==UnderwaterDayStage.ReportToMayor&&story.InspectionCount==3,"Inspections work in order 3,1,2 and unlock report only after all three");
            yield return Travel(story.mayor.Position+Vector2.left*1.4f);story.TalkToMayor();Check(player.ControlsLocked&&story.ui.Blocking,"Report conversation temporarily locks swimming");yield return new WaitForSeconds(1.1f);Capture("report-to-mayor");yield return Drain();
            Check(story.Stage==UnderwaterDayStage.EscortMayor&&story.escort.Following&&!player.ControlsLocked,"Mayor follows only after complete report dialogue");
            float speed=story.mayor.swimSpeed;story.mayor.swimSpeed=.7f;yield return Travel(story.Discovery+Vector2.down);
            Check(story.Stage==UnderwaterDayStage.EscortMayor&&!story.CanFinish,"Player reaching surface alone cannot trigger final conversation");story.mayor.swimSpeed=speed;
            wait=Time.time;while(story.Stage==UnderwaterDayStage.EscortMayor&&Time.time-wait<45)yield return new WaitForSeconds(.25f);
            Check(story.Stage==UnderwaterDayStage.FinalConversation&&Vector2.Distance(story.mayor.Position,story.Discovery)<story.finalMayorRadius+.05f,"Mayor reaches discovery through real route before final conversation");yield return new WaitForSeconds(.7f);Capture("mayor-at-surface");yield return Drain();
            Check(story.Complete&&!player.ControlsLocked&&!story.escort.Following,"Ending records completion, stops escort and restores control");
            var parked=story.mayor.Position;yield return new WaitForSeconds(.6f);Check(Vector2.Distance(parked,story.mayor.Position)<.08f,"Mayor remains parked safely after ending");
            yield return Travel(story.mayor.Position+Vector2.up*1.2f);story.TalkToMayor();Check(!story.ui.Active&&story.Complete,"Completed event and final dialogue do not restart");
            float x=body.position.x;player.testInput=Vector2.right;yield return new WaitForSeconds(.5f);Check(body.position.x>x+1,"Free swimming continues after story completion");player.testInput=null;
            Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path=="Assets/Scenes/UnderwaterWorld.unity","No scene transition or new day");Check(errors==0,"No runtime errors throughout story verification");
            story.ResetStory();Check(story.Stage==UnderwaterDayStage.Beginning&&story.InspectionCount==0&&!player.ControlsLocked,"Developer reset restores a fresh playable story");
        }
    }
}
#endif
