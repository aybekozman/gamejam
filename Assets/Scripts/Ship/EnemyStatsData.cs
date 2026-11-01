using UnityEngine;

/* ============================================================================
 * DÜŞMAN STATLARI REHBER VERİ SETİ (5 Sınıflandırma - 8 Düşman Prefabı İçin)
 * ============================================================================
 * 1. Sürü (Pink / Yellow)    -> Enemy_Pink_Fish, Enemy_Yellow_Fish
 *    HP: 20  | Dmg: 5  | Spd: 4.5 | ShipKb: 15 | BulletKb: 6
 *
 * 2. Standart (Blue)         -> Enemy_Blue_Fish, Enemy_Blue_Sharp_Fish
 *    HP: 40  | Dmg: 10 | Spd: 3.5 | ShipKb: 12 | BulletKb: 4
 *
 * 3. Avcı (Sharks)           -> Enemy_Shark, Enemy_SharkTopDown, KöpekbalığıDüşman
 *    HP: 75  | Dmg: 25 | Spd: 5.0 | ShipKb: 8  | BulletKb: 2
 *
 * 4. Elit (Orca)             -> Enemy_Orca
 *    HP: 150 | Dmg: 40 | Spd: 2.5 | ShipKb: 4  | BulletKb: 0.5
 *
 * 5. Tank Boss (Whale)       -> Enemy_Whale
 *    HP: 350 | Dmg: 75 | Spd: 1.5 | ShipKb: 1  | BulletKb: 0
 * ============================================================================ */

[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "Ship Game/Enemy Stats Data")]
public class EnemyStatsData : ScriptableObject
{
    [Header("Temel Düşman Statları")]
    public float maxHealth = 20f;
    public float damage = 5f;
    public float moveSpeed = 4.5f;

    [Header("Geri Tepme (Knockback) Statları")]
    public float shipKnockbackForce = 15f;
    public float bulletKnockbackForce = 6f;
}

