using UnityEngine;

namespace Abyss.Story
{
    public class UnderwaterCannonball:MonoBehaviour
    {
        public CannonHazards owner;public Vector2 velocity;public float radius=.26f;
        public bool Dangerous{get;private set;}public float Age{get;private set;}
        SpriteRenderer visual;float bubbleClock;readonly RaycastHit2D[] hits=new RaycastHit2D[32];
        public void Launch(CannonHazards source,Vector2 at,float drift)
        {
            owner=source;if(!visual)visual=gameObject.AddComponent<SpriteRenderer>();visual.sprite=owner.ballSprite;visual.sortingOrder=22;visual.color=Color.white;transform.localScale=Vector3.one*.58f/visual.sprite.bounds.size.x;transform.position=at;
            velocity=new Vector2(drift,-owner.initialSpeed);Age=0;bubbleClock=0;Dangerous=true;gameObject.SetActive(true);
        }
        void FixedUpdate()
        {
            if(!Dangerous||owner.Paused)return;
            Age+=Time.fixedDeltaTime;velocity.y=-Mathf.MoveTowards(-velocity.y,owner.terminalSpeed,owner.acceleration*Time.fixedDeltaTime);
            Vector2 from=transform.position,delta=velocity*Time.fixedDeltaTime;
            int count=Physics2D.CircleCast(from,radius,delta.normalized,new ContactFilter2D{useTriggers=false},hits,delta.magnitude);
            float nearest=float.PositiveInfinity;RaycastHit2D hit=default;
            for(int i=0;i<count;i++)
            {
                var collider=hits[i].collider;if(!collider||!collider.enabled||collider.isTrigger)continue;
                bool player=collider==owner.PlayerCollider;if(!player&&collider.attachedRigidbody&&collider.attachedRigidbody.bodyType==RigidbodyType2D.Dynamic)continue;
                if(hits[i].distance<nearest){nearest=hits[i].distance;hit=hits[i];}
            }
            if(hit.collider)
            {
                transform.position=from+delta.normalized*Mathf.Max(0,hit.distance-.02f);
                if(hit.collider==owner.PlayerCollider)owner.chapter.TakeHit(velocity.normalized);
                owner.Impact(transform.position);Retire();return;
            }
            var environment=owner.chapter.story.GetComponent<WarEnvironment>();if(environment&&environment.StrikeFish(from,from+delta)){owner.Impact(from);Retire();return;}
            transform.position=from+delta;
            if((bubbleClock-=Time.fixedDeltaTime)<=0){bubbleClock=.28f;owner.Wake(transform.position);}
            if(((Vector2)transform.position-owner.chapter.PlayerPosition).sqrMagnitude<25)owner.chapter.NearMiss();
            if(Age>14||transform.position.y<owner.chapter.story.player.swimMin.y-3||owner.InSafeArea(transform.position))Retire();
        }
        public void Retire(){Dangerous=false;gameObject.SetActive(false);}
    }
    public class CannonVisualEffect:MonoBehaviour
    {
        public Sprite[] frames;public float lifetime=.7f,rise;public Vector2 motion;float age;SpriteRenderer visual;
        public void Begin(Sprite[] sprites,Vector2 at,float width,float seconds,float vertical,Color color)
        {
            frames=sprites;lifetime=seconds;rise=vertical;age=0;transform.position=at;if(!visual)visual=gameObject.AddComponent<SpriteRenderer>();visual.sprite=frames[0];visual.sortingOrder=24;transform.localScale=Vector3.one*width/visual.sprite.bounds.size.x;visual.color=color;gameObject.SetActive(true);
        }
        void Update(){age+=Time.deltaTime;transform.position+=(Vector3)(new Vector2(0,rise)+motion)*Time.deltaTime;visual.sprite=frames[Mathf.Min(frames.Length-1,(int)(age/lifetime*frames.Length))];var c=visual.color;c.a=Mathf.Clamp01(1-age/lifetime);visual.color=c;if(age>=lifetime)gameObject.SetActive(false);}
    }
}
