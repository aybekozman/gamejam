# Genişletilmiş su altı dünyası

`Assets/Scenes/UnderwaterWorld.unity` sahnesini açıp Play'e basın. WASD/yön tuşlarıyla yüzün. Dikey yüzüşte balık görseli yaklaşık 58 derece yönelir; gövde collider'ı dönmez. `FishPilot.tilt` ve `pitchResponse` değerleri Inspector'dan ayarlanabilir.

Mevcut kayalar, evler, yosunlar ve collider açık/kapalı ayarları korundu. Yalnız dış sınırlar yeni kenarlara taşındı. Yeni çevre parçaları `Ocean expansion • preserved layout` altında bulunur. Harita yatayda -240 ile +240 arasındadır; özgün su rengi ve derinlik geçişi dikeyde tekrarlanmaz. Kasaba kopyalanmaz; zemine aldığınız dekorlar iki yanda tekrarlanır. Yeni dekor kayalarının collider'ları kapalıdır.

Aynı objenin `OceanHabitat` bileşeninden türler, hareket/animasyon hızları, yoğunluk, bölge mesafeleri, spawn sıklığı, silinme uzaklığı ve fade süresi ayarlanabilir. Kasaba yoğunluğu kaydettiğiniz altı yapının güncel merkezinden ölçülür. Oyuncu evlerin hemen yanındaki, ev collider’larıyla çakışmayan boşlukta başlar:

- 0–24 birim: yaklaşık 18 renkli balık, seyrek diğer canlılar (2).
- 24–60 birim: renkli balık sayısı giderek sıfıra iner.
- Kasabadan 60 birim sonra, en sağ bölgeye kadar: renkli balık ve köpekbalığı olmayan sakin geçiş; diğer canlılar normal yoğunlukta (5).
- Köpekbalıkları yalnız dünya X >= 180 olan en sağdaki 60 birimlik alanda gezer; en fazla 3 tanedir. Bu alanda ayrıca tek tük renkli balık (hedef 3) ve seyrek diğer canlılar bulunur.
- Balinalar oynanış nüfusuna dahil değildir. Yalnız düşük kontrastlı, parallax ile uzaklık hissi veren arka plan katmanında geçer.

Sayılar oyuncunun çevresindeki hedef nüfustur; her karede oluşturulan sabit sayılar değildir. Denizatı, denizanası, kaplumbağa ve vatoz rastgele seçilir. Normal spawn görüş dışında olur. Uzaklaşan canlılar yaklaşık 52 birim mesafede yumuşakça silinir; ekrandaki görüş alanı daha genişse bu mesafe otomatik artırılır. Canlılar oyuncuya saldırmaz ve takip etmez. Renkli balıklar kasaba civarı veya en sağ bölgesinde, köpekbalıkları yalnız sağ uçta kalır. Eski sabit canlılar düzenleme görünümünde korunur; Play Mode'da yeni rastgele nüfus onların yerini alır.

Vatoz ve balinalarda tutarsız ölçülü kareler elendi; seçilen karelerin gövde genişliği ve burun noktası hizalandı. Kaplumbağa kareleri kabuk ölçüsüne göre hizalandı; yüzgeç pozu gövdenin ölçüsünü değiştirmez. Animasyon sırasında transform ölçeği değişmez. Kaynak görseller korunur. `python Tools/prepare_sprites.py` sprite hazırlama ve stabilizasyon adımlarını birlikte tekrarlar.

Genişletme aracı `Underwater > Extend current scene (preserve authored layout)` mevcut sahneye ekleme yapar ve ikinci çalıştırmada çoğaltmaz. Eski `Build separate exploration scene` ilk sahneyi sıfırdan oluşturan araçtır; bu düzenlenmiş sahnede kullanmayın.

Sahnenizin genişletmeden hemen önceki yedeği: `Tools/Revision2/UnderwaterWorld.before-expansion.unity`. Koruma karşılaştırması ve animasyon ölçüleri: `Tools/Revision2/static-results.json`. Play Mode sonuçları ve görüntüler de aynı klasördedir.

