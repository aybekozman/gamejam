using UnityEngine;
namespace Abyss {
public class Bubble : MonoBehaviour {
 float age,life,top,drift,scale,rise; SpriteRenderer sr;
 public static void Spawn(Sprite sprite,Vector3 pos,float top,float delay,Vector2 lifetime,Vector2 size,float riseSpeed,float sidewaysDrift){var go=new GameObject("Bubble");go.transform.position=pos;var b=go.AddComponent<Bubble>();b.sr=go.AddComponent<SpriteRenderer>();b.sr.sprite=sprite;b.sr.sortingOrder=25;b.life=Random.Range(lifetime.x,lifetime.y)+delay;b.age=-delay;b.top=top;b.drift=Random.Range(-sidewaysDrift,sidewaysDrift);b.rise=riseSpeed;b.scale=Random.Range(size.x,size.y);}
 void Update(){age+=Time.deltaTime;transform.position+=new Vector3(drift,rise,0)*Time.deltaTime;transform.localScale=Vector3.one*scale*Mathf.Clamp01(1-age/life);sr.color=new Color(1,1,1,.65f*Mathf.Clamp01(1-age/life));if(age>life||transform.position.y>=top)Destroy(gameObject);}
}
}
