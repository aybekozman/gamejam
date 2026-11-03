using UnityEngine;

/// <summary>Only XP progression crosses visits; every other ship value is authored fresh.</summary>
public sealed class ShipVisitSnapshot
{
    int level;float xp;
    public static ShipVisitSnapshot Capture(GameObject ship,ShipUIManager ui,ShipWaterPurification purification)
        =>new ShipVisitSnapshot{level=ui.currentLevel,xp=ui.currentXP};
    public void Restore(GameObject ship,ShipUIManager ui,ShipWaterPurification purification)
    {
        ui.currentLevel=Mathf.Max(1,level);ui.currentXP=Mathf.Max(0,xp);
        ui.nextLevelXP=Mathf.Round(ui.baseXP*Mathf.Pow(ui.xpGrowthMultiplier,ui.currentLevel));
        ResetVisit(ship,ui,purification);
    }
    public static void ResetVisit(GameObject ship,ShipUIManager ui,ShipWaterPurification purification)
    {
        purification.RestoreProgress(0);
        var health=ship.GetComponent<PlayerHealth>();if(health)health.SetMaxHealth(health.maxHealth,true);
        var controller=ship.GetComponent<ShipController>();if(controller){controller.currentEnergy=controller.maxEnergy;controller.SyncEnergyUI();}
        var energy=ship.GetComponent<PlayerEnergy>();if(energy)energy.RestoreEnergy(energy.MaxEnergy);
        if(ui){if(health)ui.UpdateHealthBar(100);ui.UpdateBar(ui.xpFill,ui.xpText,ui.currentXP,ui.nextLevelXP);ui.UpdateDashFills();if(ui.levelText)ui.levelText.text=ui.currentLevel.ToString();}
        var hud=UnityEngine.Object.FindFirstObjectByType<ShipHUDController>();if(hud)hud.RefreshAll();
    }
}
