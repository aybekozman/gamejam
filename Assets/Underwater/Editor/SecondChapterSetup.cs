using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Abyss.Story;
using Object=UnityEngine.Object;

namespace Abyss.EditorTools
{
    [InitializeOnLoad]
    public static class SecondChapterSetup
    {
        const string Command="Temp/second-chapter-command.txt";
        static SecondChapterSetup()
        {
            EditorApplication.update+=Poll;
            EditorApplication.playModeStateChanged+=state=>{
                if(state!=PlayModeStateChange.EnteredPlayMode)return;
                if(SessionState.GetBool("SecondChapterVerify",false)){SessionState.SetBool("SecondChapterVerify",false);Application.runInBackground=true;EditorApplication.isPaused=false;Camera.main.gameObject.AddComponent<SecondChapterVerification>();}
                if(SessionState.GetBool("SecondChapterStart",false)){SessionState.SetBool("SecondChapterStart",false);EditorApplication.delayCall+=()=>Object.FindFirstObjectByType<UnderwaterSecondChapter>().BeginSecondChapter();}
            };
        }
        static void Poll()
        {
            if(!File.Exists(Command)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            var command=File.ReadAllText(Command).Trim();File.Delete(Command);
            try {
                if(SceneManager.GetActiveScene().path!="Assets/Scenes/UnderwaterWorld.unity")throw new InvalidOperationException("UnderwaterWorld must be open.");
                if(command=="apply")Apply();
                if(command=="verify"||command=="start"){Application.runInBackground=true;SessionState.SetBool(command=="verify"?"SecondChapterVerify":"SecondChapterStart",true);EditorApplication.isPlaying=true;}
            }catch(Exception e){Debug.LogException(e);}
        }
        [MenuItem("Underwater/Start second chapter in Play Mode")]
        static void StartSecond(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<UnderwaterSecondChapter>().BeginSecondChapter();else{SessionState.SetBool("SecondChapterStart",true);Application.runInBackground=true;EditorApplication.isPlaying=true;}}
        static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Underwater/Resources/Abyss/"+name+".png");
        static Vector2 Safe(Vector2 preferred)
        {
            for(int y=0;y<12;y++)for(int x=0;x<9;x++){var candidate=preferred+new Vector2((x%2==0?1:-1)*((x+1)/2)*.6f,y*.5f);if(WaterRoute.Clear(candidate,.6f))return candidate;}
            throw new InvalidOperationException("No clear story position near "+preferred);
        }
        static StorySwimmer Actor(string name,string spritePrefix,Transform parent,Vector2 position,float width,int phase)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;go.AddComponent<Rigidbody2D>();go.AddComponent<CircleCollider2D>();
            var visual=new GameObject("Visual");visual.transform.SetParent(go.transform,false);var renderer=visual.AddComponent<SpriteRenderer>();renderer.sprite=Sprite(spritePrefix+"0");renderer.sortingOrder=14;visual.transform.localScale=Vector3.one*width/renderer.sprite.bounds.size.x;
            var actor=go.AddComponent<StorySwimmer>();actor.visual=renderer;actor.radius=.32f;actor.swimSpeed=2.5f+phase*.12f;actor.FaceTravelDirection=true;
            actor.swimFrames=Enumerable.Range(0,4).Select(i=>Sprite(spritePrefix+i)).Where(s=>s).ToArray();
            var idle=go.AddComponent<MeetingFishIdle>();idle.actor=actor;idle.restPosition=position;idle.phase=phase*1.3f;return actor;
        }
        [MenuItem("Underwater/Install second chapter (preserve world)")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/UnderwaterWorld.unity")throw new InvalidOperationException("Open UnderwaterWorld outside Play Mode.");
            var story=Object.FindFirstObjectByType<UnderwaterDayStory>();if(!story)throw new InvalidOperationException("First chapter is required.");
            var existing=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==story.gameObject.scene).Select(t=>new{t,p=t.localPosition,r=t.localRotation,s=t.localScale,parent=t.parent}).ToArray();
            var colliders=Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>new{c,json=EditorJsonUtility.ToJson(c)}).ToArray();
            var chapter=story.GetComponent<UnderwaterSecondChapter>();
            if(!chapter)
            {
                chapter=story.gameObject.AddComponent<UnderwaterSecondChapter>();chapter.story=story;story.secondChapter=chapter;
                var homes=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Coral village home")||r.name=="House").ToArray();
                var hall=homes.First(r=>r.sprite.name=="house_1_1");
                chapter.meetingPoint=Safe(new Vector2(hall.bounds.center.x,hall.bounds.max.y+2.8f));
                chapter.homeArrival=Safe(new Vector2(story.playerHome.bounds.center.x,story.playerHome.bounds.max.y+1.1f));
                chapter.departurePoint=Safe(new Vector2(homes.Max(r=>r.bounds.max.x)+16,chapter.meetingPoint.y));
                if(WaterRoute.Find(chapter.homeArrival,chapter.meetingPoint).Count==0||WaterRoute.Find(chapter.meetingPoint,chapter.departurePoint).Count==0)throw new InvalidOperationException("Story route blocked.");
                var root=new GameObject("Second chapter • announcement participants");root.transform.SetParent(story.transform,false);chapter.participantRoot=root.transform;
                chapter.scientists=new[]{Actor("Bilim Uzmanı 1","reef_1_",root.transform,Safe(chapter.meetingPoint+new Vector2(-2.2f,.5f)),1.45f,0),Actor("Bilim Uzmanı 2","reef_2_",root.transform,Safe(chapter.meetingPoint+new Vector2(2.2f,.5f)),1.45f,1)};
                chapter.scientistPortraits=new[]{Sprite("reef_1_0"),Sprite("reef_2_0")};chapter.townPortrait=Sprite("fish_1_0");
                chapter.townsfolk=Enumerable.Range(0,4).Select(i=>Actor("Toplantı kasabalısı "+(i+1),i%2==0?"fish_1_":"fish_3_",root.transform,Safe(chapter.meetingPoint+new Vector2(new[]{-4.7f,-2.8f,2.8f,4.7f}[i],-1.8f)),1.15f,i+2)).ToArray();
                root.SetActive(false);
            }
            foreach(var saved in existing)
            {
                bool body=story.bodies.Contains(saved.t);if(body)continue;
                if(saved.t.localPosition!=saved.p||saved.t.localRotation!=saved.r||saved.t.localScale!=saved.s||saved.t.parent!=saved.parent)throw new InvalidOperationException("Authored transform changed: "+saved.t.name);
            }
            for(int i=0;i<story.bodies.Length;i++){story.bodies[i].rotation=Quaternion.Euler(0,0,UnderwaterDayStory.CorpseAngle(i));story.bodies[i].GetComponent<SpriteRenderer>().flipY=false;}
            foreach(var saved in colliders)if(EditorJsonUtility.ToJson(saved.c)!=saved.json)throw new InvalidOperationException("Authored collider changed: "+saved.c.name);
            EditorSceneManager.MarkSceneDirty(story.gameObject.scene);EditorSceneManager.SaveScene(story.gameObject.scene);
            Directory.CreateDirectory("Tools/Revision9");File.WriteAllText("Tools/Revision9/setup.txt",$"PASS {existing.Length} authored transforms and {colliders.Length} colliders preserved (requested corpse angle correction excepted).\nHome={chapter.homeArrival}; hall={chapter.meetingPoint}; exit={chapter.departurePoint}.\nExisting mayor reused; 2 scientists + 4 citizens; ship skipped without scene loading.");
        }
    }
}
