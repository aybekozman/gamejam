#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace Abyss {
public class UnderwaterVerification:MonoBehaviour {
 List<string> results=new();int errors;
 void OnEnable(){Application.logMessageReceived+=Log;}
 void OnDisable(){Application.logMessageReceived-=Log;}
 void Log(string c,string s,LogType t){if(t==LogType.Exception||t==LogType.Error)errors++;}
 void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);}
 void Capture(string name){var cam=Camera.main;var rt=RenderTexture.GetTemporary(1280,720,24);var old=cam.targetTexture;cam.targetTexture=rt;cam.Render();var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Tools/Verification/"+name+".png",image.EncodeToPNG());Destroy(image);cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
 IEnumerator Start(){
 Directory.CreateDirectory("Tools/Verification");yield return new WaitForSeconds(.5f);
 var p=FindFirstObjectByType<FishPilot>();var rb=p.GetComponent<Rigidbody2D>();
 p.testInput=Vector2.right;rb.position=new Vector2(-22,0);yield return new WaitForSeconds(1);float straight=rb.linearVelocity.magnitude;Check(FindObjectsByType<Bubble>(FindObjectsSortMode.None).Length>0,"Swimming emits small world bubbles");
 p.testInput=Vector2.one;yield return new WaitForSeconds(.5f);Check(Mathf.Abs(rb.linearVelocity.magnitude-straight)<.1f,"Diagonal speed equals cardinal speed");
 p.testInput=Vector2.left;yield return new WaitForSeconds(.3f);Check(p.visual.flipX,"Left facing");p.testInput=Vector2.up;yield return new WaitForSeconds(.2f);Check(p.visual.flipX,"Vertical input preserves horizontal facing");
 p.testInput=Vector2.zero;yield return new WaitForSeconds(.4f);Check(rb.linearVelocity.magnitude<.05f,"Braking stops drift; no gravity");
 rb.position=new Vector2(-22,11);p.testInput=Vector2.up;yield return new WaitForSeconds(1);Check(rb.position.y<=11.63f&&rb.position.y>11.4f,"Surface body bound");Check(Camera.main.transform.position.y<=9.02f,"Camera surface limit");
 p.testInput=Vector2.zero;yield return new WaitForSeconds(.6f);Capture("surface");yield return new WaitForSeconds(.3f);
 rb.position=new Vector2(-26,-7.5f);yield return new WaitForSeconds(1.2f);Capture("town");yield return new WaitForSeconds(.3f);
 var rock=System.Array.Find(FindObjectsByType<PolygonCollider2D>(FindObjectsSortMode.None),c=>c.name=="Reef rock");var bounds=rock.bounds;rb.position=new Vector2(bounds.center.x,bounds.max.y+1.7f);var contact=System.Array.Find(Physics2D.RaycastAll(rb.position,Vector2.down,20),h=>h.collider==rock);p.testInput=Vector2.down;bool touched=false;for(int step=0;step<100;step++){yield return new WaitForFixedUpdate();if(rb.IsTouching(rock)){touched=true;break;}}Check(contact.collider==rock && touched && rb.position.y>bounds.min.y,"Reef contour blocks swimming with real physics contact");results.Add("INFO Reef contact: "+touched+"; position="+rb.position+"; ray contour="+contact.point);
 rb.position=new Vector2(24,-28);rb.linearVelocity=Vector2.zero;p.testInput=Vector2.zero;yield return new WaitForSeconds(1.2f);Capture("deep");yield return new WaitForSeconds(.3f);
 rb.position=new Vector2(100,0);p.testInput=Vector2.right;yield return new WaitForSeconds(.5f);Check(rb.position.x<=45.31f,"World boundary");Check(Mathf.Abs(Camera.main.orthographicSize-7)<.01f,"Constant camera scale");
 Check(FindObjectsByType<Drift>(FindObjectsSortMode.None).Length>=20,"Ambient creatures and rooted plants running");Check(System.Array.TrueForAll(FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None),s=>s.sprite!=null),"All scene sprites resolved");Check(errors==0,"No runtime errors during verification");
 p.testInput=null;rb.linearVelocity=Vector2.zero;rb.position=new Vector2(-26,-5);File.WriteAllLines("Tools/Verification/results.txt",results);Debug.Log(string.Join("\n",results));
 }
}
}
#endif

