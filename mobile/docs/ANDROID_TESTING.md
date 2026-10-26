# Android kabulü — Phase5.1.1

25 Ekim2026. **Aşağıdaki fiziksel10senaryonun tamamı kullanıcı kabulünü bekler.** Otomatik116kategori yol erişimi ve backend gerçekHTTP sonuçları cihaz görünürlüğü kanıtı değildir. Önce güncel backend'i yeniden başlat, Development migration pending0olduğunu doğrula;Metro `npx.cmd expo start --go --clear`,doğruLANadresiveaynıWiFi. Seçicide Kataloğu yenile. Gerçek token/kişisel veri screenshot paylaşma. Önceki Phase5/5.1/6matrisleri aşağıda tarihsel olarak korunur.

| # | Fiziksel akış | Beklenen | Durum |
| --- | --- | --- | --- |
|1| Ödeme Ekle→Araç ve Ulaşım→Yakıt→Shell | Kartın ikon/boş alanına dokunmak da açar;Shell doğrudanYakıt'ta görünür;formtekseferlik,para/tarihmanuel | Bekliyor |
|2| Ödeme Ekle→Araç ve Ulaşım→Bakım ve Tamir | Bosch Car Service/Otopratik/Özel Tamirci seçilir;tekPaymentPlan | Bekliyor |
|3| Ödeme Ekle→Araç ve Ulaşım→Motorlu Taşıtlar Vergisi→MTV | Tutar/yasal taksit/tarihuydurulmaz;seçim formu açar | Bekliyor |
|4| Ödeme Ekle→Araç ve Ulaşım→Araç Muayenesi→TÜVTÜRK | Doğrukategori;mevcutplanaltyapısı;Egzozda daüyelik | Bekliyor |
|5| Abonelik Ekle→Haber ve Yayıncılık→Dijital Gazete | NYT/WSJ/FT/Bloomberg;gerçekfaturatutar/sıklığınıkullanıcıseçer;basılıgazete/spor boşhazırlistedeözelabonelikaçılır | Bekliyor |
|6| Abonelik Ekle→Film ve Dizi→Netflix | Doğrudankısayol veyaDijitalaltındanerişim;gerçekNetflixwordmark,formdolumu,teklinkedplan | Bekliyor |
|7| Abonelik Ekle→Müzik ve Podcast→Spotify | GerçekSpotifygörseli,doğrukategori,amount19,4korunur;araçkategorisiyok | Bekliyor |
|8| Herhangi bir alt kategori→Bu kategoride özel ödeme/abonelik | Manuelözelisim,doğrukategori,ortakkataloğayazımyok;heranabaşlığıdenetimtablosunagöregez | Bekliyor |
|9| Netflix/Spotify/Opet/Microsoft365/AdobeCreativeCloud ve fallback |5gerçekyerellogo,diğerlerindeikon;light/dark,uzunisim/büyükyazı/darekran,scroll,aramanet/spot/arac/dogalgaz,tamyol,Androidgeri | Bekliyor |
|10| Eski ödeme/abonelik ve Dashboard/takvim/geçmiş | Eskiad/kategori/tutar/rowversionkorunur;çiftsayım/ikinciyazımyok;logout/başkahesaptaözelverisızmaz | Bekliyor |

[Her ana kategori→altkategori→hizmet denetimi](../../docs/CATALOG_AUDIT.md), [sonuç raporu](PHASE5_1_1_REPORT.md). Cihaz/model/OS/tarih ve başarısız adımı ayrıca kaydet. Phase6gerçekpush ve Phase4inbox açıkları bu kabulden bağımsızdır. Sonraki faz başlatılmadı.

# Android kabul matrisi — Phase5.1

25 Ekim 2026. **Bu20adım fiziksel cihazda bu oturumda çalıştırılmadı; tamamı bekliyor.** Expo Go ile katalog CRUD denenebilir. Gerçek push kabulü aşağıdaki Phase6 bölümünde ayrı açık kalır. Önceki Phase5 kullanıcı başarı bildirimi bu yeni arayüzün kabulü değildir.

