using System.Collections;
using Abyss.Story;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Entry/exit text only; no combat or purification tuning.</summary>
public sealed class ShipVisitDialogue:MonoBehaviour {
 public bool ReadyForGameplay{get;private set;}
 public UnderwaterStoryUI UI{get;private set;}
 ShipController ship;ShipWeapons weapons;bool controllerEnabled,weaponsEnabled;float timeBefore;
 public void Open(JourneyStage stage,bool journey){
  UI=gameObject.AddComponent<UnderwaterStoryUI>();UI.unscaledText=true;UI.leftPanel=Resources.Load<Sprite>("Voyage/ship_dialogue_left");UI.rightPanel=Resources.Load<Sprite>("Voyage/ship_dialogue_right");UI.UseFont(Resources.Load<Font>("Voyage/PixelifySans"));UI.SetObjective("");UI.SetTarget(null,"");UI.SetHint("");
  foreach(var canvas in UI.GetComponentsInChildren<Canvas>(true))if(canvas.name=="Underwater story HUD"){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=5000;}
  ship=GetComponent<ShipController>();weapons=GetComponent<ShipWeapons>();
  var lines=stage==JourneyStage.ShipBattle&&journey?new[]{L("Kaptan","Balıklar gemiye saldırıyor!"),L("Liman Görevlisi","Gövdeyi koruyun!")}:new[]{L("Liman Görevlisi","Nehir kurudu. Kasabanın suyu tükeniyor."),L("Kaptan","Deniz suyunu arıtarak depoyu dolduracağız."),L("Liman Görevlisi","Şimdilik tek umudumuz bu gemi.")};
  if(stage==JourneyStage.SharkBattle)lines=new[]{L("Liman Görevlisi","Köpekbalıkları da yaklaşıyor."),L("Kaptan","Hazır olun.")};
  if(stage==JourneyStage.WhaleBattle)lines=new[]{L("Kaptan","Balinalar da gelmiş…"),L("Liman Görevlisi","Bu savaş hepimizi tüketiyor.")};
  // Scene journey completes its initialization before the first dialogue pauses gameplay.
  StartCoroutine(Begin(lines));
 }
 UnderwaterStoryUI.Line L(string name,string text)=>new(name,Resources.Load<Sprite>(name=="Kaptan"?"Voyage/captain":"Voyage/harbor_officer"),text,name!="Kaptan");
 public void CancelForEnding(){StopAllCoroutines();if(UI)UI.Clear();ReadyForGameplay=false;}
 public void WhalesArrive(System.Action completed){StartCoroutine(Begin(new[]{L("Kaptan","Balinalar da saldırmaya başladı!"),L("Liman Görevlisi","Bu savaş hepimizi tüketiyor.")},completed));}
 public void CompleteVisit(JourneyStage stage,System.Action completed){
  if(stage==JourneyStage.FirstShip){Departure(completed);return;}
  if(stage==JourneyStage.SharkBattle||stage==JourneyStage.WhaleBattle){completed?.Invoke();return;}
  StartCoroutine(Begin(stage==JourneyStage.ShipBattle?new[]{L("Kaptan","Saldırı durdu… Ama suyun rengi değişiyor."),L("Liman Görevlisi","Şimdilik başka su kaynağımız yok.")}:new[]{L("Liman Görevlisi","Getirdiğiniz su azalıyor."),L("Kaptan","Filtreler daha sık tıkanıyor."),L("Liman Görevlisi","Bir sefer daha… Sonra bir çözüm bulmalıyız.")},completed));
 }
 public void Departure(System.Action completed){StartCoroutine(Begin(new[]{L("Liman Görevlisi","Bu su bugün bize yetecek."),L("Kaptan","Yarın yeniden çıkacağım.")},completed));}
 IEnumerator Begin(UnderwaterStoryUI.Line[] lines,System.Action completed=null){yield return null;while(VoyagePauseMenu.IsOpen||(StorySceneJourney.Instance&&StorySceneJourney.Instance.IsTransitioning))yield return null;ReadyForGameplay=false;timeBefore=Time.timeScale;controllerEnabled=ship.enabled;weaponsEnabled=weapons&&weapons.enabled;ship.enabled=false;if(weapons)weapons.enabled=false;Time.timeScale=0;UI.Show(lines,true,()=>{ship.enabled=controllerEnabled;if(weapons)weapons.enabled=weaponsEnabled;Time.timeScale=timeBefore;ReadyForGameplay=true;completed?.Invoke();});}
 void Update(){if(!VoyagePauseMenu.IsOpen&&UI&&UI.Active&&Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)UI.Advance();}
}
