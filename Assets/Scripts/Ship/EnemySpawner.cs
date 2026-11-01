using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Başlangıç Ayarları (Test İçin)")]
    public int initialSpawnCount = 10;

    [Header("Zamanla Artış (Survivor Zorluk)")]
    public float spawnInterval = 2f;
    public float spawnRadius = 15f;
    public int enemiesPerWave = 1;
    public float difficultyIncreaseTimer = 15f;
    public int extraEnemiesPerUpgrade = 1;

    [Header("Düşman Havuzu ve Seçim")]
    [Tooltip("Tikliyse tüm düşmanlardan rastgele seçer, tiksizse sadece alttaki Seçili Düşmanlar listesini kullanır.")]
    public bool spawnRandom = true;

    public GameObject[] allEnemyPrefabs; // Oyundaki tüm düşmanlar (Random açıksa buradan seçer)
    public GameObject[] selectedEnemyPrefabs; // Sadece o bölüm çıkmasını istediklerin (Random kapalıysa buradan)

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnEnemy();
        }

        StartCoroutine(SpawnRoutine());
        StartCoroutine(DifficultyRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

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
            enemiesPerWave += extraEnemiesPerUpgrade;
        }
    }

    // İLERİSİ İÇİN: Bölüm bittiğinde UI üzerinden veya Level Manager ile yeni düşmanları buraya gönderebilirsin.
    public void SetSelectedEnemies(GameObject[] newEnemies)
    {
        selectedEnemyPrefabs = newEnemies;
        spawnRandom = false; // Yeni liste geldiğinde rastgele doğmayı otomatik kapat
    }

    private void SpawnEnemy()
    {
        if (mainCamera == null) return;

        // Tikin durumuna göre hangi havuzdan düşman seçeceğimize karar veriyoruz
        GameObject[] spawnPool = spawnRandom ? allEnemyPrefabs : selectedEnemyPrefabs;

        // Seçilen havuz boşsa hata vermemesi için koruma
        if (spawnPool == null || spawnPool.Length == 0) return;

        int randomIndex = Random.Range(0, spawnPool.Length);
        GameObject selectedEnemy = spawnPool[randomIndex];

        if (selectedEnemy == null) return;

        Vector2 cameraPos = mainCamera.transform.position;
        float randomAngle = Random.Range(0f, 360f);

        Vector2 spawnDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
        Vector2 spawnPosition = cameraPos + (spawnDirection * spawnRadius);

        Instantiate(selectedEnemy, spawnPosition, Quaternion.identity);
    }
}