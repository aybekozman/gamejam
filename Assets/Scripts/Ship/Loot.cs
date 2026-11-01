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
    private bool collected;
    private PlayerExperience recipient;

    public void Magnetize(Transform playerTransform)
    {
        if (!isMagnetized)
        {
            isMagnetized = true;
            target = playerTransform;
            recipient = target.GetComponentInParent<PlayerExperience>();
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
        if (collected) return;
        if (recipient) { TryCollect(recipient); return; }
        // Existing scenes without an XP owner retain their previous collection behaviour.
        collected = true;
        Destroy(gameObject);
    }
    public bool TryCollect(PlayerExperience player)
    {
        if (collected || !player) return false;
        collected = true; // Lock before emitting events; the reward cannot be re-entered.
        var selection=player.GetComponent<OrbRewardMode>();
        if(selection && selection.enabled)
        {
            // Resolve at collection, not magnetization: in-flight orbs follow the latest selection.
            if(!selection.TryAward(expValue)) { collected=false; return false; }
        }
        else player.AddExperience(Mathf.Max(0,expValue));
        foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled=false;
        Destroy(gameObject);
        return true;
    }
}
