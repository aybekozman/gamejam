using UnityEngine;using UnityEngine.UI;
public sealed class VoyageSafeArea:MonoBehaviour {
 public static Rect? PreviewArea;Rect last;Vector2 size;RectTransform rect;
 public static RectTransform Create(RectTransform parent){var go=new GameObject("Mobile safe area",typeof(RectTransform),typeof(VoyageSafeArea));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;go.GetComponent<VoyageSafeArea>().Apply();return r;}
 void Awake(){rect=(RectTransform)transform;}
 void Update(){Apply();}
 void Apply(){if(!rect)rect=(RectTransform)transform;var area=PreviewArea??Screen.safeArea;var screen=new Vector2(Screen.width,Screen.height);if(screen.x<=0||screen.y<=0)return;if(last==area&&size==screen)return;last=area;size=screen;rect.anchorMin=area.min/screen;rect.anchorMax=area.max/screen;rect.offsetMin=rect.offsetMax=Vector2.zero;}
}
