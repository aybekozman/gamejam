#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Linq;
namespace Abyss.Story {
public class Revision7Verification:MonoBehaviour {
 System.Collections.Generic.List<string> results=new();
 void Check(bool ok,string text){results.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("Tools/Revision7/results.txt",results);}
 IEnumerator Start(){
 yield return new WaitForSeconds(1);
 var story=FindFirstObjectByType<UnderwaterDayStory>();story.ui.Clear();
 Check(GameObject.Find("House").GetComponent<Collider2D>().enabled,"Authored House preserved with original solid collider");
 Check(story.corpseRiseSpeed==1.8f,"Corpse ascent increased from 1.1 to 1.8");
 foreach(var b in story.bodies){b.gameObject.SetActive(true);Check(b.GetComponent<SpriteRenderer>().flipY&&Mathf.Abs(Mathf.DeltaAngle(b.eulerAngles.z,0))<=15,"Other corpse is belly-up with gentle tilt");}
 Check(story.mayor.maxPitch==story.player.tilt&&story.mayor.pitchResponse==story.player.pitchResponse,"Mayor uses player pitch and smoothing");
 story.mayor.Place(story.Discovery+Vector2.down*8);story.mayor.Go(story.Discovery);yield return new WaitForSeconds(.7f);
 Check(Mathf.Abs(Mathf.DeltaAngle(story.mayor.visual.transform.localEulerAngles.z,0))>45,"Mayor visibly pitches upward during physical ascent");
 story.mayor.Stop();
 Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="UnderwaterWorld","Scene transition remains deferred");
 File.WriteAllText("Tools/Revision7/test-complete.txt","Done");EditorApplication.isPlaying=false;
 }
}}
#endif
