using UnityEngine;
using TMPro;

/// <summary>
/// DEVRE DIŞI BIRAKILDI: UI yönetimi tamamen ShipUIManager.cs üzerinden yapılmaktadır.
/// Editör scriptlerinde derleme hatası (CS1061) oluşmaması için yalnızca değişken tanımları pasif olarak tutulmaktadır.
/// </summary>
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
    public TMP_FontAsset hudFont;

    public void RefreshAll()
    {
        // Devre dışı: Tüm UI güncellemeleri ShipUIManager tarafından yönetilir.
    }

    /*
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
    void LateUpdate() { }
    */
}