Ön koşullar: aynı Wi-Fi/LAN API, güncel Development backend, uygulanmış CatalogFoundation, aktif test hesabı, test amaçlı bütçe verileri. Free plan/abonelik sınırlarını aşmamak için yalnız kendi test kayıtlarını temizle veya yetkili Premium test hesabı kullan. Credential/token ekran görüntüsü paylaşma. Yeni kayıtta fiyat ve tarihi kendin gir; otomatik fiyat/sağlayıcı teslimi yoktur. Gerçek marka logosu0; kategori ikonu beklenir.

| # | Senaryo ve beklenen sonuç | Durum |
| --- | --- | --- |
| 1 | Yeni ödeme aç: tam ekran kategori/hizmet seçici, popüler hizmetler, güvenli alan ve okunabilir başlık | Bekliyor |
| 2 | Dijital→Film/Dizi→Netflix: doğru kategori/ad ve aylık öneri; tutar/tarih uydurulmaz | Bekliyor |
| 3 | Netflix tutarını12,3456 girip kaydet: listede/detayda dört basamak korunur, tek istek/tek plan | Bekliyor |
| 4 | `net` ve `spot` araması: debounce sonrası ilgili sonuç; hızlı yazmada eski sonuç seçime karışmaz | Bekliyor |
| 5 | `dogalgaz`, `arac`, `ARAÇ`, `turk` araması: Türkçe/diakritik eşleşme, boş sonuç açıklaması | Bekliyor |
| 6 | Araç→Yakıt→Benzin→Shell: ad/kategori doğru, tek seferlik öneri; sıklık değiştirilebilir | Bekliyor |
| 7 | Motorin ve LPG'de de Shell/uygun hizmetler görünür; seçilen alt kategori korunur | Bekliyor |
| 8 | Araç 23 gider türü: bakım/tamir/lastik/sigorta/MTV/HGS/otopark vb gezin; liste scroll ve geri düzgün | Bekliyor |
| 9 | MTV seç: fiyat/yasal taksit/tarih hesaplanmaz; kullanıcı tarihi ve tutarı belirler | Bekliyor |
| 10 | Seçilen kategoride özel ödeme: manuel isim yaz, kaydet; başka hesap kataloğuna eklenmez | Bekliyor |
| 11 | Diğer→özel kayıt; seçici kapat/aç ve Android geri: taslak tutar/tarih korunur, önce alt gezinme geri gider | Bekliyor |
| 12 | Yeni abonelik→Netflix: ad/sağlayıcı ve sıklık dolar; tutar/deneme/fatura tarihi manuel, tek bağlı plan | Bekliyor |
| 13 | Abonelik seçicisinde araç/yakıt hizmetleri yok; özel abonelik manuel adla kaydedilir | Bekliyor |
| 14 | Apple TV/bp yeni seçimde yok; iCloud+/HBO Max/Google AI Pro ve eski ad araması beklenen davranışta | Bekliyor |
| 15 | Ödeme/abonelik listesi, detay, Dashboard ve takvimde kategori kimliği/fallback tutarlı; toplamlar çift sayılmaz | Bekliyor |
| 16 | Eski GUID'siz kayıt adı/kategorisi aynen görünür; düzenleyip kaydetmek zorla katalog eşleştirme yapmaz | Bekliyor |
| 17 | Önce katalog yükle, interneti kes: aynı oturum cache'i/finansal eski kayıtlar korunur. İlk soğuk offline açılış hata/tekrar dene gösterir | Bekliyor |
| 18 | API erişimini geri getir, tekrar dene: kategori/hizmet gelir; logout/başka hesapta özel ad/taslak sızmaz | Bekliyor |
| 19 | Açık/koyu tema, büyük yazı, dar ekran, uzun isim, ekran okuyucu, klavye/scroll kontrolü; gerçek logo0 olduğu anlaşılır, bozuk resim fallback unit testi fiziksel logo kanıtı sayılmaz | Bekliyor |
| 20 | Free limit/rowversion çakışması/çift dokunma ve silme: açık hata, ikinci plan/tutar yok; eski ödeme/geçmiş ve mevcut bildirim navigasyonu bozulmaz | Bekliyor |

