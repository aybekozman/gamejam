using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
namespace Abyss.EditorTools {
public class AbyssImporter:AssetPostprocessor {
 void OnPreprocessTexture(){if(!assetPath.StartsWith("Assets/Underwater/Resources/Abyss/"))return;var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=64;t.filterMode=FilterMode.Point;t.textureCompression=TextureImporterCompression.Uncompressed;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.wrapMode=TextureWrapMode.Clamp;if(assetPath.EndsWith("/shore_ground_fill.png"))t.wrapModeU=TextureWrapMode.Mirror;t.maxTextureSize=4096;if(assetPath.Contains("/kelp_")){var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,0);t.SetTextureSettings(settings);}}
}
[InitializeOnLoad]
public static class WorldBuilder {
 const string Scene="Assets/Scenes/UnderwaterWorld.unity";static string Command=>Path.GetFullPath("Temp/abyss-command.txt");
 static WorldBuilder(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlaying && SessionState.GetBool("AbyssVerify",false)){SessionState.SetBool("AbyssVerify",false);Camera.main.gameObject.AddComponent<UnderwaterVerification>();}if(!File.Exists(Command)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;var cmd=File.ReadAllText(Command).Trim();File.Delete(Command);try{if(cmd=="build")Build();if(cmd=="verify"){SessionState.SetBool("AbyssVerify",true);EditorSceneManager.OpenScene(Scene);EditorApplication.isPlaying=true;}if(cmd=="play"){EditorSceneManager.OpenScene(Scene);EditorApplication.isPlaying=true;}if(cmd=="stop")EditorApplication.isPlaying=false;}catch(System.Exception e){Debug.LogException(e);}}
 static Sprite S(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Underwater/Resources/Abyss/"+n+".png");
 static Sprite[] Frames(string n,int row,int count=4)=>Enumerable.Range(0,count).Select(i=>S($"{n}_{row}_{i}")).ToArray();
 static GameObject Art(string name,Sprite sprite,float x,float y,float width,int order,Color? color=null){var go=new GameObject(name);go.transform.position=new Vector3(x,y,0);var s=go.AddComponent<SpriteRenderer>();s.sprite=sprite;s.sortingOrder=order;s.color=color??Color.white;if(sprite)go.transform.localScale=Vector3.one*width/sprite.bounds.size.x;return go;}
 static void Sized(GameObject go,float w,float h){var s=go.GetComponent<SpriteRenderer>();go.transform.localScale=new Vector3(w/s.sprite.bounds.size.x,h/s.sprite.bounds.size.y,1);}
 static void Solid(float x,float y,float w,float h,string name){var g=new GameObject(name);g.transform.position=new Vector3(x,y,0);g.AddComponent<BoxCollider2D>().size=new Vector2(w,h);}
 static void Rock(float x,float y,float w,int type=0,bool solid=true){var g=Art("Reef rock",S($"rock_0_{type}"),x,y,w,0);if(solid)g.AddComponent<PolygonCollider2D>();}

 static System.Collections.Generic.Dictionary<string,Texture2D> pixels=new();
 static void Plant(float x,float y,float w,int kind,float phase,int order=5){float best=10,root=y;
 foreach(var collider in Object.FindObjectsByType<PolygonCollider2D>(FindObjectsSortMode.None)){
  var renderer=collider.GetComponent<SpriteRenderer>();if(!renderer||!renderer.sprite)continue;var sprite=renderer.sprite;var local=renderer.transform.InverseTransformPoint(new Vector3(x,0,0));int px=Mathf.FloorToInt(local.x*sprite.pixelsPerUnit+sprite.pivot.x);var path=AssetDatabase.GetAssetPath(sprite);
  if(!pixels.TryGetValue(path,out var texture)){texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(path));pixels[path]=texture;}
  if(px<0||px>=texture.width)continue;
  for(int py=texture.height-1;py>=0;py--){if(texture.GetPixel(px,py).a<.4f)continue;float top=renderer.transform.TransformPoint(new Vector3(0,(py+.5f-sprite.pivot.y)/sprite.pixelsPerUnit,0)).y;float distance=Mathf.Abs(top-y);if(distance<best){best=distance;root=top;}break;}
 }
 y=root-.08f;var g=Art("Rooted kelp",S($"kelp_{kind}_0"),x,y,w,order);var sr=g.GetComponent<SpriteRenderer>();var height=sr.bounds.size.y; // bottom pivot is imported separately
 var d=g.AddComponent<Drift>();d.plant=true;d.phase=phase;d.speed=.65f+phase*.03f;}
 static void Creature(string n,int row,float x,float y,float w,Vector2 travel,float phase){var g=Art(n,S($"{n}_{row}_0"),x,y,w,8);var d=g.AddComponent<Drift>();d.frames=Frames(n,row);d.phase=phase;d.amplitude=1;d.travel=travel;d.speed=.25f;}
 [MenuItem("Underwater/Build separate exploration scene")]
 public static void Build(){
 if(EditorApplication.isPlaying)return;
 var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(previous.isDirty){Debug.LogError("Save current scene before building UnderwaterWorld.");return;}
 foreach(var path in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Underwater/Resources/Abyss"}).Select(AssetDatabase.GUIDToAssetPath)){
 var ti=(TextureImporter)AssetImporter.GetAtPath(path); ti.SaveAndReimport();
 }
 EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 var camGo=new GameObject("Exploration Camera");camGo.tag="MainCamera";var cam=camGo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=7;cam.backgroundColor=new Color(.015f,.06f,.16f);cam.clearFlags=CameraClearFlags.SolidColor;camGo.transform.position=new Vector3(-23,-6,-10);camGo.AddComponent<AudioListener>();
 // One continuous world-height gradient, mirrored horizontal tiles prevent edge seams.
 for(int i=0;i<8;i++){float x=-56+i*16;var water=Art("World anchored water gradient",S("water"),x,-10,16,-100);Sized(water,16,44);water.GetComponent<SpriteRenderer>().flipX=i%2==1;var sky=Art("Sky",S("sky"),x,22,16,-100);Sized(sky,16,20);sky.GetComponent<SpriteRenderer>().flipX=i%2==1;var surf=Art("Surface",S("surface"),x,12,16,30);Sized(surf,16,.48f);surf.GetComponent<SpriteRenderer>().flipX=i%2==1;surf.AddComponent<SurfaceRipple>().phase=i;}
 // At the lower edge continue with precisely the source bottom palette.
 var tex=new Texture2D(1,1);tex.SetPixel(0,0,new Color(0,30/255f,79/255f));tex.Apply();File.WriteAllBytes("Assets/Underwater/Resources/Abyss/deep.png",tex.EncodeToPNG());AssetDatabase.ImportAsset("Assets/Underwater/Resources/Abyss/deep.png");Sized(Art("Deep continuation",S("deep"),0,-38,120,-99),120,12);
 for(int i=0;i<13;i++){var g=Art("Far rock silhouette",S($"distant_0_{i%3}"),-48+i*8,-16+(i%3)*4,14,-60,new Color(.5f,.8f,1,.35f));g.AddComponent<Parallax>().factor=.12f;}
 for(int i=0;i<8;i++){var g=Art("Middle reef",S($"rock_0_{i%3}"),-46+i*13,-23+(i%2)*10,13,-30,new Color(.3f,.65f,.75f,.5f));g.AddComponent<Parallax>().factor=.06f;}
 for(int i=0;i<8;i++){var g=Art("Submerged sun shaft",S($"ray_0_{i%5}"),-40+i*12,5,8,-40,new Color(.65f,.95f,1,.12f));Sized(g,8,13.5f);}
 // Floor, town terraces, broad central opening, alternating deep passage shelves.
 for(int i=0;i<12;i++){var g=Art("Seabed",S($"ground_0_{i%3}"),-44+i*8,-33,8.2f,1);Sized(g,8.8f,3);g.AddComponent<PolygonCollider2D>();}
 Solid(-47,-8,2,55,"West boundary");Solid(47,-8,2,55,"East boundary");
 Rock(-30,-13,18,2);Rock(-17,-16,13,0);Rock(-40,-20,14,1);Rock(7,-14,13,1);Rock(27,-20,15,2);Rock(8,-29,15,0);Rock(41,-28,13,1);Rock(40,-6,12,0);Rock(-1,-5,6,2);
 float[] hx={-35,-29,-22,-39,-18};float[] hy={-9.7f,-9.7f,-10.8f,-16.3f,-12.5f};for(int i=0;i<hx.Length;i++)Art("Coral village home",S($"house_{i/3}_{i%3}"),hx[i],hy[i],4.4f,3);
 float[,] plants={{-38,-11},{-33,-10},{-25,-11},{-19,-13},{-42,-18},{-12,-32},{-4,-32},{6,-26},{14,-32},{23,-18},{31,-18},{37,-32},{42,-24},{3,-12},{39,-3}};
 for(int i=0;i<plants.GetLength(0);i++){Plant(plants[i,0],plants[i,1],1.2f+i%3*.4f,i%4,i*.7f);if(i%3==0)Plant(plants[i,0]+1,plants[i,1],.8f,(i+1)%4,i,18);}
 Creature("reef",0,-28,-5,1.4f,new Vector2(3,.8f),0);Creature("reef",1,-18,-8,1.3f,new Vector2(2,1),1);Creature("fish",2,-4,3,1,new Vector2(3,.6f),2);
 Creature("animal",0,18,4,2.3f,new Vector2(4,1),0);Creature("animal",1,19,-11,1.1f,new Vector2(.6f,2),1);Creature("animal",2,-7,-22,2.6f,new Vector2(3,.6f),2);Creature("animal",3,-35,-5,1,new Vector2(.3f,1),3);
 Creature("shark",0,28,-27,3.7f,new Vector2(4,1),2);Creature("whale",0,-23,-25,7,new Vector2(5,1),3);
 var fish=new GameObject("Player • orange coral fish");fish.transform.position=new Vector3(-26,-7.5f,0);var rb=fish.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.freezeRotation=true;rb.interpolation=RigidbodyInterpolation2D.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
 var col=fish.AddComponent<CapsuleCollider2D>();col.direction=CapsuleDirection2D.Horizontal;col.size=new Vector2(1.25f,.68f);var mat=new PhysicsMaterial2D("Swim frictionless"){friction=0,bounciness=0};var oldMat=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Underwater/Swim.physicsMaterial2D");if(oldMat)mat=oldMat;else AssetDatabase.CreateAsset(mat,"Assets/Underwater/Swim.physicsMaterial2D");col.sharedMaterial=mat;
 var visual=Art("Fish visual",S("hero_0_0"),0,0,1.8f,15);visual.transform.SetParent(fish.transform,false);var pilot=fish.AddComponent<FishPilot>();pilot.visual=visual.GetComponent<SpriteRenderer>();pilot.swim=Frames("hero",0);pilot.blink=Frames("hero",1);pilot.bubble=S("bubble_0_0");camGo.AddComponent<FollowCamera>().target=fish.transform;
 var shoals=new GameObject("Occasional distant shoals").AddComponent<ShadowPass>();shoals.sprites=Frames("shadow",0);
 foreach(var texture in pixels.Values)Object.DestroyImmediate(texture);pixels.Clear();
 EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/abyss-build-result.txt","Scene built successfully");Debug.Log("UnderwaterWorld ready");
 }
}
}
