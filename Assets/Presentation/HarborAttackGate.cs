using UnityEngine;
public sealed class HarborAttackGate:MonoBehaviour {
 public const float DepartureX=VoyageHarbor.CoastX+25;public bool HasDeparted{get;private set;}EnemySpawner producer;
 public void Configure(EnemySpawner value){producer=value;producer.enabled=false;}
 void Update(){if(HasDeparted||Time.timeScale<=0||!producer)return;var talk=GetComponent<ShipVisitDialogue>();if(!talk||!talk.ReadyForGameplay||Vector2.Distance(transform.position,new Vector2(VoyageHarbor.CoastX+8,0))<17)return;HasDeparted=true;producer.enabled=true;}
}
