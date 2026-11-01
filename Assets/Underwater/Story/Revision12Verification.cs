#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;
using System.Linq;
using System.IO;
namespace Abyss.Story {
public class Revision12Verification:MonoBehaviour {
 System.Collections.Generic.List<string> results=new();InputSettings original,copy;int errors;
 void OnEnable()=>Application.logMessageReceived+=Log;void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;File.AppendAllText("Tools/Revision12/errors.txt",m+"\n"+s);}}
 void OnDisable(){Application.logMessageReceived-=Log;if(original)InputSystem.settings=original;if(copy)Destroy(copy);}
 void Check(bool value,string label){results.Add((value?"PASS ":"FAIL ")+label);File.WriteAllLines("Tools/Revision12/results.txt",results);}
 void Capture(UnderwaterDayStory story){var cam=Camera.main;var rt=RenderTexture.GetTemporary(1280,720,24);var old=cam.targetTexture;var active=RenderTexture.active;cam.targetTexture=rt;var canvas=story.ui.GetComponentInChildren<Canvas>();float scale=canvas.scaleFactor;canvas.scaleFactor=720f/1080;foreach(var t in canvas.GetComponentsInChildren<UnityEngine.UI.Text>())t.SetAllDirty();Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes("Tools/Revision12/pixel-font.png",texture.EncodeToPNG());Destroy(texture);cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);canvas.scaleFactor=scale;}
 void thirdStart(UnderwaterDayStory story){story.thirdChapter.StartCoroutine("SendMessenger");}
 IEnumerator Start(){original=InputSystem.settings;copy=Instantiate(original);copy.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;copy.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=copy;InputSystem.EnableDevice(Keyboard.current);yield return new WaitForSeconds(1);
 var story=FindFirstObjectByType<UnderwaterDayStory>();story.ui.Clear();var player=story.player;var body=player.GetComponent<Rigidbody2D>();var env=story.GetComponent<WarEnvironment>();story.PrepareForSecondChapter();body.position=new Vector2(-150,-20);
 Check(env&&env.pixelFont,"Scene-local pixel font assigned");story.ui.Font.RequestCharactersInTexture("çÇğĞıİöÖşŞüÜ");Check("çÇğĞıİöÖşŞüÜ".All(c=>story.ui.Font.HasCharacter(c)),"Pixel font supports all Turkish glyphs");
 InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.D));yield return new WaitForSeconds(.6f);float normal=body.linearVelocity.magnitude;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.D,Key.LeftShift));yield return new WaitForSeconds(.7f);Check(body.linearVelocity.magnitude>normal*1.4f&&body.linearVelocity.magnitude<=normal*1.55f,"Shift smoothly reaches 1.5x actual normal speed");InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.D));yield return new WaitForSeconds(.7f);Check(body.linearVelocity.magnitude<normal*1.05f,"Releasing Shift returns normal speed");InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
 player.HitReaction(Vector2.left);yield return null;Check(player.visual.color.r>player.visual.color.g+.4f,"Hit turns player red");yield return new WaitForSeconds(.25f);Check(player.visual.color==Color.white,"Hit color returns exactly after short feedback");player.HitReaction(Vector2.down);player.ControlsLocked=true;yield return null;Check(player.visual.color==Color.white,"Dialogue control lock clears reaction");player.ControlsLocked=false;
 story.thirdChapter.BeginThirdChapter();yield return new WaitForSeconds(1.5f);Check(story.thirdChapter.hazards.shotInterval==.455f,"Shot interval shortened 30 percent");Check(story.thirdChapter.DialogueLines().Any(l=>l.speakerName=="Haberci Köpekbalığı"),"Separate messenger dialogue added");
 var go=new GameObject("Test ambient fish");var r=go.AddComponent<SpriteRenderer>();r.sprite=player.swim[0];go.transform.position=new Vector2(-150,player.surfaceY-.3f);var fish=go.AddComponent<RoamingCreature>();fish.definition=new OceanSpecies{family=HabitatFamily.ReefFish};Check(env.StrikeFish((Vector2)go.transform.position+Vector2.up,(Vector2)go.transform.position+Vector2.down),"Decorative reef fish receives cannon sweep");yield return new WaitForSeconds(1.5f);Check(!fish.enabled&&go.transform.position.y<=player.surfaceY&&go.GetComponent<WarFishCorpse>(),"Dead ambient fish stays dead and stops at surface");
 env.previewStage=3;yield return new WaitForSeconds(2);Check(env.GetComponentsInChildren<WarDebris>().Length>0&&env.GetComponentsInChildren<WarDebris>().Length<=env.maximumWaste,"Pollution creates bounded harmless litter");env.previewStage=0;yield return null;yield return null;Check(env.GetComponentsInChildren<WarDebris>().Length==0,"Clean preview removes accumulated litter without multiplying");
 Check(env.GetComponentsInChildren<SpriteRenderer>(true).Count(x=>x.name=="Static surface treatment vessel")==1,"One decorative surface ship");thirdStart(story);yield return new WaitForSeconds(8);Check(!story.thirdChapter.messenger.gameObject.activeSelf,"Messenger swims to open sea and retires away from player");story.ui.Show(new[]{new UnderwaterStoryUI.Line(false,"Çevreyi korumalıyız. Şimdi köpekbalıklarından yardım isteyelim.")},false);yield return new WaitForSeconds(2);Capture(story);Check(errors==0,"No runtime errors in targeted checks");EditorApplication.isPlaying=false;
 }
}}
#endif
