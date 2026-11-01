# İlk gün: ölü balıkların keşfi

İkinci bölüm eklendi. Eve giriş artık geliştirme modunda aynı sahnede duyuru bölümüne devam eder. Doğrudan test, bağlantı olayları ve değişen dosyalar için [SECOND_CHAPTER.md](SECOND_CHAPTER.md) dosyasına bakın.

Sahne: `Assets/Scenes/UnderwaterWorld.unity`. Gemi veya su üstü sahnesinde değişiklik yapılmadı.

Play’e basın. WASD/yön tuşlarıyla evlerin yanından biraz uzaklaşın. Pembe-turuncu hikâye balığı yavaşlayıp gözlerini kapatır, ters döner ve yüzeye yükselir. Olay sırasında yüzmeye devam edebilirsiniz. Balık geride bırakıldığında silinmez; ilgili aşamada hedef işareti bulunur.

Yüzeyde üç ayrı balığa yaklaşın ve mevcut **E / Player-Interact** girdisiyle inceleyin. Üçü herhangi bir sırayla tamamlanabilir; aynı hedef tekrar sayılmaz. Diyalogda ilk E metni tamamlar, sonraki E ilerletir. İnceleme ilerlemesi oturum boyunca korunur.

Üç incelemeden sonra kasabaya dönüp mor resif balığı olan başkanla konuşun. Daha erken konuşmak hikâyeyi ilerletmez. Başkan oyuncunun gerçekten geçtiği izleri takip eder; engeller CircleCast ile kontrol edilir ve gerektiğinde yerel 2D ara nokta rotası bulunur. Çok uzaklaşırsanız “Başkanı bekle” işareti çıkar. Görünür ışınlanma yapılmaz. Oyuncu tek başına yüzeye çıkınca son konuşma başlamaz; başkanın da ölü balıklara yaklaşması gerekir.

Son konuşma bilim ekibinin görevlendirileceği kararıyla biter. Sonrasında birinci bölüm tamamlanmış olarak kalır, takip sonlanır ve eve dönüş hedefi açılır. Yeni gün, sahne geçişi, su toplama, bilim araştırmasının oynatılması, savaş veya neden açıklaması eklenmedi.

Mor üst resif balığı başkandır. Aynı atlasın alt iki türü ilerideki bilim ekibi için ayrılmıştır; bu üç görünüm ortam spawner’ından çıkarıldı. Bilim balıkları birinci bölümde araştırma yapmaz; duyuru ikinci bölümde oynanır.

Hiyerarşide `Underwater day • dead fish discovery` objesi akışı taşır. `UnderwaterDayStory` durumları: Beginning, FishDeath, FollowToSurface, InspectBodies, ReportToMayor, EscortMayor, FinalConversation, Completed. Oturum içi ilerleme bu bileşendedir; yeni kapsamlı kayıt sistemi eklenmedi.

Geliştirici sıfırlaması: Play Mode sırasında aynı objenin `UnderwaterDayStory` bileşeninin bağlam menüsünden **Development / Reset this underwater story** seçin. Bu seçenek oyuncunun HUD’ında görünmez; olayları, incelemeleri ve oyuncuyu evdeki başlangıca sıfırlar. Play Mode’dan çıkıp yeniden girmek de yeni bir oturum başlatır.

Kodlar `Assets/Underwater/Story` altında. Diyalog arayüzü mevcut sol/sağ portreli su altı panellerini, aynı InputActionAsset’in yalnız Interact eylemine ait oturum kopyasını ve Türkçe destekleyen yerleşik fontu kullanır. Input asset’i ve proje input ayarları değiştirilmez. `FishPilot.ControlsLocked` yalnız bloklayan diyaloglarda hareketi frenler; NPC ölümü/yükselişi için zaman durdurulmaz. Kamera mevcut oyuncu takibini sürdürür.

Kıyı düzeltmesi: yüzeyin gerçek sınırında mevcut zemin kenarı bir kez kullanılır. İç dolgu artık kenar/ayna taş şeritlerini tekrarlamaz; aynı koyu taş paletinden düşük kontrastlı piksel malzemesi kullanır. SpriteRenderer, PPU, eğim, iskele ve collider yerleşimi korunur.

Kaynak resimler korunur. İşlenmiş hikâye sprite’ları `Resources/Abyss/story_*`, paneller `dialogue_left/right` altındadır. Yeniden üretmek için `python Tools/prepare_story_assets.py`; kıyı için `python Tools/prepare_sprite_shore.py`.

Editor kurulumu: `Underwater > Install first-day discovery story (preserve world)` yalnız su altı sahnesine ekleme yapar; aynı hikâye zaten varsa çoğaltmaz. Kurulum öncesi dünya yedeği, sprite ölçüleri, doğrulama sonuçları ve ekran görüntüleri `Tools/Revision6` klasöründedir.
## Değişen dosya grupları

- `UnderwaterDayStory.cs`: aşamalar, bir kez çalışan ölüm/yükselme, üç bağımsız inceleme ve bitiş durumu.
- `UnderwaterStoryUI.cs`: kaynak sol/sağ diyalog panelleri, portreler, Türkçe metin yazma efekti, görev ve aşamaya bağlı hedef/hint.
- `StorySwimmer.cs`, `WaterRoute.cs`, `MayorEscort.cs`: çarpışmalı NPC yüzüşü, yerel engel rotası ve oyuncunun gerçek izlerini takip.
- `FishPilot.cs`: yalnız diyalog süresince kullanılabilen varsayılanı kapalı hareket kilidi.
- `DayStorySetup.cs`: yalnız UnderwaterWorld için çoğaltmayan Editor kurulumu ve test başlatma.
- `DayStoryVerification.cs`: gerçek E girdisi ve fiziksel yüzüşle yapılan Editor Play Mode doğrulaması; bağımsız build'e dahil edilmez.
- `UnderwaterWorld.unity`, işlenmiş `story_*`/`dialogue_*` sprite’ları ve kıyı `shore_match_*` kopyaları.

Kıyıdaki yalnız bir oluşturulmuş parçada komşularından farklı olan Y ölçeği eşitlendi. Diğer mevcut dünya transform’ları ve collider tanımları korunur. Kaynak resimlerde düzenleme yapılmaz.
