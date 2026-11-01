using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace IntegratedCampaign.EditorTools {
[InitializeOnLoad]public static class CampaignSetup {
 static CampaignSetup(){EditorApplication.update+=Poll;}
 static void Poll(){const string p="Temp/campaign-command.txt";if(!File.Exists(p)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;string c=File.ReadAllText(p).Trim();File.Delete(p);try{
 if(c=="apply")Apply();
 if(c=="stop")EditorApplication.isPlaying=false;
 if(c=="openmenu")EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
 if(c=="menu"||c=="ship"){EditorSceneManager.SaveOpenScenes();EditorSceneManager.OpenScene(c=="menu"?"Assets/Scenes/MainMenu.unity":"Assets/Scenes/ShipScene 1.unity");EditorApplication.isPlaying=true;}
 if(c=="test")CampaignVerification.Begin();if(c=="smoke")CampaignVerification.Begin(true);if(c=="campaignsmoke")CampaignVerification.BeginCampaignSmoke();
 if(c=="shot")ScreenCapture.CaptureScreenshot("Tools/ShipRestore/current.png");
 File.WriteAllText("Tools/ShipRestore/status.txt",c+" OK");
 }catch(System.Exception e){File.WriteAllText("Tools/ShipRestore/status.txt",e.ToString());Debug.LogException(e);}}
 static Sprite S(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Campaign/Resources/Campaign/"+n+".png");
 public static void Apply(){if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit play mode first.");EditorSceneManager.SaveOpenScenes();string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/ShipScene 1.unity");var ship=Object.FindFirstObjectByType<ShipController>();var d=ship.GetComponent<ShipCampaignDirector>();if(!d)d=ship.gameObject.AddComponent<ShipCampaignDirector>();d.flagFrames=Enumerable.Range(0,4).Select(i=>S("flag_"+i)).ToArray();
 if(!GameObject.Find("Story coast decoration")){var root=new GameObject("Story coast decoration");Art(root.transform,"Dry river town",S("town"),new Vector2(-110,0),32);Art(root.transform,"Harbor",S("harbor"),new Vector2(-97,0),26);}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
 }
 static void Art(Transform parent,string name,Sprite sprite,Vector2 at,float width){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=at;go.transform.rotation=Quaternion.Euler(0,0,90);var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=-80;if(sprite)go.transform.localScale=Vector3.one*width/sprite.bounds.size.x;}
}
}
