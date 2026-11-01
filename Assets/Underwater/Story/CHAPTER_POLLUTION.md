# Bölüme göre su ve ortam

- İlk bölüm: mevcut temiz su, ek savaş çöpü yok.
- Duyuru bölümü: yumuşak geçişle hafif soluk gri-yeşil su; 10 dinamik çöp hedefi, birkaç hafif parça derinde de görünür.
- Köpekbalığı yolculuğu: daha belirgin gri-yeşil su, 52 dinamik çöp hedefi; 64 mutlak bütçe. Suda asılı parçalar ve gerçek yüzeyden batan ağır atıklar birlikte bulunur.

WarEnvironment aynı çevre sistemi olarak genişletildi. Inspector'da announcementWaste, journeyWaste, maximumWaste, wasteInterval ve canFrequencyMultiplier ayarlanabilir. Atık üretim aralığı 1.3/kademe saniye; konserve frekans çarpanı 3.5. Ağır atıklar yavaş battığı için oyuncunun ilerleyeceği dünya konumlarında önceden gerçek yüzeyden düşer. Geride kalan atıklar kamera dışında yeniden kullanılır.

Renk geçişi yalnız su/arka plan SpriteRenderer'larına uygulanır. Başlangıçta renk değişimi sıfırdır; orijinal alpha, tint ve yatay/dikey çevirme korunur. Materyal çalışma anında oluşturulur, paylaşılan materyaller değiştirilmez. Oyuncu, NPC'ler, UI ve mevcut deşarj efekti bu renklendirmeden hariçtir.

Mevcut OceanHabitat, üçüncü bölümde X -160…170 savaş yolunda 8 resif balığını hedefler. İlk bölümün köy/boş su dağılımı korunur. Mevcut animasyon, rastgele gezinme ve mesafe temizliği kullanılır. Gülle isabeti aynı ölüm sistemiyle balığı ters çevirip yüzeye çıkarır. Ölmüş balık ikinci kez vurulmaz; en fazla 8 ortam cesedi, yaklaşık 80 saniyede yumuşak temizlik. Başkan, uzmanlar, lider ve görev köpekbalıkları etkilenmez.

Doğrudan deneme: Underwater > Start second chapter in Play Mode veya Start third chapter in Play Mode. WarEnvironment.previewStage=-1 hikâyeyi izler; 0/1/2 değerleri Play Mode'da önizleme sağlar.

Değişenler: WarEnvironment.cs, OceanHabitat.cs; yeni PollutedBackground.shader ve Editor/test yardımcıları. Dünya yerleşimi, çarpışmalar ve gemi sahnesi korunur. Güncel sonuçlar ve temiz/duyuru/yolculuk ekran görüntüleri Tools/Revision14 altındadır. Standalone build testi yapılmadı.

Gökyüzü düzeltmesi: ana Sky, Sky extended ve Sky shore parçalarının tamamı aynı bölüm kirliliğini kullanır; geminin arkasında temiz mavi parça kalmaz. İlk bölümde hepsi orijinal renktedir.
