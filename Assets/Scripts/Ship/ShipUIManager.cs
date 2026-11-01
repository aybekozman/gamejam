using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using UITKButton = UnityEngine.UIElements.Button;
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
    public Image[] dashFills; // 3 farklı dash slotunu (Recharge objelerini) tutacak dizi
    public Image energyFill;
    public Image xpFill;

    [Header("Canvas (uGUI) Bar ve Seviye Metinleri")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI modeText;
    public TextMeshProUGUI levelText;

    [Header("Canvas (uGUI) Bar Konteynerleri ve Pusula İğnesi")]
    public GameObject xpBarContainer;
    public GameObject energyBarContainer;
    [FormerlySerializedAs("compass")]
    public RectTransform compassNeedle;

    [Header("UI Toolkit Level Up Ekranı")]
    public UIDocument levelUpDocument;

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

    [Header("Dash (3 Slotlu) Ayarları")]
    public int maxDashCharges = 3;
    public float currentDash = 3f;
    public float dashRegenRate = 0.5f;
    public float dashCostMultiplier = 1f;

    // O an ekranda gösterilen 3 kartın verisi
    private readonly UpgradeData[] currentOfferedUpgrades = new UpgradeData[3];
    private readonly VisualElement[] boundCardElements = new VisualElement[3];
    private readonly Action[] cardButtonActions = new Action[3];
    private readonly EventCallback<ClickEvent>[] cardClickCallbacks = new EventCallback<ClickEvent>[3];
    private bool isLevelUpScreenOpen = false;

    void Awake()
    {
        // Statik mermi bonuslarını oyun başında sıfırla
        Bullet.bonusDamage = 0f;
        Bullet.bonusSpeed = 0f;
        Bullet.bonusKnockback = 0f;

        currentDash = maxDashCharges;

        for (int i = 0; i < 3; i++)
        {
            int cardIndex = i;
            cardButtonActions[i] = () => OnCardClicked(cardIndex);
            cardClickCallbacks[i] = _ => OnCardClicked(cardIndex);
        }

        CalculateNextLevelXP();
        CacheShipComponentsFromTransform();
        EnsureDefaultUpgradesIfEmpty();
    }

    void OnEnable()
    {
        CacheShipComponentsFromTransform();

        // XP ve Enerji bar konteynerlerinin HER ZAMAN görünür kalmasını sağla
        if (xpBarContainer != null) xpBarContainer.SetActive(true);
        if (energyBarContainer != null) energyBarContainer.SetActive(true);

        UpdateLootModeUI();
        HideLevelUpScreen();

        // Başlangıç bar ve metin değerlerini ayarla
        float initHealth = playerHealth != null ? playerHealth.CurrentHealth : 100f;
        float initMaxHealth = playerHealth != null ? playerHealth.MaxHealth : 100f;
        UpdateBar(healthFill, healthText, initHealth, initMaxHealth);

        float initEnergy = shipController != null ? shipController.currentEnergy : 100f;
        float initMaxEnergy = shipController != null ? shipController.maxEnergy : 100f;
        UpdateBar(energyFill, energyText, initEnergy, initMaxEnergy);

        if (levelText != null) levelText.text = currentLevel.ToString();

        UpdateDashFills();
        UpdateXPBarUI();
    }

    void OnDisable()
    {
        UnbindAllCardEvents();
    }

    void Update()
    {
        // Space tuşuna basıldığında Aktif Loot Modunu (XP <-> ENERJİ) tersine çevir
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && Time.timeScale > 0f)
        {
            ToggleLootMode();
        }

        // 1. Pusula İğnesi: Tüm pusula objesini değil, SADECE iğneyi (compassNeedle) döndür
        if (shipTransform != null && compassNeedle != null)
        {
            compassNeedle.localRotation = Quaternion.Euler(0f, 0f, -shipTransform.eulerAngles.z);
        }

        // 2. 3 Slotlu Dash Dolum Algoritması
        if (currentDash < maxDashCharges)
        {
            currentDash += dashRegenRate * Time.deltaTime;
            if (currentDash > maxDashCharges)
            {
                currentDash = maxDashCharges;
            }
        }

        UpdateDashFills();
    }

    public void UpdateDashFills()
    {
        if (dashFills != null && dashFills.Length >= 3)
        {
            if (dashFills[0] != null) dashFills[0].fillAmount = Mathf.Clamp01(currentDash);
            if (dashFills[1] != null) dashFills[1].fillAmount = Mathf.Clamp01(currentDash - 1f);
            if (dashFills[2] != null) dashFills[2].fillAmount = Mathf.Clamp01(currentDash - 2f);
        }
    }

    public void ToggleLootMode()
    {
        isLootingXP = !isLootingXP;
        UpdateLootModeUI();
    }

    private void UpdateLootModeUI()
    {
        if (modeText != null)
        {
            modeText.text = isLootingXP ? "Toplanan: XP" : "Toplanan: ENERJİ";
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
        if (levelText != null) levelText.text = currentLevel.ToString();
        CalculateNextLevelXP();
        UpdateXPBarUI();

        Time.timeScale = 0f;
        ShowLevelUpScreen();
    }

    private void UpdateXPBarUI()
    {
        UpdateBar(xpFill, xpText, currentXP, nextLevelXP);
    }

    public void UpdateEnergyBar(float percentage)
    {
        CacheShipComponentsFromTransform();
        if (shipController != null)
        {
            UpdateBar(energyFill, energyText, shipController.currentEnergy, shipController.maxEnergy);
        }
        else
        {
            UpdateBar(energyFill, energyText, percentage, 100f);
        }
    }

    public void UpdateHealthBar(float percentage)
    {
        CacheShipComponentsFromTransform();
        if (playerHealth != null)
        {
            UpdateBar(healthFill, healthText, playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
        else
        {
            UpdateBar(healthFill, healthText, percentage, 100f);
        }
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

            int randomIndex = UnityEngine.Random.Range(0, candidatePool.Count);
            UpgradeData chosenCard = candidatePool[randomIndex];
            selectedCards.Add(chosenCard);

            candidatePool.RemoveAll(c => c.upgradeType == chosenCard.upgradeType);
        }

        return selectedCards;
    }

    // --- UI TOOLKIT LEVEL UP EKRANI YÖNETİMİ ---
    private void ShowLevelUpScreen()
    {
        if (levelUpDocument == null || levelUpDocument.rootVisualElement == null)
        {
            Debug.LogWarning("[ShipUIManager] levelUpDocument atanmamış! Oyun devam ettiriliyor.");
            Time.timeScale = 1f;
            return;
        }

        VisualElement root = levelUpDocument.rootVisualElement;
        root.style.display = DisplayStyle.Flex;

        // Eğer MainUI.uxml kullanılıyorsa eski HUD panellerini gizle, sadece LevelUpOverlay'i göster
        HideLegacyHudPanelsIfPresent(root);

        VisualElement overlay = root.Q<VisualElement>("LevelUpOverlay") ?? root;
        overlay.style.display = DisplayStyle.Flex;

        int maxTier = GetMaxAllowedTierForCurrentLevel();

        Label levelUpTitle = root.Q<Label>("LevelUpTitle") ?? root.Q<Label>(className: "level-up-title");
        if (levelUpTitle != null)
        {
            levelUpTitle.text = $"SEVİYE {currentLevel}!";
        }

        Label levelUpSubtitle = root.Q<Label>("LevelUpSubtitle") ?? root.Q<Label>(className: "level-up-subtitle");
        if (levelUpSubtitle != null)
        {
            levelUpSubtitle.text = $"Gemin için bir geliştirme seç (Açık Tier: I - {GetRomanNumeral(maxTier)})";
        }

        UnbindAllCardEvents();
        List<UpgradeData> selectedUpgrades = GetSmartRandomUpgrades(3);
        isLevelUpScreenOpen = true;

        for (int i = 0; i < 3; i++)
        {
            int oneBased = i + 1;

            // Hem "Card_1 / Title_1 / Desc_1 / Icon_1" hem de "Card0 / CardTitle0 / CardDesc0 / CardIcon0" sorgularını destekle
            VisualElement cardElement = root.Q<VisualElement>($"Card_{oneBased}")
                                     ?? root.Q<VisualElement>($"Card{i}")
                                     ?? root.Q<VisualElement>($"Card{oneBased}");

            if (cardElement == null) continue;

            if (i < selectedUpgrades.Count && selectedUpgrades[i] != null)
            {
                UpgradeData data = selectedUpgrades[i];
                currentOfferedUpgrades[i] = data;
                cardElement.style.display = DisplayStyle.Flex;

                // Tier CSS sınıflarını güncelle
                cardElement.RemoveFromClassList("tier-1");
                cardElement.RemoveFromClassList("tier-2");
                cardElement.RemoveFromClassList("tier-3");
                cardElement.AddToClassList($"tier-{data.tier}");

                VisualElement tierBadge = root.Q<VisualElement>($"TierBadge_{oneBased}")
                                       ?? root.Q<VisualElement>($"CardTierBadge_{oneBased}")
                                       ?? root.Q<VisualElement>($"CardTierBadge{i}")
                                       ?? cardElement.Q<VisualElement>(className: "card-tier-badge");
                if (tierBadge != null)
                {
                    tierBadge.RemoveFromClassList("tier-1");
                    tierBadge.RemoveFromClassList("tier-2");
                    tierBadge.RemoveFromClassList("tier-3");
                    tierBadge.AddToClassList($"tier-{data.tier}");
                }

                Label tierText = root.Q<Label>($"Tier_{oneBased}")
                              ?? root.Q<Label>($"TierText_{oneBased}")
                              ?? root.Q<Label>($"CardTierText_{oneBased}")
                              ?? root.Q<Label>($"CardTierText{i}")
                              ?? cardElement.Q<Label>(className: "card-tier-text");
                if (tierText != null)
                {
                    tierText.text = GetTierLabel(data.tier);
                }

                Label titleLabel = root.Q<Label>($"Title_{oneBased}")
                                ?? root.Q<Label>($"CardTitle_{oneBased}")
                                ?? root.Q<Label>($"CardTitle{i}")
                                ?? root.Q<Label>($"Title{i}")
                                ?? cardElement.Q<Label>("Title")
                                ?? cardElement.Q<Label>(className: "card-title");
                if (titleLabel != null)
                {
                    titleLabel.text = data.upgradeName;
                }

                Label descLabel = root.Q<Label>($"Desc_{oneBased}")
                               ?? root.Q<Label>($"Description_{oneBased}")
                               ?? root.Q<Label>($"CardDesc_{oneBased}")
                               ?? root.Q<Label>($"CardDesc{i}")
                               ?? root.Q<Label>($"Desc{i}")
                               ?? cardElement.Q<Label>("Desc")
                               ?? cardElement.Q<Label>(className: "card-desc");
                if (descLabel != null)
                {
                    descLabel.text = data.description;
                }

                VisualElement iconElement = root.Q<VisualElement>($"Icon_{oneBased}")
                                         ?? root.Q<VisualElement>($"CardIcon_{oneBased}")
                                         ?? root.Q<VisualElement>($"CardIcon{i}")
                                         ?? root.Q<VisualElement>($"Icon{i}")
                                         ?? cardElement.Q<VisualElement>("Icon")
                                         ?? cardElement.Q<VisualElement>(className: "card-icon");
                if (iconElement != null)
                {
                    if (data.icon != null)
                    {
                        iconElement.style.backgroundImage = new StyleBackground(data.icon);
                        iconElement.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        iconElement.style.backgroundImage = StyleKeyword.None;
                    }
                }

                BindCardEvent(i, cardElement);
            }
            else
            {
                currentOfferedUpgrades[i] = null;
                cardElement.style.display = DisplayStyle.None;
            }
        }
    }

    private void BindCardEvent(int index, VisualElement cardElement)
    {
        boundCardElements[index] = cardElement;

        if (cardElement is UITKButton button)
        {
            button.clicked -= cardButtonActions[index];
            button.clicked += cardButtonActions[index];
        }
        else
        {
            cardElement.UnregisterCallback(cardClickCallbacks[index]);
            cardElement.RegisterCallback(cardClickCallbacks[index]);
        }
    }

    private void UnbindAllCardEvents()
    {
        for (int i = 0; i < 3; i++)
        {
            VisualElement cardElement = boundCardElements[i];
            if (cardElement == null) continue;

            if (cardElement is UITKButton button)
            {
                button.clicked -= cardButtonActions[i];
            }
            else
            {
                cardElement.UnregisterCallback(cardClickCallbacks[i]);
            }

            boundCardElements[i] = null;
        }
    }

    private void HideLegacyHudPanelsIfPresent(VisualElement root)
    {
        VisualElement topLeft = root.Q<VisualElement>(className: "top-left-panel");
        if (topLeft != null) topLeft.style.display = DisplayStyle.None;

        VisualElement topRight = root.Q<VisualElement>(className: "top-right-panel");
        if (topRight != null) topRight.style.display = DisplayStyle.None;

        VisualElement bottomCenter = root.Q<VisualElement>(className: "bottom-center-panel");
        if (bottomCenter != null) bottomCenter.style.display = DisplayStyle.None;
    }

    private void HideLevelUpScreen()
    {
        isLevelUpScreenOpen = false;
        UnbindAllCardEvents();

        if (levelUpDocument == null || levelUpDocument.rootVisualElement == null) return;

        VisualElement root = levelUpDocument.rootVisualElement;
        HideLegacyHudPanelsIfPresent(root);

        VisualElement overlay = root.Q<VisualElement>("LevelUpOverlay");
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
        }
        else
        {
            root.style.display = DisplayStyle.None;
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
        if (!isLevelUpScreenOpen) return;
        if (cardIndex < 0 || cardIndex >= currentOfferedUpgrades.Length) return;

        UpgradeData chosenUpgrade = currentOfferedUpgrades[cardIndex];
        if (chosenUpgrade != null)
        {
            ApplyUpgrade(chosenUpgrade);
        }

        HideLevelUpScreen();
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
                // 3'lü slot sisteminde (varsayılan 0.5/sn) anlamlı artış için ölçeklendir
                dashRegenRate += upgrade.value > 1f ? upgrade.value * 0.05f : upgrade.value;
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

    // Barların sadece doluluk oranını (Image.fillAmount) değiştiren metot
    public void UpdateBar(Image fillImage, float percentage)
    {
        if (fillImage != null)
        {
            percentage = Mathf.Clamp(percentage, 0f, 100f);
            fillImage.fillAmount = percentage / 100f;
        }
    }

    // Barların hem Image.fillAmount değerini hem de "MevcutDeğer / MaxDeğer" metnini güncelleyen metot
    public void UpdateBar(Image fillImage, TextMeshProUGUI valueText, float current, float max)
    {
        float percentage = max > 0f ? (current / max) * 100f : 0f;
        UpdateBar(fillImage, percentage);

        if (valueText != null)
        {
            valueText.text = Mathf.RoundToInt(current) + " / " + Mathf.RoundToInt(max);
        }
    }

    /// <summary>
    /// Gemi dash attığında çağrılır; currentDash >= 1f ise tam 1 birim düşürür.
    /// </summary>
    public bool ConsumeDash()
    {
        if (currentDash >= 1f)
        {
            currentDash -= 1f;
            UpdateDashFills();
            return true;
        }
        return false;
    }

    public void ConsumeDash(float cost)
    {
        if (currentDash >= 1f)
        {
            currentDash -= 1f;
            UpdateDashFills();
        }
    }
}
