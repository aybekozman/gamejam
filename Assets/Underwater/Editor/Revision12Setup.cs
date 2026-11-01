using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using Abyss.Story;
namespace Abyss.EditorTools {
[InitializeOnLoad] public static class Revision12Setup {
 static Revision12Setup(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("R12Verify",false)){SessionState.SetBool("R12Verify",false);Application.runInBackground=true;Camera.main.gameObject.AddComponent<Revision12Verification>();}};EditorApplication.update+=()=>{const string path="Temp/revision12-command.txt";if(!File.Exists(path)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;var cmd=File.ReadAllText(path).Trim();File.Delete(path);if(cmd=="apply")Apply();if(cmd=="verify"){Application.runInBackground=true;SessionState.SetBool("R12Verify",true);EditorApplication.isPlaying=true;}};}
 static Sprite S(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Underwater/Resources/Abyss/"+n+".png");
 static void Apply(){if(EditorApplication.isPlaying)return;var story=Object.FindFirstObjectByType<UnderwaterDayStory>();if(story.gameObject.scene.path!="Assets/Scenes/UnderwaterWorld.unity")return;
 foreach(var path in Directory.GetFiles("Assets/Underwater/Resources/Abyss","*.png").Where(p=>Path.GetFileName(p).StartsWith("waste_")||Path.GetFileName(p).StartsWith("skeleton_")||Path.GetFileName(p)=="surface_vessel.png")){var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.spritePixelsPerUnit=64;imp.SaveAndReimport();}
 story.player.movementActions=story.inputActions;var env=story.GetComponent<WarEnvironment>()??story.gameObject.AddComponent<WarEnvironment>();env.story=story;env.waste=Enumerable.Range(0,6).Select(i=>S("waste_"+i)).ToArray();env.skeletons=Enumerable.Range(0,4).Select(i=>S("skeleton_"+i)).ToArray();env.vessel=S("surface_vessel");env.pixelFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/Underwater/Fonts/PixelifySans.ttf");
 var third=story.thirdChapter;if(!third.messenger){var go=Object.Instantiate(third.guards[0].gameObject,third.npcRoot);go.name="Haberci Köpekbalığı";third.messenger=go.GetComponent<StorySwimmer>();third.messengerRest=new Vector2(227,-23);go.transform.position=third.messengerRest;}
 third.hazards.shotInterval=.455f;third.hazards.tutorialInterval=.7f;third.hazards.salvoPause=.98f;
 EditorSceneManager.MarkSceneDirty(story.gameObject.scene);EditorSceneManager.SaveScene(story.gameObject.scene);File.WriteAllText("Tools/Revision12/setup.txt","Applied sprint, war environment assets, messenger, 30% shorter shot intervals and scene-local pixel font.");}
}}
