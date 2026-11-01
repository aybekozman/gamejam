using UnityEngine;
using TMPro;

/// <summary>Read-only HUD. Gameplay state belongs to the referenced ship components.</summary>
public sealed class ShipHUDController : MonoBehaviour
{
    public ShipController ship;
    public PlayerHealth health;
    public PlayerEnergy energy;
    public PlayerExperience experience;
    public UnityEngine.UI.Image healthFill, energyFill, xpFill;
    public UnityEngine.UI.Image[] dashFills;
    public TMP_Text healthText, energyText, xpText, levelText;
    public RectTransform compassNeedle, safeArea;
    [Tooltip("Temporary project font; replace this reference when the final font is supplied.")]
    public TMP_FontAsset hudFont;
    Rect previousSafeArea;
    int previousWidth, previousHeight;
    void OnEnable()
    {
        if (health) health.HealthChanged += SetHealth;
        if (energy) energy.EnergyChanged += SetEnergy;
        if (experience) experience.ExperienceChanged += SetExperience;
        RefreshAll();
    }
    void Start() { RefreshAll(); }
    void OnDisable()
    {
        if (health) health.HealthChanged -= SetHealth;
        if (energy) energy.EnergyChanged -= SetEnergy;
        if (experience) experience.ExperienceChanged -= SetExperience;
    }
    public void RefreshAll()
    {
        if (health) SetHealth(health.CurrentHealth,health.MaxHealth);
        if (energy) SetEnergy(energy.CurrentEnergy,energy.MaxEnergy);
        if (experience) SetExperience(experience.CurrentXP,experience.RequiredXP,experience.PlayerLevel);
        if (hudFont) foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            if(text.font!=hudFont) { text.font=hudFont; text.fontSharedMaterial=hudFont.material; }
        RefreshMotion();
    }
    void SetHealth(float current,float maximum)
    {
        healthFill.fillAmount = maximum > 0 ? Mathf.Clamp01(current/maximum) : 0;
        healthText.SetText("{0:0} / {1:0}",current,maximum);
    }
    void SetEnergy(float current,float maximum)
    {
        energyFill.fillAmount = maximum > 0 ? Mathf.Clamp01(current/maximum) : 0;
        energyText.SetText("{0:0} / {1:0}",current,maximum);
    }
    void SetExperience(long current,int required,int level)
    {
        xpFill.fillAmount = required > 0 ? Mathf.Clamp01((float)current/required) : 0;
        var culture=System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
        xpText.text = current.ToString("N0",culture) + " / " + required.ToString("N0",culture);
        levelText.SetText("{0}",level);
    }
    void LateUpdate()
    {
        RefreshMotion();
        if (safeArea && (Screen.width!=previousWidth || Screen.height!=previousHeight || Screen.safeArea!=previousSafeArea))
        {
            previousWidth=Screen.width; previousHeight=Screen.height; previousSafeArea=Screen.safeArea;
            safeArea.anchorMin = previousSafeArea.position/new Vector2(Screen.width,Screen.height);
            safeArea.anchorMax = (previousSafeArea.position+previousSafeArea.size)/new Vector2(Screen.width,Screen.height);
            safeArea.offsetMin=safeArea.offsetMax=Vector2.zero;
        }
    }
    void RefreshMotion()
    {
        if (!ship) return;
        if (dashFills!=null) for(int i=0;i<dashFills.Length;i++) if(dashFills[i]) dashFills[i].fillAmount=ship.GetDashProgress(i);
        // World +Y is north, and both the ship and the supplied red needle point up at Z=0.
        // uGUI uses the same positive counter-clockwise rotation as the world transform.
        if (compassNeedle) compassNeedle.localRotation=Quaternion.Euler(0,0,ship.transform.eulerAngles.z);
    }
}
