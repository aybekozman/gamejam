# Deniz çizgilerinde sakin dalgalanma

`ShipScene` ve `HudFirstTry` denizlerine bağlandı. `CalmPixelOcean.shader`, deniz dokusundaki dar ve parlak çizgileri yerel renk karşıtlığıyla ayırır; eski konumundaki parlak detayı çıkarıp hafifçe kaydırılmış detayı geri ekler. Böylece sabit çizgi üzerine ikinci bir köpük sprite'ı bindirilmez. Daha geniş mavi su desenleri büyük ölçüde korunur. Dokuya yeni bulanıklık veya bloom uygulanmaz.

Hareket dünya konumuna göre farklı fazlarda ilerler. Örnekleme ve kayma tam kaynak piksel adımlarında, zaman ise 6 FPS adımlarında hesaplanır. Mevcut akıntı ve geniş ışık dalgalanması korunur. HUD sahnesi kendi sprite-sheet Animator'ını ve tiling ayarlarını kullanmaya devam eder; shader örneklemesi o anda oynayan sprite'ın atlas dikdörtgeninde kalır.

## Ayarlar

Deniz objesindeki `InfiniteOcean` bileşeninde:

| Alan | Başlangıç | Etkisi |
|---|---:|---|
| Crest Displacement Pixels | 1,25 | Kayma miktarı; 0 yeni efekti kapatır |
| Crest Speed | 1,1 | Salınım hızı |
| Crest Shimmer | 0,14 | Ayrılan parlak detayın hafif aydınlanıp sönmesi |
| Crest Frames Per Second | 6 | Piksel animasyonunun zaman adımları |

Saat `Time.deltaTime` ile ilerler; oyun duraklatıldığında efekt de durur. Yeni nesne üretimi, ses veya ekranın tamamına uygulanan bir efekt yoktur. Gemi, HUD, savaş ve köpük kodu değiştirilmedi. Kaynak resimler ve su altı sahneleri değişmedi.

Kurulumu tekrarlamak gerekirse: **Tools → Ship Water → Configure subtle crest shader**. `ShipScene` mevcut CalmOcean materyalini, `HudFirstTry` yeni HUDOceanCrests materyalini kullanır.

## Doğrulama

İki gerçek Play Mode oturumunda **23 shader kontrolü**, ayrıca mevcut su/köpük entegrasyonunda **25 kontrol** geçti. Başarısız kontrol: **0**. Son shader derlemesinde uyarı/hata, aktif Console sayımında error/warning yoktu.

- Orijinal akıntı ve Animator geçici sabitlenerek, yalnızca yeni shader hareketi farklı gerçek karelerde GPU üzerinden karşılaştırıldı. HUD deniz örneğinde 5.201, dünya koordinatlı deniz örneğinde 22.160 piksel iki animasyon aşaması arasında değişti.
- Yeni efekt açıkken orijinal denize göre örnek alanın yaklaşık %93'ü HUD sahnesinde, %77'si gemi sahnesinde aynı kaldı. Renk değişimi sakin tutuldu.
- Aynı 6 FPS zaman adımı içinde görüntü birebir aynı; sıfır kaymada orijinal su örneklemesi birebir geri geliyor.
- Pause/resume ve gerçek kare ilerlemesi doğrulandı; shader hata rengi görülmedi.
- Mevcut su kontrolü durma, ileri/geri hareket, dönüş, tekrar hareket, teleport, kamera kapsaması, sınırlı iz havuzu, yüzey dalgaları, pruva ve kıç efektlerini kontrol etti. Test değişiklikleri yalnızca Play Mode kopyalarında yapıldı.

Sonuçlar `Logs/OceanCrests/HudFirstTry-check.txt`, `Logs/OceanCrests/ShipScene-check.txt`, `Logs/ShipWater/play-check.txt`; görüntüler `Logs/OceanCrests/` klasöründe. Görsel karşılaştırma 1280×720'de yapıldı. Ayrı bir mobil cihaz/GPU performans profili çıkarılmadı; hareket aktifken shader ek doku örneklemeleri kullanır.

Unity'nin sahne kaydında otomatik eklediği ilgisiz oyun bileşeni alanları teslim farkından çıkarıldı; sahne farkları yalnızca deniz materyali ve dört dalga ayarıyla sınırlı. Test bağlantısı için geçici kurulan Pipeline paketi geri kaldırıldı; efekt bu pakete bağlı değildir.
