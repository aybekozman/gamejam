using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Can Ayarları")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => Mathf.Max(1, maxHealth);
    public bool IsDead => currentHealth <= 0;
    public event System.Action<float, float> HealthChanged;
    private float observedMax;
    void Awake() { observedMax = maxHealth; currentHealth = MaxHealth; }
    public void Heal(float amount)
    {
        if (amount <= 0 || IsDead) return;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }
    public void SetMaxHealth(float value, bool healToFull = false)
    {
        maxHealth = Mathf.Max(1, value);
        observedMax = maxHealth;
        currentHealth = healToFull ? MaxHealth : Mathf.Min(currentHealth, MaxHealth);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }
    void Update() { if (!Mathf.Approximately(observedMax,maxHealth)) SetMaxHealth(maxHealth); }

    [Header("Efekt Ayarları")]
    public float flashDuration = 0.1f;
    public int flashCount = 3;

    public bool IsInvulnerable { get; private set; } // ShipController buradan okuyacak

    private SpriteRenderer[] allSprites;
    private Color[] originalColors;

    void Start()
    {
        HealthChanged?.Invoke(currentHealth, MaxHealth);
        allSprites = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[allSprites.Length];

        for (int i = 0; i < allSprites.Length; i++)
        {
            originalColors[i] = allSprites[i].color;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (IsInvulnerable || IsDead || damageAmount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
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
