using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Abyss.EditorTools
{
    [InitializeOnLoad]
    public static class SpriteShoreUpgrade
    {
        [System.Serializable] public class Configuration { public Vector2[] bed;public float scaleX,scaleY,pixelsPerUnit,referenceLeft,joinHeight;public string source; }
        [System.Serializable] public class Chunk { public string name;public Vector3 position,scale;public int sortingOrder; }
        [System.Serializable] public class Manifest { public Chunk[] chunks;public Vector2[] bed; }
        static SpriteShoreUpgrade(){EditorApplication.update+=Poll;}
        static void Poll(){const string commandPath="Temp/sprite-shore-command.txt";if(!File.Exists(commandPath)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;var command=File.ReadAllText(commandPath).Trim();File.Delete(commandPath);try{if(command=="export")Export();if(command=="apply")Apply();if(command=="collider")AlignCollider();}catch(System.Exception error){Debug.LogException(error);}}
        static void Validate(){if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/UnderwaterWorld.unity")throw new System.InvalidOperationException("Open UnderwaterWorld outside Play Mode.");}
        public static void Export()
        {
            Validate();Directory.CreateDirectory("Tools/Revision5");var scene=SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene);if(!File.Exists("Tools/Revision5/user-layout-before.unity"))File.Copy(scene.path,"Tools/Revision5/user-layout-before.unity");
            var shore=Object.FindFirstObjectByType<ShorelineProfile>();float join=shore.bed[^1].x;
            var grounds=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Seabed"||r.name.StartsWith("Seabed • extended")).ToArray();
            var template=grounds.OrderBy(r=>Mathf.Abs(r.bounds.center.x-(join-4))).First();
            float surface=float.NegativeInfinity;
            foreach(var renderer in grounds.Where(r=>join>=r.bounds.min.x&&join<=r.bounds.max.x))
            {
                var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(renderer.sprite)));var local=renderer.transform.InverseTransformPoint(new Vector3(join,0,0));int x=Mathf.Clamp(Mathf.FloorToInt(local.x*renderer.sprite.pixelsPerUnit+renderer.sprite.pivot.x),0,image.width-1);if(renderer.flipX)x=image.width-1-x;
                for(int y=image.height-1;y>=0;y--)if(image.GetPixel(x,y).a>.5f){surface=Mathf.Max(surface,renderer.transform.TransformPoint(new Vector3(0,(y+.5f-renderer.sprite.pivot.y)/renderer.sprite.pixelsPerUnit,0)).y);break;}
                Object.DestroyImmediate(image);
            }
            if(float.IsNegativeInfinity(surface))throw new System.InvalidOperationException("No visible authored floor at the shore join.");
            var bed=shore.bed.ToArray();bed[^1].y=surface;bed[^2].y=Mathf.Min(bed[^2].y,surface+1.2f);
            var configuration=new Configuration{bed=bed,scaleX=template.transform.lossyScale.x,scaleY=template.transform.lossyScale.y,pixelsPerUnit=template.sprite.pixelsPerUnit,referenceLeft=template.bounds.min.x,source=AssetDatabase.GetAssetPath(template.sprite),joinHeight=surface};
            File.WriteAllText("Tools/Revision5/config.json",JsonUtility.ToJson(configuration,true));
        }
        static void AlignCollider()
        {
            Validate();var shore=Object.FindFirstObjectByType<ShorelineProfile>();var terrain=shore.GetComponentsInChildren<PolygonCollider2D>().Single(c=>c.name.StartsWith("Continuous shore"));
            var path=shore.bed.Concat(new[]{new Vector2(shore.bed[^1].x,-46),new Vector2(shore.bed[0].x,-46)}).Select(p=>(Vector2)terrain.transform.InverseTransformPoint(shore.transform.TransformPoint(p))).ToArray();
            terrain.SetPath(0,path);EditorUtility.SetDirty(terrain);EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);EditorSceneManager.SaveScene(terrain.gameObject.scene);
        }

        [MenuItem("Underwater/Apply pixel-matched sprite shore")]
        public static void Apply()
        {
            Validate();var scene=SceneManager.GetActiveScene();var shore=Object.FindFirstObjectByType<ShorelineProfile>();var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText("Tools/Revision5/manifest.json"));
            var terrain=shore.GetComponentsInChildren<PolygonCollider2D>().Single(c=>c.name.StartsWith("Continuous shore"));
            var transforms=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.gameObject.scene==scene).Select(t=>new{transform=t,position=t.position,rotation=t.rotation,scale=t.lossyScale,parent=t.parent}).ToArray();
            var colliders=Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene&&c!=terrain).Select(c=>new{collider=c,json=EditorJsonUtility.ToJson(c)}).ToArray();
            var existing=shore.transform.Find("Pixel matched shoreline sprites");if(existing)throw new System.InvalidOperationException("Sprite shore already applied; existing edits preserved.");
            var group=new GameObject("Pixel matched shoreline sprites");group.transform.SetParent(shore.transform,false);
            foreach(var chunk in manifest.chunks)
            {
                var path="Assets/Underwater/Resources/Abyss/"+chunk.name+".png";AssetDatabase.ImportAsset(path);var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!sprite)throw new System.InvalidOperationException("Missing shoreline sprite: "+path);
                var go=new GameObject("Shore floor sprite • "+chunk.name);go.transform.SetParent(group.transform,false);go.transform.position=chunk.position;go.transform.localScale=chunk.scale;var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=chunk.sortingOrder;
            }
            var meshRenderer=terrain.GetComponent<MeshRenderer>();if(meshRenderer)Object.DestroyImmediate(meshRenderer);
            var meshFilter=terrain.GetComponent<MeshFilter>();if(meshFilter)Object.DestroyImmediate(meshFilter);
            shore.bed=manifest.bed;terrain.SetPath(0,shore.bed.Concat(new[]{new Vector2(shore.bed[^1].x,-46),new Vector2(shore.bed[0].x,-46)}).Select(p=>(Vector2)terrain.transform.InverseTransformPoint(shore.transform.TransformPoint(p))).ToArray());EditorUtility.SetDirty(shore);EditorUtility.SetDirty(terrain);
            foreach(var saved in transforms)if(saved.transform.position!=saved.position||saved.transform.rotation!=saved.rotation||saved.transform.lossyScale!=saved.scale||saved.transform.parent!=saved.parent)throw new System.InvalidOperationException("Existing transform changed: "+saved.transform.name);
            foreach(var saved in colliders)if(EditorJsonUtility.ToJson(saved.collider)!=saved.json)throw new System.InvalidOperationException("Existing collider changed: "+saved.collider.name);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Tools/Revision5/apply-result.txt",$"PASS {transforms.Length} existing transforms and {colliders.Length} other colliders preserved.\nRemoved shoreline MeshRenderer/MeshFilter; installed {manifest.chunks.Length} SpriteRenderers at authored floor pixel scale.\nOnly generated shore collider end height aligned to the visible authored seabed.");
        }
    }
}
