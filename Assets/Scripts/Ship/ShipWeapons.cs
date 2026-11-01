using UnityEngine;
using System.Collections.Generic; // Sözlük (Dictionary) kullanmak için ekledik
using Unity.Cinemachine; // Ekran sallantısı için

public class ShipWeapons : MonoBehaviour
{
    [Header("Referanslar")]
    public ShipController shipController; // Enerji kontrolü için ana gemi referansı

    [Header("Silah Seviyesi")]
    [Range(1, 3)] public int shipLevel = 1;

    [Header("Atış Ayarları")]
    public float fireRate = 0.5f;
    public float attackRange = 10f;
    public LayerMask enemyLayer;

    [Header("Akıllı Hedefleme (Smart Targeting)")]
    // Bir düşmana aynı atış turunda en fazla kaç top mermi sıksın? (Tek atan mermiler için 1 yap)
    public int maxShotsPerEnemy = 1;

    [Header("Merkezi Ekran Sarsıntısı")]
    public CinemachineImpulseSource impulseSource;

    [Header("Taretler (Önce Sağlar, Sonra Sollar)")]
    public Turret[] allTurrets;

    private float fireTimer;

    void Start()
    {
        if (shipController == null) shipController = GetComponent<ShipController>();
        if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    void Update()
    {
        ManageTurretLevels();

        // Geminin enerjisi bittiyse (currentEnergy <= 0) silahlar KESİNLİKLE ATEŞ EDEMESİN
        if (shipController != null && shipController.currentEnergy <= 0f)
        {
            return;
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0)
        {
            FireActiveWeapons();
            fireTimer = fireRate;
        }
    }

    private void ManageTurretLevels()
    {
        int turretsToUse = Mathf.Min(shipLevel * 2, allTurrets.Length);

        for (int i = 0; i < allTurrets.Length; i++)
        {
            allTurrets[i].gameObject.SetActive(i < turretsToUse);
        }
    }

    private void FireActiveWeapons()
    {
        if (shipController != null && shipController.currentEnergy <= 0f) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        if (hitEnemies.Length == 0) return;

        int turretsToUse = Mathf.Min(shipLevel * 2, allTurrets.Length);
        bool firedAny = false; // Bu tur ateş edildi mi?

        // Hangi düşmana bu turda kaç mermi kilitlendiğini hafızada tutan sözlük
        Dictionary<Transform, int> targetHitCounts = new Dictionary<Transform, int>();

        for (int i = 0; i < turretsToUse; i++)
        {
            Turret currentTurret = allTurrets[i];
            bool isRightTurret = currentTurret.transform.localPosition.x > 0;

            Transform bestTarget = null;
            float minDistance = float.MaxValue;

            foreach (var enemyCol in hitEnemies)
            {
                Vector2 dirToEnemy = (enemyCol.transform.position - transform.position).normalized;
                float dotProduct = Vector2.Dot(transform.right, dirToEnemy);
                bool isEnemyOnRight = dotProduct > 0;

                if (isRightTurret == isEnemyOnRight)
                {
                    // Bu düşmana daha önce kaç top nişan almış kontrol et
                    targetHitCounts.TryGetValue(enemyCol.transform, out int currentHits);

                    // Eğer düşman henüz kapasitesini doldurmadıysa onu hedef al
                    if (currentHits < maxShotsPerEnemy)
                    {
                        float dist = Vector2.Distance(currentTurret.transform.position, enemyCol.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestTarget = enemyCol.transform;
                        }
                    }
                }
            }

            // Uygun hedef bulunduysa ateşle ve sözlüğe yaz
            if (bestTarget != null)
            {
                currentTurret.AimAt(bestTarget);
                currentTurret.Shoot();

                if (targetHitCounts.ContainsKey(bestTarget))
                    targetHitCounts[bestTarget]++;
                else
                    targetHitCounts.Add(bestTarget, 1);

                firedAny = true;
            }
        }

        // Eğer en az bir top ateşlediyse kamerayı SADECE 1 KEZ salla
        if (firedAny && impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
    }
}
