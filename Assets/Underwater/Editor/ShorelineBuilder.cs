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
    public static class ShorelineBuilder
    {
        const string RootName="Shore and grounded seabed";
        static ShorelineBuilder(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("ShoreVerify",false)){SessionState.SetBool("ShoreVerify",false);EditorApplication.isPaused=false;Application.runInBackground=true;Camera.main.gameObject.AddComponent<ShoreVerification>();}};}
        static void Poll(){const string path="Temp/shore-command.txt";if(!File.Exists(path)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;var command=File.ReadAllText(path).Trim();File.Delete(path);try{if(command=="apply")Apply();if(command=="polish")Polish();if(command=="verify"){SessionState.SetBool("ShoreVerify",true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}}catch(System.Exception e){Debug.LogException(e);}}
        static Sprite S(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Underwater/Resources/Abyss/"+name+".png");
        static GameObject Art(string name,Sprite sprite,Transform parent,Vector3 position,float width,int order,Color tint){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=order;renderer.color=tint;go.transform.localScale=Vector3.one*width/sprite.bounds.size.x;return go;}

        [MenuItem("Underwater/Add gradual western shore and grounded seabed")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
            var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/UnderwaterWorld.unity")throw new System.InvalidOperationException("Open UnderwaterWorld first.");
            if(GameObject.Find(RootName)){File.WriteAllText("Tools/Revision4/build-result.txt","Shore already present; existing work retained.");return;}
            Directory.CreateDirectory("Tools/Revision4");EditorSceneManager.SaveScene(scene);File.Copy(scene.path,"Tools/Revision4/user-layout-before.unity",true);
            var all=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.gameObject.scene==scene).ToArray();
            bool Background(Transform t)=>t.name.StartsWith("Far rock silhouette")||t.name.StartsWith("Middle reef");
            var background=all.Where(Background).ToArray();
            var preserved=all.Where(t=>!Background(t)&&t.name!="West boundary").Select(t=>new{transform=t,position=t.position,rotation=t.rotation,scale=t.lossyScale,parent=t.parent}).ToArray();
            var colliders=Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene).Select(c=>new{collider=c,json=EditorJsonUtility.ToJson(c)}).ToArray();
            if(!File.Exists("Tools/Revision5/manifest.json"))throw new System.InvalidOperationException("Prepare the pixel-matched shore sprites first.");
            var root=new GameObject(RootName);var profile=root.AddComponent<ShorelineProfile>();
            var panorama=S("seabed_panorama");if(!panorama||!S("shore_pier"))throw new System.InvalidOperationException("Processed shore sprites missing.");
            var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes("Assets/Underwater/Resources/Abyss/seabed_panorama.png"));
            int anchored=0;
            foreach(bool far in new[]{true,false})
            {
                var layer=new GameObject(far?"Far grounded seabed":"Middle grounded seabed");layer.transform.SetParent(root.transform,false);layer.AddComponent<Parallax>().factor=far?.12f:.06f;
                float tileWidth=far?56:48;float bottom=far?-32:-34;float height=tileWidth*panorama.bounds.size.y/panorama.bounds.size.x;
                var tiles=new List<SpriteRenderer>();
                for(int i=-8;i<=8;i++)
                {
                    var tile=Art("Seabed panorama • mirrored tile",panorama,layer.transform,new Vector3(i*tileWidth,bottom+height*.5f,0),tileWidth,far?-65:-33,far?new Color(.5f,.75f,.85f,.6f):new Color(.65f,.85f,.9f,.8f));
                    var renderer=tile.GetComponent<SpriteRenderer>();renderer.flipX=(i&1)!=0;tiles.Add(renderer);
                }
                foreach(var rock in background.Where(t=>far?t.name.StartsWith("Far rock silhouette"):t.name.StartsWith("Middle reef")))
                {
                    var renderer=rock.GetComponent<SpriteRenderer>();if(!renderer)continue;
                    var tile=tiles.OrderBy(t=>Mathf.Abs(t.bounds.center.x-renderer.bounds.center.x)).First();
                    float u=Mathf.InverseLerp(tile.bounds.min.x,tile.bounds.max.x,renderer.bounds.center.x);if(tile.flipX)u=1-u;int px=Mathf.Clamp(Mathf.RoundToInt(u*(image.width-1)),0,image.width-1);int py=image.height-1;while(py>0&&image.GetPixel(px,py).a<.5f)py--;
                    float top=tile.bounds.min.y+(py+.5f)/image.height*tile.bounds.size.y;
                    rock.SetParent(layer.transform,true);var position=rock.position;position.y+=top-.7f-renderer.bounds.min.y;rock.position=position;
                    var individual=rock.GetComponent<Parallax>();if(individual)individual.enabled=false;anchored++;
                }
            }
            Object.DestroyImmediate(image);
            BuildShoreSprites(root.transform,profile);
            var roots=scene.GetRootGameObjects();
            foreach(string name in new[]{"World anchored water gradient","Sky"})
            {
                var template=roots.First(g=>g.name==name);
                for(int x=-264;x>=-344;x-=16){var clone=Object.Instantiate(template,root.transform);clone.name=name+" • shore";clone.transform.position=new Vector3(x,template.transform.position.y,0);clone.GetComponent<SpriteRenderer>().flipX=((((Mathf.Abs(x)-8)/16&1)==1)!=(x<0));}
            }
            var surfaceTemplate=roots.First(g=>g.name=="Surface");
            for(int x=-264;x>=-296;x-=16){var clone=Object.Instantiate(surfaceTemplate,root.transform);clone.name="Surface • shore";clone.transform.position=new Vector3(x,12,0);clone.GetComponent<SpriteRenderer>().flipX=((((Mathf.Abs(x)-8)/16&1)==1)!=(x<0));}
            float endWidth=-304-profile.WaterlineX;
            var end=Art("Surface meets the beach",S("shore_surface_end"),root.transform,new Vector3(profile.WaterlineX+endWidth*.5f,12,0),endWidth,30,Color.white);var endRenderer=end.GetComponent<SpriteRenderer>();end.transform.localScale=new Vector3(endWidth/endRenderer.sprite.bounds.size.x,.48f/endRenderer.sprite.bounds.size.y,1);end.AddComponent<SurfaceRipple>();
            BuildPier(root.transform,profile);
            var pilot=Object.FindFirstObjectByType<FishPilot>();pilot.swimMin.x=-335.3f;EditorUtility.SetDirty(pilot);
            var follow=Object.FindFirstObjectByType<FollowCamera>();follow.worldMin.x=-336;EditorUtility.SetDirty(follow);
            var habitat=Object.FindFirstObjectByType<OceanHabitat>();habitat.worldMin.x=-336;EditorUtility.SetDirty(habitat);
            var west=all.First(t=>t.name=="West boundary");west.position=new Vector3(-337,west.position.y,west.position.z);
            foreach(var saved in preserved)if(saved.transform.position!=saved.position||saved.transform.rotation!=saved.rotation||saved.transform.lossyScale!=saved.scale||saved.transform.parent!=saved.parent)throw new System.InvalidOperationException("Authored layout changed: "+saved.transform.name);
            foreach(var saved in colliders)if(EditorJsonUtility.ToJson(saved.collider)!=saved.json)throw new System.InvalidOperationException("Authored collider changed: "+saved.collider.name);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Tools/Revision4/build-result.txt",$"PASS {preserved.Length} authored transforms and all {colliders.Length} collider definitions preserved.\n{anchored} background rocks grounded with their shared parallax seabed.\nShore rises continuously from X=-232 to waterline X={profile.WaterlineX}; pier spans X=-313..-295.\nOnly background rock grounding and the far-left boundary were changed in the existing layout.");
            Debug.Log("Gradual shore, pier and grounded background seabed ready.");
        }

        static void Polish()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Underwater/Resources/Abyss/shore_ground_fill.png");
            importer.wrapModeU=TextureWrapMode.Mirror;importer.SaveAndReimport();
            var root=GameObject.Find(RootName);
            foreach(var transform in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Shore rock edge").ToArray())Object.DestroyImmediate(transform.gameObject);
            EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
            File.WriteAllText("Tools/Revision4/polish-result.txt","Continuous shore texture reimported with mirrored horizontal sampling; discontinuous generated caps removed. Authored map untouched.");
        }

        static void BuildShoreSprites(Transform parent,ShorelineProfile profile)
        {
            var manifest=JsonUtility.FromJson<SpriteShoreUpgrade.Manifest>(File.ReadAllText("Tools/Revision5/manifest.json"));
            profile.bed=manifest.bed;
            var group=new GameObject("Pixel matched shoreline sprites");group.transform.SetParent(parent,false);
            foreach(var chunk in manifest.chunks){var go=new GameObject("Shore floor sprite • "+chunk.name);go.transform.SetParent(group.transform,false);go.transform.position=chunk.position;go.transform.localScale=chunk.scale;var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=S(chunk.name);renderer.sortingOrder=chunk.sortingOrder;}
            var collision=new GameObject("Continuous shore collider");collision.transform.SetParent(parent,false);var collider=collision.AddComponent<PolygonCollider2D>();collider.SetPath(0,profile.bed.Concat(new[]{new Vector2(profile.bed[^1].x,-46),new Vector2(profile.bed[0].x,-46)}).ToArray());
        }

        static void BuildPier(Transform parent,ShorelineProfile profile)
        {
            const float width=18,deck=12.35f,deckFromBottom=.8902148f;var sprite=S("shore_pier");float height=width*sprite.bounds.size.y/sprite.bounds.size.x;float bottom=deck-height*deckFromBottom;
            var pier=Art("Western shore pier",sprite,parent,new Vector3(-304,bottom+height*.5f,0),width,3,Color.white);
            var collision=new GameObject("Pier deck and posts");collision.transform.SetParent(parent,false);collision.transform.position=new Vector3(-304,0,0);
            var deckCollider=collision.AddComponent<BoxCollider2D>();deckCollider.size=new Vector2(17.7f,.3f);deckCollider.offset=new Vector2(0,12.2f);
            foreach(float pixel in new[]{46f,276f,487f,714f})
            {
                float x=-313+pixel/759*width;float floor=profile.BedHeight(x);float h=12.1f-floor;if(h<.1f)continue;
                var post=collision.AddComponent<BoxCollider2D>();post.size=new Vector2(.65f,h);post.offset=new Vector2(x+304,floor+h*.5f);
            }
        }
    }
}
