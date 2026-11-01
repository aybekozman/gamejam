using UnityEngine;

public class Loot : MonoBehaviour
{
    [Header("Referanslar (Manuel Atama)")]
    public ShipUIManager uiManager;
    public ShipController shipController;

    [Header("Toplanma Ayarları")]
    public float collectDistance = 0.5f; // Ne kadar yaklaşınca toplanmış sayılacak
    public float baseMoveSpeed = 5f;
    public float acceleration = 15f; // Gemiye doğru uçarken giderek hızlanması için

    [Header("Ödül Değerleri (Aktif Moda Göre)")]
    public int expValue = 1;         // XP modundayken vereceği tecrübe puanı
    public float energyValue = 15f;  // Enerji modundayken dolduracağı yakıt miktarı

    private bool isMagnetized = false;
    private Transform target;
    private float currentSpeed;

    public void Magnetize(Transform playerTransform)
    {
        if (!isMagnetized)
        {
            isMagnetized = true;
            target = playerTransform;
            currentSpeed = baseMoveSpeed;
        }
    }

    void Update()
    {
        if (isMagnetized && target != null)
        {
            // Oyuncuya doğru ivmeli hareket et
            transform.position = Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);
            currentSpeed += acceleration * Time.deltaTime;

            // Yeterince yaklaştıysa topla
            if (Vector2.Distance(transform.position, target.position) < collectDistance)
            {
                Collect();
            }
        }
    }

    private void Collect()
    {
        if (uiManager != null)
        {
            if (uiManager.isLootingXP)
            {
                uiManager.AddXP(expValue);
            }
            else if (shipController != null)
            {
                shipController.AddEnergy(energyValue);
            }
        }
        Destroy(gameObject);
    }
}
