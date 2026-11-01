# HudFirstTry — HUD ve bağımsız dash entegrasyonu

## Sonuç

HUD yalnızca `Assets/Scenes/HudFirstTry.unity` sahnesine kuruldu ve kaydedildi. Gönderilen PNG parçaları kullanıldı. Sol üstte seviye, can, enerji ve XP aynı anda; sağ üstte üç bağımsız dash göstergesi ve sabit tabanlı pusula bulunur. Başlangıç durumu gerçek veriden gelir: can 100/100, enerji 100/100, oyuncu seviyesi 1, XP 0/100 ve dash 3/3.

Son kullanıcı talimatı doğrultusunda `stardew-valley.otf.zip` içindeki font projeye eklenmedi. Yazılar geçici olarak TMP'nin yerleşik Liberation Sans fontunu kullanıyor. Yeni font seçimi daha sonra tek `HUD Font` referansıyla değiştirilebilir.

## İnceleme ve bulunan sorunlar

`ShipController`, `ShipUIManager`, `PlayerHealth`, `Loot`, `LootMagnet`, `Enemy`, `EnemySpawner`, `ShipWeapons`, `Turret`, `Bullet`, mevcut Input System asset'i, `MainUI.uxml/.uss` ve hedef sahne incelendi.

- Sprint callback'i lambda ile ekleniyor ve başka lambda ile çıkarılmaya çalışılıyordu. Kaldırma çalışmıyordu; yeniden etkinleştirme güvenli değildi.
- Dash tek cooldown kullanıyordu, aktif dash sırasında yeniden başlatılabiliyordu ve yön normalize edilmiyordu.
- Geçici çarpışma yok sayma işlemi önceki durumunu saklamıyordu. Hedef sahnede gemi Default katmanındaydı; yalnızca Player katmanına uygulanan bağışıklık gemiye ulaşmıyordu.
- `ShipUIManager` gerçek dash verisinden bağımsız 0–100 sayacı tutuyordu. Enerji/XP birbirini gizleyen toggle ile gösteriliyor, pusulanın tamamı döndürülüyordu.
- Sağlık için okunabilir veri, değişim bildirimi, iyileştirme ve üst sınır güncelleme API'si yoktu; negatif sağlık mümkündü.
- `LootMagnet` loot'u doğrudan taşıyor fakat `Loot.Magnetize` çağırmadığı için toplama/ödül akışını başlatmıyordu. `Loot.Collect` XP vermiyordu.
- Gerçek enerji ve oyuncu XP/seviye veri bileşenleri yoktu.

Düşman, spawner, silah, top, mermi, gemi sprite'ı, okyanus, su shader'ı ve sefer zorluğu kodları değiştirilmedi.

## Değiştirilen ve eklenen dosyalar

Değiştirilen mevcut scriptler:

- `Assets/Scripts/Ship/ShipController.cs`
- `Assets/Scripts/Ship/ShipUIManager.cs`
- `Assets/Scripts/Ship/PlayerHealth.cs`
- `Assets/Scripts/Ship/Loot.cs`
- `Assets/Scripts/Ship/LootMagnet.cs`

Yeni scriptler:

- `Assets/Scripts/Ship/PlayerEnergy.cs`
- `Assets/Scripts/Ship/PlayerExperience.cs`
- `Assets/UI/ShipHUD/ShipHUDController.cs`
- `Assets/Editor/ShipHUDSetup.cs`
- `Assets/Editor/ShipHUDPlayCheck.cs`

HUD prefab'ı: `Assets/UI/ShipHUD/ShipHUD.prefab`.

Orijinal PNG kopyaları: `Assets/UI/ShipHUD/Sources/`. Mavi dash ikonu referans görselden alpha silüetiyle ayrıldı: `Assets/UI/ShipHUD/Processed/DashWaveIcon.png`. Üstteki şeffaf boşluğu, cooldown'un ilk kısmını görünmez yapmaması için daraltıldı. Kaynaklar değiştirilmez. Point, Clamp, mipmap kapalı ve sıkıştırmasız ayarlar yalnızca HUD kopyalarına uygulanır. Sol birleşik boş çerçeve bir kez kullanılır; üç tekil çerçeve ayrıca üzerine çizilmez. Dolgular, metinler ve ibre ayrı nesnelerdir.

TextMeshPro'nun standart Essential Resources varlıkları, yazı/shader bağımlılıkları için `Assets/TextMesh Pro/` altında bulunur. Bu, kullanıcının iptal ettiği Stardew fontu değildir. Üçüncü taraf çalışma zamanı paketi eklenmedi.

## Dash veri sahipliği ve yenilenme

Gerçek hakların tek sahibi `ShipController`'dır. Her slot için kalan cooldown ve o kullanımın cooldown süresi ayrı dizilerde tutulur. Bir kullanım yalnızca ilk hazır slotu tüketir; diğer slotların zamanına dokunulmaz. Hazır hak yoksa veya dash hareketi hâlâ sürüyorsa yeni kullanım reddedilir.

