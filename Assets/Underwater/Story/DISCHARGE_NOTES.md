# Deşarj ve okunabilirlik revizyonu

Yalnız UnderwaterWorld genişletildi. Kaynak görseller korunmuştur. ShipScene, global proje ayarları ve input asseti başlangıç hashleriyle karşılaştırılır.

## Ayarlar

- BrineDischarge: gerçek dekoratif gemi Transform'una bağlı çıkış; dünya biriminde outletOffset=(-8.2,-2.4). 4 eşit 128x256 kare, üst-orta pivot, 6 FPS. Genişlik 1.6, uzunluk 4.5, opaklık .38. Renkli kenarlar temizlenir; yan ve alt alfa yumuşar. Sorting 7, gemi 8: akış gövdeyi boyamaz.
- Etkinlik: mevcut WarEnvironment.Stage > 0 ve dischargeEnabled. Görsel ile daralan/genişleyen görünür çekirdeğe bağlı hasar aynı anahtarı kullanır. İlk bölümde temiz durum korunur.
- damage=1, damageInterval=1 saniye. İlk girişte hasar, içeride saniyelik tekrar; çıkışta durur, hızlı giriş-çıkış cooldown'u atlayamaz. Mevcut Health/respawn ve top dokunulmazlığı paylaşılır. Temas kırmızı renk tepkisi verir, geri itme uygulamaz. Diyalog ve yeniden doğmada hasar yoktur.
- Can UI: 3 kalp, dolu/boş; mevcut sistem tam birim kullandığı için yarım kalp kullanılmaz. Kare başına yeniden oluşturulmaz. Türkçe pixel font ve paneller korunur.
- İlk yüzey bölgesinde 5 iskelet; iki yenisi surfaceY-.42 konumundadır. Boru çevresinde 10 küçük ayrı iskelet ve 8 atık bulunur. Zemindekiler gerçek katı yüzeye ray ile oturur. Dekoratif, hasarsız ve aşamaya bağlıdırlar.
- Konserve: canFrequencyMultiplier 1.5 → 2.25; yaklaşık 6.67 → 4.44 saniye. Toplardan bağımsız zamanlama ve mevcut litter listesinden yeniden kullanım; mevcut toplam sınırı aşmaz. Ağır parçalar 1.4 birim/sn çöker, hafifçe döner ve katı yüzeyde durur.
- İlk başkan başlangıcı yalnız sahnede 1.2 birim sağa taşındı; sonraki toplantı koordinatları değişmez. İnceleme hedefi sprite üst sınırından .65 birim yukarıdadır; aynı hedef sayısı ve mesafeler korunur.
- Top kapsaması: -145…-86, -74…-6, 6…69, 81…145. Güvenli kontrol noktalarının etrafında boşluk kalır. Mevcut iyi aralık/hız/sınır korunur; seyrek tutorial aralığı kaldırıldı. Derinlik ve mevcut yüzme hızıyla önden etkin kesim seçilir; tek kesim üretir, sabit dünya şeritleri kullanılır ve 10 gülle sınırı korunur. Toplar oyuncuyu takip etmez.

## Ses tanısı

Tek AudioListener, listener volume=1, tüm kaynaklar 2D, mute=0 doğrulandı. Mevcut kayıt RMS değerleri önceki ses analizinde .06933/.12806 idi; eski .1/.025 gain seviyeleri ortalama çıkışı oldukça kısıyordu. Ortam .3, baloncuk .11 yapıldı. Ortam döngü crossfade'i ve baloncuk grup/cooldown sistemi korunur. Değerler Inspector'dan ayarlanabilir; kullanıcı AudioListener/mixer ayarları zorla değiştirilmez.

Uygun top atışı kaydı Sounds klasöründe bulunmadı. distantCannon alanı boş bırakıldı. Atanınca tek yönetilen çocuk AudioSource, 4–8 saniyelik aralık, 1400 Hz low-pass, .13 gain ve diyalogda .04 kullanır. Savaş bitince durur. Diğer kaynaklar bu filtreden etkilenmez. Her gülle için yüksek ses çalmaz. Öznel dinleme/birlikte ses dengesi testi yapılmış gibi raporlanmaz.

## Test

Mevcut Underwater > Start third chapter in Play Mode menüsüyle başla. Deşarja yaklaşmak için yüzeyde geminin sol borusuna git. BrineDischarge Inspector alanlarıyla aç/kapa, aralık ve çıkış noktası denenebilir. Hedefli sonuçlar Tools/Revision13/results.txt, görsel discharge.png, ses tanısı audio-diagnostics.txt içindedir. Çalıştırılan tam bölüm sonuçları ayrıca kopyalanır; yapılmayan kontroller ve standalone build geçmiş sayılmaz.

Yeni kod BrineDischarge.cs ve revizyon Editor/test yardımcılarıdır. WarEnvironment, WarDebris, UnderwaterThirdChapter, UnderwaterStoryUI, UnderwaterDayStory, CannonHazards ve UnderwaterSceneAudio mevcut sorumluluklarıyla genişletildi. Yeni paralel sağlık, görev veya top yöneticisi oluşturulmadı.

Son doğrulama: **19 hedefli + 75 tam yol/konuşma + 58 ilk bölüm kontrolü geçti**, yeni Console hatası yok. Diğer sahne dosyaları/ayarları hash ile korundu. Öznel birlikte dinleme ve standalone build yapılmadı; ikinci bölümün tamamı bu revizyonda yeniden çalıştırılmadı.
