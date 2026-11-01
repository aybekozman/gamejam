#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Linq;
namespace Abyss.Story {
public class Revision8Verification:MonoBehaviour {
 System.Collections.Generic.List<string> results=new();
 void Check(bool ok,string text){results.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("Tools/Revision8/results.txt",results);}
 IEnumerator Start(){
 yield return new WaitForSeconds(1);var story=FindFirstObjectByType<UnderwaterDayStory>();story.ui.Clear();
 Check(story.corpseRiseSpeed==4.2f,"Ascent speed 4.2 (previously 1.8)");
 for(int i=0;i<3;i++)Check(!story.bodies[i].GetComponent<SpriteRenderer>().flipY&&Mathf.Abs(Mathf.DeltaAngle(story.bodies[i].eulerAngles.z,180))>30,"Corpse belly-up with clearly visible diagonal angle");
 Check(story.playerHome&&story.playerHome.name=="House","Player home uses authored House");
 story.enabled=false;typeof(UnderwaterDayStory).GetField("stage",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(story,UnderwaterDayStage.Completed);
 typeof(UnderwaterDayStory).GetMethod("UpdateObjective",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(story,null);
 Check(story.ui.Objective=="Eve dön.","Completion objective says Eve dön");
 story.player.GetComponent<Rigidbody2D>().position=story.playerHome.bounds.center+Vector3.up*(story.playerHome.bounds.extents.y+1);
 yield return new WaitForFixedUpdate();
 typeof(UnderwaterDayStory).GetMethod("UpdateHints",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(story,null);
 Check(story.NearHome&&story.ui.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="E • Eve gir"),"Near home shows E entry hint");
 story.TryInteractNearest();yield return null;
 Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="UnderwaterWorld"&&story.Complete&&!story.ui.Active,"Home interaction does nothing and preserves completion");
 EditorApplication.isPlaying=false;
 }
}}
#endif