[19 maddeli rapor](PHASE5_1_REPORT.md), [merkezi sözleşme](../../docs/CATALOG.md), [logo/lisans envanteri](../../docs/CATALOG_LOGOS.md). Kabul sonuçlarını gerçek cihaz/model/OS/tarih ile ayrıca kaydet. Aşağıdaki Phase6 ve eski matrisler korunmuştur.

# Android kabul matrisi — Frontend Phase 6

24 Ekim2026: kullanıcı Phase5 fiziksel testlerini başarılı bildirdi; bu bildirim Phase5 kabul kaydıdır. Aşağıdaki eski Phase5 matrisi tarihsel olarak korunur; bu oturumda yeniden fiziksel ölçülmedi. **Phase6 gerçek development APK ve fiziksel push kabulü henüz yapılmadı.** Phase4 açık maddeleri devam eder.

Ön koşullar: [PUSH_SETUP](PUSH_SETUP.md) gerçek EAS projectId, com.payahead.mobile Firebase dosyası, FCM v1 credentials, backend Expo access token ve enabled worker; development APK, güncel LAN API, aynı Wi-Fi. Expo Go ile yalnız CRUD test edilebilir. Finansal/sentetik test verilerini gerçek teslim kanıtı sayma; token/secret screenshot/log paylaşma.

| Senaryo | Kabul ölçütü | Durum |
|---|---|---|
| 1. İzin isteme | Açılışta otomatik istem yok; kullanıcı açıklama sonrası düğmeye basınca channel+Android13 runtime izin | Bekliyor |
| 2. Grant/deny | Deny uygulamayı engellemez; canAskAgain=false sistem ayarlarına gider, tekrar istemez; geri dönünce güncel durum | Bekliyor |
| 3. Cihaz kaydı | Gerçek Expo token alınır; API kaydı doğrulanır, token ekranda/logda yok; izin tek başına teslim sayılmaz | Bekliyor |
| 4. Reminder create | Ödeme detayında0/1/3/7/özel0–365, native24saat; HH:mm hesap saatinde; geçerli sunucu kaydı | Bekliyor |
| 5. Reminder edit | Gün/saat/etkinlik düzenlenir; duplicate409, Free1/Premium limit403; timeout sonrası reload | Bekliyor |
| 6. Reminder delete | Onay gerekir; liste/cache yenilenir; henüz gönderilmemiş işler iptal, çift dokunma tek işlem | Bekliyor |
| 7. Gerçek ödeme push | Yeni pending plan için gelecekteki yerel saat; worker→ticket→receipt ve telefonda görünen bildirim ayrı kaydedilir | Bekliyor |
| 8. Ödeme tap | App açık/arka plan/kapalı; sahiplik GET doğrulanır, doğru ödeme açılır; foreign/deleted/invalid payload açılmaz | Bekliyor |
| 9. Gerçek trial push | Mevcut server09:00/Free1/Premium quota kuralları; deneme bitişi ve ilk ücretli ayrı; iptal/expiry/tercih off göndermez | Bekliyor |
| 10. Çıkış | Online kendi device kaydı kalkar; offline uyarı ve binding korunur; başka hesaba transfer yok; önceden accepted mesaj geri alınamaz | Bekliyor |
| 11. Rotation/login | Native token değişince Expo token yeniden alınır, aynı cihaz id güncellenir; anonymous tap login sonrası yeni hesabın sahipliğiyle açılır | Bekliyor |
| 12. Native/regresyon | Timezone/DST açıklaması; picker iptal/geri, küçük ekran/klavye/uzun ad, tema/TalkBack, logout/cache, Phase1–5 akışları | Bekliyor |

