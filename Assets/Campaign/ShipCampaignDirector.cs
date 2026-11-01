using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Abyss.Story;

namespace IntegratedCampaign {
// Story and the explicitly requested end boundary only. Native ship systems own gameplay.
[DefaultExecutionOrder(10000)]
public sealed class ShipCampaignDirector : MonoBehaviour {
 public bool integrationEnabled=true;
 [Min(1)] public float smallFishSeconds=120, sharkSeconds=150, whaleSeconds=180;
 public Sprite[] flagFrames;
 public Vector2 flagAttachment=new(.6f,1.8f);
 public float flagHeight=1.4f;
 public UnityEvent chapterCompleted=new();
 public float ActiveSeconds {get;private set;}
 public bool ProductionStopped {get;private set;}
 public bool Completed {get;private set;}
 public int RemainingEnemies=>combatants.Count;
 public UnderwaterStoryUI Dialogue {get;private set;}
 public bool Running {get;private set;}
 public static bool BlocksFinalDamage(PlayerHealth target){var d=target.GetComponent<ShipCampaignDirector>();return d&&d.finalStarted;}
 readonly HashSet<Enemy> combatants=new();
 ShipController ship;ShipWeapons weapons;PlayerHealth health;EnemySpawner spawner;ShipUIManager nativeUI;
 CampaignFlow flow;bool retrying,finalStarted,showingStory;float previousScale;bool shipWasEnabled,weaponsWereEnabled;
 SpriteRenderer flag;
 float Limit=>flow.Phase==CampaignPhase.SmallFishBattle?smallFishSeconds:flow.Phase==CampaignPhase.SharkBattle?sharkSeconds:whaleSeconds;
 IEnumerator Start(){
  yield return null;
  flow=CampaignFlow.Instance;
  if(!integrationEnabled||!flow||!flow.SessionStarted||!CampaignFlow.IsManagedShip)yield break;
  ship=GetComponent<ShipController>();weapons=GetComponent<ShipWeapons>();health=GetComponent<PlayerHealth>();
  spawner=FindFirstObjectByType<EnemySpawner>();nativeUI=FindFirstObjectByType<ShipUIManager>();
  Dialogue=gameObject.AddComponent<UnderwaterStoryUI>();Dialogue.leftPanel=Resources.Load<Sprite>("Campaign/ship_dialogue_left");Dialogue.rightPanel=Resources.Load<Sprite>("Campaign/ship_dialogue_right");Dialogue.UseFont(Resources.Load<Font>("Campaign/PixelifySans"));Dialogue.SetObjective("");Dialogue.SetHint("");
  if(flagFrames==null||flagFrames.Length==0){flagFrames=new Sprite[4];for(int i=0;i<4;i++)flagFrames[i]=Resources.Load<Sprite>("Campaign/flag_"+i);}
  var go=new GameObject("White ceasefire flag");flag=go.AddComponent<SpriteRenderer>();flag.sortingOrder=1100;go.SetActive(false);
  chapterCompleted.AddListener(AdvanceStory);
  if(flow.Phase==CampaignPhase.IntroVoyage){
   Speak(new[]{L("Liman Görevlisi","Nehir kurudu. Kasabanın suyu tükeniyor."),L("Kaptan","Deniz suyunu arıtarak depoyu dolduracağız."),L("Liman Görevlisi","Şimdilik tek umudumuz bu gemi.")},()=>flow.Go(CampaignPhase.Announcement));yield break;
  }
  Running=true;
  if(flow.Phase==CampaignPhase.SmallFishBattle)Speak(new[]{L("Kaptan","Balıklar gemiye saldırıyor!"),L("Mürettebat","Gövdeyi koruyun!")});
  else if(flow.Phase==CampaignPhase.SharkBattle)Speak(new[]{L("Mürettebat","Köpekbalıkları da yaklaşıyor."),L("Kaptan","Hazır olun.")});
  else if(flow.Phase==CampaignPhase.WhaleBattle)Speak(new[]{L("Kaptan","Balinalar da gelmiş…"),L("Mürettebat","Bu savaş hepimizi tüketiyor.")});
 }
 UnderwaterStoryUI.Line L(string who,string text)=>new(who,who=="Balık Başkanı"?Resources.Load<Sprite>("Campaign/mayor_portrait"):null,text,who!="Kaptan");
 void Speak(UnderwaterStoryUI.Line[] lines,System.Action done=null){
  previousScale=Time.timeScale;shipWasEnabled=ship.enabled;weaponsWereEnabled=weapons&&weapons.enabled;
  ship.enabled=false;if(weapons)weapons.enabled=false;Time.timeScale=0;showingStory=true;
  Dialogue.Show(lines,true,()=>StartCoroutine(FinishSpeech(done)));
 }
 IEnumerator FinishSpeech(System.Action done){while(flow.Transitioning)yield return null;showingStory=false;if(!finalStarted&&!health.IsDead){ship.enabled=shipWasEnabled;if(weapons)weapons.enabled=weaponsWereEnabled;}Time.timeScale=previousScale;done?.Invoke();}
 bool CardOpen(){if(!nativeUI||!nativeUI.levelUpDocument||nativeUI.levelUpDocument.rootVisualElement==null)return false;var overlay=nativeUI.levelUpDocument.rootVisualElement.Q<VisualElement>("LevelUpOverlay");return overlay!=null&&overlay.resolvedStyle.display!=DisplayStyle.None;}
 void Update(){
  if(!Dialogue)return;
  if(Dialogue.Active&&Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)Dialogue.Advance();
  if(!Running||Completed||retrying||finalStarted)return;
  if(health.IsDead){retrying=true;StartCoroutine(Retry());return;}
  foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(enemy.gameObject.scene==gameObject.scene&&enemy.player==ship.transform)combatants.Add(enemy);
  combatants.RemoveWhere(e=>!e);
  if(showingStory||flow.Transitioning||Time.timeScale<=0||CardOpen())return;
  ActiveSeconds+=Time.deltaTime;
  if(!ProductionStopped&&ActiveSeconds>=Limit){ProductionStopped=true;spawner.StopAllCoroutines();}
  if(ProductionStopped&&combatants.Count==0){Completed=true;Running=false;chapterCompleted.Invoke();}
 }
 IEnumerator Retry(){yield return new WaitForSecondsRealtime(.8f);flow.Go(flow.Phase);}
 void AdvanceStory(){
  if(flow.Phase==CampaignPhase.SmallFishBattle)flow.Go(CampaignPhase.SharkJourney);
  else if(flow.Phase==CampaignPhase.SharkBattle)flow.Go(CampaignPhase.WhaleBattle);
  else if(flow.Phase==CampaignPhase.WhaleBattle)StartCoroutine(Finale());
 }
 IEnumerator Finale(){
  finalStarted=true;spawner.StopAllCoroutines();health.enabled=false;
  foreach(var turret in GetComponentsInChildren<Turret>())if(turret.audioSource)turret.audioSource.Stop();
  Speak(new[]{L("Kaptan","Arıtma sistemi durdu."),L("Mürettebat","Kasabaya su götüremiyoruz."),L("Kaptan","Ateşi kesin. Beyaz bayrağı çekin.")});while(Dialogue.Active)yield return null;
  Time.timeScale=0;flag.gameObject.SetActive(true);yield return new WaitForSecondsRealtime(1.2f);
  Speak(new[]{L("Kaptan","Savaşı durduralım. Daha fazla kayıp istemiyoruz."),L("Balık Başkanı","Bu su artık bizi de yaşatmıyor."),L("Kaptan","Arıtmayı durduracağız."),L("Balık Başkanı","Buradan ayrılın. Denizi daha fazla kirletmeyin."),L("Kaptan","Kabul ediyoruz.")});while(Dialogue.Active)yield return null;
  yield return Dialogue.Fade(true);yield return new WaitForSecondsRealtime(.5f);yield return Dialogue.Fade(false);
  Speak(new[]{L("Anlatı","İnsanlar kasabadan ayrıldı."),L("Anlatı","Denizde kalanlar da yeni bir yuva aramak zorundaydı."),L("Anlatı","Savaş bitti. Kimse kazanmadı.")});while(Dialogue.Active)yield return null;
  var choices=new GameObject("Ending choices").AddComponent<CampaignUI>();var first=choices.Button("Main menu",choices.root,new Vector2(0,-310),"Ana Menü",()=>flow.ReturnMenu());choices.Button("Restart",choices.root,new Vector2(0,-440),"Yeniden Başla",()=>flow.NewGame());UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(first.gameObject);
 }
 void LateUpdate(){if(flag&&flag.gameObject.activeSelf&&flagFrames.Length>0){flag.sprite=flagFrames[(int)(Time.unscaledTime*6)%flagFrames.Length];if(!flag.sprite)return;flag.transform.position=transform.TransformPoint(flagAttachment);flag.transform.rotation=Quaternion.identity;flag.transform.localScale=Vector3.one*flagHeight/flag.sprite.bounds.size.y;}}
}
}
