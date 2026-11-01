#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;

namespace Abyss.Story
{
    public class ThirdChapterPolishVerification:MonoBehaviour
    {
        readonly List<string> results=new();InputSettings original,copy;int errors;
        void OnEnable()=>Application.logMessageReceived+=Log;
        void Log(string m,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors++;}
        void OnDisable(){Application.logMessageReceived-=Log;if(original)InputSystem.settings=original;if(copy)Destroy(copy);}
        void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines("Tools/Revision11/playmode-results.txt",results);}
        IEnumerator Press(){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));yield return null;yield return null;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;}
        IEnumerator Start()
        {
            original=InputSystem.settings;copy=Instantiate(original);copy.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;copy.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=copy;InputSystem.EnableDevice(Keyboard.current);yield return new WaitForSeconds(1);
            var story=FindFirstObjectByType<UnderwaterDayStory>();var third=story.thirdChapter;story.ui.Clear();story.PrepareForSecondChapter();story.secondChapter.PrepareForThirdChapter();story.player.GetComponent<Rigidbody2D>().position=story.secondChapter.departurePoint;yield return null;yield return null;
            Check(story.ui.ContinuePromptVisible&&!story.ui.Active,"Continue instruction uses visible portrait textbox without consuming dialogue input");
            yield return Press();Check(third.Stage==ThirdChapterStage.Preparing,"One real E press activates continue textbox");yield return new WaitForSeconds(1.2f);
            Check(story.ui.Active&&!story.ui.Blocking&&!story.player.ControlsLocked&&story.ui.CurrentText=="Yukarıda çatışma başlamış…","Travel thought appears in standard nonblocking dialogue panel");
            third.NearMiss();yield return new WaitForSeconds(2.9f);Check(story.ui.Active&&!story.ui.Blocking&&!story.player.ControlsLocked,"Queued journey text stays readable while controls remain available");
            bool found=false;int guard=0;while(story.ui.Active&&guard++<30){found|=story.ui.CurrentText=="Toplar buraya kadar düşüyor!";yield return null;story.ui.Advance();yield return null;}Check(found,"Near-miss sentence is queued rather than overwritten by other travel dialogue");
            var hazards=third.hazards;Check(hazards.maximumActive==10&&hazards.shotInterval==.65f&&hazards.tutorialInterval==1&&hazards.laneSpacing==7,"Denser Inspector settings applied to saved scene");
            story.player.GetComponent<Rigidbody2D>().position=new Vector2(-80,-27);yield return null;hazards.Begin();int maximum=0;float end=Time.time+7;
            while(Time.time<end){maximum=Mathf.Max(maximum,hazards.ActiveCount);yield return null;}
            Check(maximum>=6&&maximum<=10,"Normal section produces dense falling stream within new cap (peak="+maximum+")");
            story.ui.QueueTravelDialogue("Toplar buraya kadar düşüyor!");Check(!hazards.Paused&&!story.player.ControlsLocked,"Travel textbox does not pause hazards or movement");
            Check(hazards.Spawn(third.leaderRest.x)==null&&hazards.Spawn(third.warningSign.transform.position.x)==null,"Leader and sign safe zones remain protected");
            Check(errors==0,"No new runtime errors");EditorApplication.isPlaying=false;
        }
    }
}
#endif