Gerçek ödeme kabulünde provider ticket ID/secret yerine test zamanı ve durum sonucu kaydet. Receipt provider kabulü olsa bile telefonda görünmediğinde başarılı teslim yazma. Trial saatini mobilde değiştirmiş gibi davranma; gereken server saatini bekle. Backend24saat catch-up politikası korunur; sınırsız geçmiş bildirim üretilmez.

## Önceki Frontend Phase 5 matrisi

24 Ekim 2026. Aşağıdaki yeni akışlar **fiziksel Android/Expo Go üzerinde henüz doğrulanmadı**. Otomatik test, Hermes ve HTTP smoke fiziksel kabul yerine geçmez. Phase 4'ün açık matrisi aşağıda korunur; gerçek SMTP/Resend yayın öncesine ertelenmiştir.

Önce SQL Server'ın sağlıklı, güncel backend `/health/ready` yanıtının 200 ve Swagger subscription path'lerinin mevcut olduğunu kontrol et. Backend kapalıysa repository kökünde `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Development.ps1` çalıştır. Phase 4 Mailpit reset akışını da kullanacaksan [mevcut capture başlatma komutunu](../../docs/PASSWORD_RESET_DIAGNOSIS.md) ve Metro portuna uygun ResetLinkBase'i kullan. `mobile/.env` içindeki API adresi güncel LAN IPv4:5080 olsun; telefonda localhost kullanma. `mobile` içinde `npx.cmd expo start --clear`, Expo Go'da QR tarama ve gerçek test hesabıyla giriş.

| Senaryo | Kabul ölçütü | Durum |
|---|---|---|
| 1. Liste | Gerçek kayıtlar; loading/empty/error-retry; pull-refresh; 20 satır pagination; arama, status, currency filtreleri | Bekliyor |
| 2. Oluşturma | Ad/sağlayıcı/tutar/döviz; 4 sıklık ve aralık; başarı sonrası tek detay; ödeme listesinde yalnız bir bağlı plan | Bekliyor |
| 3. Native picker | Başlangıç, ilk ücretli, deneme, erişim tarihleri; mevcut seçim; minimum; Android geri/iptal tarihi değiştirmesin | Bekliyor |
| 4. Trial | Bugün başlayıp 1/3/7 gün sonra biten kayıtlar; ilk ücretli tarih bitişten sonra; son gün 0; negatif gün yok | Bekliyor |
| 5. Tarih sınırı | Ay sonu/29 Şubat; cihaz/hesap saat dilimi farklı; hesap tarihine göre durum ve nextBillingDate | Bekliyor |
| 6. Düzenleme | Tutar/sağlayıcı/sıklık/deneme/yenileme tercihleri; kayıt sonrası doğru geri yığını; eski rowVersion 409 ve reload | Bekliyor |
| 7. İptal | Dış sağlayıcı uyarısı ve explicit tarih/onay; çift dokunma tek istek; seçilen son gün dahil erişim devam, sonrasında sona erdi | Bekliyor |
| 8. Kaldırma | Ayrı destructive onay; kaydı listeden gizle; mevcut ödeme geçmişi kalsın; dış abonelik iptal olmuş iddiası olmasın | Bekliyor |
| 9. Finans | TRY/USD ayrı tahmin; “kesin harcama değildir”; Dashboard/takvim/analizde plan tutarı tek kez; iptal yenilemeyi kaldırsın | Bekliyor |
| 10. Limit | Free3 takip ve Free5 plan kotası ayrı; anlamlı uyarı; Premium response limiti; satın alma taklidi yok | Bekliyor |
| 11. Oturum/ağ | Uçak modu, timeout, 409; kör tekrar engeli; logout/login başka hesapta eski abonelik görünmesin | Bekliyor |
| 12. Görsel/native | Küçük ekran, uzun ad/sağlayıcı, %200 yazı, klavye/scroll, light/dark/system, TalkBack ve Android geri | Bekliyor |

