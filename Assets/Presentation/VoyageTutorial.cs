using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Abyss;
using Abyss.Story;
public sealed class VoyageTutorial:VoyageUI {
 static readonly HashSet<string> learned=new();[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetSession()=>learned.Clear();public static void ResetLessons()=>learned.Clear();
 ShipController ship;FishPilot fish;ShipUIManager loot;Text text;Image panel;string lesson;float visibleTime,originalDash;Vector2 start;bool originalLoot,observed;
 void Start(){ship=FindFirstObjectByType<ShipController>();fish=FindFirstObjectByType<FishPilot>();loot=FindFirstObjectByType<ShipUIManager>();panel=Picture("First use controls",root,Vector2.zero,new Vector2(950,90),"button");panel.rectTransform.anchorMin=panel.rectTransform.anchorMax=new Vector2(.5f,1);panel.rectTransform.anchoredPosition=new Vector2(0,-175);text=Label("Control instructions",panel.transform,Vector2.zero,new Vector2(850,70),"",26);panel.gameObject.SetActive(false);}
 void Update(){if(!panel)return;bool ready=Time.timeScale>0&&!VoyagePauseMenu.IsOpen&&!StorySceneJourney.Instance.IsTransitioning&&(fish&&!fish.ControlsLocked||ship&&ship.enabled);var story=FindFirstObjectByType<UnderwaterStoryUI>();if(story&&story.Blocking)ready=false;if(!ready){panel.gameObject.SetActive(false);return;}
 string prefix=ship?"ship.":"fish.";string next=!learned.Contains(prefix+"move")?"move":!learned.Contains(prefix+"boost")?"boost":ship&&!learned.Contains(prefix+"loot")?"loot":"";
 if(next==""){panel.gameObject.SetActive(false);return;}string key=prefix+next;if(lesson!=key){lesson=key;visibleTime=0;observed=false;start=ship?(Vector2)ship.transform.position:(Vector2)fish.transform.position;originalLoot=loot&&loot.isLootingXP;originalDash=ship?ship.currentDash:0;text.text=next=="move"?(ship?(StorySceneJourney.Instance.Stage==JourneyStage.FirstShip?"WASD • Su arıtımı yapabilmek için gemi ile yüz.":"WASD • Gemiyi kontrol et"):"WASD • Yüz"):next=="boost"?"Shift • Hızlan / atılım yap":"Space • Toplayacağın ödülü seç: enerji veya XP";}
 panel.gameObject.SetActive(true);visibleTime+=Time.deltaTime;bool done=next=="move"?Vector2.Distance(start,ship?(Vector2)ship.transform.position:(Vector2)fish.transform.position)>1.2f:next=="boost"?(ship?(ship.IsDashing||ship.currentDash<originalDash-.4f):fish.CurrentSpeed>fish.speed*1.15f):loot&&loot.isLootingXP!=originalLoot;
 observed|=done;
 if(visibleTime>1&&observed||visibleTime>12){learned.Add(key);panel.gameObject.SetActive(false);}
 }
}
