using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
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

    [Header("Aktif Loot Modu (Taktiksel Toplama)")]
    public bool isLootingXP = true; // true = XP Topla, false = Enerji Topla

    [Header("Seviye ve Eksponansiyel XP Ayarları")]
    public int currentLevel = 1;
    public float currentXP = 0f;
    public float baseXP = 5f;
    public float xpGrowthMultiplier = 1.5f;
    public float xpGainMultiplier = 1f; // XPGain kartlarıyla artan çarpan
    public float nextLevelXP;

    [Header("60 Kartlık Roguelike Upgrade Havuzu (20 Özellik x 3 Tier)")]
    public List<UpgradeData> upgradePool = new List<UpgradeData>();

    [Header("Dash Ayarları")]
    public float dashRegenRate = 20f;
    public float dashCostMultiplier = 1f;
    private float currentDash = 100f;
    private bool isDashing = false;

    // Arayüz Elemanları (HUD)
    private VisualElement healthFill;
    private VisualElement dashFill;
    private VisualElement energyFill;
    private VisualElement xpFill;

    private VisualElement xpBarContainer;
    private VisualElement energyBarContainer;

    private VisualElement compass;
    private Toggle modeToggle;

    // Level Up Arayüz Elemanları
    private VisualElement levelUpOverlay;
    private Label levelUpTitle;
    private Label levelUpSubtitle;
    private readonly VisualElement[] cardElements = new VisualElement[3];
    private readonly VisualElement[] cardTierBadges = new VisualElement[3];
    private readonly Label[] cardTierTexts = new Label[3];
    private readonly VisualElement[] cardIcons = new VisualElement[3];
    private readonly Label[] cardTitles = new Label[3];
    private readonly Label[] cardDescs = new Label[3];

    // O an ekranda gösterilen 3 kartın verisi
    private readonly UpgradeData[] currentOfferedUpgrades = new UpgradeData[3];

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
        var root = GetComponent<UIDocument>().rootVisualElement;

        // UXML'deki isimlere (name="") göre dolgu elemanlarını bul
        healthFill = root.Q<VisualElement>("HealthFill");
        dashFill = root.Q<VisualElement>("DashFill");
        energyFill = root.Q<VisualElement>("EnergyFill");
        xpFill = root.Q<VisualElement>("XPFill");

        // XP ve Enerji ana çerçeveleri (Her ikisi de her zaman görünür kalır)
        xpBarContainer = root.Q<VisualElement>("XPBarContainer");
        energyBarContainer = root.Q<VisualElement>("EnergyBarContainer");

        if (xpBarContainer != null) xpBarContainer.style.display = DisplayStyle.Flex;
        if (energyBarContainer != null) energyBarContainer.style.display = DisplayStyle.Flex;

        compass = root.Q<VisualElement>("Compass");
        modeToggle = root.Q<Toggle>("ModeToggle");

        // Level Up Ekranı Elemanlarını Bul
        levelUpOverlay = root.Q<VisualElement>("LevelUpOverlay");
        levelUpTitle = root.Q<Label>("LevelUpTitle");
        levelUpSubtitle = root.Q<Label>("LevelUpSubtitle");

        for (int i = 0; i < 3; i++)
        {
            int cardIndex = i;
            cardElements[i] = root.Q<VisualElement>($"Card{i}");
            cardTierBadges[i] = root.Q<VisualElement>($"CardTierBadge{i}");
            cardTierTexts[i] = root.Q<Label>($"CardTierText{i}");
            cardIcons[i] = root.Q<VisualElement>($"CardIcon{i}");
            cardTitles[i] = root.Q<Label>($"CardTitle{i}");
            cardDescs[i] = root.Q<Label>($"CardDesc{i}");

            if (cardElements[i] != null)
            {
                cardElements[i].RegisterCallback<ClickEvent>(evt => OnCardClicked(cardIndex));
            }
        }

        // Toggle Mantığı: Fareyle tıklandığında da aktif loot modunu değiştirsin
        if (modeToggle != null)
        {
            modeToggle.RegisterValueChangedCallback(evt =>
            {
                isLootingXP = evt.newValue;
                UpdateLootModeUI();
            });

            UpdateLootModeUI();
        }

        if (levelUpOverlay != null)
        {
            levelUpOverlay.style.display = DisplayStyle.None;
        }

        // Başlangıç bar değerlerini ayarla
        UpdateBar(healthFill, 100f);
        UpdateBar(dashFill, 100f);
        UpdateBar(energyFill, 100f);
        UpdateXPBarUI();
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
            compass.style.rotate = new StyleRotate(new Rotate(new Angle(-shipTransform.eulerAngles.z, AngleUnit.Degree)));
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
            modeToggle.SetValueWithoutNotify(isLootingXP);
            modeToggle.label = isLootingXP ? "Toplanan: XP" : "Toplanan: ENERJİ";
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

                if (cardElements[i] != null)
                {
                    cardElements[i].style.display = DisplayStyle.Flex;
                    ApplyTierClasses(cardElements[i], data.tier);
                }

                if (cardTierBadges[i] != null)
                {
                    ApplyTierClasses(cardTierBadges[i], data.tier);
                }

                if (cardTierTexts[i] != null)
                {
                    cardTierTexts[i].text = GetTierLabel(data.tier);
                }

                if (cardTitles[i] != null) cardTitles[i].text = data.upgradeName;
                if (cardDescs[i] != null) cardDescs[i].text = data.description;

                if (cardIcons[i] != null)
                {
                    if (data.icon != null)
                        cardIcons[i].style.backgroundImage = new StyleBackground(data.icon);
                    else
                        cardIcons[i].style.backgroundImage = StyleKeyword.None;
                }
            }
            else
            {
                currentOfferedUpgrades[i] = null;
                if (cardElements[i] != null) cardElements[i].style.display = DisplayStyle.None;
            }
        }

        levelUpOverlay.style.display = DisplayStyle.Flex;
    }

    private void ApplyTierClasses(VisualElement element, int tier)
    {
        element.RemoveFromClassList("tier-1");
        element.RemoveFromClassList("tier-2");
        element.RemoveFromClassList("tier-3");
        element.AddToClassList($"tier-{Mathf.Clamp(tier, 1, 3)}");
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

    private void OnCardClicked(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= currentOfferedUpgrades.Length) return;

        UpgradeData chosenUpgrade = currentOfferedUpgrades[cardIndex];
        if (chosenUpgrade != null)
        {
            ApplyUpgrade(chosenUpgrade);
        }

        if (levelUpOverlay != null)
        {
            levelUpOverlay.style.display = DisplayStyle.None;
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

    // Barların genişliğini (Can, XP, Enerji) dışarıdan değiştirmek için kullanacağımız ana metot
    public void UpdateBar(VisualElement fillElement, float percentage)
    {
        if (fillElement != null)
        {
            percentage = Mathf.Clamp(percentage, 0, 100);
            fillElement.style.width = new Length(percentage, LengthUnit.Percent);
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
