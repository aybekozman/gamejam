#if UNITY_EDITOR
using UnityEngine;using System.Collections;using System.IO;using UnityEngine.UI;
namespace IntegratedCampaign {
public sealed class CampaignPreferenceVerification:MonoBehaviour {
 IEnumerator Start(){DontDestroyOnLoad(gameObject);var f=CampaignFlow.Instance;f.ReturnMenu();yield return new WaitForSecondsRealtime(3);var menu=FindFirstObjectByType<CampaignMenu>();menu.SendMessage("OpenSettings",true);var sliders=menu.GetComponentsInChildren<Slider>();float master=PlayerPrefs.GetFloat("Sound.Master",.85f),ambient=CampaignFlow.AmbiencePreference,effects=CampaignFlow.EffectsPreference;sliders[0].value=.37f;sliders[1].value=.42f;sliders[2].value=.24f;f.NewGame();yield return new WaitForSecondsRealtime(3);bool pass=Mathf.Abs(AudioListener.volume-.37f)<.001f&&Mathf.Abs(CampaignFlow.AmbiencePreference-.42f)<.001f&&Mathf.Abs(CampaignFlow.EffectsPreference-.24f)<.001f;PlayerPrefs.SetFloat("Sound.Master",master);PlayerPrefs.SetFloat("Sound.Ambience",ambient);PlayerPrefs.SetFloat("Sound.Effects",effects);PlayerPrefs.Save();CampaignFlow.ApplyAudioPreferences();File.WriteAllText("Tools/CampaignIntegration/preferences-verification.txt",(pass?"PASS":"FAIL")+" interactive sliders persist across New Game and scene changes; original preferences restored\n");f.ReturnMenu();}
}
}
#endif