Ücretli harici hizmet satın almak/iptal etmek gerekmez; bu akış yalnız takip uygulamasını test eder. Sahte finans verisi production'a eklenmez. Fiziksel sonuç ve cihaz/Expo Go sürümü kullanıcı kabulünde kaydedilmelidir.

## Önceki Frontend Phase 4 matrisi

Phase 4 aşağıdaki kontrolleri fiziksel Android/Expo Go üzerinde bekliyor. Kullanıcı Phase 1–3 kayıt/giriş/CRUD başarısını bildirdi; bu bildirim yeni Phase 4 native akışlarının kanıtı değildir. Ön koşul: güncel backend, additive reset migration, çalışan Metro ve [SMTP/deep link kurulumu](../../docs/PASSWORD_RESET.md).

| Senaryo | Kabul ölçütü | Durum |
|---|---|---|
| 1. Takvim günleri | Ay ileri/geri, bugün, ödemeli gün noktaları; geciken/bekleyen/ödenen/atlanan metinleri | Bekliyor |
| 2. Tek ödeme | Doğru plan, DueDate, güncel plan tutarı, actualAmount ve PaidAtUtc ayrı | Bekliyor |
| 3. Ödendi | Gerçek tutar gir; tek gönderim; sunucu onayı sonrası takvim/Dashboard/analiz yenilensin | Bekliyor |
| 4. Atlandı/bekliyor | Atlandı paid toplamını artırmasın; bekliyor kaydı kaldırıp plan tekrarını göstersin; eski sürüm 409 | Bekliyor |
| 5. Geçmiş | Ay/durum değişimi, pagination; gelecek vade listelenmesin; empty/error retry | Bekliyor |
| 6. Başlangıç seçici | Mevcut tarih seçili; Android geri/iptal değiştirmesin; 31 Ocak ve 29 Şubat doğru | Bekliyor |
| 7. Bitiş seçici | Başlangıç alt sınırı; boşaltma; end-before-start hatası; düzenlemede mevcut tarih | Bekliyor |
| 8. Analiz | Dashboard bağlantısı; ayrı ekran; currency seçimi, 6 ay barları, durum donut, kategori ve top 5 | Bekliyor |
| 9. Tutar karşılaştırma | Aynı ay/currency paid Dashboard ile eşleşsin; yaklaşan ile tüm pending farkı doğru anlaşılsın | Bekliyor |
| 10. Reset isteği | Kayıtlı/kayıtsız aynı yanıt; Mailpit gerçek capture veya gerçek SMTP provider | Bekliyor |
| 11. Linkten reset | Expo Go açık/kapalı; LAN ve Metro portu; link fallback yapıştır; expired/replayed kod reddi | Bekliyor |
| 12. Yeni giriş | Eski parola reddi; yeni parola kabulü; eski refresh iptali; logout sonrası eski finans cache'i yok | Bekliyor |
| Görsel/erişilebilirlik | Light/dark/system, büyük font, küçük ekran, TalkBack, offline/loading ve klavye/geri davranışı | Bekliyor |

Takvim yedi sütun olduğundan dar ekranda her gün 48 dp genişliğinde olamaz; günler metinsel erişilebilir etiket ve 58 dp yükseklik taşır. Native okunabilirlik/taşma kabulü yapılmalıdır. Jest native picker/SVG testleri mock'tur; Hermes bundle, dokunmatik veya gerçek cihaz performansını doğrulamaz. Yerel SMTP capture dış inbox teslimi değildir. Production verified App Links/domain olmadan production deep link hazır denmez.

## Önceki Phase 3 kaydı

