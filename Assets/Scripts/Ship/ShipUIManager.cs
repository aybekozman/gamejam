using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ShipUIManager : MonoBehaviour
{
    [Header("Gemi Referansları (Manuel Atama)")]
    public Transform shipTransform;       // Pusulanın dönmesi için ana gemi Transform'u
    public ShipController shipController; // Enerji ve hız yönetimi için
    public ShipWeapons shipWeapons;       // Silah upgrade'leri için
    public PlayerHealth playerHealth;     // Can upgrade'leri için
    public LootMagnet lootMagnet;         // Mıknatıs upgrade'leri için

    [Header("Canvas (uGUI) Bar Dolgu Görselleri")]
    public Image healthFill;
    public Image dashFill;
    public Image energyFill;
    public Image xpFill;

    [Header("Canvas (uGUI) Bar Konteynerleri ve Pusula")]
    public GameObject xpBarContainer;
    public GameObject energyBarContainer;
    public RectTransform compass;
    public TextMeshProUGUI modeText;
    public Toggle modeToggle;

    [Header("Aktif Loot Modu (Taktiksel Toplama)")]
    public bool isLootingXP = true; // true = XP Topla, false = Enerji Topla
    [Tooltip("Tikliyse sadece aktif modun barı görünür; tiksizse hem XP hem Enerji barı aynı anda açık kalır.")]
    public bool toggleBarVisibility = true;

    [Header("Seviye ve Eksponansiyel XP Ayarları")]
    public int currentLevel = 1;
    public float currentXP = 0f;
    public float baseXP = 5f;
    public float xpGrowthMultiplier = 1.5f;
    public float xpGainMultiplier = 1f; // XPGain kartlarıyla artan çarpan
    public float nextLevelXP;

    [Header("60 Kartlık Roguelike Upgrade Havuzu (20 Özellik x 3 Tier)")]
    public List<UpgradeData> upgradePool = new List<UpgradeData>();

    [Header("Canvas (uGUI) Level Up Ekranı Elemanları")]
    public GameObject levelUpOverlay;
    public TextMeshProUGUI levelUpTitle;
    public TextMeshProUGUI levelUpSubtitle;
    public Button[] cardButtons = new Button[3];
    public Image[] cardTierBadges = new Image[3];
    public TextMeshProUGUI[] cardTierTexts = new TextMeshProUGUI[3];
    public Image[] cardIcons = new Image[3];
    public TextMeshProUGUI[] cardTitles = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] cardDescs = new TextMeshProUGUI[3];

    [Header("Dash Ayarları")]
    public float dashRegenRate = 20f;
    public float dashCostMultiplier = 1f;
    private float currentDash = 100f;
    private bool isDashing = false;

    // O an ekranda gösterilen 3 kartın verisi
    private readonly UpgradeData[] currentOfferedUpgrades = new UpgradeData[3];

    // Tier rozet renkleri
    private static readonly Color Tier1BadgeColor = new Color(0.43f, 0.25f, 0.14f, 1f);
    private static readonly Color Tier2BadgeColor = new Color(0.04f, 0.23f, 0.29f, 1f);
    private static readonly Color Tier3BadgeColor = new Color(0.23f, 0.04f, 0.30f, 1f);

    void Awake()
    {
        // Statik mermi bonuslarını oyun başında sıfırla
        Bullet.bonusDamage = 0f;
        Bullet.bonusSpeed = 0f;
        Bullet.bonusKnockback = 0f;

        CalculateNextLevelXP();
        CacheShipComponentsFromTransform();
        EnsureDefaultUpgradesIfEmpty();
    }

    void OnEnable()
    {
        // Kart butonlarının tıklama olaylarını bağla
        if (cardButtons != null)
        {
            for (int i = 0; i < cardButtons.Length && i < 3; i++)
            {
                int cardIndex = i;
                if (cardButtons[i] != null)
                {
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnCardClicked(cardIndex));
                }
            }
        }

        // uGUI Toggle atanmışsa dinleyici ekle
        if (modeToggle != null)
        {
            modeToggle.onValueChanged.RemoveListener(OnModeToggleChanged);
            modeToggle.onValueChanged.AddListener(OnModeToggleChanged);
        }

        UpdateLootModeUI();

        if (levelUpOverlay != null)
        {
            levelUpOverlay.gameObject.SetActive(false);
        }

        // Başlangıç bar değerlerini ayarla
        UpdateBar(healthFill, 100f);
        UpdateBar(dashFill, 100f);
        UpdateBar(energyFill, 100f);
        UpdateXPBarUI();
    }

    void OnDisable()
    {
        if (modeToggle != null)
        {
            modeToggle.onValueChanged.RemoveListener(OnModeToggleChanged);
        }
    }

    private void OnModeToggleChanged(bool newValue)
    {
        isLootingXP = newValue;
        UpdateLootModeUI();
    }

    void Update()
    {
        // Space tuşuna basıldığında Aktif Loot Modunu (XP <-> ENERJİ) tersine çevir
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && Time.timeScale > 0f)
        {
            ToggleLootMode();
        }

        // 1. Pusula: Geminin Z rotasyonunu pusulaya aktar
        if (shipTransform != null && compass != null)
        {
            compass.localRotation = Quaternion.Euler(0f, 0f, -shipTransform.eulerAngles.z);
        }

        // 2. Dash Bar Yenilenmesi
        if (!isDashing && currentDash < 100f)
        {
            currentDash = Mathf.Min(100f, currentDash + dashRegenRate * Time.deltaTime);
            UpdateBar(dashFill, currentDash);
        }
    }

    public void ToggleLootMode()
    {
        isLootingXP = !isLootingXP;
        UpdateLootModeUI();
    }

    private void UpdateLootModeUI()
    {
        if (modeToggle != null)
        {
            modeToggle.SetIsOnWithoutNotify(isLootingXP);
        }

        if (modeText != null)
        {
            modeText.text = isLootingXP ? "Toplanan: XP" : "Toplanan: ENERJİ";
        }

        // Bar konteynerlerinin görünürlüğünü gameObject.SetActive ile yönet
        if (toggleBarVisibility)
        {
            if (xpBarContainer != null) xpBarContainer.gameObject.SetActive(isLootingXP);
            if (energyBarContainer != null) energyBarContainer.gameObject.SetActive(!isLootingXP);
        }
        else
        {
            if (xpBarContainer != null) xpBarContainer.gameObject.SetActive(true);
            if (energyBarContainer != null) energyBarContainer.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Ganimet toplandığında aktif moda (isLootingXP) göre SADECE XP veya SADECE Enerji ekler.
    /// </summary>
    public void CollectLoot(float xpAmount, float energyAmount)
    {
        if (isLootingXP)
        {
            AddXP(xpAmount);
        }
        else
        {
            CacheShipComponentsFromTransform();
            if (shipController != null)
            {
                shipController.AddEnergy(energyAmount);
            }
        }
    }

    private void CacheShipComponentsFromTransform()
    {
        // Sahne taraması (FindAnyObjectByType) KULLANMADAN, sadece atanmış shipTransform üzerinden bileşenleri al
        if (shipTransform != null)
        {
            if (shipController == null) shipController = shipTransform.GetComponent<ShipController>();
            if (shipWeapons == null) shipWeapons = shipTransform.GetComponent<ShipWeapons>();
            if (playerHealth == null) playerHealth = shipTransform.GetComponent<PlayerHealth>();
            if (lootMagnet == null) lootMagnet = shipTransform.GetComponent<LootMagnet>();
        }
    }

    // --- EKSPONANSİYEL XP VE SEVİYE ATLAMA SİSTEMİ ---
    private void CalculateNextLevelXP()
    {
        nextLevelXP = Mathf.Round(baseXP * Mathf.Pow(xpGrowthMultiplier, currentLevel));
    }

    public void AddXP(float amount)
    {
        currentXP += amount * xpGainMultiplier;

        if (currentXP >= nextLevelXP)
        {
            LevelUp();
        }
        else
        {
            UpdateXPBarUI();
        }
    }

    private void LevelUp()
    {
        currentXP -= nextLevelXP;
        currentLevel++;
        CalculateNextLevelXP();
        UpdateXPBarUI();

        Time.timeScale = 0f;
        ShowLevelUpScreen();
    }

    private void UpdateXPBarUI()
    {
        float percentage = nextLevelXP > 0f ? (currentXP / nextLevelXP) * 100f : 0f;
        UpdateBar(xpFill, percentage);
    }

    public void UpdateEnergyBar(float percentage)
    {
        UpdateBar(energyFill, percentage);
    }

    public void UpdateHealthBar(float percentage)
    {
        UpdateBar(healthFill, percentage);
    }

    // --- AKILLI HAVUZ VE RASTGELE 3 BENZERSİZ UPGRADETYPE SEÇİM ALGORİTMASI ---
    private int GetMaxAllowedTierForCurrentLevel()
    {
        if (currentLevel <= 4) return 1;
        if (currentLevel <= 9) return 2;
        return 3;
    }

    private List<UpgradeData> GetSmartRandomUpgrades(int count)
    {
        int maxTier = GetMaxAllowedTierForCurrentLevel();

        List<UpgradeData> candidatePool = new List<UpgradeData>();
        foreach (UpgradeData card in upgradePool)
        {
            if (card != null && card.tier <= maxTier)
            {
                candidatePool.Add(card);
            }
        }

        List<UpgradeData> selectedCards = new List<UpgradeData>(count);

        for (int i = 0; i < count; i++)
        {
            if (candidatePool.Count == 0) break;

            int randomIndex = Random.Range(0, candidatePool.Count);
            UpgradeData chosenCard = candidatePool[randomIndex];
            selectedCards.Add(chosenCard);

            candidatePool.RemoveAll(c => c.upgradeType == chosenCard.upgradeType);
        }

        return selectedCards;
    }

    private void ShowLevelUpScreen()
    {
        if (levelUpOverlay == null) return;

        int maxTier = GetMaxAllowedTierForCurrentLevel();
        if (levelUpTitle != null)
        {
            levelUpTitle.text = $"SEVİYE {currentLevel}!";
        }
        if (levelUpSubtitle != null)
        {
            levelUpSubtitle.text = $"Gemin için bir geliştirme seç (Açık Tier: I - {GetRomanNumeral(maxTier)})";
        }

        List<UpgradeData> selectedUpgrades = GetSmartRandomUpgrades(3);

        for (int i = 0; i < 3; i++)
        {
            if (i < selectedUpgrades.Count && selectedUpgrades[i] != null)
            {
                UpgradeData data = selectedUpgrades[i];
                currentOfferedUpgrades[i] = data;

                if (cardButtons != null && i < cardButtons.Length && cardButtons[i] != null)
                {
                    cardButtons[i].gameObject.SetActive(true);
                }

                if (cardTierBadges != null && i < cardTierBadges.Length && cardTierBadges[i] != null)
                {
                    cardTierBadges[i].color = GetTierBadgeColor(data.tier);
                }

                if (cardTierTexts != null && i < cardTierTexts.Length && cardTierTexts[i] != null)
                {
                    cardTierTexts[i].text = GetTierLabel(data.tier);
                }

                if (cardTitles != null && i < cardTitles.Length && cardTitles[i] != null)
                {
                    cardTitles[i].text = data.upgradeName;
                }

                if (cardDescs != null && i < cardDescs.Length && cardDescs[i] != null)
                {
                    cardDescs[i].text = data.description;
                }

                if (cardIcons != null && i < cardIcons.Length && cardIcons[i] != null)
                {
                    cardIcons[i].sprite = data.icon;
                    cardIcons[i].gameObject.SetActive(data.icon != null);
                }
            }
            else
            {
                currentOfferedUpgrades[i] = null;
                if (cardButtons != null && i < cardButtons.Length && cardButtons[i] != null)
                {
                    cardButtons[i].gameObject.SetActive(false);
                }
            }
        }

        levelUpOverlay.gameObject.SetActive(true);
    }

    private Color GetTierBadgeColor(int tier)
    {
        switch (tier)
        {
            case 1: return Tier1BadgeColor;
            case 2: return Tier2BadgeColor;
            case 3: return Tier3BadgeColor;
            default: return Tier1BadgeColor;
        }
    }

    private string GetTierLabel(int tier)
    {
        switch (tier)
        {
            case 1: return "TIER I ★";
            case 2: return "TIER II ★★";
            case 3: return "TIER III ★★★";
            default: return $"TIER {tier}";
        }
    }

    private string GetRomanNumeral(int tier)
    {
        switch (tier)
        {
            case 1: return "I";
            case 2: return "II";
            case 3: return "III";
            default: return tier.ToString();
        }
    }

    public void OnCardClicked(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= currentOfferedUpgrades.Length) return;

        UpgradeData chosenUpgrade = currentOfferedUpgrades[cardIndex];
        if (chosenUpgrade != null)
        {
            ApplyUpgrade(chosenUpgrade);
        }

        if (levelUpOverlay != null)
        {
            levelUpOverlay.gameObject.SetActive(false);
        }

        Time.timeScale = 1f;

        if (currentXP >= nextLevelXP)
        {
            LevelUp();
        }
    }

    // --- 20 ÖZELLİĞİN GEMİYE UYGULANMASI ---
    private void ApplyUpgrade(UpgradeData upgrade)
    {
        CacheShipComponentsFromTransform();

        switch (upgrade.upgradeType)
        {
            case UpgradeType.Heal:
                if (playerHealth != null) playerHealth.Heal(upgrade.value);
                break;

            case UpgradeType.MaxHealth:
                if (playerHealth != null) playerHealth.IncreaseMaxHealth(upgrade.value);
                break;

            case UpgradeType.HealthRegen:
                if (playerHealth != null) playerHealth.healthRegen += upgrade.value;
                break;

            case UpgradeType.MoveSpeed:
                if (shipController != null)
                {
                    shipController.maxSpeed += upgrade.value;
                    shipController.acceleration += upgrade.value * 2f;
                }
                break;

            case UpgradeType.TurnSpeed:
                if (shipController != null)
                {
                    shipController.turnSpeed += upgrade.value;
                }
                break;

            case UpgradeType.DashRegen:
                dashRegenRate += upgrade.value;
                break;

            case UpgradeType.DashCost:
                float costFactor = 1f - (upgrade.value / 100f);
                dashCostMultiplier *= costFactor;
                if (shipController != null)
                {
                    shipController.dashCooldown = Mathf.Max(0.15f, shipController.dashCooldown * costFactor);
                }
                break;

            case UpgradeType.MagnetRadius:
                if (lootMagnet != null) lootMagnet.magnetRadius += upgrade.value;
                break;

            case UpgradeType.MagnetSpeed:
                if (lootMagnet != null) lootMagnet.pullSpeed += upgrade.value;
                break;

            case UpgradeType.FireRate:
                if (shipWeapons != null)
                {
                    shipWeapons.fireRate = Mathf.Max(0.05f, shipWeapons.fireRate - Mathf.Abs(upgrade.value));
                }
                break;

            case UpgradeType.BulletDamage:
                Bullet.bonusDamage += upgrade.value;
                break;

            case UpgradeType.BulletSpeed:
                Bullet.bonusSpeed += upgrade.value;
                break;

            case UpgradeType.BulletKnockback:
                Bullet.bonusKnockback += upgrade.value;
                break;

            case UpgradeType.AttackRange:
                if (shipWeapons != null) shipWeapons.attackRange += upgrade.value;
                break;

            case UpgradeType.XPGain:
                xpGainMultiplier += upgrade.value / 100f;
                break;

            case UpgradeType.MaxEnergy:
                if (shipController != null)
                {
                    shipController.IncreaseMaxEnergy(upgrade.value);
                }
                break;

            case UpgradeType.EnergyRegen:
                if (shipController != null)
                {
                    shipController.energyRegenRate += upgrade.value;
                }
                break;

            case UpgradeType.KnockbackResist:
                if (shipWeapons != null && shipWeapons.allTurrets != null)
                {
                    float recoilMultiplier = 1f - (upgrade.value / 100f);
                    foreach (Turret turret in shipWeapons.allTurrets)
                    {
                        if (turret != null) turret.recoilForce *= recoilMultiplier;
                    }
                }
                break;

            case UpgradeType.DamageResist:
                if (playerHealth != null)
                {
                    playerHealth.damageResistPercent = Mathf.Min(80f, playerHealth.damageResistPercent + upgrade.value);
                }
                break;

            case UpgradeType.Iframes:
                if (playerHealth != null)
                {
                    playerHealth.AddIframeDuration(upgrade.value);
                }
                break;
        }

        Debug.Log($"[Upgrade Seçildi] {upgrade.upgradeName} (Tier {upgrade.tier}) -> {upgrade.description}");
    }

    private void EnsureDefaultUpgradesIfEmpty()
    {
        upgradePool.RemoveAll(item => item == null);
        if (upgradePool.Count == 0)
        {
            upgradePool = UpgradeData.CreateDefault60CardPool();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("60 Kartlık Varsayılan Havuzu Oluştur (Assets/Upgrades)")]
    public void Generate60UpgradeAssetsInEditor()
    {
        string folderPath = "Assets/Upgrades";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Upgrades");
        }

        upgradePool.Clear();
        List<UpgradeData> generatedCards = UpgradeData.CreateDefault60CardPool();

        foreach (UpgradeData card in generatedCards)
        {
            string assetPath = $"{folderPath}/{card.name}.asset";
            UpgradeData existing = AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath);
            if (existing != null)
            {
                existing.upgradeName = card.upgradeName;
                existing.description = card.description;
                existing.upgradeType = card.upgradeType;
                existing.tier = card.tier;
                existing.value = card.value;
                EditorUtility.SetDirty(existing);
                upgradePool.Add(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(card, assetPath);
                upgradePool.Add(card);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.SetDirty(this);
        Debug.Log("60 adet UpgradeData asset'i 'Assets/Upgrades' klasörüne oluşturuldu ve Upgrade Pool listesine atandı!");
    }
#endif

    // Barların doluluk oranını (Can, XP, Enerji, Dash) Canvas Image.fillAmount üzerinden değiştiren ana metot
    public void UpdateBar(Image fillImage, float percentage)
    {
        if (fillImage != null)
        {
            percentage = Mathf.Clamp(percentage, 0f, 100f);
            fillImage.fillAmount = percentage / 100f;
        }
    }

    public void ConsumeDash(float cost)
    {
        float finalCost = cost * dashCostMultiplier;
        if (currentDash >= finalCost)
        {
            currentDash -= finalCost;
            UpdateBar(dashFill, currentDash);
            isDashing = true;
            Invoke(nameof(StopDashing), 0.5f);
        }
    }

    private void StopDashing() => isDashing = false;
}