- Başlangıç: 3 hazır hak.
- Sahne cooldown'u: slot başına **5 saniye**.
- Mevcut dash hızı: **24**, süre: **1 saniye**; normal maxSpeed **15**. Hız/süre değiştirilmedi.
- Her sayaca yalnızca scaled `Time.deltaTime` uygulanır. Pause sırasında durur.
- Kullanım yönü normalize edilir; mevcut Rigidbody2D sürtünmesi korunur.
- `SetDashCapacity()` mevcut slot sayaçlarını koruyarak kapasite değişimine izin verir. `ResetDashCharges()` açık bir reset API'sidir; HUD bu metodu çağırmaz.
- Input System named callback ile OnEnable/OnDisable arasında bağlanır/çıkarılır; generated controls OnDestroy'da dispose edilir.
- Hedef sahnedeki gemi mevcut Player katmanına alındı. Player–Enemy geçici yok sayma önceki durumuna geri döner; Default katmanındaki mermiler bundan etkilenmez. Diğer sahnelerin katman atamaları değiştirilmedi.

### Neden 5 saniye?

İlk 3.5 saniyelik kurulum test edildi. Ancak sahnedeki gerçek dashDuration 0.2 değil, 1 saniyedir. Üç hakla çok sık dash/bağışıklık oluşmaması için önerilen alternatiflerden 5 saniye seçildi. Kusursuz art arda kullanımda üç saniyelik aktif seri sonrası yaklaşık iki saniye ara oluşur.

| Slot cooldown'u | Üç 1 sn dash ile yaklaşık sürdürülebilir aktif oran |
| --- | --- |
| 2.5 sn | %100'e yaklaşabilir |
| 3 sn | %100'e yaklaşabilir |
| 3.5 sn | %86 |
| 4 sn | %75 |
| 5 sn | %60 |

Bunlar süre hesabıdır; uzun düşman kovalamalarıyla yapılmış kapsamlı bir denge testi iddiası değildir. 5 saniye Inspector'da değiştirilebilir bir başlangıç değeridir. Hedef dışı sahnelerde mevcut serialized cooldown değerleri zorla değiştirilmez.

## Yukarıdan aşağıya dash dolumu

Her slotta iki aynı boyutlu Image bulunur: sürekli görünen koyu ikon ve üzerinde parlak mavi ikon. Parlak Image `Filled / Vertical / Origin Top` kullanır. Sprite alpha'sı korunduğu için ikon dışına dikdörtgen dolgu taşmaz.

HUD her slot için doğrudan `ShipController.GetDashProgress(i)` okur. Bu değer, ilgili slotun `1 - remaining / duration` hesabıdır. HUD'da ayrı cooldown saati veya hak üretimi yoktur. Başka bir slot kullanıldığında mevcut dolumların sıfırlanacağı bir kod yolu bulunmaz.

## Sağlık, enerji, XP

- Sağlık: `PlayerHealth.CurrentHealth`, `MaxHealth`, `HealthChanged`, `Heal`, `SetMaxHealth`. Hasar/iyileşme ve maksimum sağlık değişimi event ile HUD'a aktarılır. Ölümde eski zaman durdurma ve controller kapatma davranışı korunur.
- Enerji: `PlayerEnergy`, 100 maksimum ve saniyede 5 yenilenme başlangıç ayarı. `TryConsumeEnergy`, `RestoreEnergy`, `SetMaxEnergy`, `EnergyChanged` sunar. Dash enerji tüketmez. Oyunda henüz enerjiyi harcayan başka özellik tanımlı değildir; yeni yetenek uydurulmadı.
- XP: `PlayerExperience`, başlangıç gereksinimi 100, seviye başına 1.35 büyüme (tam sayıya yuvarlama). Fazla XP taşınır, bir ödülde birden fazla seviye atlanabilir. `ExperienceChanged` ve `LevelUp` event'leri bulunur; otomatik yükseltme seçimi yoktur.
- Loot: `LootMagnet` non-alloc 2D sorgu ile `Loot.Magnetize` çağırır. Taşıma ve toplama `Loot` tarafından yürütülür. Ödül kilidi event'ten önce kapanır; aynı loot iki kez XP veremez.

Oyuncu XP seviyesi, `ShipWeapons.shipLevel` veya sefer/düşman zorluğu ile birleştirilmedi.

## Pusula ve sahne yerleşimi

Dünya +Y kuzeydir. Gemi sprite'ı ve verilen kırmızı ibre Z=0'da yukarı bakar. uGUI ile dünya dönüşünün pozitif yönü aynı olduğundan ibreye geminin Z açısı doğrudan uygulanır. Rastgele ±90 derece düzeltme yoktur. Taban ve N/E/S/W sabittir.

