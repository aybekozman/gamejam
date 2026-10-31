using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Doğma (Spawn) Ayarları")]
    public GameObject enemyPrefab; // Hazırladığın balık prefabını buraya sürükle
    public float spawnInterval = 2f; // Kaç saniyede bir düşman çıkacak
    public float spawnRadius = 15f; // Kameranın merkezinden ne kadar uzakta doğacağı

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        // Oyun çalıştığı sürece sonsuz döngüde düşman üretir
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (mainCamera == null || enemyPrefab == null) return;

        // Kameranın anlık pozisyonunu merkez al
        Vector2 cameraPos = mainCamera.transform.position;

        // 0 ile 360 derece arasında rastgele bir açı seç
        float randomAngle = Random.Range(0f, 360f);

        // Bu açıyı (Radyan cinsinden) X ve Y yön vektörüne çevir
        Vector2 spawnDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));

        // Yön ile mesafeyi çarpıp kameranın pozisyonuna ekleyerek kesin doğma noktasını bul
        Vector2 spawnPosition = cameraPos + (spawnDirection * spawnRadius);

        // Düşmanı sahneye fırlat
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}