using UnityEngine;
public sealed class VoyageFlagAnimation:MonoBehaviour {
 SpriteRenderer visual;Sprite[] frames;
 void Awake(){visual=GetComponent<SpriteRenderer>();visual.sortingOrder=2000;frames=new Sprite[4];for(int i=0;i<4;i++)frames[i]=Resources.Load<Sprite>("Voyage/flag_"+i);visual.sharedMaterial=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));var shipVisual=transform.parent.GetComponentInChildren<SpriteRenderer>();if(shipVisual)visual.sortingLayerID=shipVisual.sortingLayerID;var scale=transform.parent.lossyScale;transform.localScale=new Vector3(.64f/scale.x,.64f/scale.y,1);}
 void Update(){visual.sprite=frames[(int)(Time.unscaledTime*6)%4];transform.rotation=Quaternion.identity;transform.position=transform.parent.position+transform.parent.up*2.5f+transform.parent.right*.5f;}
}

