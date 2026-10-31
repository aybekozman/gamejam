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
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.3f;

    [Header("Hasar Ayarları")]
    public float damage = 10f;
    public float hitStunDuration = 0.2f;

    [Header("Görsel Ayarları")]
    public float rotationOffset = 0f;

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

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IDamageable damageableObj = collision.gameObject.GetComponentInParent<IDamageable>();

        if (damageableObj != null && !isKnockedBack)
        {
            damageableObj.TakeDamage(damage);

            Vector2 knockbackDir = (transform.position - collision.transform.position).normalized;
            StartCoroutine(KnockbackRoutine(knockbackDir));
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        StartCoroutine(HitFlashRoutine());
        if (currentHealth <= 0) Destroy(gameObject);
    }

    private IEnumerator KnockbackRoutine(Vector2 direction)
    {
        isKnockedBack = true;
        rb.linearVelocity = direction * knockbackForce;
        yield return new WaitForSeconds(knockbackDuration);
        isKnockedBack = false;
    }

    private IEnumerator HitFlashRoutine()
    {
        isStunned = true;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(hitStunDuration);
        spriteRenderer.color = originalColor;
        isStunned = false;
    }
}