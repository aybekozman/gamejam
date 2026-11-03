using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Abyss;
public sealed class VoyagePauseMenu:VoyageUI {
 public static bool IsOpen{get;private set;}GameObject panel;float savedTime;ShipController ship;ShipWeapons weapons;FishPilot fish;bool shipEnabled,weaponsEnabled,fishLocked;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){IsOpen=false;}
 void Start(){root.GetComponent<Canvas>().sortingOrder=8000;panel=Picture("Pause panel",root,Vector2.zero,new Vector2(1050,950),"settings").gameObject;Label("Pause title",panel.transform,new Vector2(0,370),new Vector2(800,60),"Duraklatıldı",40);Button("Resume",panel.transform,new Vector2(0,240),"Devam Et",Close);VoyageSoundControls.Add(this,panel.transform,105);Button("Menu",panel.transform,new Vector2(0,-260),"Ana Menü",()=>{Close();Time.timeScale=1;SceneManager.LoadScene("MainMenu");});Button("Exit",panel.transform,new Vector2(0,-390),"Çıkış",Quit);panel.SetActive(false);}
 void Update(){if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&StorySceneJourney.Instance&&!StorySceneJourney.Instance.IsTransitioning&&StorySceneJourney.Instance.Stage!=JourneyStage.Finale){if(IsOpen)Close();else Open();}}
 public void Open(){if(IsOpen)return;savedTime=Time.timeScale;ship=FindFirstObjectByType<ShipController>();weapons=FindFirstObjectByType<ShipWeapons>();fish=FindFirstObjectByType<FishPilot>();shipEnabled=ship&&ship.enabled;weaponsEnabled=weapons&&weapons.enabled;fishLocked=fish&&fish.ControlsLocked;if(ship)ship.enabled=false;if(weapons)weapons.enabled=false;if(fish)fish.ControlsLocked=true;IsOpen=true;Time.timeScale=0;panel.SetActive(true);EventSystem.current.SetSelectedGameObject(panel.GetComponentInChildren<UnityEngine.UI.Button>().gameObject);}
 public void Close(){if(!IsOpen)return;panel.SetActive(false);if(ship)ship.enabled=shipEnabled;if(weapons)weapons.enabled=weaponsEnabled;if(fish)fish.ControlsLocked=fishLocked;Time.timeScale=savedTime;IsOpen=false;}
 void OnDestroy(){IsOpen=false;}
 void Quit(){
 #if UNITY_EDITOR
 UnityEditor.EditorApplication.isPlaying=false;
 #else
 Application.Quit();
 #endif
 }
}
