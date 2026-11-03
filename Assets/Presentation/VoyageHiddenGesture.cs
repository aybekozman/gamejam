using System.Collections.Generic;using UnityEngine;using UnityEngine.EventSystems;using UnityEngine.InputSystem;using UnityEngine.SceneManagement;using Abyss.Story;
// Four short empty-space taps, then a fifth empty-space hold. No UI feedback is shown.
public sealed class VoyageHiddenGesture:MonoBehaviour {
 int taps,owner=int.MinValue;float downAt,lastUp;Vector2 start,position;bool fifth;readonly List<RaycastResult> hits=new();
 bool ContextAllowed(){var flow=StorySceneJourney.Instance;if(!flow||flow.IsTransitioning||VoyagePauseMenu.IsOpen)return false;string path=SceneManager.GetActiveScene().path;if(path!=StorySceneJourney.UnderwaterPath&&path!=StorySceneJourney.ShipPath)return false;if(Time.timeScale<=0&&flow.Stage!=JourneyStage.Finale){var ui=FindFirstObjectByType<UnderwaterStoryUI>();if(!ui||!ui.Active)return false;}return true;}
 bool OverUI(Vector2 point){if(!EventSystem.current)return false;hits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);foreach(var hit in hits){if(hit.module is UnityEngine.UIElements.PanelRaycaster && Time.timeScale>0)continue;var text=hit.gameObject.GetComponent<UnityEngine.UI.Text>();if(text&&string.IsNullOrWhiteSpace(text.text))continue;var tmp=hit.gameObject.GetComponent<TMPro.TMP_Text>();if(tmp&&string.IsNullOrWhiteSpace(tmp.text))continue;return true;}return false;}
 void Reset(){taps=0;owner=int.MinValue;fifth=false;}
 void Down(int id,Vector2 point,int count){if(count>1||!ContextAllowed()||OverUI(point)){Reset();return;}if(owner!=int.MinValue){Reset();return;}if(Time.unscaledTime-lastUp>.55f)taps=0;owner=id;start=position=point;downAt=Time.unscaledTime;fifth=taps==4;}
 void Move(int id,Vector2 point){if(owner!=id)return;position=point;if(Vector2.Distance(start,point)>Mathf.Max(20,Screen.height*.03f)||OverUI(point))Reset();}
 void Up(int id,Vector2 point,bool canceled){if(owner!=id)return;if(canceled||OverUI(point)){Reset();return;}float held=Time.unscaledTime-downAt;if(fifth||held>.3f){Reset();return;}taps++;lastUp=Time.unscaledTime;owner=int.MinValue;}
 void Update(){if(!ContextAllowed()){Reset();return;}if(owner==int.MinValue&&Time.unscaledTime-lastUp>.55f)taps=0;bool touchFrame=false;var screen=Touchscreen.current;if(screen!=null){int count=0;foreach(var t in screen.touches)if(t.press.isPressed)count++;if(count>1)Reset();foreach(var t in screen.touches){int id=t.touchId.ReadValue();var p=t.position.ReadValue();if(t.press.wasPressedThisFrame){touchFrame=true;Down(id,p,count);}if(t.press.isPressed){touchFrame=true;Move(id,p);}if(t.press.wasReleasedThisFrame){touchFrame=true;Up(id,p,t.phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled);}}}
 if(!touchFrame&&Mouse.current!=null){var mouse=Mouse.current;var p=mouse.position.ReadValue();if(mouse.leftButton.wasPressedThisFrame)Down(-1,p,1);if(mouse.leftButton.isPressed)Move(-1,p);if(mouse.leftButton.wasReleasedThisFrame)Up(-1,p,false);}
 if(fifth&&owner!=int.MinValue&&Time.unscaledTime-downAt>=1){Reset();VoyageTouchInput.Reset();StorySceneJourney.Instance.SkipCurrentPart();}}
 void OnDisable(){Reset();}void OnApplicationFocus(bool focused){if(!focused)Reset();}
}

