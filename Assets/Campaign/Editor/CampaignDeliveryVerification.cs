using System.IO;
namespace IntegratedCampaign.EditorTools {
// The former delivery tests are intentionally replaced, not treated as current evidence.
public static class CampaignDeliveryVerification {
 public static void AssertRemovedMechanics(){string s=File.ReadAllText("Assets/Campaign/ShipCampaignDirector.cs");foreach(string obsolete in new[]{"ReadyForDelivery","purificationSeconds","CompleteDelivery","collectionPoints","Tank{"})if(s.Contains(obsolete))throw new System.Exception("Cancelled mechanic remains: "+obsolete);}
}
}
