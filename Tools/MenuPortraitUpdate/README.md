# Menü ve hikâye geçişi düzeltmeleri

- Mevcut StorySceneJourney genişletildi; ikinci gemi ziyaretindeki eksik %100 su bağlantısı giderildi.
- Sıra: ilk keşif → gemi → duyuru → gemi → üçüncü su altı/köpekbalığı yardım görüşmesi → gemi → sonraki gemi → beyaz bayrak/final.
- Bu isteğe göre gemi ziyaretlerinin tamamlanması mevcut arıtılan su göstergesinin %100 olmasına bağlıdır. Yeni teslimat, arıtma bölgesi, rota veya ekonomi eklenmedi.
- Native düşman üretim sıklığı/hızı/havuzu, savaş değerleri, kart seçim mantığı, hareket ve arıtma hızı değiştirilmedi. Hikâye evrelerinin adları native düşman havuzunu ayrıca filtrelemez.
- Kasaba ve liman mevcut gemi başlangıcının etrafında yalnız görsel çevre olarak yerleşir; kamera veya gemi başlangıç koordinatı değiştirilmez. Yeni engel/collider eklenmedi.
- Kullanıcının deniz arka planı, mevcut logosu, menü sheet'i, yükseltme atlası, kaptan/liman görevlisi portreleri ve dört karelik beyaz bayrağı kullanıldı. Kaynak PNG dosyaları korunuyor; hazırlanmış kopyalar Presentation/Resources/Voyage içinde.
- Finalde zaman durur; konuşma ve bayrak animasyonu ölçeklenmemiş zamanı kullanır. Ana Menü/Yeniden Başla mevcut yeni oyun akışına bağlanır.

Doğrulama:
- Logs/StoryJourney/play-check.txt: ilk keşif ve duyuru dahil gerçek sahne geçişleri, mevcut arıtma ve orb seçimi, XP korunması ve yeni oyun sıfırlaması. Testler yalnız runtime kopyalarda aktörleri taşır ve arıtmayı hızlandırır.
- verification.txt: video decode/skip, menü, iki portre, can/enerji/su sıfırlanması, yalnız XP korunması ve normal arıtma.
- flow-check.txt: sonraki sahne sınırları, üçüncü bölümün ilk keşfi tekrarlamaması, yardım sonrası gemiye dönüş, %100 geçişleri ve final. Yardım görüşmesinin tamamlanma durumu testte kurulur; baştan sona manuel savaş oynanışı testi değildir.
- 1280×720 menü, kartlar, liman ve final ekran görüntüleri gözden geçirildi. Standalone build alınmadı.
