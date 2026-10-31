using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Başlangıç Ayarları (Test İçin)")]
    public int initialSpawnCount = 10; // Oyun başlar başlamaz bir kerede doğacak düşman sayısı

    [Header("Zamanla Artış (Survivor Zorluk)")]
    public float spawnInterval = 2f; // Dalga bekleme süresi
    public float spawnRadius = 15f;
    public int enemiesPerWave = 1; // Her dalgada (2 saniyede bir) kaç düşman doğacak
    public float difficultyIncreaseTimer = 15f; // Kaç saniyede bir düşman sayısı artacak
    public int extraEnemiesPerUpgrade = 1; // Zorluk artışında dalgaya eklenecek ekstra düşman

    [Header("Düşmanlar")]
    public GameObject[] enemyPrefabs;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // 1. Oyun başladığı an test/aksiyon için toplu düşman doğur
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnEnemy();
        }

        // Sürekli doğma ve zorluk artırma döngülerini başlat
        StartCoroutine(SpawnRoutine());
        StartCoroutine(DifficultyRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // O anki zorluk seviyesi (enemiesPerWave) kadar düşman doğur
            for (int i = 0; i < enemiesPerWave; i++)
            {
                SpawnEnemy();
            }
        }
    }

    private IEnumerator DifficultyRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(difficultyIncreaseTimer);
            enemiesPerWave += extraEnemiesPerUpgrade; // Her 15 saniyede bir, dalga başına düşen sayıyı artır
        }
    }

    private void SpawnEnemy()
    {
        if (mainCamera == null || enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject selectedEnemy = enemyPrefabs[randomIndex];

        if (selectedEnemy == null) return;

        Vector2 cameraPos = mainCamera.transform.position;
        float randomAngle = Random.Range(0f, 360f);

        // Matematiksel çarpım hatası düzeltildi
        Vector2 spawnDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
        Vector2 spawnPosition = cameraPos + (spawnDirection * spawnRadius);

        Instantiate(selectedEnemy, spawnPosition, Quaternion.identity);
    }
}