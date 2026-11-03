using System.Collections.Generic;
using UnityEngine;
using Abyss;using Abyss.Story;
public sealed class VoyageFishScale:MonoBehaviour {
 void Start(){var seen=new HashSet<Transform>();void Grow(SpriteRenderer r){if(r&&seen.Add(r.transform))r.transform.localScale*=1.15f;}var story=FindFirstObjectByType<UnderwaterDayStory>();if(!story)return;Grow(story.player.visual);story.player.bodyHalfHeight*=1.15f;foreach(var actor in story.gameObject.scene.GetRootGameObjects())foreach(var swimmer in actor.GetComponentsInChildren<StorySwimmer>(true))Grow(swimmer.visual);foreach(var body in story.bodies)Grow(body.GetComponent<SpriteRenderer>());}
}