## Arka plan ve güncel yerleşim

`Distant ocean life • schools and whales` objesinde arka plan balıkları, orijinal gölge sürü sprite’ları ve balinalar ayarlanır. `DistantOceanLife` içindeki yoğunluk, opaklık, doğma aralığı ve parallax değerleri Inspector’dan değiştirilebilir. Arka plan canlıları oynanışın arkasında çizilir, collider taşımaz ve görüş dışında sahneden ayrılır. Görünen balinalarda ölçek animasyon boyunca sabittir.

Güncel ev konumlarına göre başlangıcı yeniden seçmek gerekirse `Underwater > Update village start and distant life (preserve layout)` komutunu kullanın. Bu komut evlerin konumuna, ölçeğine ve collider’larına dokunmaz; oyuncu ve kameranın başlangıcını, habitat merkezini ve yeni arka plan sistemini günceller. Son değişiklikten önceki sahne yedeği: `Tools/Revision3/user-layout-before.unity`. Koruma ve Play Mode kontrol raporları aynı klasördedir.
## Kademeli batı kıyısı ve deniz tabanı

Yeni deniz tabanı ve iskele `Shore and grounded seabed` grubundadır. Orijinal PNG dosyaları korunur; temizlenmiş kopyaları `Resources/Abyss` altındadır. Yeni görselleri tekrar hazırlama komutu: `python Tools/prepare_shore_assets.py`.

Arka planın iki derinlik katmanında deniz tabanı panoraması yatayda aynalanarak tekrarlanır. Uzak ve orta kayalar tabanla aynı parallax ebeveynine bağlıdır; kamera hareket ettiğinde ayakları tabandan ayrılmaz. Bu düzenleme ön plandaki kayaların, evlerin ve collider’ların yerleşimini değiştirmez.

Köyün solunda taban yaklaşık X=-232’den başlayarak kademeli yükselir ve X=-308 civarında su yüzeyiyle buluşur. Önceki sol sınır kıyının gerisine taşınmıştır. Balık dikey bir harita duvarı yerine doğal biçimde sığlaşan suyla sınırlanır. İskele X=-313..-295 arasında, suyun üstünde bir güverte ve eğimli tabana ulaşan desteklerle yerleşir. Eğimin görseli ve collider’ı aynı yükseklik profilinden oluşturulur. Normal kamera zoom’u değişmez.

Evler, oyuncunun köydeki başlangıcı ve mevcut collider tanımları korunur. Son işlem öncesi yedek: `Tools/Revision4/user-layout-before.unity`. Kıyı ve taban kontrolleri ile ekran görüntüleri aynı klasördedir. Araç: `Underwater > Add gradual western shore and grounded seabed`; ikinci çalıştırmada sahneyi çoğaltmaz.
## Mevcut zeminle aynı sprite kıyısı

Sığlaşan bölümde MeshRenderer veya esnetilmiş panorama dolgusu kullanılmaz. Kıyı, mevcut `ground_0_0` sprite’ının piksellerinden oluşturulan 14 SpriteRenderer parçasıdır. Her parça mevcut zeminin aynı PPU, X/Y ölçeği, Point filtresi ve taş renklerini kullanır; görüntü yeniden örneklenmez. Taş dokusu dikeyde uzatılmadan tekrarlanır. Son parçalar mevcut zeminin arkasına bindirilir ve birleşim yüksekliği görünür zemin piksellerine hizalanır.

İskele, kıyının ana eğimi, köy, kamera başlangıcı ve diğer collider’lar korunur. Yalnız oluşturulan kıyı collider’ının eski zeminle birleşen son yüksekliği düzeltilmiştir. Yedek ve piksel ölçeği raporu: `Tools/Revision5`. Sprite üretim betiği: `Tools/prepare_sprite_shore.py` (sahne konfigürasyonu `config.json`, yerleşim `manifest.json`).
