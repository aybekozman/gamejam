using UnityEngine;
// Mode changes update every ground pickup so its icon matches the reward it will grant.
[RequireComponent(typeof(Loot))]
public sealed class ShipLootVisual:MonoBehaviour {
 Loot owner;SpriteRenderer icon;Sprite star,energy;bool last;bool initialized;
 public Sprite CurrentIcon=>icon?icon.sprite:null;
 void Awake(){owner=GetComponent<Loot>();star=Resources.Load<Sprite>("Voyage/loot_star");energy=Resources.Load<Sprite>("Voyage/loot_energy");var old=GetComponent<SpriteRenderer>();if(old)old.enabled=false;var go=new GameObject("Selected loot reward");go.transform.SetParent(transform,false);icon=go.AddComponent<SpriteRenderer>();icon.sortingOrder=9;Refresh();}
 void Update(){Refresh();}
 void Refresh(){if(!owner.uiManager)return;bool xp=!owner.energyOnly&&owner.uiManager.isLootingXP;if(initialized&&last==xp)return;initialized=true;last=xp;icon.sprite=xp?star:energy;icon.color=Color.white;if(icon.sprite){float size=Mathf.Max(icon.sprite.bounds.size.x,icon.sprite.bounds.size.y);float parentScale=Mathf.Max(.001f,Mathf.Abs(transform.lossyScale.x));icon.transform.localScale=Vector3.one*(1.4f/size/parentScale);}icon.transform.rotation=Quaternion.identity;}
}
