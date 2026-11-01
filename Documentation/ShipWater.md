# Gemi sahnesindeki deniz ve dalgalar

Unity 6000.3.19f1, URP 17.3.0. Normal kullanım için `Assets/Scenes/ShipScene.unity` açılıp Play'e basılması yeterlidir. Su altı sahnesi, gemi hareket kodu, kamera ve savaş kodları değiştirilmedi.

## Yüzey dalgaları

Sahne kökündeki `Surface Wavelets` nesnesine `OceanSurfaceWavelets` bağlandı. Okyanus nesnesinin ölçeklenen/taşınan çocuğu değildir; aktif dalgaların dünya konumu değişmez. Havuz sahneye aittir; yeniden yüklemede kalıcı ikinci bir yönetici oluşturulmaz.

| Inspector ayarı | Başlangıç değeri |
| --- | --- |
| Aktif dalga sınırı | 8 |
| Önceden oluşturulan havuz | 16 sprite; çalışma sırasında Instantiate/Destroy yok |
| FPS | 5; her türde soldan sağa dört kare |
| Üretim aralığı | 0.075 sn; sınır doluyken üretim yapılmaz |
| Opaklık | 0.32 |
| Ölçek aralığı | 0.95–1.05; animasyon boyunca sabit |
| Minimum merkez mesafesi | 6 dünya birimi |
| Kamera kenar payı | 2 dünya birimi |
| Fade-in / fade-out | 0.12 / 0.25 sn |
| Yön | Sabit, sıfır rotasyon |
| Sorting | Default, -60; deniz -100, gemi köpükleri -20, gövde/toplar 100 |

Başlangıçta birkaç dalga farklı yaşlarda oluşturulur; üç tür rastgele seçilir. Animasyon dört karenin oluşma, yayılma ve kaybolma sırasını korur. Sürekli büyüme veya rastgele dönüş yoktur. Duraklatma scaled time kullanır. Eski deniz dokusu ve shader ayarları bu güncellemede değiştirilmedi.

Üretim, dalganın tüm sprite ayak izini kontrol eder. `Exclusion Layers` varsayılan olarak Default ve Player katmanlarındaki 2D collider'ları dışlar. Gemi ayrıca gerçek gövde SpriteRenderer bounds'u ve 1 birim pay ile dışlanır. Aktif dalga geminin/engelin alanına girerse görünümü kapatılır. Enemy ve Loot katmanları üretimi durdurmaz; dalgalar bunların altında çizilir.

Yeni kara/liman/iskele nesneleri eklendiğinde collider'larını dışlama maskesine dahil edin veya `Exclusion Areas` listesine koyun. Collider'ı olmayan görsel kara bölgeleri için görünmez bir 2D trigger dışlama alanı kullanılabilir. `Use Water Bounds` dikdörtgen bir su sınırını etkinleştirir; `Water Area` isteğe bağlı su collider'ıdır (Water katmanına koyun). Dalga ayak izinin köşeleri su içinde olmalıdır. Bu güncellemede mevcut sahnede kıyı/liman nesnesi bulunmadı; sahneye yeni kara ya da oyun mekaniği eklenmedi.

## Pruva ve kıç

`Gemi` üzerindeki mevcut `ShipMovementFoam` geliştirildi:

- Eski ters pruva görselinin referansı kaldırıldı. Yeni sayfanın üst satırındaki dört V karesi kullanılır. Yerel rotasyon 0'dır: V'nin sivri köşesi ileri bakar, kolları gövdenin iki yanına ve geriye uzanır.
- `Gemi/Foam Bow`: `(0,7.15,0)`, ölçek `(1.65,1.05)`. Kareler farklı büyüklüğe kırpılmaz; her karenin sivri bağlantısı aynı pivota kaydırılır. Gövde çok küçük bir iç bağlantı payını örter.
- `Gemi/Foam Stern`: `(0,-6.8,0)`. Daha önce üretilen kabarcıklı kıç köpüğü ve dünya uzayındaki iz havuzu korunur.
- Yeni sayfanın alt satırındaki dört yay karesi, kıç köpüğünün üzerinde ayrı bir sprite olarak çizilir: ölçek `(1.05,1.15)`, 5 FPS, sorting -19. Gövde ve topların altında kalır.
- Mevcut yan köpükler korunur. Gemi efektleri Rigidbody2D'nin gerçek ileri hızını okur; tuşlara bağlanmaz. Geri yönde güçlü ileri köpük oluşmaz. Durunca 0.6 sn içinde sönme, teleport ve kapanıp açılmada iz temizliği korunur.
- Kıç izi 40 sabit havuz elemanı kullanır; önceki parçalar dönüşlerde dünya uzayında kalır. Kaynak eski pruva dosyaları korunur fakat yeni sahne bağlantısında kullanılmaz.

## Görseller ve alpha temizliği

Kullanıcının üç dosyası değiştirilmeden `Assets/Sprites/ShipWaterSources/` içine kopyalandı:

