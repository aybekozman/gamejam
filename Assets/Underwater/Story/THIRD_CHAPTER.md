# Üçüncü su altı bölümü: güllelerden kaçış ve köpekbalığı lideri

Sahne: `Assets/Scenes/UnderwaterWorld.unity`. Gemi sahnesi veya global ayarlar değiştirilmez. İlk iki bölüm mevcut bileşenlerle devam eder.

Normal akışta ikinci bölümün bitiş noktasına gelince, mevcut `Gemi bölümünü geç — geliştirme modu` açıkken **E ile devam et** görünür. Kısa kararma aynı sahnede üçüncü bölümü başlatır. Oyuncu yolun güvenli başlangıcına alınır; önceki konuşmalar/ölüm olayı tekrar oynatılmaz.

## Doğrudan test

Unity menüsünden **Underwater > Start third chapter in Play Mode**. Alternatif olarak `Underwater day • dead fish discovery` nesnesindeki `UnderwaterThirdChapter` bileşeninde **Start Here In Editor** açılıp Play'e basılabilir. Play sırasında bileşenin **Development / Start third underwater chapter** bağlam menüsü de aynı giriş noktasını kullanır.

WASD/yön tuşları ile sağdaki köpekbalığı bölgesine yüz. Mevcut görev oku doğrudan `leader` Transform'una bağlıdır. Lider görünürken işaret başının üstündedir; yaklaşınca konuşma tuşu görünür. Uyarı levhası dünya zeminindedir, görevin hedefi değildir. Lider ve iki dost köpekbalığı saldırmaz.

Birinci bölümdeki **Development / Reset this underwater story** bütün hikâyeyi ve yerel tehlikeleri sıfırlar. Başlangıç seçenekleri yalnız Editor içindir; oyuncu menüsü eklenmedi.

## Başlangıç ayarları

- Oyuncu hızı değişmedi: 6 birim/sn. Gülle başlangıcı 2.8, kontrollü son hızı 8.1 birim/sn (1.35 kat); ivme 5.5, yatay sapma en fazla 0.12.
- Dört sabit kısa tehlike kesimi: X -140…-100, -65…-25, 15…55, 90…130. Aralarda güvenli alanlar bulunur. Yüzey girişleri gerçek `surfaceY` üzerinden yapılır; kamera üstüne taşınmaz.
- Kesimler oyuncudan 32 birim önce etkinleşir. İlk kesimde 1 saniyede tekli, sonra 0.65 saniyede deneme; her üç atışta iki güllelik salvo ve 1.4 saniye ara. Sabit düşüş şeritleri yaklaşık 7 birim aralıklı, etkin tehlike sınırı 10.
- Görüş yüksekliği ve son hız yaklaşık 0.8–1.2 saniyelik tepki alanı verir. Yüzeyde oyuncuya 1 saniyelik düşüş mesafesinden yakın yeni gülle oluşturulmaz. Atışlar oyuncuyu takip etmez.
- 3 can; 1.3 saniye hasar dokunulmazlığı. Son güvenli kontrol noktasında kısa kararmayla yeniden doğulur. Diyalog ve yeniden doğma sırasında yerel tehlikeler durur, dünyadaki diğer sistemler çalışmaya devam eder.
- Kontrol noktaları X -162.27, -80, 0, 75, 155. Tabela X173 civarında gerçek tabana 0.12 birim gömülü. Lider X216/Y-25 civarında; güvenli yol başlangıcı Y-26.95 ve kontrol noktaları yaklaşık Y-27'dir. Normal köpekbalığı 4, lider 5.2 birim genişliktedir.
- Gülle hareketi boyunca CircleCast taraması oyuncu ve gerçek statik colliderları denetler; colliderı olmayan dekoratif arka plan kayaları zemin sayılmaz. Etki ve gülle nesneleri tekrar kullanılır; en fazla 48 kısa efekt, gülleler en fazla 14 saniye kalır.

## Ses

Bulunan gerçek dosyalar: `Assets/Sounds/freesound_community-underwater-loop-amb-6182.mp3` ve `soundshmyak-bubbles-159281.mp3`. Kaynak dosyalar değiştirilmedi. Unity örnek analizi `Tools/Revision10/audio-analysis.txt` içindedir.

