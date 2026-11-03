using UnityEngine;
namespace Abyss {
public sealed class SurfaceSplash:MonoBehaviour {
 static Sprite pixel;Vector2 velocity;float age;SpriteRenderer visual;
 public static void Emit(Vector2 at,int count){if(!pixel){var texture=new Texture2D(1,1);texture.SetPixel(0,0,Color.white);texture.filterMode=FilterMode.Point;texture.Apply();pixel=Sprite.Create(texture,new Rect(0,0,1,1),Vector2.one*.5f,1);}for(int i=0;i<count;i++){var go=new GameObject("Surface splash droplet");go.transform.position=at+Vector2.right*Random.Range(count>20?-.5f:-.25f,count>20?.5f:.25f);go.transform.localScale=new Vector3(Random.Range(.04f,.09f),Random.Range(.08f,.16f),1);var r=go.AddComponent<SpriteRenderer>();r.sprite=pixel;r.color=new Color(.65f,.95f,1,.95f);r.sortingOrder=24;var drop=go.AddComponent<SurfaceSplash>();drop.visual=r;drop.velocity=new Vector2(Random.Range(-2.6f,2.6f),Random.Range(1.2f,count>20?5.4f:3.2f));}}
 void Update(){age+=Time.deltaTime;velocity.y-=12*Time.deltaTime;transform.position+=(Vector3)(velocity*Time.deltaTime);var color=visual.color;color.a=Mathf.Clamp01(1-age/.8f);visual.color=color;if(age>.8f)Destroy(gameObject);}
}
}