Sahnedeki `ShipHUDCanvas` prefab instance'ı Screen Space Overlay, sorting 200, pixelPerfect ve 1920×1080 CanvasScaler referansıyla çalışır. `ShipHUD` safe-area kökü altında `LeftHUD` ve `RightHUD` köşe anchor'larına bağlıdır. Solda seviye ve üç bar; sağda üç dash slotu ve pusula bulunur. Dekoratif Image ve metinler raycast engellemez. Bir EventSystem, yeni Input System UI modülüyle eklendi.

Mevcut `UIManager` nesnesi silinmedi; hedef sahnede `ShipUIManager` ve `UIDocument` kapatıldı. Diğer sahneler için eski ShipUIManager, sahte sayaç tutmayan ve gerçek veriyi okuyan bir uyumluluk adaptörü olarak korunur.

## Font kararı

Gönderilen fontun karakter tablosunda ÇçĞğİıÖöŞşÜü bulunmadı. ZIP lisansında değişiklik ve izinsiz ticari kullanım kısıtları vardı. Kullanıcı daha sonra fontun hiç eklenmemesini istedi; bu talimat uygulandı. ZIP ve orijinal dosyalar değiştirilmedi. Geçici TMP fontunun istenen Türkçe örnek metinlerini fallback atlasıyla desteklediği Play Mode'da kontrol edildi.

Yeni font geldiğinde Unity'de TMP Font Asset oluşturup sahnedeki `ShipHUDController > HUD Font` alanına atamak yeterlidir. Start/RefreshAll, bütün etiketlere bu fontu uygular; farklı font atlası için materyal referansı da yenilenir. Mevcut kurulumun çalışması için şu an kullanıcının tamamlaması gereken Inspector bağlantısı yoktur.

## Gerçekten çalıştırılan kontroller ve sınırlar

`Tools → Ship HUD → Run Play Mode checks` menüsündeki Editör entegrasyon kontrolü gerçek HudFirstTry sahnesinin test kopyasında çalışır. Bu bir NUnit/Test Runner paketi değil, gerçek Play Mode ve Input System üzerinden çalışan proje içi kontrol aracıdır. Test sırasında düşman üretimi/silahlar geçici kapatılır; sahneye bu test değişiklikleri kaydedilmez.

- Üç hazır hak, tek tüketim, bağımsız sayaçlar, üç farklı ilerleme, yenilenen slotu yeniden kullanma, hak yokken reddetme ve devam eden dash'i yeniden başlatmama.
- Pause ve devam; normalize diagonal hız (mevcut fizik damping'iyle); collision durumunu geri yükleme; mermi katmanını etkilememe.
- Controller kapatılıp açıldıktan sonra sanal klavyeden W+Shift girdisinin yalnızca bir hak tüketmesi.
- Sağlık hasarı, iyileştirme, üst sınır değişimi, ölüm; enerji tüketimi/yenilenmesi; gerçek loot mıknatısıyla XP toplama, çift ödül engeli, çoklu level-up ve kalan XP.
- Dört ana yönde pusula; eski HUD'ın kapalı olması; tek EventSystem; ortak değiştirilebilir font.
- Gerçek Game View ölçüleri: 1920×1080, 1280×720, 1280×1024, 2560×1080. Anchor sınırları ve görüntüler kontrol edildi. Boş/siyah görüntü kontrolü de eklendi. Direct3D Game View readback yönü PNG'ye doğru çevrilir.

İlk derleme denemelerinde yakalanan XP ifadesi ve test tuş adı çakışmaları düzeltildi. İlk siyah/ters ekran görüntüleri teslim görüntüsü olarak kullanılmadı; doğrudan Game View çizimiyle yeniden alındı. Son sonuç için `Logs/ShipHUD/play-check.txt`, görseller için aynı klasördeki PNG'ler esas alınır.

Uzun süreli düşman kovalamaları, fiziksel klavyeyle manuel oynanış ve ticari yayın font seçimi bu testlerin kapsamı dışındadır. Kullanıcının iptal ettiği font için lisans/karakter desteği tamamlanmış gibi rapor verilmez.

Son doğrulama: 5 saniyelik ayarla 43 Play Mode kontrolü geçti, Failures: 0. Son aktif Unity Console sayımı: 0 error, 0 warning; compilationFailed=false. Sahne yeniden açılarak sağlık, enerji, XP, üç ikon ve pusula referansları ile prefab bağlantısı doğrulandı. 11 kaynak PNG kopyası orijinalleriyle SHA-256 olarak eşleşiyor. Diğer sahne içerikleri, düşman/spawner/silah kodu, prefab'ları ve deniz dosyaları değişmedi. Test erişimi için geçici yüklenen Unity Pipeline paketi kaldırıldı; kurulum veya oyun bu pakete bağlı değildir.
