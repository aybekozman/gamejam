using UnityEngine;

/// <summary>Runs after existing scene Start methods, including story reset and ship initialization.</summary>
[DefaultExecutionOrder(10000)]
public sealed class StorySceneEntry : MonoBehaviour
{
    void Start()
    {
        if (StorySceneJourney.Instance) StorySceneJourney.Instance.EnterAfterSceneStart(gameObject.scene);
    }
}
