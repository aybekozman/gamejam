using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Abyss.Story;

namespace IntegratedCampaign {
public enum CampaignPhase { Menu, UnderwaterDiscovery, IntroVoyage, Announcement, SmallFishBattle, SharkJourney, SharkBattle, WhaleBattle, Ending }
[DefaultExecutionOrder(9000)]
public sealed class CampaignFlow:MonoBehaviour {
 public static CampaignFlow Instance{get;private set;}
 public const string ShipScene="ShipScene 1",UnderwaterScene="UnderwaterWorld",MenuScene="MainMenu";
 public CampaignPhase Phase{get;private set;}=CampaignPhase.Menu;
 public bool SessionStarted{get;private set;}
 public int CalendarDay{get;private set;}=1;
 public int BattleWave=>Phase==CampaignPhase.SmallFishBattle?1:Phase==CampaignPhase.SharkBattle?2:Phase==CampaignPhase.WhaleBattle?3:0;
 public int Pollution=>Phase>=CampaignPhase.WhaleBattle?3:Phase>=CampaignPhase.SharkJourney?2:Phase>=CampaignPhase.Announcement?1:0;
 public bool Transitioning{get;private set;}public bool ReplayingUpgrades{get;private set;}
 public static bool IsManagedShip=>Instance&&SceneManager.GetActiveScene().name==ShipScene;
 public static float AmbiencePreference=>PlayerPrefs.GetFloat("Sound.Ambience",.8f);
 public static float EffectsPreference=>PlayerPrefs.GetFloat("Sound.Effects",.8f);
 public readonly List<UpgradeData> Upgrades=new();
 int checkpointUpgrades,checkpointLevel=1;float checkpointXP;long checkpointPlayerXP;int checkpointPlayerLevel=1;
 int savedLevel=1;float savedXP;long savedPlayerXP;int savedPlayerLevel=1;
 readonly HashSet<CampaignPhase> delivered=new();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Bootstrap(){if(!Instance)new GameObject("Campaign flow").AddComponent<CampaignFlow>();}
 void Awake(){if(Instance&&Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);SceneManager.sceneLoaded+=Loaded;ApplyAudioPreferences();}
 bool PreparingChapter(UnderwaterDayStory story)=>story&&((story.secondChapter&&story.secondChapter.Stage==SecondChapterStage.Preparing)||(story.thirdChapter&&(story.thirdChapter.Stage==ThirdChapterStage.Preparing||story.thirdChapter.Stage==ThirdChapterStage.Respawning)));
 void LateUpdate(){bool preparing=SessionStarted&&(Phase==CampaignPhase.Announcement||Phase==CampaignPhase.SharkJourney)&&PreparingChapter(FindFirstObjectByType<UnderwaterDayStory>());if(!Transitioning&&!preparing)return;var fish=FindFirstObjectByType<Abyss.FishPilot>();if(fish)fish.ControlsLocked=true;var ship=FindFirstObjectByType<ShipController>();if(ship)ship.enabled=false;}
 void OnDestroy(){if(Instance==this){SceneManager.sceneLoaded-=Loaded;Instance=null;}}
 void Loaded(Scene scene,LoadSceneMode mode){StartCoroutine(PrepareScene(scene));}
 IEnumerator PrepareScene(Scene scene){
  if(scene.name==ShipScene){SessionStarted=true;foreach(var spawner in FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))spawner.StopAllCoroutines();if(Phase==CampaignPhase.Menu||Phase==CampaignPhase.UnderwaterDiscovery)Phase=CampaignPhase.IntroVoyage;}
  yield return null;
  CampaignUI.EnsureEventSystem();
  if(scene.name==MenuScene){Phase=CampaignPhase.Menu;if(!FindFirstObjectByType<CampaignMenu>())new GameObject("Main menu UI").AddComponent<CampaignMenu>();yield break;}
  if(scene.name==UnderwaterScene){var story=FindFirstObjectByType<UnderwaterDayStory>();if(!story||!SessionStarted)yield break;
   story.secondChapter.skipShipForDevelopment=false;
   story.secondChapter.firstChapterDepartureRequested.AddListener(()=>Go(CampaignPhase.IntroVoyage));
   story.secondChapter.secondChapterCompleted.AddListener(()=>Go(CampaignPhase.SmallFishBattle));
   story.thirdChapter.chapterCompleted.AddListener(()=>Go(CampaignPhase.SharkBattle));
   if(Phase==CampaignPhase.Announcement)story.secondChapter.BeginSecondChapter();
   else if(Phase==CampaignPhase.SharkJourney)story.thirdChapter.BeginThirdChapter();
   else if(Phase==CampaignPhase.Menu)Phase=CampaignPhase.UnderwaterDiscovery;
  }
  if(scene.name==ShipScene){var ship=FindFirstObjectByType<ShipController>();if(ship&&!ship.GetComponent<ShipCampaignDirector>())ship.gameObject.AddComponent<ShipCampaignDirector>();}
 }
 public void NewGame(){if(Transitioning)return;SessionStarted=true;Upgrades.Clear();delivered.Clear();savedLevel=checkpointLevel=1;savedXP=checkpointXP=0;savedPlayerXP=checkpointPlayerXP=0;savedPlayerLevel=checkpointPlayerLevel=1;Bullet.bonusDamage=Bullet.bonusSpeed=Bullet.bonusKnockback=0;CalendarDay=1;Go(CampaignPhase.UnderwaterDiscovery);}
 public void Go(CampaignPhase next){if(Transitioning)return;StartCoroutine(Transition(next));}
 IEnumerator Transition(CampaignPhase next){Transitioning=true;Time.timeScale=1;var overlay=new GameObject("Scene transition").AddComponent<CampaignUI>();overlay.KeepAcrossScenes=true;DontDestroyOnLoad(overlay.gameObject);overlay.ShowFade(0,next==CampaignPhase.Announcement?"2. GÃ¼n":"");
  var fish=FindFirstObjectByType<Abyss.FishPilot>();if(fish)fish.ControlsLocked=true;var ship=FindFirstObjectByType<ShipController>();if(ship)ship.enabled=false;
  for(float t=0;t<.45f;t+=Time.unscaledDeltaTime){overlay.ShowFade(t/.45f,next==CampaignPhase.Announcement?"2. GÃ¼n":"");yield return null;}
  if(next==CampaignPhase.Announcement)yield return new WaitForSecondsRealtime(.75f);
  Phase=next;if(next==CampaignPhase.Announcement)CalendarDay=2;if(next==CampaignPhase.SharkBattle)CalendarDay=3;if(next==CampaignPhase.WhaleBattle)CalendarDay=4;
  string name=next==CampaignPhase.Menu?MenuScene:next==CampaignPhase.UnderwaterDiscovery||next==CampaignPhase.Announcement||next==CampaignPhase.SharkJourney?UnderwaterScene:ShipScene;
  overlay.ShowFade(1,next==CampaignPhase.Announcement?"2. Gün":"");yield return SceneManager.LoadSceneAsync(name);yield return null;
  for(float t=0;t<.45f;t+=Time.unscaledDeltaTime){overlay.ShowFade(1-t/.45f,next==CampaignPhase.Announcement?"2. GÃ¼n":"");yield return null;}
  Destroy(overlay.gameObject);Transitioning=false;var currentFish=FindFirstObjectByType<Abyss.FishPilot>();var currentStory=FindFirstObjectByType<UnderwaterDayStory>();if(currentFish)currentFish.ControlsLocked=currentStory&&(currentStory.ui.Blocking||PreparingChapter(currentStory));
 }
 public void RecordUpgrade(UpgradeData upgrade){if(!Transitioning&&!ReplayingUpgrades&&upgrade)Upgrades.Add(upgrade);}
 public void RestoreShip(ShipUIManager ui,PlayerExperience xp,PlayerHealth health){ReplayingUpgrades=true;foreach(var upgrade in Upgrades)ui.ApplyCampaignUpgrade(upgrade);ReplayingUpgrades=false;ui.RestoreCampaignProgress(savedLevel,savedXP);if(xp)xp.RestoreCampaignProgress(savedPlayerLevel,savedPlayerXP);health.SetMaxHealth(health.maxHealth,true);checkpointUpgrades=Upgrades.Count;checkpointLevel=savedLevel;checkpointXP=savedXP;checkpointPlayerXP=savedPlayerXP;checkpointPlayerLevel=savedPlayerLevel;}
 public bool CompleteDelivery(ShipUIManager ui,PlayerExperience xp){if(!delivered.Add(Phase))return false;savedLevel=ui.currentLevel;savedXP=ui.currentXP;if(xp){savedPlayerLevel=xp.PlayerLevel;savedPlayerXP=xp.CurrentXP;}return true;}
 public void SaveShipProgress(ShipUIManager ui,PlayerExperience xp){savedLevel=ui.currentLevel;savedXP=ui.currentXP;if(xp){savedPlayerLevel=xp.PlayerLevel;savedPlayerXP=xp.CurrentXP;}}
 public void RetryShip(){if(Transitioning)return;while(Upgrades.Count>checkpointUpgrades)Upgrades.RemoveAt(Upgrades.Count-1);savedLevel=checkpointLevel;savedXP=checkpointXP;savedPlayerXP=checkpointPlayerXP;savedPlayerLevel=checkpointPlayerLevel;Go(Phase);}
 public void ReturnMenu(){Go(CampaignPhase.Menu);}
 public static void ApplyAudioPreferences(){AudioListener.volume=PlayerPrefs.GetFloat("Sound.Master",.85f);}
}
}
