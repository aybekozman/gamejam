using UnityEngine;
using UnityEngine.EventSystems;
public sealed class VoyageJoystick:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler,IInitializePotentialDragHandler {
 public RectTransform knob,visual;public float travel=68;int pointer=int.MinValue;RectTransform area;Vector2 center;Vector3 restPosition;bool restCaptured;
 void Awake(){area=(RectTransform)transform;}
 void Start(){CaptureRest();}
 void CaptureRest(){if(!restCaptured&&visual){restPosition=visual.localPosition;restCaptured=true;}}
 public void OnInitializePotentialDrag(PointerEventData e){e.useDragThreshold=false;}
 public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;CaptureRest();pointer=e.pointerId;if(RectTransformUtility.ScreenPointToLocalPointInRectangle(area,e.position,e.pressEventCamera,out var point)){center=point;if(visual)visual.localPosition=point;}UpdateStick(e);}
 public void OnDrag(PointerEventData e){if(e.pointerId==pointer)UpdateStick(e);}
 void UpdateStick(PointerEventData e){if(RectTransformUtility.ScreenPointToLocalPointInRectangle(area,e.position,e.pressEventCamera,out var point)){var value=Vector2.ClampMagnitude((point-center)/travel,1);if(value.magnitude<.1f)value=Vector2.zero;knob.anchoredPosition=value*travel;VoyageTouchInput.SetMove(value,true);}}
 public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer)Release();}
 public void Cancel()=>Release();
 void Release(){pointer=int.MinValue;if(visual&&restCaptured)visual.localPosition=restPosition;if(knob)knob.anchoredPosition=Vector2.zero;VoyageTouchInput.SetMove(Vector2.zero,false);}
 void OnDisable(){Release();}
}
