using UnityEngine;

namespace Abyss.Story
{
    public class MeetingFishIdle:MonoBehaviour
    {
        public StorySwimmer actor;
        public Vector2 restPosition;
        public float phase;
        void Update(){actor.visual.transform.localPosition=new Vector3(Mathf.Sin(Time.time*.6f+phase)*.09f,Mathf.Sin(Time.time*.85f+phase)*.07f,0);}
    }
}
