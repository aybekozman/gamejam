using System.Collections.Generic;
using UnityEngine;

namespace Abyss.Story
{
    [RequireComponent(typeof(Rigidbody2D),typeof(CircleCollider2D))]
    public class StorySwimmer:MonoBehaviour
    {
        public SpriteRenderer visual;
        public float swimSpeed=4.5f;
        public float radius=.4f;
        public bool Animate=true;
        public bool FaceTravelDirection;
        public float maxPitch=58,pitchResponse=9;
        Vector2 travelDirection;
        public Sprite[] swimFrames;
        public bool Arrived=>path.Count==0;
        public Vector2 Position=>body?body.position:(Vector2)transform.position;
        public bool Blocked{get;private set;}
        Rigidbody2D body;readonly List<Vector2> path=new();float clock,retry;
        void Awake(){body=GetComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.interpolation=RigidbodyInterpolation2D.Interpolate;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;GetComponent<CircleCollider2D>().radius=radius;}
        public bool Go(Vector2 target)
        {
            var route=WaterRoute.Find(Position,target,radius);path.Clear();path.AddRange(route);Blocked=route.Count==0&&Vector2.Distance(Position,target)>.5f;return !Blocked;
        }
        public void Stop(){travelDirection=Vector2.zero;path.Clear();if(body)body.linearVelocity=Vector2.zero;}
        public void Place(Vector2 p){Stop();if(body)body.position=p;else transform.position=p;}
        void Update(){if(FaceTravelDirection){float pitch=travelDirection.sqrMagnitude>.001f?Mathf.Clamp(Mathf.Atan2(travelDirection.y,Mathf.Abs(travelDirection.x))*Mathf.Rad2Deg,-maxPitch,maxPitch):0;float target=pitch*(visual.flipX?-1:1);visual.transform.localRotation=Quaternion.Euler(0,0,Mathf.LerpAngle(visual.transform.localEulerAngles.z,target,1-Mathf.Exp(-pitchResponse*Time.deltaTime)));}clock+=Time.deltaTime;if(Animate&&swimFrames!=null&&swimFrames.Length>0)visual.sprite=swimFrames[(int)(clock*4)%swimFrames.Length];}
        void FixedUpdate()
        {
            if(path.Count==0){travelDirection=Vector2.zero;body.linearVelocity=Vector2.zero;return;}
            if(Vector2.Distance(body.position,path[0])<.16f){path.RemoveAt(0);if(path.Count==0){travelDirection=Vector2.zero;body.linearVelocity=Vector2.zero;return;}}
            var direction=path[0]-body.position;var step=Vector2.MoveTowards(body.position,path[0],swimSpeed*Time.fixedDeltaTime);
            if(!WaterRoute.Segment(body.position,step,radius*.92f)){body.linearVelocity=Vector2.zero;Blocked=true;if((retry-=Time.fixedDeltaTime)<=0){retry=.8f;var goal=path[^1];Go(goal);}return;}
            Blocked=false;travelDirection=direction.normalized;if(Mathf.Abs(direction.x)>.04f)visual.flipX=direction.x<0;body.MovePosition(step);
        }
    }
}
