# Ship oynanışını geri getirme ve sınırlı hikâye bağlantısı

## Referans ve yedek

Yedeklerin konumu: `C:/Users/PC/.codex/backups/gamejam-ship-restore-2026-11-01`. Büyük yedekler Git çalışma klasörünün dışında tutulur.

- Remote: https://github.com/aybekozman/gamejam.git
- Fetch edilen `origin/Ship`: **0cd6cdf89930a069c0691d662b53d836dba0233b**.
- Ana çalışma dalı değiştirilmedi; `fish` üzerinde kalındı. Referans arşivi ayrı `C:/Users/PC/.codex/backups/gamejam-ship-restore-2026-11-01/reference` klasöründe incelendi.
- Değişikliklerden önce tüm Git referansları `checkpoint.bundle`, Assets/Packages/ProjectSettings ve mevcut sahne yedekleri `working-checkpoint.zip` içine alındı. Başlangıçta açık MainMenu sahnesi dirty=False ve playing=False idi. `checkpoint-head.txt` başlangıç commit'ini içerir.
- Reset/clean veya bütün projeyi eski sürümle değiştirme yapılmadı. Gemiye ait dosyalar ve gerçek ShipScene 1'in bağımlılıkları seçilerek geri getirildi.

## Geri getirilen sistemler

`Assets/Scripts/Ship`, `Assets/UI/ShipHUD`, `Assets/EnemyStats` ve `Assets/Prefabs/Enemies` dosyaları referansla karşılaştırıldı. Gemi hareketi/dash, kamera, atış, mermi, efekt, üretici, ganimet/mıknatıs, enerji, XP, kartlar ve HUD Ship davranışına döndürüldü. Gerekli orb görseli/prefabı, mermi prefabı ve UI Toolkit panel referansları da geri getirildi.

Bu gruplarda referanstan farklı kalan tek oynanış dosyası `PlayerHealth.cs`: yalnızca gerçek final başladıktan sonra hasarı reddeden tek satırlık koruma eklidir. Normal oyundaki hasar/ölüm formülü ve değerleri aynıdır. `parity.json` karşılaştırmayı kaydeder.

Gerçek `Assets/Scenes/ShipScene 1.unity` kullanıldı. Referansın hiçbir nesnesi kaldırılmadı; eski nesnelerin position/rotation/scale değerleri değişmedi (`scene-parity.json`). Yalnızca hikâye adaptörü ve çok solda, collidersız kasaba/liman dekoru eklendi. Kamera, gemi başlangıcı, dünya sınırı veya fizik alanı değiştirilmedi. Gemi denizi, materyalleri, fontları ve ses kaynakları referanstaki gibi bırakıldı.

Su altı sahnesi, çalışma başlangıcı yedeğiyle aynı SHA256 değerine sahiptir. Görevler, yerleşim ve diyaloglar yeniden yazılmadı.

## Kaldırılan kapsam

Tank, hareket ederken su doldurma, toplama bölgeleri, 65 saniyelik arıtma, zorunlu liman dönüşü/E teslimatı, teslimat ödülü, kampanya kart/stat tekrar uygulaması, özel sonlu spawn bütçeleri, düşman havuzu filtreleri, kampanya kamera/konum zorlaması ve ek görev/HUD katmanı kaldırıldı. Native kart ekranı UI Toolkit olarak korundu; sahte `levelUpOverlay`/`upgradeProgress` alanları eklenmedi. Eski doğrulamalar Editor klasörüne taşınıp yeni gerçek API'lere göre değiştirildi; tank/teslimat raporları güncel test kabul edilmedi.

Menü mevcut görselleriyle korundu; kullanıcı tarafından verilen **Remnants of the Sea** adı gösterilir. Yeni Oyun su altından başlar ve ses tercihlerini silmez.

## Onaylanan bölüm sınırı ve eşleme

Ship referansında zafer/bölüm bitiş olayı yoktur: üretim sonsuzdur. Kullanıcının sonraki açıklaması uyarınca yalnızca aşağıdaki son sınır eklendi:

