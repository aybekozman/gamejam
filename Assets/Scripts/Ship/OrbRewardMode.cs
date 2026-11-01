using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>The ship owns the selection; UI and loot read the same current mode.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerEnergy),typeof(PlayerExperience))]
public sealed class OrbRewardMode : MonoBehaviour
{
    public enum RewardKind { Energy, Experience }
    [SerializeField] private RewardKind startingMode=RewardKind.Experience;
    [SerializeField] private string toggleBinding="<Keyboard>/q";
    [SerializeField,Min(0)] private float energyPerPoint=1;
    public PlayerEnergy energy;
    public PlayerExperience experience;
    public RewardKind CurrentMode { get; private set; }=RewardKind.Experience;
    public event System.Action<RewardKind> ModeChanged;
    InputAction toggle;
    void Awake()
    {
        if(!energy) energy=GetComponent<PlayerEnergy>();
        if(!experience) experience=GetComponent<PlayerExperience>();
        CurrentMode=startingMode;
        toggle=new InputAction("Toggle orb reward",InputActionType.Button,toggleBinding);
    }
    void OnEnable() { toggle.performed+=OnToggle; toggle.Enable(); }
    void OnDisable() { toggle.performed-=OnToggle; toggle.Disable(); }
    void OnDestroy() { toggle?.Dispose(); }
    void OnToggle(InputAction.CallbackContext context) { ToggleMode(); }
    public bool ToggleMode()=>TrySelectMode(CurrentMode==RewardKind.Energy?RewardKind.Experience:RewardKind.Energy);
    public bool TrySelectMode(RewardKind mode)
    {
        if(!isActiveAndEnabled || Time.timeScale<=0 || (mode!=RewardKind.Energy && mode!=RewardKind.Experience)) return false;
        if(mode==RewardKind.Energy && !energy || mode==RewardKind.Experience && !experience) return false;
        if(CurrentMode==mode) return true;
        CurrentMode=mode;
        ModeChanged?.Invoke(mode);
        return true;
    }
    public bool TryAward(int value)
    {
        if(!isActiveAndEnabled || Time.timeScale<=0) return false;
        if(CurrentMode==RewardKind.Energy)
        {
            if(!energy) return false;
            energy.RestoreEnergy(Mathf.Max(0,value)*energyPerPoint);
        }
        else
        {
            if(!experience) return false;
            experience.AddExperience(Mathf.Max(0,value));
        }
        return true;
    }
}
