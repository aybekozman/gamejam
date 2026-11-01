using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Hedef Referansları (Manuel Atama)")]
    public Transform playerTransform; // Doğacak düşmanlara atanacak ana gemi Transform'u
    public Collider2D playerCollider; // EnemyFishVisual için gemi Collider'ı

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

    private Camera mainCamera;public int CampaignSpawned{get;private set;}public int CampaignBudget{get;private set;}public float campaignSeaLeft=-30;bool campaignWave;
    public void BeginCampaignWave(GameObject[] prefabs,int budget){StopAllCoroutines();mainCamera=Camera.main;SetSelectedEnemies(prefabs);CampaignSpawned=0;CampaignBudget=budget;campaignWave=true;StartCoroutine(SpawnRoutine());}
    public void StopCampaignWave(){campaignWave=false;StopAllCoroutines();}


    void Start()
    {
        mainCamera = Camera.main;
        if (playerTransform != null && playerCollider == null)
        {
            playerCollider = playerTransform.GetComponent<Collider2D>();
        }

        if(IntegratedCampaign.CampaignFlow.IsManagedShip)return;
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

            if(campaignWave&&CampaignSpawned>=CampaignBudget)yield break;
            for (int i = 0; i < (campaignWave?1:enemiesPerWave); i++)
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
        if(IntegratedCampaign.CampaignFlow.IsManagedShip&&!campaignWave)return;
        if(campaignWave&&CampaignSpawned>=CampaignBudget)return;

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

        if(campaignWave)spawnPosition.x=Mathf.Max(campaignSeaLeft,spawnPosition.x);
        GameObject spawnedObj = Instantiate(selectedEnemy, spawnPosition, Quaternion.identity);

        if(campaignWave)CampaignSpawned++;
        // Sahne taraması (FindAnyObjectByType) yapmadan, doğan düşmana gemi referansını doğrudan aktar
        if (playerTransform != null)
        {
            if (spawnedObj.TryGetComponent(out Enemy enemyScript) && enemyScript.player == null)
            {
                enemyScript.player = playerTransform;
            }

            if (spawnedObj.TryGetComponent(out EnemyFishVisual fishVisual))
            {
                if (fishVisual.ship == null) fishVisual.ship = playerTransform;
                if (fishVisual.shipCollider == null) fishVisual.shipCollider = playerCollider;
            }
        }
    }
}
