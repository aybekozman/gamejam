using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>Session-only scalar stats, not scene objects or a disk save.</summary>
public sealed class ShipVisitSnapshot
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    sealed class ComponentState
    {
        public Type type;
        public readonly Dictionary<FieldInfo, object> values = new();
    }
    readonly List<ComponentState> states = new();
    float damage, speed, knockback, water;
    float[] recoil;
    static readonly Type[] Types = { typeof(ShipController), typeof(ShipUIManager), typeof(ShipWeapons),
        typeof(PlayerHealth), typeof(LootMagnet), typeof(PlayerEnergy), typeof(PlayerExperience) };
    static readonly HashSet<string> RuntimeFields = new() { "currentHealth", "observedMax", "dashRemaining", "<CurrentEnergy>k__BackingField" };

    public static ShipVisitSnapshot Capture(GameObject ship, ShipUIManager ui, ShipWaterPurification purification)
    {
        var result = new ShipVisitSnapshot { damage = Bullet.bonusDamage, speed = Bullet.bonusSpeed,
            knockback = Bullet.bonusKnockback, water = purification.NormalizedProgress };
        foreach (var type in Types)
        {
            var component = type == typeof(ShipUIManager) ? ui : ship.GetComponent(type);
            if (!component) continue;
            var state = new ComponentState { type = type };
            foreach (var field in type.GetFields(Flags))
            {
                if (field.IsInitOnly || (!field.IsPublic && !Attribute.IsDefined(field, typeof(SerializeField)) && !RuntimeFields.Contains(field.Name))) continue;
                if (!field.FieldType.IsPrimitive && !field.FieldType.IsEnum && field.FieldType != typeof(float[])) continue;
                var value = field.GetValue(component);
                state.values[field] = value is float[] array ? array.Clone() : value;
            }
            result.states.Add(state);
        }
        var weapons = ship.GetComponent<ShipWeapons>();
        if (weapons && weapons.allTurrets != null)
        {
            result.recoil = new float[weapons.allTurrets.Length];
            for (int i = 0; i < result.recoil.Length; i++) result.recoil[i] = weapons.allTurrets[i] ? weapons.allTurrets[i].recoilForce : 0;
        }
        return result;
    }
    public void Restore(GameObject ship, ShipUIManager ui, ShipWaterPurification purification)
    {
        // All destination Awake/Start initializers have already run before this restore.
        foreach (var state in states)
        {
            var component = state.type == typeof(ShipUIManager) ? ui : ship.GetComponent(state.type);
            if (!component) continue;
            foreach (var pair in state.values) pair.Key.SetValue(component, pair.Value is float[] a ? a.Clone() : pair.Value);
        }
        Bullet.bonusDamage = damage; Bullet.bonusSpeed = speed; Bullet.bonusKnockback = knockback;
        var weapons = ship.GetComponent<ShipWeapons>();
        if (recoil != null && weapons && weapons.allTurrets != null)
            for (int i = 0; i < Math.Min(recoil.Length, weapons.allTurrets.Length); i++)
                if (weapons.allTurrets[i]) weapons.allTurrets[i].recoilForce = recoil[i];
        purification.RestoreProgress(water);
        var health = ship.GetComponent<PlayerHealth>();
        if (health) health.SetMaxHealth(health.maxHealth);
        var energy = ship.GetComponent<PlayerEnergy>();
        if (energy) energy.SetMaxEnergy(energy.MaxEnergy);
        var hud = UnityEngine.Object.FindFirstObjectByType<ShipHUDController>();
        if (hud) hud.RefreshAll();
        if (ui)
        {
            var controller = ship.GetComponent<ShipController>();
            if (controller) controller.SyncEnergyUI();
            if (health) ui.UpdateHealthBar(health.CurrentHealth / health.MaxHealth * 100);
            ui.UpdateBar(ui.xpFill, ui.xpText, ui.currentXP, ui.nextLevelXP);
            ui.UpdateDashFills();
            if (ui.levelText) ui.levelText.text = ui.currentLevel.ToString();
        }
    }
}
