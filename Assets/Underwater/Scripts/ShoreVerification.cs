#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Abyss
{
    public class ShoreVerification:MonoBehaviour
    {
        readonly List<string> results=new();int errors;
        void OnEnable()=>Application.logMessageReceived+=Log;
        void OnDisable()=>Application.logMessageReceived-=Log;
        void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception)errors++;}
        void Check(bool condition,string label)=>results.Add((condition?"PASS ":"FAIL ")+label);
        void Capture(string name,Vector3? position=null)
        {
            var camera=Camera.main;var savedPosition=camera.transform.position;if(position.HasValue)camera.transform.position=position.Value;
            var rt=RenderTexture.GetTemporary(1280,720,24);var old=camera.targetTexture;var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes("Tools/Revision4/"+name+".png",texture.EncodeToPNG());Destroy(texture);camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);camera.transform.position=savedPosition;
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;Time.timeScale=1;Directory.CreateDirectory("Tools/Revision4");yield return new WaitForSeconds(2);
            var shore=FindFirstObjectByType<ShorelineProfile>();var pilot=FindFirstObjectByType<FishPilot>();var body=pilot.GetComponent<Rigidbody2D>();var start=body.position;
            var collider=shore.GetComponentsInChildren<PolygonCollider2D>().Single(c=>c.name.StartsWith("Continuous shore"));
            Check(shore&&collider,"Continuous shore profile and solid slope present");
            var spriteGroup=shore.transform.Find("Pixel matched shoreline sprites");var floorSprites=spriteGroup.GetComponentsInChildren<SpriteRenderer>();
            Check(shore.GetComponentsInChildren<MeshRenderer>().Length==0,"Shallow shore contains no MeshRenderer");
            Check(floorSprites.Length>=10&&floorSprites.All(r=>r.sprite.texture.filterMode==FilterMode.Point),"Shore is built from crisp point-filtered SpriteRenderer pieces");
            var authoredFloor=FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).First(r=>r.name=="Seabed");
            Check(floorSprites.All(r=>Vector2.Distance(r.transform.lossyScale,authoredFloor.transform.lossyScale)<.01f&&r.sprite.pixelsPerUnit==authoredFloor.sprite.pixelsPerUnit),"Shore pixels use the same world scale as the existing floor");
            Check(shore.bed.Zip(shore.bed.Skip(1),(a,b)=>b.x>a.x&&b.y<=a.y).All(v=>v),"Depth decreases monotonically toward the left coast");
            Check(Mathf.Abs(shore.BedHeight(shore.WaterlineX)-shore.surfaceY)<.01f,"Natural waterline meets the rising seabed");
            Check(GameObject.Find("West boundary").transform.position.x<-330&&pilot.swimMin.x<-330,"Old west wall removed from the village approach");
            bool matched=true;foreach(float x in new[]{-239f,-260f,-280f,-300f}){var hits=Physics2D.RaycastAll(new Vector2(x,11.8f),Vector2.down,60);var hit=System.Array.Find(hits,h=>h.collider==collider);matched&=hit.collider&&Mathf.Abs(hit.point.y-shore.BedHeight(x))<.05f;}
            Check(matched,"Slope collider matches the rendered shoreline profile");
            var rocks=FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Far rock silhouette")||r.name.StartsWith("Middle reef")).ToArray();
            Check(rocks.Length>=100&&rocks.All(r=>r.transform.parent.GetComponent<Parallax>()&&r.transform.parent.GetComponent<Parallax>().enabled&&!r.GetComponent<Parallax>().enabled),"All background rocks share parallax with their supporting seabed");
            var positions=rocks.Select(r=>r.transform.localPosition).ToArray();
            Capture("village-grounding");
            pilot.testInput=Vector2.zero;body.position=new Vector2(-235,-27);body.linearVelocity=Vector2.zero;yield return new WaitForSeconds(1);Capture("floor-join");
            pilot.testInput=Vector2.left;body.position=new Vector2(-237,-26);body.linearVelocity=Vector2.zero;yield return new WaitForSeconds(4);
            Check(body.position.x<-249&&body.position.y>-26.1f,"Fish swims beyond the former wall and follows the gradual rising ground");
            pilot.testInput=Vector2.zero;body.linearVelocity=Vector2.zero;body.position=new Vector2(-274,-6);yield return new WaitForSeconds(1);Capture("gradual-slope");
            body.position=new Vector2(-299,10.5f);yield return new WaitForSeconds(1);Capture("shore-pier",new Vector3(-304,9,-10));
            var pier=GameObject.Find("Western shore pier");var pierColliders=GameObject.Find("Pier deck and posts").GetComponents<BoxCollider2D>();
            Check(pier&&pier.GetComponent<SpriteRenderer>().bounds.max.y>12.35f&&pierColliders.Length>=3,"Pier deck above water, with underwater support posts");
            Check(pierColliders.Skip(1).All(c=>Mathf.Abs(c.bounds.min.y-shore.BedHeight(c.bounds.center.x))<.02f),"Pier posts reach the sloping seabed");
            body.position=new Vector2(-304,11);body.linearVelocity=Vector2.zero;pilot.testInput=Vector2.left;yield return new WaitForSeconds(3);
            Check(body.position.x>-310&&body.position.x<-304&&body.position.y<11.85f,"Fish stops naturally where shallow water closes at shore");
            Check(rocks.Select((r,i)=>Vector3.Distance(r.transform.localPosition,positions[i])<.001f).All(v=>v),"Background rock feet stay attached during camera movement");
            pilot.testInput=Vector2.zero;body.position=new Vector2(20,-23);body.linearVelocity=Vector2.zero;yield return new WaitForSeconds(1);Capture("deep-grounding");
            pilot.testInput=null;body.position=start;body.linearVelocity=Vector2.zero;Check(errors==0,"No runtime errors during shore verification");File.WriteAllLines("Tools/Revision4/playmode-results.txt",results);Debug.Log(string.Join("\n",results));
        }
    }
}
#endif
