using System.Collections.Generic;
using UnityEngine;
// Renewable exploration pickups for the non-combat visit; they always grant energy.
public sealed class PeacefulEnergyField:MonoBehaviour {
 readonly List<Loot> pickups=new();System.Random random=new(725);float nextSpawn;
 void Start(){for(int i=0;i<20;i++)Spawn(i);}
 void Spawn(int index){float angle=(float)random.NextDouble()*Mathf.PI*2;float radius=14+(float)random.NextDouble()*25;Vector2 at=VoyageHarbor.NearestWater((Vector2)transform.position+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);var go=new GameObject("Sea energy pickup");go.layer=LayerMask.NameToLayer("Loot");go.transform.position=at;go.AddComponent<CircleCollider2D>().isTrigger=true;var loot=go.AddComponent<Loot>();loot.energyOnly=true;loot.energyValue=25;loot.uiManager=GetComponent<ShipController>().uiManager;loot.shipController=GetComponent<ShipController>();pickups.Add(loot);}
 void Update(){if(Time.timeScale<=0||StorySceneJourney.Instance.Stage!=JourneyStage.FirstShip)return;pickups.RemoveAll(p=>!p);foreach(var pickup in pickups)if(Vector2.Distance(pickup.transform.position,transform.position)>55){float angle=(float)random.NextDouble()*Mathf.PI*2;pickup.transform.position=VoyageHarbor.NearestWater((Vector2)transform.position+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*28);}if(pickups.Count<20&&(nextSpawn-=Time.deltaTime)<=0){Spawn(pickups.Count);nextSpawn=1.4f;}}
 void OnDestroy(){foreach(var pickup in pickups)if(pickup)Destroy(pickup.gameObject);}
}
