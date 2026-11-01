# Orb ödül seçicisi

`Assets/Scenes/HudFirstTry.unity` ve mevcut `Assets/UI/ShipHUD/ShipHUD.prefab` içine kuruldu. Ahşap–altın çerçeve, şimşek ve yıldız mevcut HUD kaynaklarından türetildi; kaynak görseller değiştirilmedi. Point filtreleme, sıkıştırmasız sprite ve dilimlenebilir çerçeve kullanılır. Yeni font eklenmedi.

- **Q:** iki konum arasında geçiş. Basılı tutmak tekrarlamaz.
- **Sol yarıya tıklama:** Enerji; yeşil vurgu ve sola kayan seçim.
- **Sağ yarıya tıklama:** XP; altın vurgu ve sağa kayan seçim.
- Başlangıç **XP**. Seçim sahne yeniden başladığında başlangıç değerine döner.
- Ödül, orb gemiye ulaşıp toplandığı anda seçili olan türe gider. Uçuş sırasında değişim geçerlidir.
- Varsayılan oran: orbun `expValue` değeri kadar XP veya enerji. Enerji maksimumda sınırlandırılır; fazlası XP'ye dönüştürülmez.
- Duraklatıldığında seçim ve ödül işlemi bekler. Ses eklenmedi.

## Kod ve ayarlar

`Assets/Scripts/Ship/OrbRewardMode.cs` gemide gerçek seçim durumunu ve Q girdisini yönetir. Inspector'daki `Starting Mode`, `Toggle Binding` ve `Energy Per Point` alanları başlangıç seçimini, tuşu ve enerji çarpanını ayarlar.

`Assets/UI/ShipHUD/OrbRewardToggleView.cs` yalnızca görünümü, tıklamayı ve kayan seçiciyi yönetir. `Slide Duration` varsayılan 0,16 saniyedir. `Loot.TryCollect` mevcut çift ödül kilidini koruyarak ödülü gemideki modele yönlendirir. Bu bileşenin bulunmadığı sahnelerde eski XP davranışı devam eder. Su altı ve diğer sahnelerin orb davranışı değiştirilmedi.

`Assets/Editor/OrbRewardToggleSetup.cs`: **Tools → Ship HUD → Install energy-XP orb selector** kurulumu yeniden çalıştırabilir; mevcut seçiciyi çoğaltmaz. Tam HUD kurulumu da bunu çağırır. Manuel Inspector bağlantısı gerekmiyor.

## Kontrol sonucu

Gerçek Play Mode'da **22 seçici kontrolü ve 43 mevcut HUD/dash kontrolü geçti**, başarısız kontrol: **0**. Q girdisi sanal Input System klavyesiyle; tıklamalar gerçek EventSystem raycast'iyle kontrol edildi. XP/enerji yönlendirme, çift toplama engeli, enerji üst sınırı, uçuş sırasında seçim, basılı Q, pause ve bileşenlerin yeniden açılması doğrulandı. 1920×1080 ve 1280×720 seçici görüntüleri incelendi. Eski HUD kontrolleri ayrıca dört ekran oranını doğruladı. Fiziksel klavyeyle manuel oyun testi yapılmadı.

- Sonuçlar: `Logs/ShipHUD/orb-toggle-check.txt`, `Logs/ShipHUD/play-check.txt`.
- Görseller: `Logs/ShipHUD/orb-energy.png`, `Logs/ShipHUD/orb-xp-1280.png`.
- Derleme hatası yok. Kurulum sırasında kullanıcının kaydedilmemiş ShipScene sahnesini korumak için HudFirstTry ek olarak açıldı; iki sahnenin global ışıkları birlikte açıkken Unity `More than one global light` kaydı üretti. Kontroller sonrasında yalnızca geçici açılan, kaydedilmiş HudFirstTry kapatılarak önceki sahne düzenine dönüldü. Kullanıcının kaydedilmemiş sahnesi ve ışıkları değiştirilmedi.

Test erişimi için geçici eklenen Pipeline paketi teslim değişikliğinin parçası değildir; oyun ve kontroller bu pakete bağlı değildir.