- `Piksel_denizde_pruva_dalgas_ve_kc_izi.png`: 1774×887, 4 sütun × 2 satır. Üst sıra pruva V, alt sıra kıç yayları.
- `Sakin_okyanus_dalgack_sprite_sayfas-1.png`: 1536×1024, 4 sütun × 3 satır. Mevcut sakin turkuaz denize uygun daha yumuşak paleti nedeniyle kullanılan yüzey sayfası.
- `Seffaf_4x3_piksel_dalga_sprite-2.png`: aynı 3×4 düzen; alpha kanalı ayrıca incelendi. Daha doygun alternatif kaynak olarak saklanır, ikinci bir sistem oluşturulmaz.

Kaynaklar gerçek ama geniş yarı saydam alanlar içeriyordu. Temizleme yalnızca siyah renge bakmaz: alpha, beyaz/açık mavi renk eşikleri ve yerel keskin kenarlar birlikte değerlendirilir. Yumuşak turkuaz parlama çıkarılır; çıktı yalnızca 0 veya 255 alpha içerir. Küçük çizgileri korumak için küçültmede her hedef pikselin kaynak alanındaki en parlak geçerli çizgi pikseli seçilir. Sahnedeki yumuşaklık renderer opaklığı ve fade ile sağlanır; additive materyal kullanılmaz.

Temiz tam sayfalar ve 20 ayrı kare `Assets/Sprites/ShipWaterProcessed/UserWaves/` altındadır. Yüzeyde eşit hücre koordinatları ve her karede aynı 96×86 tuval / merkez pivot kullanılır; içerik karesine göre ölçeklenmez. Kaynak yüksekliği 3'e tam bölünmediği için hücre sınırları kesirli hesaplanır. Pruva/kıç kareleri 112×112 tuval kullanır. Point, mipmap kapalı, Clamp ve sıkıştırmasız import yalnızca bu çıktılara uygulanır. Diğer sprite'ların ayarları değiştirilmez.

Kontrol edilen 20 karede yarı saydam piksel sayısı 0, hücre sınırına değen görünür piksel sayısı 0 bulundu. Temiz sayfa ve gerçek oyun görüntüleri ayrıca gözle incelendi; dikdörtgen zemin ve bulanık turkuaz leke görünmedi.

## Dosyalar ve yeniden kurulum

- `Assets/Scripts/Ship/OceanSurfaceWavelets.cs`: yeni bağımsız yüzey dalgası havuzu ve alan kontrolleri.
- `Assets/Scripts/Ship/ShipMovementFoam.cs`: pruva yönü düzeltmesi ve kıç yay sprite'ı.
- `Assets/Editor/ShipOceanSetup.cs`: kaynak temizleme, ayırma, importer ayarları ve yalnızca ShipScene bağlantıları.
- `Assets/Editor/ShipWaterPlayCheck.cs`: isteğe bağlı Play Mode kontrolleri.
- `Assets/Scenes/ShipScene.unity`: bağlantıları ve başlangıç değerleri hazır.

Yeni görselleri yeniden hazırlama menüsü: **Tools → Ship Water → Update supplied bow, stern and surface waves**. Bu menü mevcut deniz ayarlarını değiştirmez. Tam eski okyanus kurulum menüsü de yeni dalgaları son adımda bağlar. Yeni bir çalışma zamanı paketi gerekmez.

## Doğrulama ve sınırlar

Play Mode kontrolü: **Tools → Ship Water → Run Play Mode integration check**. Hareket testleri gerçek Rigidbody2D hızları uygular; test kopyasında silah ve düşman üretimi geçici kapatılır. Klavye kullanımı ve savaş dengesi testin kapsamı dışındadır.

Hareket, durma, sert dönüş, geri hız, yeniden hızlanma, teleport, kapanma/açılma, pause, sabit havuz, pruva yönü, kıç yayları, kamera hareketinde dünya konumunun korunması, minimum mesafe ve su dışlama kontrolleri çalıştırıldı. Gerçek sahnede kıyı/liman bulunmadığı için bu koşullar yalnızca test kopyasında geçici kıyı/iskele collider'larıyla doğrulandı; gerçek kıyı görseli yanında manuel oynanış testi yapıldığı iddia edilmez.

Rapor: `Logs/ShipWater/play-check.txt`. Görüntüler: `idle.png`, `full-speed.png`, `turn.png`, `stopped.png`. Unity Inspector'ın sahne geçişlerinde görülen `SerializedObjectNotCreatableException` kayıtları oyun sistemi/derleme hatası olarak gizlenmez; Console incelemesinde ayrı değerlendirilir.

Son doğrulama sonucu: 25 Play Mode kontrolü geçti (Failures: 0). 22 çıktı PNG'sinin Point/Clamp, mipmap kapalı ve sıkıştırmasız import ayarları doğrulandı. C# derleme ve shader hatası yok. Inspector geçiş istisnaları Console'da ayrı olarak hâlâ görülebilir. Üç kaynak kopyasının SHA-256 eşitliği doğrulandı. Test erişimi için geçici eklenen Pipeline paketi kaldırıldı; çalışma zamanı bu pakete bağlı değildir.
