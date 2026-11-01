# Arıtılan su barı — yalnızca görsel kurulum

Mevcut `Assets/UI/ShipHUD/ShipHUD.prefab` içine `ShipHUD/LeftHUD/WaterPurificationBar` eklendi. ShipScene ve HudFirstTry bu ortak prefabı kullanır. Bar mevcut seçicinin altında, sağlık/enerji/XP grubunun sağ kenarına hizalıdır; kamera veya dünya objesi yerine mevcut ekran HUD'ına bağlıdır.

- Panel: 327,5 × 47; LeftHUD'a göre üst-sol konum (118, -234).
- Çerçeve ve damla ikonu: `WaterPurificationBar` üzerindeki Image.
- `Fill`: mavi Image; Filled / Horizontal / Left; başlangıç `fillAmount=0`.
- `Title`: sabit “ARITILAN SU” PNG'si.
- `PurifiedWaterPercent`: ayrı TextMeshProUGUI; başlangıç `%0`, mevcut HUD fontu ve materyali.
- PNG'ler: `Assets/UI/ShipHUD/WaterPurification/`; Point, mipmap kapalı, sıkıştırmasız.
- Yeni UI öğelerinde Raycast Target kapalı; oyun ve diğer HUD kontrollerinin tıklamalarını engellemez.

**Hareketle otomatik dolum ve dinamik yüzde bağlanmadı.** Kullanıcının “kod olarak hiçbir şeye dokunma” talimatına göre script eklenmedi/değiştirilmedi. Mevcut kodda su arıtma ilerlemesi için bir alan bulunmadığından görsel kurulum %0 başlangıcıyla bırakıldı. Daha sonra bağlantı yapılacak öğeler `Fill` ve `PurifiedWaterPercent` olarak açıkça adlandırılmıştır.

Play Mode'da 1280×720'de %0, 1920×1080'de tam dolgu ve %100 görünümü incelendi. Geçici dolum değerleri yalnızca Play Mode kopyasında uygulandı; kaydedilmiş prefabın dolumu 0, yüzde metni %0. Yerleşimde taşma/örtüşme görülmedi. Ekran boyutu, başlangıç sahnesi ve arka planda çalışma ayarları kontrol sonrası geri alındı. 21 Ship/HUD scriptinin önce/sonra dosya hash'leri eşleşti. Bu işlemde sahne dosyası kaydedilmedi; yeni görünüm ortak prefabdan gelir.

Görüntüler: `Logs/WaterPurification/layout-1280-zero.png`, `Logs/WaterPurification/layout-1920-full.png`. Yalnızca tam dolu görünüm doğrulamak için kullanıldı; otomatik dolum testi yapıldığı anlamına gelmez.

Kontrol sırasında Unity Editor Inspector tarafında `SerializedObjectNotCreatableException: Object at index 0 is null` kayıtları görüldü. Derleme hatası yoktu; bu nedenle Console sıfır hata olarak raporlanmadı. Oyun scriptlerine müdahale edilmedi.
