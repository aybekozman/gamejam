using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Enemy : MonoBehaviour
{
    [Header("Can Ayarları")]
    public float maxHealth = 10f;
    private float currentHealth;

    [Header("Hareket Ayarları")]
    public float moveSpeed = 3f;
    public float shipKnockbackForce = 10f;
    public float bulletKnockbackForce = 3f;
    public float knockbackDuration = 0.3f;

    [Header("Hasar Verme Ayarları")]
    public float damage = 10f;
    public float hitStunDuration = 0.2f;

    [Header("Efekt Ayarları")]
    public float flashDuration = 0.05f;
    public int flashCount = 3;

    [Header("Ödül (Loot) Ayarları")]
    public GameObject lootPrefab;

    [Header("Görsel Ayarları")]
    public float rotationOffset = 0f;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private bool isKnockedBack;
    private bool isStunned;
    private bool isDead = false; // Çifte ölümü engellemek için kilit

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        ShipController targetShip = FindAnyObjectByType<ShipController>();
        if (targetShip != null)
        {
            player = targetShip.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null || isKnockedBack || isStunned || isDead)
        {
            if (isStunned && !isKnockedBack && rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IDamageable damageableObj = collision.gameObject.GetComponentInParent<IDamageable>();

        if (damageableObj != null && !isKnockedBack && !isDead)
        {
            damageableObj.TakeDamage(damage);
            Vector2 knockbackDir = (transform.position - collision.transform.position).normalized;
            StartCoroutine(KnockbackRoutine(knockbackDir, shipKnockbackForce));
        }
    }

    public void TakeDamage(float damageAmount, Vector2 damageSourcePos)
    {
        if (isDead) return; // Zaten öldüyse işlemi anında kes

        currentHealth -= damageAmount;

        if (!isKnockedBack)
        {
            Vector2 knockbackDir = ((Vector2)transform.position - damageSourcePos).normalized;
            StartCoroutine(KnockbackRoutine(knockbackDir, bulletKnockbackForce));
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