Kullanıcı Phase 1 tasarımı ve Phase 2 gerçek kayıt/giriş akışını fiziksel Android/Expo Go'da başarıyla denediğini bildirdi. Bu çalışma sırasında Phase 3 için bağlı telefon/emülatör veya kullanılabilir browser/app kontrol yüzeyi bulunmadı; aşağıdaki native kontroller **çalıştırılmadı**. RNTL input/switch/Alert testleri gerçek dokunmatik/klavye veya geri tuşu testi değildir.

| Kontrol | Beklenen / cihaz durumu |
|---|---|
| Tam kapat/aç, restore | Gerçek oturum geri gelir, restore sırasında yanlış ekran görünmez. Bekliyor; mock restore geçti. |
| Logout / başka kullanıcı | Eski Dashboard/list/detail görünmez; query verileri temizlenir, geri tuşuyla açılamaz. Bekliyor; cache/epoch testleri geçti. |
| İlk kullanıcı Dashboard | Örnek tutar yok; kaliteli boş görünüm ve ödeme ekleme CTA. Bekliyor; RNTL geçti. |
| Gerçek Dashboard | Aylık tutarlar para birimi bazında ayrı; timezone/context, bu ay geciken, en yakın ve yaklaşan kayıtlar doğru. Bekliyor; gerçek HTTP smoke geçti. |
| Pull-to-refresh | Ana Sayfa ve Ödemeler aşağı çekilince yenilenir; spinner ve hata/retry düzgün. Bekliyor. |
| Liste / filtre / sayfa | Arama, aktif/pasif, ISO para birimi ve kategori sunucuda uygulanır; toplam/sayfa bilgisi görünür. Bekliyor; API pagination smoke geçti. |
| Create | Tarih YYYY-AA-GG, virgüllü ondalık, kategori ve enum seçenekleri; çift basma tek mutation. Bekliyor; form/API testleri geçti. |
| Ay sonu / 29 Şubat | Başlangıç ilk tarihtir; sonraki tarihler ay sonuna kırpılır, sonraki Mart/artık yılda geri döner. Bekliyor; otomatik tarih testleri geçti. |
| Edit / 409 | Mevcut değerler yüklenir; eski sürüm yeni veriyi ezmez; açık reload değişiklikleri sıfırlar. Bekliyor; mock ve HTTP 409 geçti. |
| Silme / pasifleştirme | Onay diyaloğu iptal edilebilir; güçlü ETag kullanılır; geçmiş/ilişki varsa backend reddi anlaşılır. Bekliyor; Alert ve canlı CRUD testleri geçti. |
| Free limit | Yanıttaki activePaymentPlans limiti ve premium bilgi bağlantısı; sahte satın alma yok. Bekliyor; canlı 403 entitlement_limit geçti. |
| Network/timeout | Belirsiz write sonucu kullanıcıya açıklanır, otomatik çift create olmaz. Bekliyor; otomatik test geçti. |
| Klavye ve safe area | Uzun formun tüm alan/CTA'sı erişilebilir; decimal-pad ve Android resize; gesture ve 3-button nav çakışmaz. Bekliyor. |
| Küçük ekran/uzun isim/büyük tutar | 320/360 dp, uzun 150 karakter isim ve (19,4) tutarda kesilme/taşma yok. Bekliyor; tutar lexical testleri geçti. |
| Light/dark/system | Mevcut Inter/navy/teal sistemi ve semantik kontrast korunur. Bekliyor. |
| Geri / deep link | Edit/detail normal geri; logout sonrası eski korunan route açılamaz. Bekliyor. |

Çalıştırma: `cd mobile`, `npx.cmd expo start`, SDK 57 uyumlu Expo Go'da QR okut. Aynı güvenilir Wi-Fi, doğru LAN API adresi ve backend erişimi gerekir. [DEVELOPMENT.md](DEVELOPMENT.md) bağlantı adımlarını, [PAYMENTS_API.md](PAYMENTS_API.md) tarih/precision/sürüm kurallarını açıklar.

