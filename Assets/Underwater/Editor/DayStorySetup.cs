using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Abyss.Story;
using System.Linq;
using System.IO;

namespace Abyss.EditorTools
{
    [InitializeOnLoad]
    public static class DayStorySetup
    {
        static DayStorySetup(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("DayStoryVerify",false)){SessionState.SetBool("DayStoryVerify",false);EditorApplication.isPaused=false;Application.runInBackground=true;Camera.main.gameObject.AddComponent<DayStoryVerification>();}};}
        static void Poll(){if(EditorApplication.isPlaying&&SessionState.GetBool("Revision8Verify",false)){SessionState.SetBool("Revision8Verify",false);Camera.main.gameObject.AddComponent<Revision8Verification>();}if(EditorApplication.isPlaying&&SessionState.GetBool("Revision7Verify",false)){SessionState.SetBool("Revision7Verify",false);Camera.main.gameObject.AddComponent<Revision7Verification>();}const string path="Temp/day-story-command.txt";if(!File.Exists(path)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;var command=File.ReadAllText(path).Trim();File.Delete(path);try{if(command=="verify7"){Application.runInBackground=true;EditorApplication.isPaused=false;SessionState.SetBool("Revision7Verify",true);EditorApplication.isPlaying=true;}if(command=="verify8"){Application.runInBackground=true;EditorApplication.isPaused=false;SessionState.SetBool("Revision8Verify",true);EditorApplication.isPlaying=true;}if(command=="revision8")ApplyRevision8();if(command=="revision7")ApplyRevision7();if(command=="apply")Apply();if(command=="arrange")ArrangeBodies();if(command=="shore-scale")RepairShoreScale();if(command=="verify"){if(SceneManager.GetActiveScene().path!="Assets/Scenes/UnderwaterWorld.unity")throw new System.InvalidOperationException("Story verification is limited to UnderwaterWorld.");SessionState.SetBool("DayStoryVerify",true);EditorApplication.isPlaying=true;}}catch(System.Exception e){Debug.LogException(e);}}
        static void ApplyRevision8()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/UnderwaterWorld.unity")throw new System.InvalidOperationException("Open UnderwaterWorld outside Play Mode.");
            var story=Object.FindFirstObjectByType<UnderwaterDayStory>();story.corpseRiseSpeed=4.2f;story.playerHome=GameObject.Find("House").GetComponent<Collider2D>();
            for(int i=0;i<story.bodies.Length;i++){story.bodies[i].rotation=Quaternion.Euler(0,0,UnderwaterDayStory.CorpseAngle(i));story.bodies[i].GetComponent<SpriteRenderer>().flipY=false;}
            EditorSceneManager.MarkSceneDirty(story.gameObject.scene);EditorSceneManager.SaveScene(story.gameObject.scene);
            Directory.CreateDirectory("Tools/Revision8");File.WriteAllText("Tools/Revision8/setup.txt","Applied 4.2 ascent, diagonal belly-up corpses, House marker only. No scene transition.");
        }
        static void ApplyRevision7()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/UnderwaterWorld.unity")throw new System.InvalidOperationException("Open UnderwaterWorld outside Play Mode.");
            var story=Object.FindFirstObjectByType<UnderwaterDayStory>();
            var house=GameObject.Find("House");if(!house)throw new System.InvalidOperationException("Authored House missing.");
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(house);
            story.corpseRiseSpeed=1.8f;story.mayor.FaceTravelDirection=true;story.mayor.maxPitch=story.player.tilt;story.mayor.pitchResponse=story.player.pitchResponse;
            for(int i=0;i<story.bodies.Length;i++){story.bodies[i].rotation=Quaternion.Euler(0,0,i%2==0?-12:12);story.bodies[i].GetComponent<SpriteRenderer>().flipY=true;}
            EditorSceneManager.MarkSceneDirty(story.gameObject.scene);EditorSceneManager.SaveScene(story.gameObject.scene);
            File.WriteAllText("Tools/Revision7/setup.txt","PASS Authored House preserved; scene transition deferred. Rise speed 1.8, mayor pitch matches player, three belly-up bodies.");
        }
        static void RepairShoreScale()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
            var manifest=JsonUtility.FromJson<SpriteShoreUpgrade.Manifest>(File.ReadAllText("Tools/Revision5/manifest.json"));int changed=0;
            foreach(var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Shore floor sprite"))){var chunk=manifest.chunks.First(c=>c.name==Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(renderer.sprite)));if(Vector3.Distance(renderer.transform.localScale,chunk.scale)>.0001f){renderer.transform.localScale=chunk.scale;changed++;}}
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());File.WriteAllText("Tools/Revision6/shore-scale-result.txt",$"Corrected {changed} generated shore piece scale to match its neighbors and authored floor pixels. Other world transforms untouched.");
        }
        static void ArrangeBodies()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
            var story=Object.FindFirstObjectByType<UnderwaterDayStory>();for(int i=0;i<3;i++)story.bodies[i].position=new Vector3(story.discoveryCenter.x+new[]{-5f,-2.4f,3.8f}[i],story.discoveryCenter.y-(i%2)*.12f,0);
            EditorSceneManager.MarkSceneDirty(story.gameObject.scene);EditorSceneManager.SaveScene(story.gameObject.scene);
        }
        static Sprite S(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Underwater/Resources/Abyss/"+name+".png");
        static StorySwimmer Swimmer(string name,Transform parent,Sprite sprite,float width,Vector2 position)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;var rb=go.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.freezeRotation=true;go.AddComponent<CircleCollider2D>().radius=.4f;
            var visual=new GameObject("Aligned visual");visual.transform.SetParent(go.transform,false);var renderer=visual.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=14;visual.transform.localScale=Vector3.one*width/sprite.bounds.size.x;
            var actor=go.AddComponent<StorySwimmer>();actor.visual=renderer;actor.swimFrames=new[]{sprite};return actor;
        }
        [MenuItem("Underwater/Install first-day discovery story (preserve world)")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/UnderwaterWorld.unity")throw new System.InvalidOperationException("Open UnderwaterWorld outside Play Mode.");
            if(Object.FindFirstObjectByType<UnderwaterDayStory>())return;
            Directory.CreateDirectory("Tools/Revision6");var scene=SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene);File.Copy(scene.path,"Tools/Revision6/user-world-before.unity",true);
            var transforms=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.gameObject.scene==scene).Select(t=>new{t,position=t.position,rotation=t.rotation,scale=t.lossyScale,parent=t.parent}).ToArray();
            var colliders=Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene).Select(c=>new{c,json=EditorJsonUtility.ToJson(c)}).ToArray();
            var player=Object.FindFirstObjectByType<FishPilot>();var homes=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Coral village home")).ToArray();
            Physics2D.SyncTransforms();float corridor=homes.Max(r=>r.bounds.max.y)+2.3f;corridor=Mathf.Min(corridor,-12);
            Vector2 passStart=new(player.transform.position.x-4,corridor),passEnd=new(player.transform.position.x+3,corridor),discovery=new(player.transform.position.x+3,player.surfaceY-.95f);
            if(!WaterRoute.Clear(passStart)||!WaterRoute.Segment(passStart,passEnd)||!WaterRoute.Segment(passEnd,discovery))throw new System.InvalidOperationException("Story corridor is blocked; select a clear route before installing.");
            Vector2 mayorPosition=new(player.transform.position.x+5,corridor-1);
            for(int i=0;i<20&&!WaterRoute.Clear(mayorPosition);i++)mayorPosition.y+=.4f;
            if(!WaterRoute.Clear(mayorPosition)||WaterRoute.Find(mayorPosition,passEnd).Count==0)throw new System.InvalidOperationException("Mayor starting lane is blocked.");
            var root=new GameObject("Underwater day • dead fish discovery");var ui=root.AddComponent<UnderwaterStoryUI>();ui.leftPanel=S("dialogue_left");ui.rightPanel=S("dialogue_right");ui.playerPortrait=player.swim[0];ui.mayorPortrait=S("reef_0_0");
            var story=root.AddComponent<UnderwaterDayStory>();story.player=player;story.ui=ui;story.inputActions=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");story.openEyes=S("story_fish_open");story.closedEyes=S("story_fish_closed");story.passingStart=passStart;story.passingEnd=passEnd;story.discoveryCenter=discovery;
            story.eventFish=Swimmer("Story pink fish • one-time event",root.transform,story.openEyes,1.8f,passStart);story.eventFish.gameObject.SetActive(false);
            story.mayor=Swimmer("Başkan • mor resif balığı",root.transform,S("reef_0_0"),1.8f,mayorPosition);story.mayor.swimSpeed=4.8f;
            story.escort=root.AddComponent<MayorEscort>();story.escort.player=player.transform;story.escort.mayor=story.mayor;
            story.bodies=new Transform[3];for(int i=0;i<3;i++){var go=new GameObject("Inspectable body "+(i+1));go.transform.SetParent(root.transform,false);go.transform.position=new Vector3(discovery.x+new[]{-5f,-2.4f,3.8f}[i],discovery.y-(i%2)*.12f,0);go.transform.rotation=Quaternion.Euler(0,0,180);var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=S("story_dead_"+i);renderer.sortingOrder=14;go.transform.localScale=Vector3.one*(1.4f+i*.12f)/renderer.sprite.bounds.size.x;story.bodies[i]=go.transform;go.SetActive(false);}
            var habitat=Object.FindFirstObjectByType<OceanHabitat>();habitat.species=habitat.species.Where(s=>!s.frames.Any(f=>f&&f.name.StartsWith("reef_"))).ToArray();EditorUtility.SetDirty(habitat);
            foreach(var saved in transforms)if(saved.t.position!=saved.position||saved.t.rotation!=saved.rotation||saved.t.lossyScale!=saved.scale||saved.t.parent!=saved.parent)throw new System.InvalidOperationException("Authored transform changed: "+saved.t.name);
            foreach(var saved in colliders)if(EditorJsonUtility.ToJson(saved.c)!=saved.json)throw new System.InvalidOperationException("Authored collider changed: "+saved.c.name);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Tools/Revision6/setup-result.txt",$"PASS {transforms.Length} existing transforms and {colliders.Length} collider definitions preserved.\nPass lane={passStart} to {passEnd}; surface={discovery}; mayor={mayorPosition}.\nMayor uses reef_0; reef_1 and reef_2 reserved for future science team. Player/Interact asset reused; no ship assets modified.");
        }
    }
}

