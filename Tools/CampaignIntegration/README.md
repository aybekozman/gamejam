# Gemi / su altı entegrasyonu

Gerçek gemi sahnesi: `Assets/Scenes/ShipScene 1.unity`. Yeni gemi sahnesi kopyalanmadı.
Başlangıç: `Assets/Scenes/MainMenu.unity` (Build Settings ilk sahne).

Akış: Menü → ilk su altı keşif → savaşsız su seferi → 2. gün duyuru → küçük balık savaşı → köpekbalığı yolculuğu → köpekbalığı savaşı → balina savaşı → beyaz bayrak / final → menü.

- `CampaignFlow`: oturum ilerlemesi, takvim, savaş dalgası, kirlilik, fade, sefer başlangıcı kontrol noktası ve sahneler arası yükseltmeler. Ses tercihleri PlayerPrefs ile kalıcıdır; hikâye için disk kaydı/Devam Et eklenmedi.
- `ShipCampaignDirector`: mevcut gemi, düşman üreticisi, hasar ve yükseltme sistemlerine bağlanır. Tank yalnızca toplama alanında hareket ederken dolar; varsayılan 65 saniye etkin arıtma gerekir. Üç yakın alan vardır. E ile limanda teslim edilir. Ödül, mevcut XP/kart sistemiyle bir kez verilir.
- Mevcut HUD, silahlar, statlar ve orb XP/enerji seçimi korundu. Birleşmede boş kalan HUD gemi/can/enerji/XP referansları native gemi sistemine bağlandı; ayrı enerji veya XP yöneticisi eklenmedi. Birleşmiş sahnede boş kalan kart ekranı referansları mevcut ShipUIManager'a bağlandı. Eski Enemy_Shark prefabının eksik Hunter stat/loot referansı tamamlandı; stat assetlerinin değerleri değiştirilmedi.
- Kıyı kasabası ve liman sağa açılacak şekilde döndürüldü. Kıyı, liman arka duvarı ve iki rıhtımın ayrı colliderları var; orta su kanalı açık.
- Yeni kaynak görseller değiştirilmedi. Temizlenmiş kopyalar `Assets/Campaign/Resources/Campaign` altında. Menüdeki oyun adı `CampaignMenu.gameTitle` alanından değiştirilebilir; varsayılan boş.
- Gemi çöpleri kirlilik aşamasına göre 0/12/24/36 adetle sınırlıdır. Son dalga sonrası toplar/hasar/üretim durur; bayrak 6 FPS oynar ve deniz kirli kalır.
- Su altı sahnesinin işe başlamadan alınan kopyasıyla SHA256 değeri aynıdır; kullanıcı yerleşimi ve collider düzenlemeleri korunmuştur.

Kontroller:
- `verification.txt`: hikâye bitiş eventlerini çağırarak bütün sahne sırası, ayrı takvim/dalga, doğru bölüm başlangıçları, üç seferin gerçek teslimat yordamıyla sonraki aşamaya ilerlemesi, tek yönetici/AudioListener/EventSystem, sefer tekrarı, gerçek düşman hasar/ölüm API'siyle sonlu üç dalga, bütün final konuşmalarından sonra bitiş butonları, final bayrağı, menü ve Yeni Oyun sıfırlaması. Otomatik dalga testinde yalnızca runtime spawn aralığı kısaltılır; öldürme hasarı test tarafından verilir.
- `delivery-verification.txt`: Input System D girdisi, gerçek hasara bağlı HUD canı, gerçek gemi enerjisi, pointer ile enerji/Space ile XP seçimi, ganimetlerin seçilen kaynağı vermesi ve HUD XP eşleşmesi, toplama alanında tank dolumu, E girdisiyle teslimat, çift ödül engeli, mevcut kart seçimi, yükseltmelerin sonraki sahneye taşınması, gerçek ölüm çağrısıyla aynı sefere dönüş, can/tank sıfırlanması.
- `preferences-verification.txt`: gerçek slider callbackleriyle üç ses tercihi değiştirilip Yeni Oyun/sahne geçişinde korunduğu kontrol edilir; sonunda önceki tercihler geri konur.
- Menü 1920×1080 ve ayarlar 1280×1024 görüntülendi; son ekran görüntüleri bu klasördedir.

Sınır: Tam hikâye ve savaşlar baştan sona insan tarafından oynanarak dengelenmedi. Testler bağlantı, durum ve native sistem çağrılarını doğrular. Standalone build alınmadı. Yeni ses kaydı eklenmedi.

Görsel kopyaları yeniden üretmek için Python/Pillow/numpy ile `Tools/prepare_campaign_assets.py` ve `Tools/prepare_campaign_environment.py` kullanılabilir. `CampaignSetup.Apply` import/9-slice ayarlarını ve sahne bağlarını kurar. Bu yardımcı sahne düzenleme aracıdır; normal oyunda çalışmaz.

Güncel görseller: `menu-1920.png`, `settings-1280.png`, `harbor-1920.png`, `final.png`.
