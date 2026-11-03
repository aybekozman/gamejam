# Son düzenlemeler — 3 Kasım 2026

Uygulananlar:
- Ana ses ve müzik için ayrı, düzgün oranlı slider; müzik tercihi kalıcıdır.
- Kullanıcının üç MP3 dosyası menü/su altı, su altı tehlike ve gemi sahnelerinde çalar. Sahneler arasında iki müzik kaynağıyla kısa geçiş uygulanır; intro sırasında müzik sessizdir.
- Atla butonu kaldırıldı, videonun son karesi 1.7 saniye tutulur. Menüde Andronova Games yazısı vardır. Company Name Andronova Games; Unity splash ve Unity logo yeni build için kapalıdır.
- Mevcut ilk gemi arası ve küçük balık savaşında küçük balıklar; köpekbalığı savaşında küçük balıklar + köpekbalıkları; son savaşta mevcut havuzun tamamı kullanılır. Üretim aralığı, sayısı, hasar/HP ve ödül istatistikleri korunur. Yalnız düşman hareketi spawn edilen örnekte 1.15 çarpanıyla hızlandırılır.
- Can veya enerji biterse geçerli gemi ziyareti yeniden yüklenir; önceki seferden gelen XP kontrol noktası korunur, su/can/enerji ziyaret başlangıcına döner.
- Batı kıyısı sabit ve devamlıdır; kasaba ve liman buradadır. Kara ve liman duvarları collider ile engellenir. Limanın doğu açıklığı ve serbest deniz açık kalır. Düşmanlar kara/liman duvarları içinde doğmaz.
- Top, namlu parlaması ve mermiler gövdenin önünde çizilir.
- Konuşma erişimleri: normal konuşma/inceleme en az 4.5, duyuru 6, köpekbalığı lideri 7 dünya birimi. Takip ve duyuru işaretleri görselin üstündedir. Köpekbalıkları yönündeki geçiş dikey sınırın tamamını kabul eder.
- Shift ile yüzeye yükselince kısa balistik sıçrama; çıkışta 12, inişte 22 su damlası.
- Esc menüsü zamanı ve kontrolleri durdurur; Devam Et önceki durumu geri getirir. Kart seçimi sırasında açılırsa kart seçiminin duraklamasını korur.
- WASD, Shift ve Space ilk kullanım ipuçları eylem gerçekleştikten sonra kaybolur; zaman aşımı vardır ve aynı oyun oturumunda bölüm tekrarlarında yinelenmez. Yeni Oyun öğreticiyi yeniden başlatır.
- Gemi suyu ilk ziyarette normal; sonraki savaşlarda su altıyla aynı gri-yeşil dönüşüm formülünü .4/.8/.92 kademeleriyle uygular. Dekoratif yüzen çöpler eklenir.
- Seviye başlığı mevcut kart atlasından çerçeve ve piksel font kullanır.

Doğrulama:
- check.txt: 41 Play Mode kontrolü geçti; gerçek klavye girdileri, müzik oynatımı, intro son karesi, yeniden başlama, kara ile gerçek Rigidbody2D teması ve yüzey sıçraması dahil.
- ../MenuPortraitUpdate/flow-check.txt: 11 güncel sahne sınırı/final kontrolü geçti. Test, arıtmayı yalnız runtime kopyada hızlandırır ve üçüncü bölüm bitiş durumunu kurar; tam manuel hikâye/savaş oynanışı testi değildir.
- settings.png, pause.png, level.png, coast.png görsel kontrolleri yapıldı.
- Mevcut UnderwaterWorld ve ShipScene 1 sahne dosyaları ile EnemyStats varlıklarında değişiklik yok; yeni kıyı ve destek bileşenleri runtime eklenir.
- Standalone build alınmadı; Unity açılış ekranı değişikliği yeni build'de geçerlidir.

Yedek: C:/Users/PC/.codex/backups/gamejam-polish-2026-11-03/before-polish.zip
