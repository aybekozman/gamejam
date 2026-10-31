using UnityEngine;

public class LootMagnet : MonoBehaviour
{
    [Header("Mıknatıs Ayarları")]
    public float magnetRadius = 4f; // Çekim alanının büyüklüğü
    public LayerMask lootLayer; // Inspector'dan sadece "Loot" katmanını seç

    void Update()
    {
        // Etraftaki Loot katmanındaki objeleri tarar
        Collider2D[] hitLoots = Physics2D.OverlapCircleAll(transform.position, magnetRadius, lootLayer);

        foreach (var hit in hitLoots)
        {
            if (hit.TryGetComponent(out Loot loot))
            {
                loot.Magnetize(transform);
            }
        }
    }

    // Sahne ekranında (Scene) çekim alanını sarı bir çember olarak görmeni sağlar
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}