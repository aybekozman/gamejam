using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Can Ayarları")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Efekt Ayarları")]
    public float flashDuration = 0.1f;
    public int flashCount = 3; // Kaç kere beyaz yanıp söneceği

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isInvulnerable = false; // Hasar aldıktan hemen sonra tekrar hasar almamak için (i-frame)

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isInvulnerable) return;

        currentHealth -= damageAmount;
        Debug.Log("Gemi hasar aldı! Kalan can: " + currentHealth);

        StartCoroutine(DamageFlashRoutine());

        if (currentHealth <= 0)
        {
            Debug.Log("Oyun Bitti! Gemi Parçalandı.");
            // İleride buraya Game Over ekranı çağrısı gelecek
        }
    }

    // İleride sarı/yeşil modu veya level-up ile çağıracağın fonksiyon
    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount;
        Debug.Log("Max can yükseltildi. Yeni Max Can: " + maxHealth);
    }

    private IEnumerator DamageFlashRoutine()
    {
        isInvulnerable = true;
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
        isInvulnerable = false;
    }
}