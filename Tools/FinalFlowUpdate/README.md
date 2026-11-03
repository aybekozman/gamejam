# Güncel loot, font ve hikâye akışı

- XP seçiliyken balıklardan HUD'daki yıldız, enerji seçiliyken şimşek görünümünde loot düşer. Yerdeki loot da seçim değişince güncellenir; verilen mevcut XP/enerji değerleri değiştirilmez.
- ShipUIManager'ın seviye alanı gerçek sol üst LevelValue yazısına bağlanır. Ayrı PlayerExperience sayacı görünür seviyeyi yönetmez.
- Kart başlıkları, açıklamalar, seçim yazıları ve sayılar Pixelify kullanır. HUD sayıları aynı fonttan üretilmiş, küçük boyutlarda okunabilir SDF varlığını kullanır.

Normal oyun sırası:
1. İlk su altı keşfi → balıkları başkana gösterme → evden çıkış.
2. İlk gemi seferi: saldırı yok, mevcut serbest hareketle arıtma. Enerji loot'u düşmeyeceği için yalnız bu ziyarette enerji tüketimi kapalıdır. %100'de su altı duyurusuna dönülür.
3. Mevcut başkan/bilim ekibi duyurusu ve saldırı kararı. Köpekbalıkları yönündeki dikey sınırı geçince küçük balık savaşına gidilir.
4. Küçük balık savaşı + arıtma. %100'de toplardan kaçarak köpekbalıklarına ulaşma bölümüne dönülür.
5. Yardım görüşmesi ve mevcut grup toplanması tamamlanınca son gemi seferi başlar.
6. Son sefer küçük balıklar + köpekbalıklarıyla başlar. Arıtma %20'ye ulaşınca bir kez textbox açılır; mevcut üretici havuzuna balinalar eklenir ve ilk balina gelir. Sahne, arıtma ve XP sıfırlanmaz; ayrı bir balina seferi açılmaz.
7. Aynı savaşta %100: sistemin durması, beyaz bayrak, anlaşma, kısa ayrılış anlatımı, final yazısı. Final yazısından 9 saniye sonra otomatik ana menüye dönüş. Önceki Ana Menü/Yeniden Başla butonları da çalışır.

Müzik:
- Önceki menü müziği yalnız MainMenu'de.
- İlk keşif ve duyuru su altında müziksiz; mevcut ortam sesleri ve efektler korunur.
- TopSUaltı.mp3 yalnız üçüncü su altı/köpekbalıklarına yolculukta.
- Mevcut gemi müziği korunur.

Doğrulama:
- check.txt: 31 güncel Play Mode kontrolü geçti. Gerçek balık öldürme ve loot toplama, gerçek LevelUp ile görünür LV2, fontlar, saldırısız ilk sefer, doğal duyuru/görüşme API'leri, %20 katılımı, final ve otomatik menü dönüşü dahil.
- Testler ilk keşfin tamamlanma durumunu kurar, NPC'leri yalnız test sırasında yerleştirir ve arıtmayı runtime kopyada hızlandırır. Tamamen manuel bir oynanış turu veya standalone build testi değildir.
- level-and-fonts.png, whales-join.png ve ending.png görsel kontrolleri yapıldı.
- Yerleşimi korumak için mevcut UnderwaterWorld/ShipScene 1 sahne dosyaları ve düşman stat varlıkları değiştirilmedi. Kayıtlı önceki raporlar önceki kapsama aittir; güncel akış için bu rapor geçerlidir.

Yedek: C:/Users/PC/.codex/backups/gamejam-loot-final-flow-2026-11-03/before-change.zip
