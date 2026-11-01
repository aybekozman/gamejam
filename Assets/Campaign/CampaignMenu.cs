using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;
namespace IntegratedCampaign {
public sealed class CampaignMenu:CampaignUI {
 public string gameTitle="";GameObject settings;SpriteRenderer background;
 void Start(){EnsureEventSystem();var camera=Camera.main;if(camera){camera.orthographic=true;camera.orthographicSize=10;var go=new GameObject("Menu ocean");background=go.AddComponent<SpriteRenderer>();background.sprite=Resources.Load<Sprite>("Campaign/menu_ocean");background.sharedMaterial=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));background.sortingOrder=-100;}
  var title=Image("Title plate",root,new Vector2(0,310),new Vector2(1100,185),Resources.Load<Sprite>("Campaign/title"));Label("Game title",title.transform,Vector2.zero,new Vector2(850,110),gameTitle,48);
  var first=Button("New Game",root,new Vector2(0,90),"Yeni Oyun",()=>CampaignFlow.Instance.NewGame());Button("Settings",root,new Vector2(0,-50),"Ayarlar",()=>OpenSettings(true));Button("Exit",root,new Vector2(0,-190),"Çıkış",Quit);
  settings=Image("Settings panel",root,Vector2.zero,new Vector2(1100,780),Resources.Load<Sprite>("Campaign/settings")).gameObject;Label("Settings title",settings.transform,new Vector2(0,290),new Vector2(800,70),"Ayarlar",40);
  var close=Image("Close settings",settings.transform,new Vector2(450,300),new Vector2(65,65),Resources.Load<Sprite>("Campaign/close"));close.gameObject.AddComponent<Button>().onClick.AddListener(()=>OpenSettings(false));Image("Sound icon",settings.transform,new Vector2(-400,290),new Vector2(60,60),Resources.Load<Sprite>("Campaign/sound")).raycastTarget=false;
  Slider("Ana ses", "Master",230);Slider("Ortam / Müzik","Ambience",70);Slider("Efektler","Effects",-90);Button("Back",settings.transform,new Vector2(0,-280),"Geri",()=>OpenSettings(false));settings.SetActive(false);EventSystem.current.SetSelectedGameObject(first.gameObject);
 }
 void Slider(string label,string key,float y){Label(label,settings.transform,new Vector2(0,y),new Vector2(850,50),label);var track=Image(label+" slider",settings.transform,new Vector2(0,y-58),new Vector2(620,55),Resources.Load<Sprite>("Campaign/slider"));var slider=track.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;var fillArea=Rect("Fill travel",track.transform,Vector2.zero,new Vector2(550,12));var fill=Image("Fill",fillArea,Vector2.zero,new Vector2(550,18),Resources.Load<Sprite>("Campaign/slider_fill"));fill.rectTransform.anchorMin=new Vector2(0,.5f);fill.rectTransform.anchorMax=new Vector2(1,.5f);fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;var area=Rect("Handle travel",track.transform,Vector2.zero,new Vector2(550,50));var handle=Image("Handle",area,Vector2.zero,new Vector2(46,50),Resources.Load<Sprite>("Campaign/slider_knob"));slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.value=PlayerPrefs.GetFloat("Sound."+key,key=="Master"?.85f:.8f);slider.onValueChanged.AddListener(value=>{PlayerPrefs.SetFloat("Sound."+key,value);PlayerPrefs.Save();CampaignFlow.ApplyAudioPreferences();});}
 void OpenSettings(bool open){settings.SetActive(open);foreach(Transform child in root)if(child.gameObject!=settings)child.gameObject.SetActive(!open);if(open)EventSystem.current.SetSelectedGameObject(settings.GetComponentInChildren<Slider>().gameObject);else EventSystem.current.SetSelectedGameObject(root.GetComponentsInChildren<Button>()[0].gameObject);}
 void Update(){if(UnityEngine.InputSystem.Keyboard.current!=null&&UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame&&settings.activeSelf)OpenSettings(false);if(background&&Camera.main){float h=Camera.main.orthographicSize*2+1,w=h*Camera.main.aspect+1;float scale=Mathf.Max(w/background.sprite.bounds.size.x,h/background.sprite.bounds.size.y);background.transform.localScale=Vector3.one*scale;background.transform.position=new Vector3(Mathf.Sin(Time.unscaledTime*.13f)*.15f,Mathf.Sin(Time.unscaledTime*.1f)*.1f,0);}}
 void Quit(){
 #if UNITY_EDITOR
 UnityEditor.EditorApplication.isPlaying=false;
 #else
 Application.Quit();
 #endif
 }
}
}
