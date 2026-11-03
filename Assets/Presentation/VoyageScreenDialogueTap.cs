using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Abyss.Story;
// Short screen taps advance dialogue. Movement, boost and menu touches retain their own actions.
public sealed class VoyageScreenDialogueTap:MonoBehaviour {
 readonly List<RaycastResult> hits=new();int owner=-1;Vector2 start;float downAt;UnderwaterStoryUI ui;
 bool ControlAt(Vector2 point){hits.Clear();if(!EventSystem.current)return false;EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);foreach(var h in hits)if(h.gameObject.GetComponentInParent<UnityEngine.UI.Button>()||h.gameObject.GetComponentInParent<VoyageJoystick>()||h.gameObject.GetComponentInParent<VoyageBoostButton>()||h.gameObject.GetComponentInParent<OrbRewardToggleView>())return true;return false;}
 void Down(int id,Vector2 p){if(owner!=-1||ControlAt(p))return;owner=id;start=p;downAt=Time.unscaledTime;}
 void Up(int id,Vector2 p){if(id!=owner)return;owner=-1;if(Time.unscaledTime-downAt>.45f||Vector2.Distance(start,p)>30||ControlAt(p))return;if(ui.Active)ui.Advance();else if(ui.ContinuePromptVisible){var story=FindFirstObjectByType<UnderwaterDayStory>();if(story)story.RequestInteraction();}}
 void Update(){if(!ui)ui=FindFirstObjectByType<UnderwaterStoryUI>();if(!ui||VoyagePauseMenu.IsOpen||StorySceneJourney.Instance.IsTransitioning||!ui.Active&&!ui.ContinuePromptVisible){owner=-1;return;}bool touch=false;if(Touchscreen.current!=null)foreach(var t in Touchscreen.current.touches){int id=t.touchId.ReadValue();var p=t.position.ReadValue();if(t.press.wasPressedThisFrame){touch=true;Down(id,p);}if(t.press.isPressed)touch=true;if(t.press.wasReleasedThisFrame){touch=true;if(t.phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled)owner=-1;else Up(id,p);}}if(!touch&&Mouse.current!=null){var m=Mouse.current;var p=m.position.ReadValue();if(m.leftButton.wasPressedThisFrame)Down(-2,p);if(m.leftButton.wasReleasedThisFrame)Up(-2,p);}}
 void OnDisable(){owner=-1;}
}
