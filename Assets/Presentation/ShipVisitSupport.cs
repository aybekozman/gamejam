using UnityEngine;
// Visit checkpoint retries and draw ordering only; no changes to weapon/energy balance.
public sealed class ShipVisitSupport:MonoBehaviour {
 ShipController ship;PlayerHealth health;ShipVisitDialogue dialogue;bool retry;
 void Start(){ship=GetComponent<ShipController>();health=GetComponent<PlayerHealth>();dialogue=GetComponent<ShipVisitDialogue>();var weapons=GetComponent<ShipWeapons>();if(weapons)foreach(var turret in weapons.allTurrets)if(turret)foreach(var sprite in turret.GetComponentsInChildren<SpriteRenderer>(true))sprite.sortingOrder=sprite==turret.flashRenderer?1120:1100;}
 void LateUpdate(){if(retry||!ship||!health||!dialogue||!dialogue.ReadyForGameplay||StorySceneJourney.Instance.IsTransitioning||StorySceneJourney.Instance.Stage==JourneyStage.Finale)return;if(health.IsDead||ship.currentEnergy<=0){retry=true;StorySceneJourney.Instance.RetryShip();}}
}
