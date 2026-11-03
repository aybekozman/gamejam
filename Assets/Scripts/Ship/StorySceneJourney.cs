using System.Collections;
using System.Linq;
using Abyss.Story;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public enum JourneyStage { FirstUnderwater, FirstShip, SecondUnderwater, ShipBattle, ThirdUnderwater, SharkBattle, WhaleBattle, Finale }

/// <summary>Authored chapter boundaries and the final combined shark/whale visit.</summary>
public sealed class StorySceneJourney : MonoBehaviour
{
    public const string UnderwaterPath = "Assets/Scenes/UnderwaterWorld.unity";
    public const string ShipPath = "Assets/Scenes/ShipScene 1.unity";
    public static StorySceneJourney Instance { get; private set; }
    public JourneyStage Stage { get; private set; } = JourneyStage.FirstUnderwater;
    public bool IsTransitioning { get; private set; }
    public bool JourneyStarted { get; private set; }
    public int TransitionCount { get; private set; }
    ShipVisitSnapshot shipState;
    ShipWaterPurification purification;
    ShipUIManager shipUI;
    ShipController ship;
    UnderwaterSecondChapter second;
    bool firstShipCompletePending,whalesJoined;
    GameObject[] finalEnemyPool;EnemySpawner producer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance) return;
        var go = new GameObject("Story scene journey (session only)");
        go.AddComponent<StorySceneJourney>();
    }
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);gameObject.AddComponent<VoyageMusic>();
        SceneManager.sceneLoaded += SceneLoaded;
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (Instance == this) Instance = null;
    }
    public void NewGame()
    {
        if (IsTransitioning) return;
        VoyageTutorial.ResetLessons();
        Stage = JourneyStage.FirstUnderwater; shipState = null; firstShipCompletePending = false; JourneyStarted = true; TransitionCount = 0;
        StartCoroutine(Load(UnderwaterPath));
    }
    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.path != UnderwaterPath && scene.path != ShipPath) return;
        if (scene.path == UnderwaterPath)
        {
            JourneyStarted = true;
            var story = FindIn<UnderwaterDayStory>(scene);
            if (!story || !story.secondChapter) { Debug.LogError("Journey: authored underwater story missing"); return; }
            second = story.secondChapter;
            // Disable development shortcuts before the authored story's Start runs.
            second.skipShipForDevelopment = false; second.startHereInEditor = false;
            if (story.thirdChapter) story.thirdChapter.startHereInEditor = false;
            second.firstChapterDepartureRequested.AddListener(FirstUnderwaterFinished);
            second.secondChapterCompleted.AddListener(SecondUnderwaterFinished);
            if(story.thirdChapter) story.thirdChapter.chapterCompleted.AddListener(ThirdUnderwaterFinished);
        }
        if(scene.path==ShipPath){
            producer=FindIn<EnemySpawner>(scene);
            whalesJoined=false;finalEnemyPool=producer?producer.allEnemyPrefabs:null;
            if(producer){producer.enabled=false;if(Stage!=JourneyStage.FirstShip){producer.initialSpawnCount=Mathf.Max(6,Mathf.CeilToInt(producer.initialSpawnCount*1.5f));producer.enemiesPerWave+=1;producer.spawnInterval*=.75f;var vessel=FindIn<ShipController>(scene);if(vessel)vessel.gameObject.AddComponent<HarborAttackGate>().Configure(producer);}}
            if(producer){var pool=producer.spawnRandom?producer.allEnemyPrefabs:producer.selectedEnemyPrefabs;
                if(pool!=null){var selected=pool.Where(p=>p&&AllowedEnemy(p.name,Stage)).ToArray();if(selected.Length>0)producer.SetSelectedEnemies(selected);}}
        }
        if(scene.path==ShipPath){var vessel=FindIn<ShipController>(scene);if(vessel){VoyageHarbor.Place(vessel.transform);}}
        var support=new GameObject("Voyage scene support");SceneManager.MoveGameObjectToScene(support,scene);support.AddComponent<VoyagePauseMenu>();support.AddComponent<VoyageTutorial>();if(scene.path==UnderwaterPath)support.AddComponent<VoyageFishScale>();
        var entry = new GameObject("Scene journey entry");
        SceneManager.MoveGameObjectToScene(entry, scene);
        entry.AddComponent<StorySceneEntry>();
    }
    public static T FindIn<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).FirstOrDefault();
    internal void EnterAfterSceneStart(Scene scene)
    {
        if (scene.path == UnderwaterPath)
        {
            if (Stage == JourneyStage.SecondUnderwater) second.BeginSecondChapter();
            if(Stage == JourneyStage.ThirdUnderwater) second.story.thirdChapter.BeginThirdChapter();
        }
        else if (scene.path == ShipPath)
        {
            ship = FindIn<ShipController>(scene); shipUI = FindIn<ShipUIManager>(scene);
            if (!ship) { Debug.LogError("Journey: authored ship missing"); return; }
            ShipPresentationBindings.Configure(scene,shipUI);
            purification = ship.GetComponent<ShipWaterPurification>();
            if (!purification) purification = ship.gameObject.AddComponent<ShipWaterPurification>();
            var toggle = FindIn<OrbRewardToggleView>(scene);
            var bar = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RectTransform>(true))
                .FirstOrDefault(r => r.name == "WaterPurificationBar");
            if (bar)
            {
                purification.fill = bar.Find("Fill").GetComponent<UnityEngine.UI.Image>();
                purification.percentage = bar.Find("PurifiedWaterPercent").GetComponent<TMPro.TMP_Text>();
            }
            if (toggle) { toggle.lootMode = shipUI; toggle.Refresh(true); }
            if (JourneyStarted && Stage >= JourneyStage.ShipBattle && shipState != null) shipState.Restore(ship.gameObject, shipUI, purification);
            else ShipVisitSnapshot.ResetVisit(ship.gameObject, shipUI, purification);
            ship.gameObject.AddComponent<ShipVisitSupport>();ship.gameObject.AddComponent<VoyageSurfaceEnvironment>();
            if(Stage==JourneyStage.FirstShip){purification.forwardDistanceForFull=420;ship.gameObject.AddComponent<PeacefulEnergyField>();}
            ship.gameObject.AddComponent<ShipPipeDischarge>();
            purification.Refresh();
            purification.Completed += FirstShipPurified;
            var dialogue=ship.gameObject.AddComponent<ShipVisitDialogue>();dialogue.Open(Stage, JourneyStarted);
        }
        IsTransitioning = false;
        // Start/end of the transition only; ordinary scene time, controls and spawners are untouched.
        if (resumeAfterLoad) { Time.timeScale = 1; resumeAfterLoad = false; }
        if (JourneyStarted && Stage == JourneyStage.FirstShip && purification && purification.NormalizedProgress >= 1) FirstShipPurified();
    }
    public static bool AllowedEnemy(string name,JourneyStage stage){bool shark=name.Contains("Shark")||name.Contains("Köpek");bool large=name.Contains("Whale")||name.Contains("Orca");return stage==JourneyStage.WhaleBattle||(!large&&(stage==JourneyStage.SharkBattle||!shark));}
    void Update(){if(Keyboard.current!=null&&Keyboard.current.pKey.wasPressedThisFrame)SkipCurrentPart();}
    public void SkipCurrentPart(){
        var scene=SceneManager.GetActiveScene();if(IsTransitioning||(scene.path!=UnderwaterPath&&scene.path!=ShipPath))return;
        if(Stage==JourneyStage.Finale){Time.timeScale=1;SceneManager.LoadScene("MainMenu");return;}
        if(VoyagePauseMenu.IsOpen)FindFirstObjectByType<VoyagePauseMenu>().Close();
        if(scene.path==ShipPath&&shipUI&&ship){shipState=ShipVisitSnapshot.Capture(ship.gameObject,shipUI,purification);shipUI.DismissForStory();ship.GetComponent<ShipVisitDialogue>()?.CancelForEnding();}
        firstShipCompletePending=false;JourneyStarted=true;
        Stage=Stage switch {JourneyStage.FirstUnderwater=>JourneyStage.FirstShip,JourneyStage.FirstShip=>JourneyStage.SecondUnderwater,JourneyStage.SecondUnderwater=>JourneyStage.ShipBattle,JourneyStage.ShipBattle=>JourneyStage.ThirdUnderwater,JourneyStage.ThirdUnderwater=>JourneyStage.SharkBattle,_=>JourneyStage.Finale};
        if(Stage==JourneyStage.Finale){Time.timeScale=0;ship.gameObject.AddComponent<VoyageEnding>();}else StartCoroutine(Load(IsShipStage(Stage)?ShipPath:UnderwaterPath));
    }
    public void RetryShip(){if(IsTransitioning||Stage==JourneyStage.Finale||SceneManager.GetActiveScene().path!=ShipPath)return;firstShipCompletePending=false;StartCoroutine(Load(ShipPath));}
    bool resumeAfterLoad;
    void FirstUnderwaterFinished()
    {
        if (!JourneyStarted || IsTransitioning || Stage != JourneyStage.FirstUnderwater || !second || !second.story.Complete || !second.story.NearHome) return;
        Stage = JourneyStage.FirstShip; StartCoroutine(Load(ShipPath));
    }
    void FirstShipPurified()
    {
        if (!JourneyStarted || IsTransitioning || !IsShipStage(Stage) || !purification || purification.NormalizedProgress < 1) return;
        firstShipCompletePending = true;
    }
    void LateUpdate()
    {
        // Let the existing frame finish awarding loot. An open upgrade selection pauses time;
        // leave it intact until the player picks a card, then carry the applied stats.
        if(IsTransitioning||!IsShipStage(Stage)||!ship||Time.timeScale<=0)return;
        var speech=ship.GetComponent<ShipVisitDialogue>();if(!speech||!speech.ReadyForGameplay)return;
        if(ship.currentEnergy<=0||ship.GetComponent<PlayerHealth>().IsDead)return;
        if(Stage==JourneyStage.SharkBattle&&!whalesJoined&&(!ship.GetComponent<HarborAttackGate>()||ship.GetComponent<HarborAttackGate>().HasDeparted)&&purification&&purification.NormalizedProgress>=.2f){
            whalesJoined=true;
            speech.WhalesArrive(()=>{if(producer&&Stage==JourneyStage.SharkBattle){producer.SetSelectedEnemies(finalEnemyPool);var whale=finalEnemyPool?.FirstOrDefault(p=>p&&p.name.Contains("Whale"));if(whale)producer.SpawnReinforcement(whale);}});return;
        }
        if(!firstShipCompletePending)return;
        if(ship&&(ship.currentEnergy<=0||ship.GetComponent<PlayerHealth>().IsDead))return;
        firstShipCompletePending = false;
        var completedStage = Stage;
        ship.GetComponent<ShipVisitDialogue>().CompleteVisit(completedStage,()=>{
            if(IsTransitioning || Stage!=completedStage)return;
            shipState=ShipVisitSnapshot.Capture(ship.gameObject,shipUI,purification);
            Stage=completedStage switch {
                JourneyStage.FirstShip=>JourneyStage.SecondUnderwater,
                JourneyStage.ShipBattle=>JourneyStage.ThirdUnderwater,
                JourneyStage.SharkBattle=>JourneyStage.Finale,
                _=>JourneyStage.Finale
            };
            if(Stage==JourneyStage.Finale) {Time.timeScale=0;ship.gameObject.AddComponent<VoyageEnding>();}
            else StartCoroutine(Load(Stage==JourneyStage.WhaleBattle?ShipPath:UnderwaterPath));
        });
    }
    static bool IsShipStage(JourneyStage stage)=>stage==JourneyStage.FirstShip||stage==JourneyStage.ShipBattle||stage==JourneyStage.SharkBattle||stage==JourneyStage.WhaleBattle;
    void ThirdUnderwaterFinished(){
        if(!JourneyStarted||IsTransitioning||Stage!=JourneyStage.ThirdUnderwater||!second.story.thirdChapter.Complete)return;
        Stage=JourneyStage.SharkBattle;StartCoroutine(Load(ShipPath));
    }

    void SecondUnderwaterFinished()
    {
        if (!JourneyStarted || IsTransitioning || Stage != JourneyStage.SecondUnderwater || !second || !second.Complete) return;
        Stage = JourneyStage.ShipBattle; StartCoroutine(Load(ShipPath));
    }
    IEnumerator Load(string path)
    {
        if (IsTransitioning) yield break;
        IsTransitioning = true; resumeAfterLoad = true; TransitionCount++;
        Time.timeScale = 0;
        var current = SceneManager.GetActiveScene();
        if (current.path == ShipPath)
        {
            var input = FindIn<ShipController>(current);
            if (input) input.enabled = false;
        }
        else if (current.path == UnderwaterPath)
        {
            var story = FindIn<UnderwaterDayStory>(current);
            if (story) story.player.ControlsLocked = true;
        }
        var operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        if (operation == null) { IsTransitioning = false; resumeAfterLoad = false; Time.timeScale = 1; Debug.LogError("Journey: scene could not load: " + path); yield break; }
        yield return operation;
        // Entry.Start finishes initialization, not an assumed wait duration.
    }
}
