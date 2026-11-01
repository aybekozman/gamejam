using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    Heal,            // 1. Anlık Can: +25 / +50 / +100
    MaxHealth,       // 2. Max Can: +20 / +40 / +75
    HealthRegen,     // 3. Can Yenileme: +0.5 / +1.0 / +2.0 (sn)
    MoveSpeed,       // 4. Gemi Hızı: +1.0 / +2.0 / +3.5
    TurnSpeed,       // 5. Dönüş Hızı: +20 / +40 / +70
    DashRegen,       // 6. Dash Dolumu: +5 / +10 / +20
    DashCost,        // 7. Dash Maliyeti: -%10 / -%20 / -%35
    MagnetRadius,    // 8. Mıknatıs Menzili: +2 / +4 / +7
    MagnetSpeed,     // 9. Mıknatıs Çekim Hızı: +3 / +6 / +10
    FireRate,        // 10. Atış Hızı: -0.05 / -0.10 / -0.18 sn
    BulletDamage,    // 11. Mermi Hasarı: +5 / +12 / +25
    BulletSpeed,     // 12. Mermi Hızı: +2 / +5 / +10
    BulletKnockback, // 13. Mermi İtme Gücü: +1 / +3 / +6
    AttackRange,     // 14. Atış Menzili: +2 / +5 / +9
    XPGain,          // 15. XP Artışı: +%15 / +%30 / +%50
    MaxEnergy,       // 16. Max Enerji: +20 / +50 / +100
    EnergyRegen,     // 17. Enerji Yenileme: +2 / +5 / +10
    KnockbackResist, // 18. Çarpışma Direnci: -%15 / -%30 / -%50 sekme
    DamageResist,    // 19. Hasar Direnci: -%10 / -%20 / -%35 hasar
    Iframes          // 20. Hasar Sonrası Dokunulmazlık: +0.2 / +0.4 / +0.7 sn
}

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Ship Game/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Kart Bilgileri")]
    public string upgradeName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Etki ve Seviye (Tier) Ayarları")]
    public UpgradeType upgradeType;
    [Range(1, 3)]
    public int tier = 1;
    public float value = 1f;

    /// <summary>
    /// 20 farklı yetenek ve her birinin 3 seviyesi (Tier 1, 2, 3) olmak üzere toplam 60 kartlık veri setini üretir.
    /// </summary>
    public static List<UpgradeData> CreateDefault60CardPool()
    {
        List<UpgradeData> pool = new List<UpgradeData>(60)
        {
            // 1. Anlık Can (Heal): +25 / +50 / +100
            CreateCard("Acil Onarım I", "Gemiyi anında +25 Can onarır.", UpgradeType.Heal, 1, 25f),
            CreateCard("Acil Onarım II", "Gemiyi anında +50 Can onarır.", UpgradeType.Heal, 2, 50f),
            CreateCard("Acil Onarım III", "Gemiyi anında +100 Can onarır.", UpgradeType.Heal, 3, 100f),

            // 2. Max Can (MaxHealth): +20 / +40 / +75
            CreateCard("Meşe Gövde I", "Maksimum canı +20 artırır.", UpgradeType.MaxHealth, 1, 20f),
            CreateCard("Meşe Gövde II", "Maksimum canı +40 artırır.", UpgradeType.MaxHealth, 2, 40f),
            CreateCard("Meşe Gövde III", "Maksimum canı +75 artırır.", UpgradeType.MaxHealth, 3, 75f),

            // 3. Can Yenileme (HealthRegen): +0.5 / +1.0 / +2.0 (saniyede)
            CreateCard("Marangoz Ekibi I", "Saniyede +0.5 Can yeniler.", UpgradeType.HealthRegen, 1, 0.5f),
            CreateCard("Marangoz Ekibi II", "Saniyede +1.0 Can yeniler.", UpgradeType.HealthRegen, 2, 1.0f),
            CreateCard("Marangoz Ekibi III", "Saniyede +2.0 Can yeniler.", UpgradeType.HealthRegen, 3, 2.0f),

            // 4. Gemi Hızı (MoveSpeed): +1.0 / +2.0 / +3.5
            CreateCard("Rüzgar Yelkeni I", "Gemi maksimum hızını +1.0 artırır.", UpgradeType.MoveSpeed, 1, 1.0f),
            CreateCard("Rüzgar Yelkeni II", "Gemi maksimum hızını +2.0 artırır.", UpgradeType.MoveSpeed, 2, 2.0f),
            CreateCard("Rüzgar Yelkeni III", "Gemi maksimum hızını +3.5 artırır.", UpgradeType.MoveSpeed, 3, 3.5f),

            // 5. Dönüş Hızı (TurnSpeed): +20 / +40 / +70
            CreateCard("Çevik Dümen I", "Gemi dönüş hızını +20 artırır.", UpgradeType.TurnSpeed, 1, 20f),
            CreateCard("Çevik Dümen II", "Gemi dönüş hızını +40 artırır.", UpgradeType.TurnSpeed, 2, 40f),
            CreateCard("Çevik Dümen III", "Gemi dönüş hızını +70 artırır.", UpgradeType.TurnSpeed, 3, 70f),

            // 6. Dash Dolumu (DashRegen): +5 / +10 / +20
            CreateCard("Akıntı İtici I", "Dash dolum hızını +5 artırır.", UpgradeType.DashRegen, 1, 5f),
            CreateCard("Akıntı İtici II", "Dash dolum hızını +10 artırır.", UpgradeType.DashRegen, 2, 10f),
            CreateCard("Akıntı İtici III", "Dash dolum hızını +20 artırır.", UpgradeType.DashRegen, 3, 20f),

            // 7. Dash Maliyeti (DashCost): -10% / -20% / -35%
            CreateCard("Hafif Omurga I", "Dash maliyetini ve beklemesini %10 azaltır.", UpgradeType.DashCost, 1, 10f),
            CreateCard("Hafif Omurga II", "Dash maliyetini ve beklemesini %20 azaltır.", UpgradeType.DashCost, 2, 20f),
            CreateCard("Hafif Omurga III", "Dash maliyetini ve beklemesini %35 azaltır.", UpgradeType.DashCost, 3, 35f),

            // 8. Mıknatıs Menzili (MagnetRadius): +2 / +4 / +7
            CreateCard("Ganimet Ağı I", "Mıknatıs çekim menzilini +2 artırır.", UpgradeType.MagnetRadius, 1, 2f),
            CreateCard("Ganimet Ağı II", "Mıknatıs çekim menzilini +4 artırır.", UpgradeType.MagnetRadius, 2, 4f),
            CreateCard("Ganimet Ağı III", "Mıknatıs çekim menzilini +7 artırır.", UpgradeType.MagnetRadius, 3, 7f),

            // 9. Mıknatıs Çekim Hızı (MagnetSpeed): +3 / +6 / +10
            CreateCard("Girdap Çekimi I", "Ganimet çekim hızını +3 artırır.", UpgradeType.MagnetSpeed, 1, 3f),
            CreateCard("Girdap Çekimi II", "Ganimet çekim hızını +6 artırır.", UpgradeType.MagnetSpeed, 2, 6f),
            CreateCard("Girdap Çekimi III", "Ganimet çekim hızını +10 artırır.", UpgradeType.MagnetSpeed, 3, 10f),

            // 10. Atış Hızı (FireRate): -0.05 / -0.10 / -0.18
            CreateCard("Seri Barut I", "Atış bekleme süresini 0.05 sn düşürür.", UpgradeType.FireRate, 1, 0.05f),
            CreateCard("Seri Barut II", "Atış bekleme süresini 0.10 sn düşürür.", UpgradeType.FireRate, 2, 0.10f),
            CreateCard("Seri Barut III", "Atış bekleme süresini 0.18 sn düşürür.", UpgradeType.FireRate, 3, 0.18f),

            // 11. Mermi Hasarı (BulletDamage): +5 / +12 / +25
            CreateCard("Ağır Gülle I", "Mermi hasarını +5 artırır.", UpgradeType.BulletDamage, 1, 5f),
            CreateCard("Ağır Gülle II", "Mermi hasarını +12 artırır.", UpgradeType.BulletDamage, 2, 12f),
            CreateCard("Ağır Gülle III", "Mermi hasarını +25 artırır.", UpgradeType.BulletDamage, 3, 25f),

            // 12. Mermi Hızı (BulletSpeed): +2 / +5 / +10
            CreateCard("Yivli Namlu I", "Mermi uçuş hızını +2 artırır.", UpgradeType.BulletSpeed, 1, 2f),
            CreateCard("Yivli Namlu II", "Mermi uçuş hızını +5 artırır.", UpgradeType.BulletSpeed, 2, 5f),
            CreateCard("Yivli Namlu III", "Mermi uçuş hızını +10 artırır.", UpgradeType.BulletSpeed, 3, 10f),

            // 13. Mermi İtme Gücü (BulletKnockback): +1 / +3 / +6
            CreateCard("Sarsıcı Gülle I", "Mermi itme gücünü +1 artırır.", UpgradeType.BulletKnockback, 1, 1f),
            CreateCard("Sarsıcı Gülle II", "Mermi itme gücünü +3 artırır.", UpgradeType.BulletKnockback, 2, 3f),
            CreateCard("Sarsıcı Gülle III", "Mermi itme gücünü +6 artırır.", UpgradeType.BulletKnockback, 3, 6f),

            // 14. Atış Menzili (AttackRange): +2 / +5 / +9
            CreateCard("Gözcü Dürbünü I", "Taret atış menzilini +2 artırır.", UpgradeType.AttackRange, 1, 2f),
            CreateCard("Gözcü Dürbünü II", "Taret atış menzilini +5 artırır.", UpgradeType.AttackRange, 2, 5f),
            CreateCard("Gözcü Dürbünü III", "Taret atış menzilini +9 artırır.", UpgradeType.AttackRange, 3, 9f),

            // 15. XP Artışı (XPGain): +15% / +30% / +50%
            CreateCard("Korsan Bilgeliği I", "Kazanılan XP miktarını %15 artırır.", UpgradeType.XPGain, 1, 15f),
            CreateCard("Korsan Bilgeliği II", "Kazanılan XP miktarını %30 artırır.", UpgradeType.XPGain, 2, 30f),
            CreateCard("Korsan Bilgeliği III", "Kazanılan XP miktarını %50 artırır.", UpgradeType.XPGain, 3, 50f),

            // 16. Max Enerji (MaxEnergy): +20 / +50 / +100
            CreateCard("Buhar Kazanı I", "Maksimum enerjiyi +20 artırır.", UpgradeType.MaxEnergy, 1, 20f),
            CreateCard("Buhar Kazanı II", "Maksimum enerjiyi +50 artırır.", UpgradeType.MaxEnergy, 2, 50f),
            CreateCard("Buhar Kazanı III", "Maksimum enerjiyi +100 artırır.", UpgradeType.MaxEnergy, 3, 100f),

            // 17. Enerji Yenileme (EnergyRegen): +2 / +5 / +10
            CreateCard("Dinamo Çarkı I", "Saniyede +2 Enerji yeniler.", UpgradeType.EnergyRegen, 1, 2f),
            CreateCard("Dinamo Çarkı II", "Saniyede +5 Enerji yeniler.", UpgradeType.EnergyRegen, 2, 5f),
            CreateCard("Dinamo Çarkı III", "Saniyede +10 Enerji yeniler.", UpgradeType.EnergyRegen, 3, 10f),

            // 18. Çarpışma Direnci (KnockbackResist): %15 / %30 / %50
            CreateCard("Demir Çapa I", "Alınan sekme kuvvetini %15 azaltır.", UpgradeType.KnockbackResist, 1, 15f),
            CreateCard("Demir Çapa II", "Alınan sekme kuvvetini %30 azaltır.", UpgradeType.KnockbackResist, 2, 30f),
            CreateCard("Demir Çapa III", "Alınan sekme kuvvetini %50 azaltır.", UpgradeType.KnockbackResist, 3, 50f),

            // 19. Hasar Direnci (DamageResist): %10 / %20 / %35
            CreateCard("Çelik Kaplama I", "Alınan hasarı %10 azaltır.", UpgradeType.DamageResist, 1, 10f),
            CreateCard("Çelik Kaplama II", "Alınan hasarı %20 azaltır.", UpgradeType.DamageResist, 2, 20f),
            CreateCard("Çelik Kaplama III", "Alınan hasarı %35 azaltır.", UpgradeType.DamageResist, 3, 35f),

            // 20. Hasar Sonrası Dokunulmazlık (Iframes): +0.2 / +0.4 / +0.7 sn
            CreateCard("Hayalet Sis I", "Hasar sonrası dokunulmazlığı +0.2 sn uzatır.", UpgradeType.Iframes, 1, 0.2f),
            CreateCard("Hayalet Sis II", "Hasar sonrası dokunulmazlığı +0.4 sn uzatır.", UpgradeType.Iframes, 2, 0.4f),
            CreateCard("Hayalet Sis III", "Hasar sonrası dokunulmazlığı +0.7 sn uzatır.", UpgradeType.Iframes, 3, 0.7f)
        };

        return pool;
    }

    private static UpgradeData CreateCard(string title, string desc, UpgradeType type, int tierLevel, float val)
    {
        UpgradeData data = CreateInstance<UpgradeData>();
        data.name = $"{type}_Tier{tierLevel}";
        data.upgradeName = title;
        data.description = desc;
        data.upgradeType = type;
        data.tier = tierLevel;
        data.value = val;
        return data;
    }
}