| Hikâye aşaması | Koşul | Sonraki adım |
|---|---|---|
| İlk keşif sonrası gemi arası | Üç kısa konuşmanın bitmesi; yeni oynanış/görev yok | İkinci gün duyurusu |
| Duyuru sonrası gemi | 120 saniye aktif savaş, ardından kalan savaş düşmanlarının temizlenmesi | Köpekbalıklarına su altı yolculuğu |
| Yardım görüşmesi sonrası gemi | 150 saniye aktif savaş ve kalan düşmanların temizlenmesi | Balina hikâye aşaması |
| Son gemi bölümü | 180 saniye aktif savaş ve kalan düşmanların temizlenmesi | Beyaz bayrak ve final |

120/150/180 değerleri ShipCampaignDirector Inspector alanlarıdır; kesin denge değerleri değildir. Süre konuşmada, Time.timeScale=0 iken ve native kart seçiminde ilerlemez. Süre dolunca mevcut üreticinin coroutineleri durur; düşmanlar silinmez. Takip yalnızca gemiyi hedefleyen `Enemy` savaş bileşenlerini kapsar. Dekoratif canlılar dahil edilmez. Tamamlanma olayı bir kez çalışır.

**Tür havuzu:** ShipScene 1 karma havuz kullanır. Referansı korumak için filtrelenmedi. Küçük balık/köpekbalığı/balina adları hikâye aşaması etiketleridir; her aşamada aynı native havuz ve aynı üretim/zorluk hesabı geçerlidir.

Gemi yeniden yüklendiğinde Ship'in kendi başlangıç verileri kullanılır; ek kampanya yükseltme/kaynak aktarımı yoktur. Doğrudan açılan gemide native ölüm davranışı korunur. Hikâye üzerinden oynarken batma, kullanıcının istediği şekilde aynı bölümü yeniden yükler; süre ve düşman takibi yeni başlar.

Beyaz bayrak yalnızca son bölümün gerçek tamamlanmasından sonra görünür ve 6 FPS oynar. Final sırasında kontrol/ateş durur, hasar engellenir. Menü ve Yeniden Başla bağlantıları korunur.

## Bu çalışma sırasında gerçekten çalıştırılan kontroller

- Unity'de güncel script derlemesi başarılı.
- `verification.txt`: doğrudan gemide başlangıç üretimi, hareket/enerji, Space seçimi, native UI Toolkit kart seçimi ve ölüm; menü/su altı bağlantıları; eşit başlangıç ayarları; konuşma sonrası kontrol; duraklatma/kart seçiminde duran süre; sınır sonrası native spawn aralığı geçmesine rağmen yeni düşman gelmemesi; mevcut düşmanların kalması; native hasarla temizlendikten sonra tek tamamlanma; aynı bölümden ölüm tekrarı; köpekbalığı/balina geçişi; final hasar koruması, beyaz bayrak ve bitiş butonları; menüde timeScale temizliği. Güncel çalışmada runtime error sayısı sıfır.
- `native-smoke.txt`: yeni yüklenmiş doğrudan gemide gerçek girdilerle hareket/tüketim, kamera hedefi, hasara tepki veren HUD, gerçek orb/mıknatısla enerji ve XP, gerçek otomatik hedefleme/mermi üretimi.
- `campaign-smoke.txt`: aynı hareket, kamera, hasar/HUD, orb/enerji/XP ve atış kontrolleri ana menü → su altı → gemi arası → duyuru → gemi yoluyla tekrarlandı. Runtime error sayısı sıfır.
- Çalışma başlangıcındaki su altı sahnesiyle dosya hash eşitliği ve referanstaki gemi nesnelerinin korunması kontrol edildi.

Otomatik sınır testlerinde yalnızca runtime savaş süreleri kısaltıldı; native üretim aralığı/hız/statlar değiştirilmedi. Test hedefleri native TakeDamage üzerinden temizlendi. Inspector'da kayıtlı süreler 120/150/180 olarak kaldı. Tam sürelerle insan oynanışı ve zorluk/ödül dengesi testi yapılmadı; standalone build alınmadı. Önceki kapsamın başarılı test raporları güncel kanıt olarak kullanılmadı.
