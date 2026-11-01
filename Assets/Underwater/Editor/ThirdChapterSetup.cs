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
    public static class ThirdChapterSetup
    {
        const string Command="Temp/third-chapter-command.txt";
        static ThirdChapterSetup(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=state=>{
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            if(SessionState.GetBool("ThirdPolishVerify",false)){SessionState.SetBool("ThirdPolishVerify",false);Application.runInBackground=true;EditorApplication.isPaused=false;Camera.main.gameObject.AddComponent<ThirdChapterPolishVerification>();}
            if(SessionState.GetBool("ThirdChapterVerify",false)){SessionState.SetBool("ThirdChapterVerify",false);Application.runInBackground=true;EditorApplication.isPaused=false;Camera.main.gameObject.AddComponent<ThirdChapterVerification>();}
            if(SessionState.GetBool("ThirdChapterStart",false)){SessionState.SetBool("ThirdChapterStart",false);EditorApplication.delayCall+=()=>Object.FindFirstObjectByType<UnderwaterThirdChapter>().BeginThirdChapter();}
        };}
        static void Poll()
        {
            if(!File.Exists(Command)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            var command=File.ReadAllText(Command).Trim();File.Delete(Command);
            try{if(SceneManager.GetActiveScene().path!="Assets/Scenes/UnderwaterWorld.unity")throw new InvalidOperationException("Open UnderwaterWorld first.");if(command=="density")ApplyDensity();if(command=="verify-polish"){Application.runInBackground=true;SessionState.SetBool("ThirdPolishVerify",true);EditorApplication.isPlaying=true;}if(command=="floor-route")FloorRoute(Object.FindFirstObjectByType<UnderwaterThirdChapter>());if(command=="apply")Apply();if(command=="start"||command=="verify"){Application.runInBackground=true;SessionState.SetBool(command=="verify"?"ThirdChapterVerify":"ThirdChapterStart",true);EditorApplication.isPlaying=true;}}catch(Exception e){Debug.LogException(e);}
        }
        [MenuItem("Underwater/Start third chapter in Play Mode")]
        static void StartThird(){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<UnderwaterThirdChapter>().BeginThirdChapter();else{SessionState.SetBool("ThirdChapterStart",true);Application.runInBackground=true;EditorApplication.isPlaying=true;}}
        static Sprite S(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Underwater/Resources/Abyss/"+name+".png");
        static Sprite[] Frames(string prefix,int count)=>Enumerable.Range(0,count).Select(i=>S(prefix+i)).ToArray();
        static Vector2 Safe(Vector2 p,float radius=.9f){for(int i=0;i<24;i++){var at=p+Vector2.up*i*.5f;if(WaterRoute.Clear(at,radius))return at;}throw new InvalidOperationException("No safe position near "+p);}
        static StorySwimmer Shark(string name,Transform root,Vector2 at,Sprite[] frames,float width,float radius)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=at;go.AddComponent<Rigidbody2D>();go.AddComponent<CircleCollider2D>();var visual=new GameObject("Aligned visual");visual.transform.SetParent(go.transform,false);var renderer=visual.AddComponent<SpriteRenderer>();renderer.sprite=frames[0];renderer.sortingOrder=15;visual.transform.localScale=Vector3.one*width/renderer.sprite.bounds.size.x;
            var actor=go.AddComponent<StorySwimmer>();actor.visual=renderer;actor.swimFrames=frames;actor.swimSpeed=2;actor.radius=radius;actor.FaceTravelDirection=true;actor.maxPitch=25;return actor;
        }
        static void ImportSprites()
        {
            foreach(var path in Directory.GetFiles("Assets/Underwater/Resources/Abyss","*.png").Where(p=>Path.GetFileName(p).StartsWith("leader_")||Path.GetFileName(p).StartsWith("cannon_")||Path.GetFileName(p)=="shark_warning.png"))
            {
                AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=64;
                if(Path.GetFileName(path)=="shark_warning.png"){var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,0);importer.SetTextureSettings(settings);}importer.SaveAndReimport();
            }
        }
        static void AnalyzeAudio(UnderwaterSceneAudio sound)
        {
            string report="No cannon splash/impact recordings present; assign optional Inspector fields.\nNo existing AudioMixer found; scene-local assignable Mixer field.\n";
            foreach(var clip in new[]{sound.ambience,sound.bubble})if(clip){clip.LoadAudioData();var data=new float[clip.samples*clip.channels];if(clip.GetData(data,0)){double sum=0,peak=0;foreach(float value in data){sum+=value*value;peak=Math.Max(peak,Math.Abs(value));}report+=$"{clip.name}: duration={clip.length:F3}s RMS={Math.Sqrt(sum/data.Length):F5} peak={peak:F5}; ";int edge=Math.Min(clip.frequency/10*clip.channels,data.Length);double first=0,last=0;for(int i=0;i<edge;i++){first+=data[i]*data[i];last+=data[data.Length-edge+i]*data[data.Length-edge+i];}report+=$"edge RMS={Math.Sqrt(first/edge):F5}/{Math.Sqrt(last/edge):F5}\n";
                    if(clip==sound.bubble){int step=clip.frequency*clip.channels/10,window=(int)(clip.frequency*.65f)*clip.channels;double best=-1;int bestAt=0;for(int p=0;p+window<data.Length;p+=step){double energy=0;for(int j=p;j<p+window;j+=8)energy+=data[j]*data[j];if(energy>best){best=energy;bestAt=p;}}sound.bubbleStartTime=(float)bestAt/(clip.frequency*clip.channels);}
                }}
            File.WriteAllText("Tools/Revision10/audio-analysis.txt",report+"Ambience uses two fixed sources with 0.6s loop crossfade and 1s fade-in. Bubble uses one source, 0.65s window, 1.6s cooldown and fade edges. No subjective listening/build test claimed.");
        }
        static void ApplyDensity()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
            var hazards=Object.FindFirstObjectByType<CannonHazards>();hazards.maximumActive=10;hazards.shotInterval=.65f;hazards.laneSpacing=7;hazards.tutorialInterval=1;hazards.salvoEvery=3;hazards.salvoPause=1.4f;
            EditorSceneManager.MarkSceneDirty(hazards.gameObject.scene);EditorSceneManager.SaveScene(hazards.gameObject.scene);
            File.WriteAllText("Tools/Revision11/density.txt","Max 10 active; normal shots 0.65s; tutorial 1s; 7-unit lanes; every 3 shots a two-ball salvo with 1.4s pause. Speed/player movement and safe columns unchanged.");
        }
        static void FloorRoute(UnderwaterThirdChapter chapter)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
            chapter.roadStart=Safe(chapter.second.departurePoint+Vector2.down*4);
            chapter.leaderRest=Safe(new Vector2(216,-25),1.2f);chapter.leader.transform.position=chapter.leaderRest;
            chapter.checkpoints=new[]{chapter.roadStart,Safe(new Vector2(-80,-27)),Safe(new Vector2(0,-27)),Safe(new Vector2(75,-27)),Safe(new Vector2(155,-27))};
            chapter.guardRest=new[]{Safe(chapter.leaderRest+new Vector2(-6,-3.7f)),Safe(chapter.leaderRest+new Vector2(6,-3.7f))};
            for(int i=0;i<2;i++)chapter.guards[i].transform.position=chapter.guardRest[i];
            chapter.rallyPositions=new[]{Safe(chapter.leaderRest+new Vector2(-4,1),1),Safe(chapter.leaderRest+new Vector2(-10,-3.7f)),Safe(chapter.leaderRest+new Vector2(2,-3.7f))};
            EditorSceneManager.MarkSceneDirty(chapter.gameObject.scene);EditorSceneManager.SaveScene(chapter.gameObject.scene);
            File.WriteAllText("Tools/Revision10/route-polish.txt",$"Only new chapter road/NPC heights adjusted for ground sign visibility. Road={chapter.roadStart}; leader={chapter.leaderRest}. Existing world and camera unchanged.");
        }
        [MenuItem("Underwater/Install third chapter (preserve world)")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/UnderwaterWorld.unity")throw new InvalidOperationException("Open UnderwaterWorld outside Play Mode.");
            Directory.CreateDirectory("Tools/Revision10");ImportSprites();
            var story=Object.FindFirstObjectByType<UnderwaterDayStory>();if(story.thirdChapter)return;
            var transforms=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==story.gameObject.scene).Select(t=>new{t,p=t.localPosition,r=t.localRotation,s=t.localScale,parent=t.parent}).ToArray();
            var colliders=Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>new{c,json=EditorJsonUtility.ToJson(c)}).ToArray();
            var chapter=story.gameObject.AddComponent<UnderwaterThirdChapter>();chapter.story=story;chapter.second=story.secondChapter;story.thirdChapter=chapter;
            chapter.roadStart=Safe(story.secondChapter.departurePoint);chapter.leaderRest=Safe(new Vector2(216,-20),1.2f);
            chapter.checkpoints=new[]{chapter.roadStart,Safe(new Vector2(-80,-23)),Safe(new Vector2(0,-23)),Safe(new Vector2(75,-23)),Safe(new Vector2(155,-23))};
            var root=new GameObject("Third chapter • shark allies");root.transform.SetParent(story.transform,false);chapter.npcRoot=root.transform;
            var poses=Frames("leader_",4);chapter.leaderSwim=poses.Take(2).ToArray();chapter.leaderTalk=poses.Skip(2).Take(2).ToArray();
            var habitat=Object.FindFirstObjectByType<OceanHabitat>();float width=habitat.species.First(s=>s.family==HabitatFamily.Shark).width;
            chapter.leader=Shark("Köpekbalığı Lideri",root.transform,chapter.leaderRest,chapter.leaderSwim,width*1.3f,.8f);
            chapter.guardRest=new[]{Safe(chapter.leaderRest+new Vector2(-6,-5)),Safe(chapter.leaderRest+new Vector2(6,-5))};chapter.guards=new[]{Shark("Liderin dost köpekbalığı 1",root.transform,chapter.guardRest[0],Frames("shark_0_",4),width,.6f),Shark("Liderin dost köpekbalığı 2",root.transform,chapter.guardRest[1],Frames("shark_1_",4),width,.6f)};
            chapter.rallyPositions=new[]{Safe(chapter.leaderRest+new Vector2(-4,2),1),Safe(chapter.leaderRest+new Vector2(-10,-4)),Safe(chapter.leaderRest+new Vector2(2,-4))};root.SetActive(false);
            var sign=new GameObject("Köpekbalıkları bölgesi • warning sign");sign.transform.SetParent(story.transform,false);chapter.warningSign=sign.AddComponent<SpriteRenderer>();chapter.warningSign.sprite=S("shark_warning");chapter.warningSign.sortingOrder=12;sign.transform.localScale=Vector3.one*2.8f/chapter.warningSign.sprite.bounds.size.x;
            Physics2D.SyncTransforms();var ground=Physics2D.RaycastAll(new Vector2(173,story.player.surfaceY),Vector2.down,60).Where(h=>h.collider&&!h.collider.isTrigger&&!h.collider.attachedRigidbody).OrderBy(h=>h.distance).First();sign.transform.position=new Vector3(173,ground.point.y-.12f,0);
            var sound=story.gameObject.AddComponent<UnderwaterSceneAudio>();sound.player=story.player;sound.ui=story.ui;sound.ambience=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/freesound_community-underwater-loop-amb-6182.mp3");sound.bubble=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/soundshmyak-bubbles-159281.mp3");AnalyzeAudio(sound);
            var hazardsRoot=new GameObject("Third chapter • local cannon hazards");hazardsRoot.transform.SetParent(story.transform,false);var hazards=hazardsRoot.AddComponent<CannonHazards>();chapter.hazards=hazards;hazards.chapter=chapter;hazards.audioSource=sound;hazards.ballSprite=S("cannon_ball");hazards.splashFrames=Frames("cannon_splash_",4);hazards.wakeFrames=Frames("cannon_wake_",4);hazards.sedimentFrames=Enumerable.Range(1,3).Select(i=>S("cannon_sediment_"+i)).ToArray();
            hazards.sections=new[]{new CannonSection{left=-140,right=-100},new CannonSection{left=-65,right=-25},new CannonSection{left=15,right=55},new CannonSection{left=90,right=130}};hazards.terminalSpeed=story.player.speed*1.35f;
            FloorRoute(chapter);
            foreach(var p in chapter.checkpoints)if(!chapter.IsSafe(p))throw new InvalidOperationException("Checkpoint in hazard section.");
            foreach(var saved in transforms)if(saved.t.localPosition!=saved.p||saved.t.localRotation!=saved.r||saved.t.localScale!=saved.s||saved.t.parent!=saved.parent)throw new InvalidOperationException("Authored transform changed: "+saved.t.name);
            foreach(var saved in colliders)if(EditorJsonUtility.ToJson(saved.c)!=saved.json)throw new InvalidOperationException("Authored collider changed: "+saved.c.name);
            EditorSceneManager.MarkSceneDirty(story.gameObject.scene);EditorSceneManager.SaveScene(story.gameObject.scene);
            File.WriteAllText("Tools/Revision10/setup.txt",$"PASS {transforms.Length} existing transforms and {colliders.Length} colliders preserved.\nRoad {chapter.roadStart}, leader {chapter.leaderRest}, sign grounded at {sign.transform.position}.\nLeader width {width*1.3f}, ordinary shark width {width}; cannon terminal speed {hazards.terminalSpeed}.\nNo ship or global settings touched.");
        }
    }
}
