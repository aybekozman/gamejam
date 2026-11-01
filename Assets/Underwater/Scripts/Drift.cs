using UnityEngine;
namespace Abyss {
public class Drift : MonoBehaviour {
 public Sprite[] frames;public float rate=4,amplitude=.3f,speed=.4f,phase;public Vector2 travel=new(1,.2f); public bool plant;Vector3 origin;SpriteRenderer sr;
 void Start(){origin=transform.position;sr=GetComponent<SpriteRenderer>();}
 void Update(){float t=Time.time*speed+phase;if(plant){transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t)*3);return;}var p=origin+new Vector3(Mathf.Sin(t)*travel.x,Mathf.Sin(t*.8f)*travel.y,0)*amplitude;transform.position=p;if(travel.x>.1f)sr.flipX=Mathf.Cos(t)<0;if(frames!=null&&frames.Length>0)sr.sprite=frames[(int)(Time.time*rate+phase)%frames.Length];}
}
}
