namespace IntegratedCampaign.EditorTools {
public static class CampaignPreferenceVerification {
 public static bool Matches(float master,float ambience,float effects)=>UnityEngine.Mathf.Approximately(UnityEngine.AudioListener.volume,master)&&UnityEngine.Mathf.Approximately(CampaignFlow.AmbiencePreference,ambience)&&UnityEngine.Mathf.Approximately(CampaignFlow.EffectsPreference,effects);
}
}
