using System;
using TMPro;
using UnityEngine;

/// <summary>Counts actual forward travel; independent of combat, energy and XP.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D))]
public sealed class ShipWaterPurification : MonoBehaviour
{
    [Min(1)] public float forwardDistanceForFull = 1000;
    public UnityEngine.UI.Image fill;
    public TMP_Text percentage;
    [SerializeField, Range(0, 1)] float normalizedProgress;
    public float NormalizedProgress => normalizedProgress;
    public event Action Completed;
    Rigidbody2D body;
    bool completionRaised;

    void Awake() { body = GetComponent<Rigidbody2D>(); }
    void Start() { Refresh(); }
    void FixedUpdate()
    {
        if (normalizedProgress >= 1 || Time.timeScale <= 0) return;
        float speed = Mathf.Max(0, Vector2.Dot(body.linearVelocity, transform.up));
        if (speed < .05f) return;
        normalizedProgress = Mathf.Clamp01(normalizedProgress + speed * Time.fixedDeltaTime / Mathf.Max(1, forwardDistanceForFull));
        Refresh();
        if (normalizedProgress >= 1 && !completionRaised)
        {
            completionRaised = true;
            Completed?.Invoke();
        }
    }
    public void RestoreProgress(float value)
    {
        normalizedProgress = Mathf.Clamp01(value);
        completionRaised = normalizedProgress >= 1;
        Refresh();
    }
    public void Refresh()
    {
        if (fill) fill.fillAmount = normalizedProgress;
        if (percentage) percentage.text = "%" + Mathf.FloorToInt(normalizedProgress * 100);
    }
}
