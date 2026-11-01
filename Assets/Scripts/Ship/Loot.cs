using UnityEngine;

public class Loot : MonoBehaviour
{
    [Header("Toplanma Ayarları")]
    public float collectDistance = 0.5f; // Ne kadar yaklaşınca toplanmış sayılacak
    public float baseMoveSpeed = 5f;
    public float acceleration = 15f; // Gemiye doğru uçarken giderek hızlanması için
    public int expValue = 1; // İleride level sistemi için vereceği tecrübe puanı

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
        // İleride buraya Exp/Para barını dolduracak kodu ekleyeceğiz
        Debug.Log("Ganimet toplandı! Değer: " + expValue);
        Destroy(gameObject);
    }
}