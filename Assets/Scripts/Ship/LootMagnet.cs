using UnityEngine;

public class LootMagnet : MonoBehaviour
{
    [Header("Mıknatıs Ayarları")]
    public float magnetRadius = 5f; // Ganimetleri çekmeye başlayacağı mesafe
    public float pullSpeed = 8f;    // Ganimetlerin gemiye uçuş hızı
    public LayerMask lootLayer;     // Inspector'dan "Loot" katmanını seç

    void Update()
    {
        // Çekim alanındaki tüm ganimetleri radarla bul
        Collider2D[] loots = Physics2D.OverlapCircleAll(transform.position, magnetRadius, lootLayer);

        foreach (Collider2D loot in loots)
        {
            // Ganimeti yavaşça gemiye doğru kaydır
            loot.transform.position = Vector2.MoveTowards(loot.transform.position, transform.position, pullSpeed * Time.deltaTime);
        }
    }

    // Seçiliyken editörde çekim alanını yeşil bir çemberle gösterir
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}