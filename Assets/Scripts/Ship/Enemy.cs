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
    public float shipKnockbackForce = 10f; // Gemiye çarptığında ne kadar sekeceği
    public float bulletKnockbackForce = 3f; // Mermi yediğinde ne kadar sekeceği
    public float knockbackDuration = 0.3f;

    [Header("Hasar Verme Ayarları")]
    public float damage = 10f;
    public float hitStunDuration = 0.2f;

    [Header("Efekt Ayarları")]
    public float flashDuration = 0.05f;
    public int flashCount = 3; // Kaç kere yanıp söneceği

    [Header("Ödül (Loot) Ayarları")]
    public GameObject lootPrefab; // Düşecek ganimet

    [Header("Görsel Ayarları")]
    public float rotationOffset = 0f; // Yön düzeltme (örn: -90, 90, 180)

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private bool isKnockedBack;
    private bool isStunned;

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
        if (player == null || isKnockedBack || isStunned)
        {
            if (isStunned && !isKnockedBack) rb.linearVelocity = Vector2.zero;
            return;
        }

        // Oyuncuyu takip etme
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        // Görsel dönüş hesaplaması
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Gemiye çarpma kontrolü
        IDamageable damageableObj = collision.gameObject.GetComponentInParent<IDamageable>();

        if (damageableObj != null && !isKnockedBack)
        {
            damageableObj.TakeDamage(damage);

            Vector2 knockbackDir = (transform.position - collision.transform.position).normalized;
            // Gemi sekmesi gücü uygulanır
            StartCoroutine(KnockbackRoutine(knockbackDir, shipKnockbackForce));
        }
    }

    // Mermiden veya dışarıdan alınan hasar
    public void TakeDamage(float damageAmount, Vector2 damageSourcePos)
    {
        currentHealth -= damageAmount;

        if (!isKnockedBack)
        {
            Vector2 knockbackDir = ((Vector2)transform.position - damageSourcePos).normalized;
            // Mermi sekmesi gücü uygulanır
            StartCoroutine(KnockbackRoutine(knockbackDir, bulletKnockbackForce));
        }

        if (!isStunned)
        {
            StartCoroutine(HitFlashRoutine());
        }

        if (currentHealth <= 0)
        {
            if (lootPrefab != null) Instantiate(lootPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force)
    {
        isKnockedBack = true;
        rb.linearVelocity = direction * force;
        yield return new WaitForSeconds(knockbackDuration);
        isKnockedBack = false;
    }

    private IEnumerator HitFlashRoutine()
    {
        isStunned = true;
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
        isStunned = false;
    }
}