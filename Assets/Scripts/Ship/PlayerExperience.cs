using UnityEngine;

/// <summary>Player XP only; independent of ship weapons and expedition difficulty.</summary>
[DisallowMultipleComponent]
public sealed class PlayerExperience : MonoBehaviour
{
    [SerializeField, Min(1)] private int baseRequiredXP = 100;
    [SerializeField, Min(1)] private float requirementGrowth = 1.35f;
    [SerializeField, Min(1)] private int playerLevel = 1;
    [SerializeField] private long currentXP;
    public int PlayerLevel => playerLevel;
    public long CurrentXP => currentXP;
    public int RequiredXP => (int)System.Math.Max(1,System.Math.Min(int.MaxValue,
        System.Math.Round(baseRequiredXP * System.Math.Pow(System.Math.Max(1,requirementGrowth),playerLevel-1),System.MidpointRounding.AwayFromZero)));
    public event System.Action<long,int,int> ExperienceChanged;
    public event System.Action<int> LevelUp;
    public void RestoreCampaignProgress(int level,long xp){playerLevel=level;currentXP=xp;ExperienceChanged?.Invoke(currentXP,RequiredXP,playerLevel);}
    public void AddExperience(int amount)
    {
        if (amount <= 0) return;
        currentXP += amount;
        while (currentXP >= RequiredXP && playerLevel < int.MaxValue)
        {
            currentXP -= RequiredXP;
            playerLevel++;
            LevelUp?.Invoke(playerLevel);
        }
        ExperienceChanged?.Invoke(currentXP,RequiredXP,playerLevel);
    }
}
