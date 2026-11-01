using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Can Ayarları")]
    public float maxHealth = 100f;
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
    }

    public void TakeDamage(float damageAmount)
    {
        if (IsInvulnerable) return;

        currentHealth -= damageAmount;
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