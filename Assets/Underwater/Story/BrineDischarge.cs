using UnityEngine;
namespace Abyss.Story {
public class BrineDischarge:MonoBehaviour {
 public WarEnvironment environment;public Sprite[] frames;public float fps=6,width=1.6f,length=4.5f,opacity=.38f,damageInterval=1;public int damage=1;public bool dischargeEnabled=true;public Vector2 outletOffset=new(-8.2f,-2.4f);public Transform outlet,vesselTransform;public bool Active{get;private set;}float nextDamage;bool warned;SpriteRenderer visual;
 void Awake(){var anchor=new GameObject("Pipe outlet (adjust offset)");anchor.transform.SetParent(transform,false);outlet=anchor.transform;var go=new GameObject("Low contrast brine flow");go.transform.SetParent(outlet,false);visual=go.AddComponent<SpriteRenderer>();visual.sortingOrder=7;}
 public bool Contains(Vector2 p){var local=p-(Vector2)outlet.position;float depth=-local.y;return depth>=.1f&&depth<length*.78f&&Mathf.Abs(local.x)<Mathf.Lerp(.15f,width*.32f,depth/length);}
 void Update(){outlet.position=(vesselTransform?(Vector2)vesselTransform.position:new Vector2(-45,environment.story.player.surfaceY+.9f))+outletOffset;Active=dischargeEnabled&&environment.Stage>0;visual.enabled=Active;if(!Active){if(environment.Stage==0){warned=false;nextDamage=0;}return;}if(environment.story.thirdChapter.Stage==ThirdChapterStage.Respawning)nextDamage=Time.time+environment.story.thirdChapter.invulnerability;visual.sprite=frames[(int)(Time.time*fps)%frames.Length];visual.transform.localScale=new Vector3(width/visual.sprite.bounds.size.x,length/visual.sprite.bounds.size.y,1);visual.color=new Color(.72f,.82f,.86f,opacity);
 var chapter=environment.story.thirdChapter;var p=chapter.PlayerPosition;if(Contains(p)){if(!warned){warned=true;environment.story.ui.QueueTravelDialogue("Yoğun tuzlu su! Buradan uzaklaşmalıyım.");}if(Time.time>=nextDamage){nextDamage=Time.time+damageInterval;chapter.TakeBrineHit(damage,damageInterval);}} }
}
}
