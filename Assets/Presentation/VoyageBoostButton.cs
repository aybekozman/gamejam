using UnityEngine;using UnityEngine.EventSystems;using UnityEngine.UI;
public sealed class VoyageBoostButton:MonoBehaviour,IPointerDownHandler,IPointerUpHandler {
 public bool fish;public ShipController ship;int pointer=int.MinValue;Image image;
 void Awake(){image=GetComponent<Image>();}
 public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue||Time.timeScale<=0)return;pointer=e.pointerId;if(fish)VoyageTouchInput.SetFishBoost(true);else if(ship&&ship.enabled)ship.TryDash(VoyageTouchInput.StickActive?VoyageTouchInput.Move:Vector2.zero);image.color=new Color(.8f,.85f,.9f);}
 public void OnPointerUp(PointerEventData e){if(pointer==e.pointerId)Release();}
 public void Cancel()=>Release();
 void Release(){pointer=int.MinValue;if(fish)VoyageTouchInput.SetFishBoost(false);if(image)image.color=Color.white;}
 void OnDisable(){Release();}
}
