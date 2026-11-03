# Deniz/gemi son düzenlemeleri — 3 Kasım 2026

- Saldırı üreticisi savaş sahnesine girişte kapalıdır. Gemi liman merkezinden 17 birim uzaklaşıp rıhtımdan ayrılınca bir kez açılır; doğu ve kuzey/güney çıkışları çalışır. İlk saldırısız seferde hiç açılmaz.
- Başlangıç grubu en az 6 balık; mevcut başlangıç ayarının 1.5 katı daha büyükse o değer kullanılır. Mevcut sahnede 2 → 6. Üretim aralığı mevcut değerin %75'i (5.14 → 3.855 sn), grup başına balık sayısı +1 (1 → 2). Mevcut artış zamanı, hız/hasar/HP değerleri korunur.
- İlk seferde Ossuary 7 - Resolve çalar. Enerji tüketimi geri açılmıştır. Arıtma mesafesi yalnız bu ziyarette 1000 yerine 420 dünya birimidir.
- Saldırısız denizde 20 adet yenilenen, farklı konumlara dağıtılan enerji loot'u bulunur. Her biri 25 enerji verir; XP seçili olsa bile bu özel deniz pickup'ları şimşek olarak görünür ve yalnız enerji verir. Savaş loot'larının mevcut miktarları değiştirilmedi.
- İlk arıtma ipucu: “WASD • Su arıtımı yapabilmek için gemi ile yüz.”
- Oyun içinde P mevcut kısmı atlar; konuşma ve kart seçimi sırasında da çalışır. Oyun arayüzünde, tutorial veya menüde bu kısayol hakkında bilgi gösterilmez.
- Boost yönü, hareket girdisiyle boost devam ederken değişir; mevcut dash hızı, süresi ve hak tüketimi korunur.
- Tüm saldırgan balıklar, su altı karakter/NPC/balık cesetleri, rastgele canlılar ve arka plan balıkları aynı 1.15 oranıyla büyütülür. Kaynak sprite/stat varlıkları değiştirilmez.
- Yıldız ve şimşek görselleri .7 yerine 1.4 dünya birimi boyutundadır.
- Su altı ortam sesi 1.8, baloncuk seviyesi 1.35 çarpanıyla yükseltilir. Yüzey sıçrama hızı 6.5 → 7.8; çıkışta 16, inişte 42 damla. Kullanıcının blup.wav dosyası çıkışta .12, inişte .42 kazançla çalar.
- Kasaba dışındaki kıyı şeridi kaynak görselden hazırlanmış, iki ucu eşleşen bir parçayı tekrarlar. Önceki parça birleşimlerindeki kesikler ve tekrarlanan nehir çatlakları giderildi.
- Kullanıcının “Pikselli Tuzlu Su Akış Sprit Sayfası” sekiz kareye ayrılır; ağız pivotu sabitlenir, şeffaflık saçakları hazırlanmış kopyada temizlenir. Gemi ilerleyip arıtırken akış boru ağzını ve geminin yönünü takip eder; finalde kapanır.
- Final: önce hareket/savaş durur; kamera 1.8 saniyede hafif yaklaşır; iki kat büyük beyaz bayrak ölçeklenmemiş zamanda 6 FPS dalgalanır. .9 sn bekledikten sonra konuşmalar başlar. Ayrılış anlatımı 10000 sıralı Screen Space Overlay ve tam opak siyah örtü üzerinde gösterilir. Gemi, namlu ve bayrak yazının önüne çizilemez. Son final metninden ana menüye otomatik dönüş korunur.

Doğrulama:
- check.txt: güncel 33 Play Mode kontrolü geçti. Gerçek klavye P atlamaları, boostta yön değiştirme, kuzey/doğu liman çıkışı, yoğunluk, enerji toplama, sıçrama/ses ve final örtüsü dahil.
- ../FinalFlowUpdate/check.txt: normal hikâye, loot, %20 balina katılımı ve otomatik menü dönüşünü içeren 31 kontrol bu düzenlemelerle yeniden çalıştırıldı ve geçti. Son liman algısı genişletmesinden sonra yeni kuzey çıkışı ayrıca check.txt ile doğrulandı.
- Testler runtime kopyalarda aktörleri taşır/arıtmayı hızlandırır; tamamen manuel denge oynanışı veya standalone build testi değildir.
- peaceful-energy.png, pipe-flow.png, combat.png, flag-closeup.png ve departure.png görselleri incelendi.
- Mevcut UnderwaterWorld ve ShipScene 1 sahne dosyaları, kaynak sanat/ses dosyaları ve EnemyStats varlıkları değiştirilmedi. Hazırlanmış kopyalar Presentation/Resources içindedir.

Yedek: C:/Users/PC/.codex/backups/gamejam-final-polish-2026-11-03/before-change.zip
