using UnityEngine;

public class LootMagnet : MonoBehaviour
{
    [Header("Mıknatıs Ayarları")]
    public float magnetRadius = 5f; // Ganimetleri çekmeye başlayacağı mesafe
    [HideInInspector] public float pullSpeed = 8f; // Legacy serialized value; Loot owns movement.
    public LayerMask lootLayer;     // Inspector'dan "Loot" katmanını seç
    readonly Collider2D[] results = new Collider2D[128];
    ContactFilter2D filter;

    void Update()
    {
        // Çekim alanındaki tüm ganimetleri radarla bul
        filter.SetLayerMask(lootLayer);
        filter.useTriggers=true;
        int count = Physics2D.OverlapCircle(transform.position,magnetRadius,filter,results);
        for (int i=0; i<count; i++)
        {
            var loot = results[i].GetComponentInParent<Loot>();
            if (loot) loot.Magnetize(transform);
        }
    }

    // Seçiliyken editörde çekim alanını yeşil bir çemberle gösterir
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}
