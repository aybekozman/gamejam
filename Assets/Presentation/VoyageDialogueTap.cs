using UnityEngine;using UnityEngine.EventSystems;using Abyss.Story;
public sealed class VoyageDialogueTap:MonoBehaviour,IPointerClickHandler {
 public UnderwaterStoryUI owner;
 public void OnPointerClick(PointerEventData e){if(owner&&owner.Active&&!VoyagePauseMenu.IsOpen)owner.Advance();}
}
