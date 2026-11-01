using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerEnergy : MonoBehaviour
{
    [SerializeField, Min(1)] private float maxEnergy = 100;
    [SerializeField, Min(0)] private float regenerationPerSecond = 5;
    public float CurrentEnergy { get; private set; }
    public float MaxEnergy => Mathf.Max(1, maxEnergy);
    public float RegenerationPerSecond { get => regenerationPerSecond; set => regenerationPerSecond = Mathf.Max(0,value); }
    public event System.Action<float,float> EnergyChanged;
    void Awake() { CurrentEnergy = MaxEnergy; }
    void Update()
    {
        if (Time.deltaTime > 0 && CurrentEnergy < MaxEnergy) RestoreEnergy(regenerationPerSecond * Time.deltaTime);
    }
    public bool TryConsumeEnergy(float amount)
    {
        if (amount < 0 || amount > CurrentEnergy) return false;
        CurrentEnergy -= amount;
        EnergyChanged?.Invoke(CurrentEnergy,MaxEnergy);
        return true;
    }
    public void RestoreEnergy(float amount)
    {
        if (amount <= 0) return;
        CurrentEnergy = Mathf.Min(MaxEnergy,CurrentEnergy+amount);
        EnergyChanged?.Invoke(CurrentEnergy,MaxEnergy);
    }
    public void SetMaxEnergy(float maximum)
    {
        maxEnergy = Mathf.Max(1,maximum);
        CurrentEnergy = Mathf.Min(CurrentEnergy,MaxEnergy);
        EnergyChanged?.Invoke(CurrentEnergy,MaxEnergy);
    }
}
