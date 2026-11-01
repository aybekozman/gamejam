using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Compatibility adapter for the existing UI Toolkit HUD in other scenes.
/// Reads the same gameplay state as the new uGUI HUD; owns no cooldown or XP values.</summary>
public class ShipUIManager : MonoBehaviour
{
    public Transform shipTransform;
    ShipController ship;
    PlayerHealth health;
    PlayerEnergy energy;
    PlayerExperience experience;
    VisualElement healthFill, dashFill, energyFill, xpFill, xpContainer, energyContainer, needle;
    Toggle modeToggle;
    void OnEnable()
    {
        if (!TryGetComponent<UIDocument>(out var document) || document.rootVisualElement==null) return;
        if (shipTransform)
        {
            ship=shipTransform.GetComponent<ShipController>();
            health=shipTransform.GetComponent<PlayerHealth>();
            energy=shipTransform.GetComponent<PlayerEnergy>();
            experience=shipTransform.GetComponent<PlayerExperience>();
        }
        var root=document.rootVisualElement;
        healthFill=root.Q<VisualElement>("HealthFill"); dashFill=root.Q<VisualElement>("DashFill");
        energyFill=root.Q<VisualElement>("EnergyFill"); xpFill=root.Q<VisualElement>("XPFill");
        xpContainer=root.Q<VisualElement>("XPBarContainer"); energyContainer=root.Q<VisualElement>("EnergyBarContainer");
        needle=root.Q<VisualElement>("CompassNeedle"); modeToggle=root.Q<Toggle>("ModeToggle");
        if(modeToggle!=null) { modeToggle.RegisterValueChangedCallback(OnModeChanged); ApplyMode(modeToggle.value); }
    }
    void OnDisable() { if(modeToggle!=null) modeToggle.UnregisterValueChangedCallback(OnModeChanged); }
    void OnModeChanged(ChangeEvent<bool> change) { ApplyMode(change.newValue); }
    void ApplyMode(bool xp)
    {
        if(xpContainer!=null) xpContainer.style.display=xp?DisplayStyle.Flex:DisplayStyle.None;
        if(energyContainer!=null) energyContainer.style.display=xp?DisplayStyle.None:DisplayStyle.Flex;
    }
    void Update()
    {
        if(shipTransform && needle!=null) needle.style.rotate=new StyleRotate(new Rotate(new Angle(-shipTransform.eulerAngles.z,AngleUnit.Degree)));
        if(health) UpdateBar(healthFill,100*health.CurrentHealth/health.MaxHealth);
        if(energy) UpdateBar(energyFill,100*energy.CurrentEnergy/energy.MaxEnergy);
        if(experience) UpdateBar(xpFill,100f*experience.CurrentXP/experience.RequiredXP);
        if(ship)
        {
            float total=0; for(int i=0;i<ship.MaxDashCharges;i++) total+=ship.GetDashProgress(i);
            UpdateBar(dashFill,100*total/ship.MaxDashCharges);
        }
    }
    public void UpdateBar(VisualElement element,float percentage)
    {
        if(element!=null) element.style.width=new Length(Mathf.Clamp(percentage,0,100),LengthUnit.Percent);
    }
}
