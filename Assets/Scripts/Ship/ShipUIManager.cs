using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using UGUIButton = UnityEngine.UI.Button;
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
    public Image waterFill;

    [Header("Canvas (uGUI) Bar ve Seviye Metinleri")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI waterPercentText;
    public TextMeshProUGUI modeText;
    public TextMeshProUGUI levelText;

    [Header("Canvas (uGUI) Bar Konteynerleri ve Pusula İğnesi")]
    public GameObject xpBarContainer;
    public GameObject energyBarContainer;
    [FormerlySerializedAs("compass")]
    public RectTransform compassNeedle;

    [Header("Canvas (uGUI) Loot Modu Seçici (SlidingThumb)")]
    public RectTransform slidingThumb;
    public Image slidingThumbImage;
    public Vector2 energyThumbPos = new Vector2(8f, 0f);
    public Vector2 xpThumbPos = new Vector2(152f, 0f);
    public Color energyColor = Color.green;
    public Color xpColor = Color.yellow;

    [Header("UI Toolkit Level Up Ekranı")]
    public UIDocument levelUpDocument;

    [Header("uGUI Level Up Uyumluluk Alanları")]
    public GameObject levelUpOverlay;
    public TextMeshProUGUI levelUpTitle;
    public TextMeshProUGUI levelUpSubtitle;
    public UGUIButton[] cardButtons = new UGUIButton[3];
    public Image[] cardTierBadges = new Image[3];
    public TextMeshProUGUI[] cardTierTexts = new TextMeshProUGUI[3];
    public Image[] cardIcons = new Image[3];
    public TextMeshProUGUI[] cardTitles = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] cardDescs = new TextMeshProUGUI[3];

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

    // Tier rozet renkleri (uGUI fallback için)
    private static readonly Color Tier1BadgeColor = new Color(0.43f, 0.25f, 0.14f, 1f);
    private static readonly Color Tier2BadgeColor = new Color(0.04f, 0.23f, 0.29f, 1f);
    private static readonly Color Tier3BadgeColor = new Color(0.23f, 0.04f, 0.30f, 1f);

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

        // uGUI kart butonları atanmışsa tıklama olaylarını bağla
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

        UpdateLootModeUI();
        HideLevelUpScreen();

        // Başlangıç bar ve metin değerlerini ayarla
        float initHealth = playerHealth != null ? playerHealth.CurrentHealth : 100f;
        float initMaxHealth = playerHealth != null ? playerHealth.MaxHealth : 100f;
        UpdateBar(healthFill, healthText, initHealth, initMaxHealth);

        float initEnergy = shipController != null ? shipController.currentEnergy : 100f;
        float initMaxEnergy = shipController != null ? shipController.maxEnergy : 100f;
        UpdateBar(energyFill, energyText, initEnergy, initMaxEnergy);

        float initWater = shipController != null ? shipController.currentWater : 0f;
        float initMaxWater = shipController != null ? shipController.maxWater : 100f;
        UpdateWaterBar(initWater, initMaxWater);

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

        // 3. Su Arıtma Barını Güncelle
        if (shipController != null)
        {
            UpdateWaterBar(shipController.currentWater, shipController.maxWater);
        }
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

    public void UpdateWaterBar(float currentWater, float maxWater)
    {
        if (maxWater <= 0f) maxWater = 100f;

        if (waterFill != null)
        {
            waterFill.fillAmount = currentWater / maxWater;
        }

        if (waterPercentText != null)
        {
            waterPercentText.text = "%" + Mathf.RoundToInt((currentWater / maxWater) * 100f);
        }
    }

    public void ToggleLootMode()
    {
        isLootingXP = !isLootingXP;
        UpdateLootModeUI();
    }

    private void UpdateLootModeUI()
    {
        if (slidingThumb != null && slidingThumbImage == null)
        {
            slidingThumbImage = slidingThumb.GetComponent<Image>();
        }

        if (!isLootingXP)
        {
            if (slidingThumb != null) slidingThumb.anchoredPosition = energyThumbPos;
            if (slidingThumbImage != null) slidingThumbImage.color = energyColor;
        }
        else
        {
            if (slidingThumb != null) slidingThumb.anchoredPosition = xpThumbPos;
            if (slidingThumbImage != null) slidingThumbImage.color = xpColor;
        }

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

    public void RestoreCampaignProgress(int level, float xp)
    {
        currentLevel = Mathf.Max(1, level);
        currentXP = Mathf.Max(0f, xp);
        if (levelText != null) levelText.text = currentLevel.ToString();
        CalculateNextLevelXP();
        UpdateXPBarUI();
    }

    public void ApplyCampaignUpgrade(UpgradeData upgrade)
    {
        if (upgrade != null)
        {
            ApplyUpgrade(upgrade);
        }
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

    // --- LEVEL UP EKRANI YÖNETİMİ (UI TOOLKIT + UGUI UYUMLU) ---
    private void EnsureLevelUpOverlayProxy()
    {
        if (levelUpOverlay == null)
        {
            levelUpOverlay = new GameObject("LevelUpStateProxy");
            levelUpOverlay.transform.SetParent(transform, false);
            levelUpOverlay.SetActive(false);
        }
    }

    private void ShowLevelUpScreen()
    {
        EnsureDefaultUpgradesIfEmpty();
        EnsureLevelUpOverlayProxy();

        int maxTier = GetMaxAllowedTierForCurrentLevel();
        List<UpgradeData> selectedUpgrades = GetSmartRandomUpgrades(3);

        for (int i = 0; i < 3; i++)
        {
            currentOfferedUpgrades[i] = (i < selectedUpgrades.Count) ? selectedUpgrades[i] : null;
        }

        bool hasUIToolkitDoc = levelUpDocument != null && levelUpDocument.rootVisualElement != null;
        bool hasUGuiCards = cardButtons != null && cardButtons.Length > 0 && cardButtons[0] != null;

        if (!hasUIToolkitDoc && !hasUGuiCards)
        {
            Debug.LogWarning("[ShipUIManager] Level Up ekranı (levelUpDocument veya uGUI kartları) atanmamış! Oyun devam ettiriliyor.");
            Time.timeScale = 1f;
            return;
        }

        isLevelUpScreenOpen = true;
        if (levelUpOverlay != null)
        {
            levelUpOverlay.SetActive(true);
        }

        // 1. UI Toolkit (levelUpDocument) atanmışsa UI Toolkit kartlarını doldur ve göster
        if (hasUIToolkitDoc)
        {
            VisualElement root = levelUpDocument.rootVisualElement;
            root.style.display = DisplayStyle.Flex;
            HideLegacyHudPanelsIfPresent(root);

            VisualElement overlay = root.Q<VisualElement>("LevelUpOverlay") ?? root;
            overlay.style.display = DisplayStyle.Flex;

            Label toolkitTitle = root.Q<Label>("LevelUpTitle") ?? root.Q<Label>(className: "level-up-title");
            if (toolkitTitle != null)
            {
                toolkitTitle.text = $"SEVİYE {currentLevel}!";
            }

            Label toolkitSubtitle = root.Q<Label>("LevelUpSubtitle") ?? root.Q<Label>(className: "level-up-subtitle");
            if (toolkitSubtitle != null)
            {
                toolkitSubtitle.text = $"Gemin için bir geliştirme seç (Açık Tier: I - {GetRomanNumeral(maxTier)})";
            }

            UnbindAllCardEvents();

            for (int i = 0; i < 3; i++)
            {
                int oneBased = i + 1;
                VisualElement cardElement = root.Q<VisualElement>($"Card_{oneBased}")
                                         ?? root.Q<VisualElement>($"Card{i}")
                                         ?? root.Q<VisualElement>($"Card{oneBased}");

                if (cardElement == null) continue;

                UpgradeData data = currentOfferedUpgrades[i];
                if (data != null)
                {
                    cardElement.style.display = DisplayStyle.Flex;

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
                    cardElement.style.display = DisplayStyle.None;
                }
            }
        }

        // 2. uGUI kart elemanları atanmışsa onları da senkronize doldur
        if (hasUGuiCards)
        {
            if (levelUpTitle != null)
            {
                levelUpTitle.text = $"SEVİYE {currentLevel}!";
            }
            if (levelUpSubtitle != null)
            {
                levelUpSubtitle.text = $"Gemin için bir geliştirme seç (Açık Tier: I - {GetRomanNumeral(maxTier)})";
            }

            for (int i = 0; i < 3; i++)
            {
                UpgradeData data = currentOfferedUpgrades[i];
                if (data != null)
                {
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
                    if (cardButtons != null && i < cardButtons.Length && cardButtons[i] != null)
                    {
                        cardButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }
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

        if (levelUpOverlay != null)
        {
            levelUpOverlay.SetActive(false);
        }

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
        if (!isLevelUpScreenOpen && (levelUpOverlay == null || !levelUpOverlay.activeSelf)) return;
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
