using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Abyss.Story
{
    [Serializable] public class CannonSection {public float left,right;[NonSerialized] public float nextShot;[NonSerialized] public int shots;}
    public class CannonHazards:MonoBehaviour
    {
        public UnderwaterThirdChapter chapter;
        [FormerlySerializedAs("audio")] public UnderwaterSceneAudio audioSource;
        public Sprite ballSprite;public Sprite[] splashFrames,wakeFrames,sedimentFrames;
        public CannonSection[] sections;
        public int maximumActive=10;
        public float terminalSpeed=8.1f,initialSpeed=2.8f,acceleration=5.5f,shotInterval=.65f,activationLead=32,laneSpacing=7,reactionSeconds=1;
        public float tutorialInterval=1f,salvoPause=1.4f;
        public int salvoEvery=3;
        public bool Running{get;private set;}
        public bool Paused=>!Running||chapter.Suspended;
        public Collider2D PlayerCollider=>chapter.story.player.GetComponent<Collider2D>();
        public int ActiveCount=>balls.Count(b=>b.gameObject.activeSelf&&b.Dangerous);
        public IReadOnlyList<UnderwaterCannonball> Balls=>balls;
        readonly List<UnderwaterCannonball> balls=new();readonly List<CannonVisualEffect> effects=new();
        public void Begin(){Clear();foreach(var section in sections){section.nextShot=Time.time+.8f;section.shots=0;}Running=true;}
        public void Halt(){Running=false;Clear();}
        public void Clear(){foreach(var ball in balls)if(ball)ball.Retire();foreach(var effect in effects)if(effect)effect.gameObject.SetActive(false);}
        public bool InSafeArea(Vector2 p)=>chapter.IsSafe(p);
        void Update()
        {
            if(Paused)return;var player=chapter.PlayerPosition;
            float lead=Mathf.Clamp((chapter.story.player.surfaceY-player.y)/terminalSpeed*chapter.story.player.CurrentSpeed,6,activationLead);float focus=player.x+lead;var activeSection=sections.OrderBy(s=>Mathf.Abs(focus-Mathf.Clamp(focus,s.left,s.right))).First();
            foreach(var section in sections)
            {
                if(section!=activeSection)continue;
                if(player.x<section.left-activationLead||player.x>section.right+12||Time.time<section.nextShot)continue;
                // Frequent fixed-lane shots; short salvos keep pressure between safe columns.
                section.nextShot=Time.time+(shotInterval);
                int total=Mathf.Max(1,Mathf.FloorToInt((section.right-section.left-8)/laneSpacing));int count=Mathf.Min(4,total);int first=Mathf.Clamp(Mathf.FloorToInt((focus-section.left-4)/laneSpacing)-count/2,0,total-count);int lane=first+(section.shots+section.shots/count+1)%count;
                float x=section.left+4+lane*laneSpacing;
                if(ActiveCount<maximumActive)Spawn(x);
                section.shots++;
                if(section!=sections[0]&&section.shots%Mathf.Max(1,salvoEvery)==0){if(ActiveCount<maximumActive)Spawn(section.right-4);section.nextShot=Time.time+salvoPause;}
            }
            UnderwaterCannonball closest=null;float best=float.MaxValue;
            foreach(var ball in balls)if(ball.gameObject.activeSelf&&ball.Dangerous){var p=(Vector2)ball.transform.position;float dy=p.y-player.y;if(dy>0&&dy<terminalSpeed*(reactionSeconds+.3f)&&Mathf.Abs(p.x-player.x)<4&&dy<best){closest=ball;best=dy;}}
            chapter.story.ui.SetDangerWarning(closest?"↓ Yaklaşan gülle":"");
        }
        public UnderwaterCannonball Spawn(float x)
        {
            if(ActiveCount>=maximumActive)return null;var at=new Vector2(x,chapter.story.player.surfaceY+.15f);if(InSafeArea(at)||Vector2.Distance(at,chapter.PlayerPosition)<terminalSpeed*reactionSeconds+1)return null;
            var ball=balls.FirstOrDefault(b=>!b.gameObject.activeSelf);
            if(!ball){var go=new GameObject("Sinking cannonball");go.transform.SetParent(transform,false);ball=go.AddComponent<UnderwaterCannonball>();balls.Add(ball);}
            // Always originate at the real surface, even when it is outside the camera.
            ball.Launch(this,at,UnityEngine.Random.Range(-.12f,.12f));Effect(splashFrames,new Vector2(x,chapter.story.player.surfaceY),1.4f,.6f,0);audioSource?.PlayImpact(true);return ball;
        }
        public void Impact(Vector2 at){Effect(sedimentFrames,at,1.6f,.85f,.35f);audioSource?.PlayImpact(false);}
        public void Wake(Vector2 at){Effect(wakeFrames,at+Vector2.up*.25f,.45f,.65f,.9f);}
        void Effect(Sprite[] sprites,Vector2 at,float width,float life,float rise)
        {
            if(sprites==null||sprites.Length==0)return;var effect=effects.FirstOrDefault(e=>!e.gameObject.activeSelf);
            if(!effect){if(effects.Count>=48)return;var go=new GameObject("Underwater cannon effect");go.transform.SetParent(transform,false);effect=go.AddComponent<CannonVisualEffect>();effects.Add(effect);}
            effect.Begin(sprites,at,width,life,rise,Color.white);
        }
    }
}
