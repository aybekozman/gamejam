using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Enemy : MonoBehaviour
{
    [Header("Hedef Referansı (Manuel / Spawner Ataması)")]
    public Transform player;

    [Header("Veri Odaklı Statlar (ScriptableObject)")]
    public EnemyStatsData stats;
    private float currentHealth;

    [Header("Süre ve Efekt Ayarları")]
    public float knockbackDuration = 0.3f;
    public float hitStunDuration = 0.2f;
    public float flashDuration = 0.05f;
    public int flashCount = 3;

    [Header("Ödül (Loot) Ayarları")]
    public GameObject lootPrefab;

    [Header("Görsel Ayarları")]
    public float rotationOffset = 0f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private bool isKnockedBack;
    private bool isStunned;
    private bool isDead = false; // Çifte ölümü engellemek için kilit

    void Start()
    {
        if (stats == null)
        {
            Debug.LogError($"{gameObject.name} üzerinde EnemyStatsData (stats) atanmamış!", this);
            enabled = false;
            return;
        }

        currentHealth = stats.maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        if (stats == null || player == null || isKnockedBack || isStunned || isDead)
        {
            if (isStunned && !isKnockedBack && rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * stats.moveSpeed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (stats == null) return;

        IDamageable damageableObj = collision.gameObject.GetComponentInParent<IDamageable>();

        if (damageableObj != null && !isKnockedBack && !isDead)
        {
            damageableObj.TakeDamage(stats.damage);
            Vector2 knockbackDir = (transform.position - collision.transform.position).normalized;
            StartCoroutine(KnockbackRoutine(knockbackDir, stats.shipKnockbackForce));
        }
    }

    public void TakeDamage(float damageAmount, Vector2 damageSourcePos)
    {
        if (isDead || stats == null) return; // Zaten öldüyse işlemi anında kes

        currentHealth -= damageAmount;

        if (!isKnockedBack)
        {
            Vector2 knockbackDir = ((Vector2)transform.position - damageSourcePos).normalized;
            StartCoroutine(KnockbackRoutine(knockbackDir, stats.bulletKnockbackForce + Bullet.bonusKnockback));
        }

        if (!isStunned)
        {
            StartCoroutine(HitFlashRoutine());
        }

        if (currentHealth <= 0)
        {
            isDead = true; // Ölüm kilidini kapat
            if (lootPrefab != null)
            {
                Instantiate(lootPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force)
    {
        isKnockedBack = true;
        if (rb != null) rb.linearVelocity = direction * force;
        yield return new WaitForSeconds(knockbackDuration);
        isKnockedBack = false;
    }

    private IEnumerator HitFlashRoutine()
    {
        isStunned = true;
        for (int i = 0; i < flashCount; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
        isStunned = false;
    }
}
