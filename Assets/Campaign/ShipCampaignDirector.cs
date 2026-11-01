using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Abyss.Story;

namespace IntegratedCampaign {
public sealed class ShipCampaignDirector:MonoBehaviour {
 public Vector2 flagAttachment=new(.6f,1.8f);public float flagHeight=1.4f;
 public Sprite townSprite,harborSprite;public Vector2 dockPoint=new(-32,0);public Vector2[] collectionPoints={new(-7,0),new(15,13),new(34,-9)};
 public float collectionRadius=17,purificationSeconds=65,dockRadius=5;public int smallFishBudget=14,sharkBudget=8,whaleBudget=2;
 public GameObject[] smallFish,sharks,whales;public Sprite[] flagFrames;
 public float Tank{get;private set;}public bool WaveCleared{get;private set;}public bool Delivered{get;private set;}
 public UnderwaterStoryUI Dialogue{get;private set;}public EnemySpawner Spawner{get;private set;}
 SpriteRenderer[] seaWaste;
 ShipController ship;ShipWeapons weapons;PlayerHealth health;ShipUIManager upgrades;PlayerExperience experience;Rigidbody2D body;
 bool announcedFull,departed,battleStarted,delivering,finalStarted;SpriteRenderer flag;float flagClock;float voyageTime;CampaignFlow flow;CampaignUI endingUI;
 public bool ReadyForDelivery=>Tank>=1&&(flow.BattleWave==0||WaveCleared);
 void Awake(){ship=GetComponent<ShipController>();weapons=GetComponent<ShipWeapons>();health=GetComponent<PlayerHealth>();body=GetComponent<Rigidbody2D>();if(health)health.CampaignProtected=true;}
 IEnumerator Start(){yield return null;flow=CampaignFlow.Instance;if(!CampaignFlow.IsManagedShip){enabled=false;yield break;}Time.timeScale=1;
  upgrades=FindFirstObjectByType<ShipUIManager>();experience=GetComponent<PlayerExperience>();Spawner=FindFirstObjectByType<EnemySpawner>();
  foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))Destroy(enemy.gameObject);
  if(Spawner){Spawner.StopCampaignWave();if(smallFish==null||smallFish.Length==0)smallFish=Spawner.allEnemyPrefabs.Where(p=>p&&p.name.Contains("Fish")).ToArray();}
  flow.RestoreShip(upgrades,experience,health);body.position=dockPoint+Vector2.right*2;body.linearVelocity=Vector2.zero;transform.position=new Vector3(body.position.x,body.position.y,transform.position.z);
  if(ship.vcam){ship.vcam.PreviousStateIsValid=false;ship.vcam.Lens.OrthographicSize=18;ship.vcam.ForceCameraPosition(new Vector3(body.position.x,body.position.y,-10),Quaternion.identity);}if(Camera.main){Camera.main.orthographicSize=18;Camera.main.transform.position=new Vector3(body.position.x,body.position.y,-10);}
  BuildHarbor();BuildSeaWaste();Dialogue=gameObject.AddComponent<UnderwaterStoryUI>();Dialogue.leftPanel=Resources.Load<Sprite>("Campaign/ship_dialogue_left");Dialogue.rightPanel=Resources.Load<Sprite>("Campaign/ship_dialogue_right");Dialogue.UseFont(Resources.Load<Font>("Campaign/PixelifySans"));
  var objective=Dialogue.transform.Find("Underwater story HUD/Objective").GetComponent<RectTransform>();objective.anchorMin=objective.anchorMax=objective.pivot=new Vector2(1,1);objective.anchoredPosition=new Vector2(-20,-190);objective.sizeDelta=new Vector2(550,140);objective.GetComponentInChildren<UnityEngine.UI.Text>().rectTransform.sizeDelta=new Vector2(514,120);
  Dialogue.SetObjective("");
  var flagObject=new GameObject("White ceasefire flag");flag=flagObject.AddComponent<SpriteRenderer>();flag.sortingOrder=1100;flagObject.SetActive(false);
  if(flow.Phase==CampaignPhase.IntroVoyage)Speak(new[]{L("Liman Görevlisi","Nehir kurudu. Depomuzda çok az su kaldı."),L("Kaptan","Denize açılıp tankları dolduracağım."),L("Liman Görevlisi","Dönüşünü bekliyoruz.")});
  else if(flow.Phase==CampaignPhase.SmallFishBattle)Speak(new[]{L("Liman Görevlisi","Depo yeniden azalıyor. Bir sefere daha ihtiyacımız var."),L("Kaptan","Hazırlanıyoruz.")});
  else if(flow.Phase==CampaignPhase.SharkBattle)Speak(new[]{L("Kaptan","Bu kez köpekbalıkları da geliyor."),L("Mürettebat","Gövdeyi ve topları güçlendirmeliyiz.")});
  else Speak(new[]{L("Kaptan","Balinalar… Onlar da gelmiş."),L("Mürettebat","Sistem bu yükü daha fazla kaldıramıyor.")});
 }
 UnderwaterStoryUI.Line L(string speaker,string text)=>new(speaker,speaker=="Balık Başkanı"?Resources.Load<Sprite>("Campaign/mayor_portrait"):null,text,speaker!="Kaptan");
 void Speak(UnderwaterStoryUI.Line[] lines,System.Action completed=null){SetLock(true);Dialogue.Show(lines,true,completed);}
 void SetLock(bool value){if(ship)ship.enabled=!value;if(weapons)weapons.enabled=!value&&battleStarted&&!finalStarted;if(health)health.CampaignProtected=value||finalStarted;if(value&&body)body.linearVelocity=Vector2.zero;}
 void Update(){if(!Dialogue||!flow)return;
  UpdateSeaWaste();
  bool upgradeOpen=upgrades.levelUpOverlay&&upgrades.levelUpOverlay.activeSelf;
  bool blocked=Dialogue.Blocking||flow.Transitioning||finalStarted||delivering;
  SetLock(blocked||upgradeOpen);if(Dialogue.Blocking)Time.timeScale=0;else if(!upgradeOpen&&!flow.Transitioning)Time.timeScale=1;
  if(Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame){if(Dialogue.Active){Dialogue.Advance();return;}if(!blocked&&!upgradeOpen&&ReadyForDelivery&&Vector2.Distance(body.position,dockPoint)<dockRadius)StartCoroutine(Deliver());}
  if(blocked||upgradeOpen)return;
  if(!departed&&body.position.x>dockPoint.x+10){departed=true;if(flow.BattleWave>0)BeginBattle();}
  if(departed&&!finalStarted){bool collecting=collectionPoints.Any(p=>Vector2.Distance(body.position,p)<collectionRadius)&&body.linearVelocity.sqrMagnitude>.15f;
   if(collecting){Tank=Mathf.Min(1,Tank+Time.deltaTime/purificationSeconds);ship.AddEnergy(ship.energyDrainRate*Time.deltaTime);}
   if(battleStarted&&Spawner&&Spawner.CampaignSpawned>=Spawner.CampaignBudget&&FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length==0){WaveCleared=true;Spawner.StopCampaignWave();if(flow.Phase==CampaignPhase.WhaleBattle){StartCoroutine(Finale());return;}}
  }
  if(ReadyForDelivery&&!announcedFull){announcedFull=true;Dialogue.Show(new[]{L("Kaptan","Tank doldu. Kasabaya dönmeliyim.")},false,null,true);}
  string objective=ReadyForDelivery?"Limana dön ve suyu teslim et.":$"Açık suya çık ve temiz su tankını doldur. {(Tank*100):0}%";
  if(flow.BattleWave>0&&!WaveCleared)objective+=$"\nSavaş {flow.BattleWave}/3 • {FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length} etkin saldırgan";
  Dialogue.SetObjective(objective);
  if(ReadyForDelivery){Dialogue.SetTarget(dockPoint,"Liman");Dialogue.SetHint(Vector2.Distance(body.position,dockPoint)<dockRadius?"E • Suyu teslim et":"");}
  else{var target=collectionPoints.OrderBy(p=>Vector2.Distance(body.position,p)).First();Dialogue.SetTarget(departed?target:dockPoint+Vector2.right*16,departed?"Su toplama alanı":"Açık deniz");Dialogue.SetHint("WASD • Yüzüş / dümen     Shift • Atılım     Space / HUD • XP / Enerji seçimi");}
 }
 void BuildSeaWaste(){seaWaste=new SpriteRenderer[flow.Pollution*12];for(int i=0;i<seaWaste.Length;i++){var sprite=Resources.Load<Sprite>("Abyss/waste_"+(i%6));var p=new Vector2(Mathf.Clamp(body.position.x+Random.Range(8,55),-27,75),Random.Range(-32,32));seaWaste[i]=Art("Floating voyage waste",null,sprite,p,Random.Range(.45f,.8f),-10);seaWaste[i].transform.rotation=Quaternion.Euler(0,0,Random.Range(0,360));var drift=seaWaste[i].gameObject.AddComponent<WarDebris>();drift.suspended=true;}}
 void UpdateSeaWaste(){if(seaWaste==null)return;foreach(var r in seaWaste)if(Vector2.Distance(body.position,r.transform.position)>48){var p=body.position+Random.insideUnitCircle.normalized*35;p.x=Mathf.Clamp(p.x,-27,75);p.y=Mathf.Clamp(p.y,-33,33);r.transform.position=p;r.GetComponent<WarDebris>().ResetMotion();}}
 void FixedUpdate(){if(!body)return;var p=body.position;p.x=Mathf.Clamp(p.x,-34,75);p.y=Mathf.Clamp(p.y,-35,35);if(p!=body.position)body.position=p;}
 void LateUpdate(){if(flag&&flag.gameObject.activeSelf&&flagFrames.Length>0){flagClock+=Time.unscaledDeltaTime;flag.sprite=flagFrames[(int)(flagClock*6)%flagFrames.Length];flag.transform.position=transform.TransformPoint(flagAttachment);flag.transform.rotation=Quaternion.identity;flag.transform.localScale=Vector3.one*flagHeight/flag.sprite.bounds.size.y;}}
 void BeginBattle(){battleStarted=true;GameObject[] pool=flow.BattleWave==1?smallFish:flow.BattleWave==2?sharks:whales;int budget=flow.BattleWave==1?smallFishBudget:flow.BattleWave==2?sharkBudget:whaleBudget;
  if(pool==null||pool.Length==0){Debug.LogError("Campaign enemy prefab pool is empty for wave "+flow.BattleWave);return;}
  Spawner.campaignSeaLeft=-27;Spawner.BeginCampaignWave(pool,budget);
  if(flow.BattleWave==1)Speak(new[]{L("Kaptan","Balıklar gemiye saldırıyor!"),L("Mürettebat","Tankları koruyun!")});
 }
 IEnumerator Deliver(){if(delivering||Delivered||!ReadyForDelivery)yield break;delivering=true;Delivered=true;SetLock(true);
  if(!flow.CompleteDelivery(upgrades,experience)){delivering=false;yield break;}
  if(flow.Phase==CampaignPhase.IntroVoyage)Speak(new[]{L("Liman Görevlisi","Bu su bugün bize yetecek."),L("Kaptan","Yarın yeniden çıkacağım.")});
  else if(flow.Phase==CampaignPhase.SmallFishBattle)Speak(new[]{L("Kaptan","Saldırı durdu… Ama suyun rengi değişiyor."),L("Liman Görevlisi","Şimdilik başka su kaynağımız yok.")});
  else Speak(new[]{L("Liman Görevlisi","Getirdiğiniz su azalıyor."),L("Kaptan","Filtreler daha sık tıkanıyor."),L("Liman Görevlisi","Bir sefer daha… Sonra bir çözüm bulmalıyız.")});
  while(Dialogue.Active)yield return null;
  upgrades.AddXP(10);while(upgrades.levelUpOverlay&&upgrades.levelUpOverlay.activeSelf)yield return null;
  flow.SaveShipProgress(upgrades,experience);flow.Go(flow.Phase==CampaignPhase.IntroVoyage?CampaignPhase.Announcement:flow.Phase==CampaignPhase.SmallFishBattle?CampaignPhase.SharkJourney:CampaignPhase.WhaleBattle);
 }
 IEnumerator Finale(){if(finalStarted)yield break;finalStarted=true;Dialogue.SetHint("");Dialogue.SetObjective("");Dialogue.SetTarget(null,"");var hud=FindFirstObjectByType<ShipHUDController>();if(hud)hud.gameObject.SetActive(false);foreach(var loot in FindObjectsByType<Loot>(FindObjectsSortMode.None))Destroy(loot.gameObject);SetLock(true);Spawner.StopCampaignWave();foreach(var turret in GetComponentsInChildren<Turret>())if(turret.audioSource)turret.audioSource.Stop();foreach(var bullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None))Destroy(bullet.gameObject);foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))Destroy(enemy.gameObject);
  Speak(new[]{L("Kaptan","Filtreler tıkandı. Arıtma durdu."),L("Mürettebat","Artık kasabaya su götüremiyoruz."),L("Kaptan","Ateşi kesin. Beyaz bayrağı çekin.")});while(Dialogue.Active)yield return null;
  flag.gameObject.SetActive(true);yield return new WaitForSecondsRealtime(1.2f);
  Speak(new[]{L("Kaptan","Savaşı durduralım. Daha fazla kayıp istemiyoruz."),L("Balık Başkanı","Bu su artık bizi de yaşatmıyor."),L("Kaptan","Arıtmayı durduracağız."),L("Balık Başkanı","Kasabayı terk edin. Denize daha fazla atık bırakmayın."),L("Kaptan","Kabul ediyoruz.")});while(Dialogue.Active)yield return null;
  yield return Dialogue.Fade(true);yield return new WaitForSecondsRealtime(.5f);yield return Dialogue.Fade(false);
  Speak(new[]{L("Anlatı","İnsanlar kasabadan ayrıldı."),L("Anlatı","Denizde kalanlar da yeni bir yuva aramak zorundaydı."),L("Anlatı","Savaş bitti. Kimse kazanmadı.")});while(Dialogue.Active)yield return null;
  Dialogue.SetObjective("");Dialogue.SetTarget(null,"");endingUI=new GameObject("Ending choices").AddComponent<CampaignUI>();var firstChoice=endingUI.Button("Main menu",endingUI.root,new Vector2(0,-310),"Ana Menü",()=>flow.ReturnMenu());endingUI.Button("Restart",endingUI.root,new Vector2(0,-440),"Yeniden Başla",()=>flow.NewGame());UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstChoice.gameObject);
 }
 public void BuildHarbor(){var ocean=FindFirstObjectByType<InfiniteOcean>();if(ocean)ocean.tint=Color.Lerp(Color.white,new Color(.65f,.76f,.65f),(flow?flow.Pollution:0)*.2f);
  if(GameObject.Find("Campaign coast and harbor"))return;var root=new GameObject("Campaign coast and harbor");
  var town=Art("Dry river town",root.transform,townSprite,new Vector2(-52,0),32,10);town.transform.rotation=Quaternion.Euler(0,0,90);
  var port=Art("Harbor",root.transform,harborSprite,new Vector2(-39,0),26,12);port.transform.rotation=Quaternion.Euler(0,0,90);
  Box(root.transform,"Coast collision",new Vector2(-54,0),new Vector2(20,100));
  Box(root.transform,"Harbor back wall",new Vector2(-43.5f,0),new Vector2(3,21));
  Box(root.transform,"Upper pier",new Vector2(-38.5f,9.2f),new Vector2(12.5f,3.6f));Box(root.transform,"Lower pier",new Vector2(-38.5f,-9.2f),new Vector2(12.5f,3.6f));
 }
 SpriteRenderer Art(string name,Transform parent,Sprite sprite,Vector2 at,float width,int order){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=at;var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;if(sprite)go.transform.localScale=Vector3.one*width/sprite.bounds.size.x;return r;}
 void Box(Transform parent,string name,Vector2 at,Vector2 size){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=at;go.AddComponent<BoxCollider2D>().size=size;}
}
}
