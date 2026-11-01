using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Referanslar (Manuel Atama)")]
    public ShipUIManager uiManager;

    [Header("Can Ayarları")]
    public float maxHealth = 100f;
    public float healthRegen = 0f; // Saniyede yenilenen can miktarı
    [Range(0f, 80f)] public float damageResistPercent = 0f; // Alınan hasarı yüzde olarak düşürür
    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public bool CampaignProtected;
    public bool IsDead => currentHealth <= 0f;
    public event System.Action<float, float> HealthChanged;

    private float observedMax;

    [Header("Efekt ve Dokunulmazlık (I-Frame) Ayarları")]
    public float flashDuration = 0.1f;
    public int flashCount = 3;

    public bool IsInvulnerable { get; private set; } // ShipController buradan okuyacak

    private SpriteRenderer[] allSprites;
    private Color[] originalColors;

    void Awake()
    {
        observedMax = maxHealth;
        currentHealth = MaxHealth;
    }

    void Start()
    {
        NotifyHealthChanged();
        allSprites = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[allSprites.Length];

        for (int i = 0; i < allSprites.Length; i++)
        {
            originalColors[i] = allSprites[i].color;
        }
    }

    void Update()
    {
        if (!Mathf.Approximately(observedMax, maxHealth))
        {
            SetMaxHealth(maxHealth);
        }

        // Can yenileme (HealthRegen) mantığı (Max canı geçmez)
        if (healthRegen > 0f && !IsDead && currentHealth < MaxHealth)
        {
            currentHealth = Mathf.Min(MaxHealth, currentHealth + healthRegen * Time.deltaTime);
            NotifyHealthChanged();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead) return;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        NotifyHealthChanged();
    }

    public void SetMaxHealth(float value, bool healToFull = false)
    {
        maxHealth = Mathf.Max(1f, value);
        observedMax = maxHealth;
        currentHealth = healToFull ? MaxHealth : Mathf.Min(currentHealth, MaxHealth);
        NotifyHealthChanged();
    }

    public void IncreaseMaxHealth(float amount)
    {
        if (amount <= 0f) return;
        maxHealth = Mathf.Max(1f, maxHealth + amount);
        observedMax = maxHealth;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        NotifyHealthChanged();
    }

    public void AddIframeDuration(float amount)
    {
        if (amount <= 0f) return;
        int totalSteps = Mathf.Max(1, flashCount * 2);
        flashDuration += amount / totalSteps;
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHealth, MaxHealth);
        if (uiManager != null && MaxHealth > 0f)
        {
            uiManager.UpdateHealthBar((currentHealth / MaxHealth) * 100f);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (CampaignProtected || IsInvulnerable || IsDead || damageAmount <= 0f) return;

        // Hasar direnci (damageResistPercent) oranında gelen hasarı düşür
        float resistMultiplier = 1f - (Mathf.Clamp(damageResistPercent, 0f, 80f) / 100f);
        float finalDamage = damageAmount * resistMultiplier;

        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        NotifyHealthChanged();
        Debug.Log("Gemi hasar aldı! Kalan can: " + currentHealth);

        StartCoroutine(DamageFlashRoutine());

        if (currentHealth <= 0f)
        {
            if(IntegratedCampaign.CampaignFlow.IsManagedShip){IntegratedCampaign.CampaignFlow.Instance.RetryShip();return;}
            Debug.Log("Oyun Bitti! Gemi Parçalandı.");
            Time.timeScale = 0f;
            if (TryGetComponent(out ShipController shipController)) shipController.enabled = false;
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        IsInvulnerable = true;
        // Color.white orijinal renktir. Biz yarı saydam (hayalet) beyaz yapıyoruz.
        Color ghostColor = new Color(1f, 1f, 1f, 0.5f);

        for (int i = 0; i < flashCount; i++)
        {
            foreach (var sr in allSprites) { if (sr != null) sr.color = ghostColor; }
            yield return new WaitForSecondsRealtime(flashDuration);

            for (int j = 0; j < allSprites.Length; j++)
            {
                if (allSprites[j] != null) allSprites[j].color = originalColors[j];
            }
            yield return new WaitForSecondsRealtime(flashDuration);
        }
        IsInvulnerable = false;
    }
}
