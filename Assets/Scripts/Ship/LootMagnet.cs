using UnityEngine;

public class LootMagnet : MonoBehaviour
{
    [Header("Referanslar (Manuel Atama)")]
    public ShipUIManager uiManager;
    public ShipController shipController;

    [Header("Mıknatıs Ayarları")]
    public float magnetRadius = 5f;        // Ganimetleri çekmeye başlayacağı mesafe
    public float pullSpeed = 8f;           // Ganimetlerin gemiye uçuş hızı
    public float collectDistance = 0.8f;   // Gemiye ne kadar yaklaşınca toplanacağı
    public float defaultXPValue = 1f;      // Loot scripti yoksa verilecek varsayılan XP
    public float defaultEnergyValue = 15f; // Enerji modundayken verilecek varsayılan Enerji
    public LayerMask lootLayer;            // Inspector'dan "Loot" katmanını seç

    private PlayerExperience playerExperience;
    private OrbRewardMode orbRewardMode;

    void Awake()
    {
        if (shipController == null) shipController = GetComponent<ShipController>();
        playerExperience = GetComponent<PlayerExperience>();
        orbRewardMode = GetComponent<OrbRewardMode>();
    }

    void Update()
    {
        // Çekim alanındaki tüm ganimetleri radarla bul
        Collider2D[] loots = Physics2D.OverlapCircleAll(transform.position, magnetRadius, lootLayer);

        foreach (Collider2D loot in loots)
        {
            if (loot == null) continue;

            // Ganimeti yavaşça gemiye doğru kaydır
            loot.transform.position = Vector2.MoveTowards(loot.transform.position, transform.position, pullSpeed * Time.deltaTime);

            // Gemiye yeterince yaklaştıysa aktif moda göre SADECE XP veya SADECE Enerji olarak topla
            if (Vector2.Distance(loot.transform.position, transform.position) <= collectDistance)
            {
                float xpToGain = defaultXPValue;
                float energyToGain = defaultEnergyValue;

                if (loot.TryGetComponent(out Loot lootItem))
                {
                    xpToGain = lootItem.expValue;
                    energyToGain = lootItem.energyValue;

                    if (playerExperience != null || (orbRewardMode != null && orbRewardMode.isActiveAndEnabled))
                    {
                        lootItem.TryCollect(playerExperience);
                        continue;
                    }
                }

                if (uiManager != null)
                {
                    if (uiManager.isLootingXP)
                    {
                        uiManager.AddXP(xpToGain);
                    }
                    else if (shipController != null)
                    {
                        shipController.AddEnergy(energyToGain);
                    }
                }

                Destroy(loot.gameObject);
            }
        }
    }

    // Seçiliyken editörde çekim alanını yeşil bir çemberle gösterir
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}