Cihaz sonucu kaydedilirken model, Android/Expo Go sürümü, ekran/font ölçeği, tema ve her satırın gözlenen sonucu yazılmalı. Token/parola/gerçek finansal veri ekran görüntüsüne veya loga konmamalı. Otomatik testler ve canlı Node HTTP smoke fiziksel Expo Go auth/CRUD kanıtı olarak sunulmaz.

---

## Önceki fazın tarihsel kontrol listesi

# Android kabul matrisi — Frontend Phase 2

Kullanıcı Phase 1 premium tasarımını fiziksel Android/Expo Go üzerinden beğendiğini bildirdi. Bu oturumda Phase 2 için fiziksel telefon/emülatör erişimi yoktur; aşağıdaki native kabul adımları **bekliyor**. Jest navigation-policy testleri Android donanım geri tuşunun gerçek cihaz testi değildir. Hermes export bir APK veya cihaz testi sayılmaz.

## Hazırlık

SDK 57 uyumlu [Expo Go](https://expo.dev/go), aynı Wi-Fi, çalışan SQL/backend ve bilgisayarın LAN IP'si gerekir. [Bağlantı kılavuzundaki](DEVELOPMENT.md) Kestrel/firewall/TLS adımlarını izle. Proje hazır; `cd mobile`, `npx.cmd expo start`, ardından QR okut. İlk hedef Android Studio gerektirmeyen fiziksel Expo Go'dur.

## Auth senaryoları — gerçek cihazda çalıştırılmadı

| Adım | Beklenen |
|---|---|
| İlk açılış | Restore tamamlanana kadar yükleme; sonra login. Demo giriş yok. |
| Geçersiz form | Email/boş parola, register 12 karakter/ad kuralları; field error okunabilir. |
| Register | 201 sonrası login ve kayıt başarı mesajı; otomatik oturum yok. |
| Login | Loading/disabled CTA, çift tıklamada tek giriş; yanlış kimlik genel mesaj. |
| Başarılı login | SecureStore yazımı ve /me tamamlanınca Dashboard; Ayarlar'da gerçek ad/email. |
| Uygulamayı kapat/aç | Güvenli kayıt okunur; gerekiyorsa rotation, /me doğrulaması; yanlış ekrana ani geçiş yok. |
| Token süresi | Access dolunca tek refresh; paralel istekler eski token'ı tekrar consume etmiyor. |
| Network/timeout | Kullanıcıya anlaşılır hata; token/parola console'a basılmıyor. |
| Arka plan/ön plan | /me tekrar doğrulanır; pasif/silinmiş hesap anonymous olur. |
| Logout | Local token ve query verileri temizlenir; login açılır. |
| Logout offline | Local çıkış tamam; server iptali belirsiz notice'u görünür. |
| Android geri | Login sonrası auth'a dönmez; detail/modal normal geri; logout sonrası eski protected ekran açılamaz. |
| Deep link | Oturumsuz tabs/detail/modal açılamaz; restore bitmeden protected render yok. |
| Klavye/küçük ekran | 320/360 dp'de Register son input/CTA kaydırılabilir; şifre göster/gizle çalışır. |
| Safe area | Gesture ve 3-button navigation içerik/buton kapatmaz. |
| Light/dark/font | Inter/Türkçe karakter, yüksek font ölçeği, badge ve hata kontrastı korunur. |
| Reduced motion/TalkBack | Hareket tercihi ve label/state/alert'ler doğru. |

Production HTTP reddi otomatik testte doğrulandı; gerçek cihaz production build testi yapılmadı. Web SecureStore oturumu desteklenmez. Örnek finans preview flag'i açılsa bile auth guard atlanamaz; varsayılan kapalıdır.

Sonuç kaydı: cihaz modeli, Android ve Expo Go sürümü, boyut/font ölçeği, API erişim yolu, test satırı ve gözlenen sonucu yaz. JWT/parola/refresh body ekran görüntüsü veya loga koyma. Bu faz push cihazı kaydetmez ve gerçek finans verisini bağlamaz; bunları auth başarısı olarak test etme.
## Phase 5.2 — Fiziksel Android kabulü (bekliyor)

Otomatik Jest/SQL/HTTP testleri fiziksel Expo Go veya development build kabulü değildir. Her adımı light/dark ve büyük sistem yazı boyutuyla deneyin; gerçek kişisel veriler yerine kendi test hesabınızı kullanın.

1. **Türkçe → English:** Ayarlar → Uygulama Dili. Sekmeler, dashboard, auth/reset, hata ve boş ekran metinleri hemen değişmeli.
2. **English → Deutsch:** Açık ödeme taslağı varsa alanlar korunmalı; tutar ve currency aynı kalmalı.
3. **Deutsch → Français:** Takvim, geçmiş, analiz, hatırlatıcı ve bildirim tercihleri Fransızca olmalı.
4. **Français → Español:** Aranabilir dil listesini ve erişilebilir seçili radio durumunu kontrol edin.
5. **Español → العربية:** JS metin/hizalama hemen RTL olmalı; gereken native yeniden yükleme uyarısı görünmeli, otomatik reload olmamalı.
6. **Arapça RTL:** Geri/ileri okları, ayar satırları, form/klavye ve tam ekran katalog modalını inceleyin. Taslağı kaydettikten sonra uyarıdaki yeniden başlatmayı deneyin. Expo Go ile development build davranışlarını ayrı kaydedin.
7. **Japonca ve Çince:** Japonca, 简体中文, ayrıca 한국어 ve हिन्दी seçerek karakter/font/uzun metin taşmasını kontrol edin. Italiano, Português (Brasil), Nederlands ve Русский için de aynı ekran turunu tamamlayın.
8. **Kapat-aç:** 14 dilin her birinde uygulamayı tam kapatıp yeniden açın; manuel dil korunmalı. İlk kurulumda desteklenen cihaz dili, desteklenmeyen dilde İngilizce beklenir.
9. **Logout/login:** 14 dilin her birinde çıkış ve giriş yapın; manuel seçim korunmalı. Farklı hesapta para birimi/saat dilimi değişmemeli; eski hesaptan gelen geç yanıt seçimi bozmamalı.
10. **Kategoriler:** Kök ve alt kategorileri değişik dillerde açın; toplam 116 kategori mevcut olmalı. Almanca `Kraftstoff` ve genel elektrik faturası adının yerel çevirisiyle arama yapın; Türkçe/İngilizce arama da çalışmalı.
11. **Markalar:** Netflix/Spotify/Shell adları aynı kalmalı; genel fatura/kira/tamirci şablonları çevrilmeli. Kendi ödeme ve sağlayıcı isimleriniz değişmemeli.
12. **Para:** `250 TRY`, `12.3456 USD` ve büyük tutarları gözlemleyin; dil seçimi döviz dönüştürmemeli. Nokta/virgül ondalık, Arapça/Hindi rakamları denenebilir; karma binlik ayraçları reddedilmeli.
13. **Tarih:** Aynı DateOnly günü Türkçe/İngilizce/Almanca dahil tüm dillerde değişen biçimde gösterilmeli, gün kaymamalı. Android native tarih/saat penceresinin işletim sistemi dilinde olması bilinen sınırdır.
14. **CRUD ve offline:** Her dilde ödeme/abonelik oluştur-güncelle-sorgula-sil akışını, generic validation/401/409/429 hatalarını ve kaydedilmemiş taslağı deneyin. Uçak modunda dil seçip uygulamayı açın; seçim korunmalı, bağlantı sonrası uygulamayı öne getirince hesap eşitlemesi tamamlanmalı. Gerçek push teslimini bu fazın sonucu saymayın.

Sonuç: **Bu 14 fiziksel senaryo bu çalışma sırasında yapılmadı; kullanıcı kabulü açık.** AI taslak çeviriler ayrıca hedef dil uzmanı incelemesi bekler. SMTP/Resend veya production deployment yapılmadı.
