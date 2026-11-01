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
    private bool collected = false;
    private Transform target;
    private float currentSpeed;

    public void Magnetize(Transform playerTransform)
    {
        if (!isMagnetized && !collected)
        {
            isMagnetized = true;
            target = playerTransform;
            currentSpeed = baseMoveSpeed;
        }
    }

    void Update()
    {
        if (isMagnetized && target != null && !collected)
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

    public bool TryCollect(PlayerExperience experience)
    {
        if (collected) return false;

        if (experience != null)
        {
            OrbRewardMode rewardMode = experience.GetComponent<OrbRewardMode>();
            if (rewardMode != null && rewardMode.isActiveAndEnabled)
            {
                if (!rewardMode.TryAward(expValue)) return false;
            }
            else
            {
                experience.AddExperience(expValue);
            }
        }

        collected = true;
        Destroy(gameObject);
        return true;
    }

    private void Collect()
    {
        if (collected) return;

        if (target != null)
        {
            OrbRewardMode rewardMode = target.GetComponent<OrbRewardMode>();
            if (rewardMode != null && rewardMode.isActiveAndEnabled)
            {
                rewardMode.TryAward(expValue);
                collected = true;
                Destroy(gameObject);
                return;
            }

            PlayerExperience exp = target.GetComponent<PlayerExperience>();
            if (exp != null)
            {
                TryCollect(exp);
                return;
            }
        }

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

        collected = true;
        Destroy(gameObject);
    }
}
