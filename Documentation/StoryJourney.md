# İki sahneli hikâye akışı

## Dosyalar ve kapsam

Gerçek sahneler `Assets/Scenes/UnderwaterWorld.unity` ve `Assets/Scenes/ShipScene 1.unity`. Yeni sahne kopyası oluşturulmadı. Build listesine bu iki dosya eklendi; ilk etkin sahne UnderwaterWorld. Projede mevcut Yeni Oyun menüsü veya sahneler arası taşıma sistemi bulunmadı. `StorySceneJourney.NewGame()` oturumu sıfırlayarak ilk su altını yeniden yükler; disk kaydı yoktur.

Değişen mevcut dosyalar:

- `Assets/UI/ShipHUD/OrbRewardToggleView.cs`: görseller mevcut `ShipUIManager.isLootingXP` değerini okur; tıklama mevcut `ToggleLootMode()` API'sini kullanır. Mevcut Space tuşu bağlaması korunur. Eski `OrbRewardMode` bağlantısı olan diğer sahneler için önceki davranış korunur.
- `ProjectSettings/EditorBuildSettings.asset`: mevcut iki hedef sahne etkin olarak liste başına eklendi.

Yeni dosyalar (ve Unity'nin oluşturduğu .meta dosyaları):

- `Assets/Scripts/Ship/StorySceneJourney.cs`: yalnızca İlkSuAltı → İlkGemi → İkinciSuAltı → GemiSavaşı oturumu, Single sahne yükleme ve çift geçiş kilidi.
- `Assets/Scripts/Ship/StorySceneEntry.cs`: mevcut sahne Start işlemlerinden sonra, açık bir execution order ile giriş/geri yükleme. Tahmini süre beklemesi kullanılmaz.
- `Assets/Scripts/Ship/ShipVisitSnapshot.cs`: gerçek ShipUIManager seviyesi/XP'si, uygulanmış skaler yükseltmeler, sağlık/enerji, dash değerleri, taret geri tepmesi, statik mermi bonusları, ganimet modu ve arıtma ilerlemesinin oturum içi kopyası. Sahne nesnelerini veya ses kaynaklarını taşımaz. Hedef Awake/Start sıfırlamalarından sonra uygulanır.
- `Assets/Scripts/Ship/ShipWaterPurification.cs`: kullanıcı tarafından sonradan onaylanan ileri hareketle arıtma ve mevcut HUD bağlantısı.
- `Assets/Editor/StoryJourneyPlayCheck.cs`: isteğe bağlı, Play Mode kopyasında çalışan entegrasyon kontrolü.
- Bu rapor.

Hedef gemi sahnesine doğrudan geliştirici Play Mode girişi, hikâye oturumu başlamadığı için otomatik su altı dönüşü başlatmaz; yeni arıtma ve ganimet görseli bağlantısı yine kurulur. Normal oyun Build listesindeki ilk su altından başlar. Editör kontrol menüsü de aynı sahneyi başlangıç sahnesi olarak kullanır.

## Mevcut bölüm bitiş bağlantıları

1. `UnderwaterSecondChapter.RequestHomeEntry()` içindeki `firstChapterDepartureRequested`: ilk bölüm gerçekten tamamlandıktan sonra, eve yakınken ve diyalog kapalıyken mevcut devam etkileşimi. Geliştirme atlaması yalnızca normal oturumda runtime kapatılır; ilk ölüm, incelemeler ve başkan görüşmesi değiştirilmez.
2. `UnderwaterSecondChapter.secondChapterCompleted`: duyuru ve mevcut ayrılış yarıçapı koşulundaki `Finish()`. Koşul veya konum değiştirilmez. İkinci gemi sahnesini yükler; üçüncü su altı bölümü başlatılmaz.

Gemi dönüşünde mevcut `BeginSecondChapter()` kullanılır. Bu API zaten ilk bölüm durumunu tamamlanmış kurar; homeArrival, başkan, bilim balıkları, kasabalılar ve duyuru görevini kendi mevcut akışıyla hazırlar. Bölüm kodlarına, yerleşime ve diyaloglara dokunulmadı.

## Arıtma kaynağı

İncelemede yalnızca görsel bar vardı. Kullanıcının son mesajıyla yeni ilerleme mantığına izin verildi. Gerçek kaynak `ShipWaterPurification.NormalizedProgress` değeridir; **0–1** aralığındadır. Yüzde yazısı okunmaz. İlerleme, Rigidbody2D'nin geminin ileri yönüne izdüşen pozitif hızı × fixedDeltaTime / forwardDistanceForFull ile hesaplanır. Başlangıç ayarı **1.000 dünya birimi = %100**; ilerleme bağımsızdır, can/enerji/XP tüketmez. Durma, yan kayma, geri hareket ve pause ilerleme üretmez. Işınlanma mesafesi toplanmaz.

Bar Image.fillAmount doğrudan bu değeri; ayrı TMP metni `%0`–`%100` görünümünü kullanır. Tek seferlik `Completed` olayı yalnızca İlkGemi aşamasında dönüş ister. Mevcut kart seçimi oyunu duraklatıyorsa seçim tamamlanana kadar geçiş bekler; ardından uygulanmış yükseltmeler alınır. İkinci gemi girişinde %100 korunur, tamamlanma olayı yeniden üretilmez ve GemiSavaşı aşamasında dönüş kontrolü çalışmaz.

Üretim kodunda düşman üretimine aç/kapa kuralı, yeni savaş süresi, liman/teslimat/su bölgesi veya savaş bitişi yoktur. Sadece yükleme sırasında zaman geçici durur ve hareket girdisi kilitlenir. Tek küçük oturum nesnesi DontDestroyOnLoad kullanır; sahne yüklemeleri Single'dır.

## Gerçek kontrol ve sınırları

`Tools → Story Journey → Run two-scene Play Mode check` gerçek iki sahneyi yükler. Otomatik testte oyuncu ve başkan test kopyasında ışınlanır; mevcut diyaloglar `Advance()` ile ilerletilir. İlk ölüm/yüzeye çıkma akışı gerçek coroutine ile çalışır; incelemeler, başkan görüşmesi, eve dönüş ve ikinci duyuru kendi API'leri/koşulları üzerinden tamamlanır. Sonraki hareket testinde yalnızca test kopyasının gemi kontrolü ve düşmanları sabitlenir; arıtma bekleme süresini kısaltmak için test kopyasında mesafe son adımda 2'ye indirilir. Bu değer sahneye veya üretim varsayılanına kaydedilmez.

Son çalıştırmada **34 kontrol geçti, Failures: 0** (`Logs/StoryJourney/play-check.txt`). Kapsanan kontroller: dört ziyaret, çift olay engeli, ilk bölümün tekrar başlamaması, mevcut ikinci bölüm aktörleri/başlangıcı, %100 dönüşünün yalnız ilk gemide olması, ikinci gemide döngü oluşmaması, Yeni Oyun sıfırlaması, gerçek seviye/XP ve yükseltmelerin taşınması; Space ve gerçek EventSystem raycast'iyle ganimet seçimi, gerçek mevcut loot API'sinde enerji/XP ayrımı; ileri/yan/geri/durma/pause arıtması ve HUD; ilk gemide başlangıç düşmanlarından sonra yeni üretim; ikinci gemide mevcut silah/kontrol/üreticinin etkinliği; tek yönetici, tek AudioListener ve en fazla bir EventSystem.

Korunan 13 sahne/oynanış/hikâye dosyasının önce/sonra SHA-256 değerleri eşleşti (`Logs/StoryJourney/protected-before.json`). Özellikle gemi hareketi, EnemySpawner/Enemy/EnemyFishVisual, silahlar, sağlık ve mevcut su altı bölüm kodları ile iki sahnenin dosyaları değişmedi. EnemySpawner'ın sürekli `while (true)` üretim ve zorluk döngüleri korunmuştur.

Fiziksel klavyeyle baştan sona manuel oynanış, uzun süreli savaş dengesi, bağımsız player build çalıştırma ve sesin dinlenerek değerlendirilmesi yapılmadı. Ses kaynakları taşınmadı/eklenmedi; listener sayısı kontrol edildi. Unity Console'da önceden görülen Editor Inspector `SerializedObjectNotCreatableException` kayıtları kaldığı için Console sıfır hata olarak raporlanmaz; derleme hatası yoktur.
