# Su altı genişletmesi

FishPilot mevcut Player/Sprint eyleminin oturum kopyasını kullanır. Shift hız çarpanı 1.5, geçiş yanıtı 8. Hasar rengi 0.2 saniye; gelen gülle yönünde 3 başlangıç hızı ve 20 sönüm kullanılır. Mevcut fizik/sınırlar korunur.

Top aralığı .455 saniye, ilk kesim .7, salvo arası .98: önceki değerlerden %30 kısa. Düşüş hızı ve güvenli alanlar değişmez.

WarEnvironment.previewStage: -1 hikâyeyi izler; 0 temiz, 1 ilk çatışma, 2 ilerleyen savaş, 3 gelecekteki son görünüm. Önizleme Play Mode Inspector'ındadır. İlk bölüm zamanla kirlenmez. Yalnız seçili arka planların SpriteRenderer renkleri değişir; paylaşılan materyal kullanılmaz. Çöp sınırı 24; aşamaya göre 0/8/16/24. Ağır parçalar 1.4 hızla gerçek statik zemine çöker, hasar vermez.

Dekoratif ölüm yalnız RoamingCreature/ReefFish türlerinde çalışır; en fazla üç ceset, .85 yükselme hızı, yüzey sınırı ve 80 saniye temizlik süresi. Hikâye NPC'leri hariçtir. İlk bölüm cesetleri ve küçük iskeletler surfaceY'ye bağlıdır.

Haberci ayrı bir dost köpekbalığıdır; ek konuşma sonrasında sağdaki açık denize gider, uygun uzaklıkta kaybolur. Lider ve iki dostun mevcut bölüm sonu korunur. Balina bölümü eklenmedi.

Dekoratif gemi X=-45, genişlik17, merkezY=surfaceY+.9. Kontrol, silah, can veya sahne geçişi yoktur. Kaynak PNG'ler korunmuş; waste_*, skeleton_* ve surface_vessel kopyaları Point filtrelidir.

Pixelify Sans yalnız su altı Canvas'ına atanır. Türkçe karakterler Unity Font.HasCharacter ile doğrulanır. Font ve OFL lisansı Assets/Underwater/Fonts altındadır; Google Fonts deposundan alınmıştır.

Yeni kod: WarEnvironment.cs, Revision12Setup.cs, Revision12Verification.cs. Genişletilenler: FishPilot, UnderwaterDayStory, UnderwaterThirdChapter, UnderwaterCannonball, UnderwaterStoryUI ve ilk bölüm yüzey sınırı testi. Sahne: UnderwaterWorld.

Doğrudan test: Underwater > Start third chapter in Play Mode. Ortam önizlemesi için WarEnvironment.previewStage kullanılır. Hedefli sonuçlar Tools/Revision12/results.txt, font görüntüsü pixel-font.png içindedir. Standalone build ve gerçek insanla zorluk değerlendirmesi yapılmadı.
