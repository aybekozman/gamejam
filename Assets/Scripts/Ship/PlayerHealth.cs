using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Referanslar (Manuel Atama)")]
    public ShipUIManager uiManager;

    [Header("Can Ayarları")]
    public float maxHealth = 100f;
    public float healthRegen = 0f; // Saniyede yenilenen can
    [Range(0f, 80f)] public float damageResistPercent = 0f; // Hasar direnci yüzdesi
    private float currentHealth;

    [Header("Efekt Ayarları")]
    public float flashDuration = 0.1f;
    public int flashCount = 3;

    public bool IsInvulnerable { get; private set; } // ShipController buradan okuyacak

    private SpriteRenderer[] allSprites;
    private Color[] originalColors;

    void Start()
    {
        currentHealth = maxHealth;
        allSprites = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[allSprites.Length];

        for (int i = 0; i < allSprites.Length; i++)
        {
            originalColors[i] = allSprites[i].color;
        }

        UpdateHealthUI();
    }

    void Update()
    {
        if (healthRegen > 0f && currentHealth > 0f && currentHealth < maxHealth)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegen * Time.deltaTime);
            UpdateHealthUI();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthUI();
    }

    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount;
        UpdateHealthUI();
    }

    public void AddIframeDuration(float extraSeconds)
    {
        int totalFlashes = Mathf.Max(1, flashCount * 2);
        flashDuration += extraSeconds / totalFlashes;
    }

    private void UpdateHealthUI()
    {
        if (uiManager != null && maxHealth > 0f)
        {
            uiManager.UpdateHealthBar((currentHealth / maxHealth) * 100f);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (IsInvulnerable) return;

        float multiplier = 1f - (Mathf.Clamp(damageResistPercent, 0f, 80f) / 100f);
        float finalDamage = damageAmount * multiplier;

        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        UpdateHealthUI();
        Debug.Log("Gemi hasar aldı! Kalan can: " + currentHealth);

        StartCoroutine(DamageFlashRoutine());

        if (currentHealth <= 0)
        {
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