Ortam: 0.1 ses seviyesi (yaklaşık -20 dB), 1 saniye giriş, 0.6 saniye iki sabit AudioSource arasında döngü geçişi; diyalogda hafif kısılır. Baloncuk: 0.025 (yaklaşık -32 dB), kayıt içinden 0.65 saniyelik pencere, yumuşak kenarlar, en az 1.6 saniye ara. Mevcut oyuncu baloncuk grubunun olayı kullanılır; yeni parçacık sistemi oluşturulmadı. Toplam beş sabit ses kaynağı vardır; bölümlerde/yeniden doğmada çoğalmaz.

Uygun **suya giriş ve boğuk çarpma sesleri yok**. `UnderwaterSceneAudio.splash` ve `impact` Inspector alanları boş bırakıldı. Rastgele ses atanmadı. Mevcut AudioMixer bulunmadı; Mixer alanı daha sonra atanabilir. Dalga seviye/sınır analizi yapılır; öznel dinleme yapılmış gibi raporlanmaz.

## İlerideki bağlantılar

**BeginThirdChapter()** gelecekte gemiden dönüşte, su altı hikâye Start'ı tamamlandıktan sonra çağrılacak ayrı giriş noktasıdır. **chapterCompleted** tek seferlik UnityEvent, lider ve iki köpekbalığı kısa güvenli toplanmasını bitirince yayınlanır. Sonrasında görev “Köpekbalıklarının yardımı sağlandı.” olur, işaretler/tehlikeler temizlenir ve kontrol serbesttir. Dönüş yolculuğu, gemi savaşı veya başka sahne yüklenmez.

## Değişen dosyalar

Yeni: `UnderwaterThirdChapter.cs`, `CannonHazards.cs`, `UnderwaterCannonball.cs` (kısa efekt bileşeni de burada), `UnderwaterSceneAudio.cs`, `ThirdChapterSetup.cs`, `ThirdChapterVerification.cs`, `Tools/prepare_third_chapter_assets.py`, işlenmiş `leader_*`, `cannon_*`, `shark_warning` sprite kopyaları.

Genişletilen: `UnderwaterDayStory.cs` ortak etkileşim yönlendirmesi; `UnderwaterSecondChapter.cs` bölüm-üç hazırlık bağlantısı; `UnderwaterStoryUI.cs` mevcut Canvas'ta kısa düşünce/can/uyarı ve ekran boyutuna uyumlu hedef; `FishPilot.cs` mevcut baloncuk grubu için ses olayı; `UnderwaterWorld.unity` yalnız ek bileşen/NPC/tabela. Önceki `SecondChapterVerification.cs` testine bilim uzmanlarını kasabada koruyan bölüm-üç bağlantı kontrolü eklendi.

Play Mode sonuçları, normal WASD güzergâh süresi, görsel kontroller ve koruma doğrulaması `Tools/Revision10` altındadır. Standalone build kapsam dışı testtir; çalıştırılmadıkça geçmiş sayılmaz.

Son doğrulama: üçüncü bölüm **67 PASS / 0 FAIL**, birinci bölüm **58 PASS / 0 FAIL**, ikinci bölüm ve yeni bağlantısı **87 PASS / 0 FAIL**. Normal hızda gerçek Input System WASD/E girdileriyle yol yaklaşık 64 saniye yüzülerek tamamlandı; güzergâhta can kaybı olmadı. İzole hasar/yeniden doğma testleri ayrıca çalıştırıldı. Yeni Console hatası görülmedi. Standalone build testi yapılmadı.

Yoğunluk/görünürlük revizyonu: ikinci bölümün bitişindeki devam etkileşimi oyuncu portreli panelde gösterilir; tek E ile başlar. Üçüncü bölümün yolculuk düşünceleri aynı panellerde, sırayı bozmayan otomatik diyalog olarak gösterilir. Hareket ve gülleler bu otomatik metinlerde durmaz. Normal atış aralığı 0.65s, tutorial 1s, gülle sınırı 10, şerit aralığı 7 ve her üç atışta salvo/1.4s ara. Güncel revizyon kontrolleri Tools/Revision11 altındadır; önceki sayılar önceki yoğunluk revizyonuna aittir.
