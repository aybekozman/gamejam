# İkinci su altı hikâye bölümü

Sahne: `Assets/Scenes/UnderwaterWorld.unity`. Takvim günü/savaş sayacı eklenmedi.

Normal akış: ilk bölümü bitir, `House` evine dön, ekranda gösterilen Interact girdisine (klavyede E) bas. Aynı sahnede kısa kararma ve “Bir süre sonra…” sonrasında evin önünde ikinci bölüm başlar. İlk bölümün tamamlanması korunur. Büyük mevcut kasaba binası önündeki duyuruya yaklaş ve E ile katıl. Duyurudan sonra sağa, mevcut köpekbalıkları yolundaki hedefe yüz. Kasabadan yaklaşık 16 birim sonraki güvenli noktada bölüm bir kez tamamlanır. Kısa tamamlanma yazısından sonra aktif hedef temizlenir; yüzmeye devam edebilirsin.

## İlk bölümü oynamadan test

Unity menüsü: **Underwater > Start second chapter in Play Mode**. UnderwaterWorld açık olmalıdır. Alternatif: `Underwater day • dead fish discovery` nesnesindeki `UnderwaterSecondChapter` bileşeninde **Start Here In Editor** seçeneğini açıp Play'e bas. Bu seçenek yalnız Editor'da kullanılır ve varsayılanı kapalıdır. Play sırasında bileşen bağlam menüsündeki **Development / Start second underwater chapter** de aynı giriş noktasını çağırır. Tam sıfırlama birinci bölüm bileşenindeki mevcut **Development / Reset this underwater story** menüsüdür.

## İleride gemi bağlantısı

`UnderwaterSecondChapter.skipShipForDevelopment` Inspector'da “Gemi bölümünü geç — geliştirme modu” adıyla görünür ve şimdilik açıktır. Kapalıysa ilk bölüm sonunda evdeki etkileşim `firstChapterDepartureRequested` UnityEvent'ini yalnız bir kez yayınlar. Kontrol kilitlenmez; sahne yüklenmez. Gerçek gemi dönüşü ekipçe bağlandığında **BeginSecondChapter()** aynı hazırlığı çalıştırır. Bu çağrıyı su altı sahnesindeki hikâye Start'ı tamamlandıktan sonra yapın.

İkinci bölüm sonundaki **secondChapterCompleted** UnityEvent'i de bir kez yayınlanır. Henüz savaş, başka sahne yükleme, köpekbalığı konuşması, su teslimatı veya sonraki görevler yoktur.

## Durum ve NPC'ler

Ayrı ikinci bölüm durumları: AwaitingHomeEntry, Preparing, AttendAnnouncement, Announcement, LeaveForSharks, Completed. Birinci bölümün Complete bilgisi korunur. Aynı mor başkan kullanılır. Alt iki resif türü bilim uzmanıdır; dört ayrı kasabalı eklenir. NPC'ler bir kez kurulmuştur; başlangıç/reset aynı nesneleri kullanır. Üç kasabalı gecikmeli iki küçük grup hâlinde çarpışma denetimli rotadan yüzeye çıkar, biri kasabada kalır. Başkan ve uzmanlar toplantıda kalır. İlk bölümün takip ve inceleme etkileşimleri tekrar açılmaz.

Toplantı ve çıkış koordinatları Inspector'da düzenlenebilir; mevcut evler ve dünya taşınmadı. `homeArrival` House colliderının üstündeki güvenli noktadır. Kaynak sprite'lar değişmez; ölü yüzey balıklarında fazladan 180 derece dönüş kaldırıldı, kaynak poz üzerine -25/+25 derece salınım uygulanır.

## Değişen dosyalar

- `UnderwaterDayStory.cs`: ikinci bölüme giriş ve ortak E yönlendirmesi; kaynak ölü balık pozu düzeltmesi.
- `UnderwaterStoryUI.cs`: mevcut panellerde konuşmacı adı/portre desteği, gerçek Interact tuş etiketi, aynı Canvas'ta kısa kararma ve hedef temizleme.
- `UnderwaterSecondChapter.cs`: hazırlık, 21 kısa duyuru cümlesi, grupların ayrılması, görev ve tek seferlik bağlantı olayları.
- `MeetingFishIdle.cs`: toplantıdaki hafif bekleme hareketi.
- `StorySwimmer.cs`: durunca eski hareket yönünü temizleyerek yeniden yerleştirme sonrası dönüşü sıfırlar.
- `SecondChapterSetup.cs`: dünyayı koruyan idempotent Editor kurulumu ve doğrudan başlatma menüsü.
- `SecondChapterVerification.cs`: gerçek Input System E girdisi ve fiziksel yüzüşle Editor Play Mode kontrolleri.
- `UnderwaterWorld.unity`: ikinci bölüm bileşeni, iki uzman ve dört kasabalı; istenen ölü balık dönüş düzeltmesi.

Doğrulama sonuçları ve görseller `Tools/Revision9` altındadır. Çalıştırılan testler sonuç dosyalarında gösterilir; standalone build testi bunların kapsamında değildir.

Son çalıştırılan doğrulama: ikinci bölüm 85 PASS / 0 FAIL; birinci bölüm regresyonu 58 PASS / 0 FAIL. Dünya/sahne kapsam kontrolü geçti. Standalone build çalıştırılmadı.
