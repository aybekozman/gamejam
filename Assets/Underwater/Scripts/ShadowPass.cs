using UnityEngine;
namespace Abyss {
public class ShadowPass : MonoBehaviour {
 public Sprite[] sprites;float next;void Update(){next-=Time.deltaTime;if(next>0)return;next=Random.Range(7,12);var cam=Camera.main;float half=cam.orthographicSize*cam.aspect;bool right=Random.value>.5f;for(int i=0;i<3;i++){var go=new GameObject("Distant passing fish");var s=go.AddComponent<SpriteRenderer>();s.sprite=sprites[Random.Range(0,sprites.Length)];s.sortingOrder=-35;s.color=new Color(.3f,.65f,.75f,.28f);s.flipX=!right;go.transform.position=new Vector3(cam.transform.position.x+(right?-1:1)*(half+5+i),Mathf.Min(9,cam.transform.position.y+Random.Range(-4,4)),0);go.transform.localScale=Vector3.one*.5f;var m=go.AddComponent<PassingFish>();m.velocity=right?1.1f:-1.1f;}}
}
}
