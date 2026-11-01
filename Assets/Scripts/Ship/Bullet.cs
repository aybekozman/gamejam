using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 5f;
    public float lifetime = 2f;

    // Upgrade sistemi tarafından artırılan bonus değerler
    public static float bonusDamage = 0f;
    public static float bonusSpeed = 0f;
    public static float bonusKnockback = 0f;

    void Start() => Destroy(gameObject, lifetime);

    void Update() => transform.Translate(Vector2.up * (speed + bonusSpeed) * Time.deltaTime);

    private void OnTriggerEnter2D(Collider2D collision) => HitTarget(collision.gameObject);
    private void OnCollisionEnter2D(Collision2D collision) => HitTarget(collision.gameObject);

    private void HitTarget(GameObject hitObject)
    {
        if (hitObject.TryGetComponent(out Enemy enemy))
        {
            // Artık mermi itme gücü göndermiyor, sadece hasar ve kendi konumunu iletiyor
            enemy.TakeDamage(damage + bonusDamage, transform.position);
            Destroy(gameObject);
        }
    }
}
