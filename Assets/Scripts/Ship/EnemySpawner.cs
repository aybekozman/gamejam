using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Doğma (Spawn) Ayarları")]
    public GameObject[] enemyPrefabs; // Artık bir dizi. Inspector'dan boyutunu belirleyip prefabları sürükle.
    public float spawnInterval = 2f;
    public float spawnRadius = 15f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        // Kamera yoksa veya dizi boşsa işlemi iptal et
        if (mainCamera == null || enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        // Diziden rastgele bir düşman prefabı seç
        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject selectedEnemy = enemyPrefabs[randomIndex];

        if (selectedEnemy == null) return;

        Vector2 cameraPos = mainCamera.transform.position;
        float randomAngle = Random.Range(0f, 360f);
        Vector2 spawnDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
        Vector2 spawnPosition = cameraPos + (spawnDirection * spawnRadius);

        Instantiate(selectedEnemy, spawnPosition, Quaternion.identity);
    }
}