using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Abyss.Story;
// Runs after Cinemachine so the frozen final camera can ease in using unscaled time.
[DefaultExecutionOrder(20000)]
public sealed class VoyageEnding:VoyageUI {
 public bool FocusComplete{get;private set;}public bool DepartureCovered{get;private set;}
 Camera view;Vector3 focusPosition;float focusZoom;bool holdCamera;ShipVisitDialogue dialogue;Image cover;
 IEnumerator Start(){
  Time.timeScale=0;root.GetComponent<Canvas>().sortingOrder=10000;
  var controller=GetComponent<ShipController>();if(controller)controller.enabled=false;var weapons=GetComponent<ShipWeapons>();if(weapons)weapons.enabled=false;var body=GetComponent<Rigidbody2D>();if(body){body.linearVelocity=Vector2.zero;body.angularVelocity=0;}
  foreach(var spawner in FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)){spawner.StopAllCoroutines();spawner.enabled=false;}
  var uiManager=FindFirstObjectByType<ShipUIManager>();if(uiManager)uiManager.DismissForStory();var hud=FindFirstObjectByType<ShipHUDController>();if(hud)hud.gameObject.SetActive(false);
  dialogue=GetComponent<ShipVisitDialogue>();dialogue.CancelForEnding();
  var flag=new GameObject("White flag • ending only").AddComponent<SpriteRenderer>();flag.transform.SetParent(transform,false);flag.gameObject.AddComponent<VoyageFlagAnimation>();
  view=Camera.main;holdCamera=true;var start=view.transform.position;float zoom=view.orthographicSize;focusPosition=start;focusZoom=zoom;Vector3 target=new Vector3(transform.position.x,transform.position.y+.4f,start.z);float targetZoom=zoom*.82f;
  for(float t=0;t<1;t+=Time.unscaledDeltaTime/1.8f){float smooth=Mathf.SmoothStep(0,1,t);focusPosition=Vector3.Lerp(start,target,smooth);focusZoom=Mathf.Lerp(zoom,targetZoom,smooth);yield return null;}focusPosition=target;focusZoom=targetZoom;yield return new WaitForSecondsRealtime(.9f);FocusComplete=true;
  var captain=Resources.Load<Sprite>("Voyage/captain");var officer=Resources.Load<Sprite>("Voyage/harbor_officer");dialogue.UI.Show(new[]{new UnderwaterStoryUI.Line("Kaptan",captain,"Filtreler tıkandı. Arıtma durdu."),new UnderwaterStoryUI.Line("Liman Görevlisi",officer,"Artık kasabaya su götüremiyoruz."),new UnderwaterStoryUI.Line("Kaptan",captain,"Beyaz bayrağı çektik. Ateşi kesin."),new UnderwaterStoryUI.Line("Kaptan",captain,"Savaşı durduralım. Daha fazla kayıp istemiyoruz."),new UnderwaterStoryUI.Line("Balık Başkanı",null,"Bu su artık bizi de yaşatmıyor."),new UnderwaterStoryUI.Line("Kaptan",captain,"Arıtmayı durduracağız."),new UnderwaterStoryUI.Line("Balık Başkanı",null,"Buradan ayrılın. Denizi daha fazla kirletmeyin."),new UnderwaterStoryUI.Line("Kaptan",captain,"Kabul ediyoruz.")},true,()=>StartCoroutine(Departure()));
 }
 void LateUpdate(){if(holdCamera&&view){view.transform.position=focusPosition;view.orthographicSize=focusZoom;}}
 IEnumerator Departure(){dialogue.UI.Clear();cover=Picture("Opaque departure cover",root,Vector2.zero,new Vector2(6000,6000),"");cover.color=Color.clear;for(float t=0;t<1;t+=Time.unscaledDeltaTime/.55f){cover.color=new Color(0,0,0,t);yield return null;}cover.color=Color.black;DepartureCovered=true;var text=Label("Departure narration",root,Vector2.zero,new Vector2(1400,160),"İnsanlar kasabadan ayrıldı.",40);yield return new WaitForSecondsRealtime(2);Destroy(text.gameObject);ShowEnd();}
 IEnumerator ReturnToMenu(){yield return new WaitForSecondsRealtime(9);Time.timeScale=1;SceneManager.LoadScene("MainMenu");}
 void ShowEnd(){StartCoroutine(ReturnToMenu());var panel=Picture("Ending",root,Vector2.zero,new Vector2(6000,6000),"");panel.color=new Color(.015f,.045f,.06f,1);Label("Epilogue",root,new Vector2(0,130),new Vector2(1300,350),"İnsanlar kasabadan ayrıldı.\n\nDenizde kalanlar da yeni bir yuva aramak zorundaydı.\n\nSavaş bitti. Kimse kazanmadı.",36);Button("Menu",root,new Vector2(-280,-220),"Ana Menü",()=>{Time.timeScale=1;SceneManager.LoadScene("MainMenu");});Button("Restart",root,new Vector2(280,-220),"Yeniden Başla",()=>StorySceneJourney.Instance.NewGame());}
}
