using System.Collections;
using System.Linq;
using Abyss.Story;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum JourneyStage { FirstUnderwater, FirstShip, SecondUnderwater, ShipBattle }

/// <summary>Only the four scene visits. Does not control enemy spawning or battle duration.</summary>
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
    bool firstShipCompletePending;

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
        Instance = this; DontDestroyOnLoad(gameObject);
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
        }
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
        }
        else if (scene.path == ShipPath)
        {
            ship = FindIn<ShipController>(scene); shipUI = FindIn<ShipUIManager>(scene);
            if (!ship) { Debug.LogError("Journey: authored ship missing"); return; }
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
            if (JourneyStarted && Stage == JourneyStage.ShipBattle && shipState != null) shipState.Restore(ship.gameObject, shipUI, purification);
            purification.Refresh();
            purification.Completed += FirstShipPurified;
        }
        IsTransitioning = false;
        // Start/end of the transition only; ordinary scene time, controls and spawners are untouched.
        if (resumeAfterLoad) { Time.timeScale = 1; resumeAfterLoad = false; }
        if (JourneyStarted && Stage == JourneyStage.FirstShip && purification && purification.NormalizedProgress >= 1) FirstShipPurified();
    }
    bool resumeAfterLoad;
    void FirstUnderwaterFinished()
    {
        if (!JourneyStarted || IsTransitioning || Stage != JourneyStage.FirstUnderwater || !second || !second.story.Complete || !second.story.NearHome) return;
        Stage = JourneyStage.FirstShip; StartCoroutine(Load(ShipPath));
    }
    void FirstShipPurified()
    {
        if (!JourneyStarted || IsTransitioning || Stage != JourneyStage.FirstShip || !purification || purification.NormalizedProgress < 1) return;
        firstShipCompletePending = true;
    }
    void LateUpdate()
    {
        // Let the existing frame finish awarding loot. An open upgrade selection pauses time;
        // leave it intact until the player picks a card, then carry the applied stats.
        if (!firstShipCompletePending || IsTransitioning || Stage != JourneyStage.FirstShip || Time.timeScale <= 0) return;
        firstShipCompletePending = false;
        shipState = ShipVisitSnapshot.Capture(ship.gameObject, shipUI, purification);
        Stage = JourneyStage.SecondUnderwater; StartCoroutine(Load(UnderwaterPath));
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
