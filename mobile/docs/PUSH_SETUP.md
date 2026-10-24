# Android push kurulumu — Frontend Phase 6

24 Ekim 2026. Mobil kod ve yerel API/SQL testleri hazırdır; **gerçek Expo gönderimi, FCM teslimi, development APK ve fiziksel cihaz kabulü henüz doğrulanmadı**. Gerçek projectId/Firebase dosyası/sağlayıcı yetkileri olmadan bu zincir çalışıyormuş gibi gösterilmez. SMTP/Resend bu belgenin kapsamı değildir.

## Doğrulanan mevcut durum

| Alan | Durum |
|---|---|
| Expo / RN | 57.0.31 / 0.86.3; yükseltilmedi |
| Android application ID | `com.payahead.mobile` (mevcut proje kimliği, mağaza sahipliği kanıtı değildir) |
| Paketler | expo-notifications57.0.24, expo-device57.0.2, expo-dev-client57.0.20, expo-build-properties57.0.22 |
| EAS projectId | Yerel config/env'de yok; `push:preflight` başarısız |
| Firebase Android dosyası | GOOGLE_SERVICES_JSON yolu tanımsız; preflight başarısız |
| EAS hesabı / FCM v1 yetkisi | Giriş/yükleme yapılmadı; doğrulanmadı |
| Backend Expo access token | Kontrol edilen UserSecrets anahtarı ve süreç env kaynağında yok; değeri yazdırılmadı |
| Worker | Varsayılan Notifications:Enabled=false; HTTP smoke API'si de açıkça kapalı çalıştırıldı |
| Gerçek push token | Bu bilgisayarda alınmadı; fiziksel development build gerekli |

Android Expo Go SDK53 ve sonrasında remote push desteklemez; development build gerekir. Expo Go içinde uygulamanın reminder/settings CRUD akışını `npx.cmd expo start --go` ile kullanabilirsin; bu, remote push testi değildir. [SDK57 Notifications](https://docs.expo.dev/versions/v57.0.0/sdk/notifications/), [Expo push kurulumu](https://docs.expo.dev/push-notifications/push-notifications-setup/).

## 1. Gerçek EAS/Firebase bağlantısı

1. Kendi Expo hesabında mevcut EAS projesini seç veya oluştur. EAS CLI gerektiğinde `npx.cmd eas-cli@latest login` kullan; hesap parolasını/token'ını sohbete veya Git'e yazma. Projenin gerçek UUID'sini al. CLI bu teslimde hesaba bağlanmadı.
2. Firebase projesinde Android uygulamasının package adı **com.payahead.mobile** olmalı. O uygulamaya ait gerçek `google-services.json` dosyasını kaynak kontrolünün dışında sakla. Dosya adı/yolu örnektir; sahte Firebase JSON'u üretme.
3. FCM v1 için gereken Google servis hesabı yetkisini kendi hesabında ayarla ve EAS'te Android credentials bölümüne yükle: `npx.cmd eas-cli@latest credentials --platform android`. Servis hesabı anahtarını mobil uygulamaya gömme. [Resmî FCM v1 adımları](https://docs.expo.dev/push-notifications/fcm-credentials/).
4. Yerelde `mobile/.env` veya süreç env: `EXPO_PUBLIC_EAS_PROJECT_ID` gerçek UUID; `GOOGLE_SERVICES_JSON` gerçek dosyanın mutlak yolu. projectId public olabilir; servis hesabı/Expo access token **EXPO_PUBLIC_** ile tanımlanmaz. `.env` ve Firebase/service-account dosya adları gitignore kapsamındadır; özel isimli anahtar dosyalarını da repository dışında tut.
5. Cloud build için EAS development environment içinde projectId ve API LAN kökünü tanımla. `GOOGLE_SERVICES_JSON` için EAS file environment kullan; dosya build makinesinde ulaşılabilir olmalıdır. Yerel bilgisayarındaki mutlak yol cloud makinesinde geçerli değildir. Google services dosyasını Git'e ekleyerek çözme.

`mobile` içinde `npm.cmd run push:preflight` çalıştır. Script gerçek UUID, dosyanın okunabilirliği ve JSON'daki Android client package eşleşmesini denetler; dosya/token içeriğini yazdırmaz. EAS `eas-build-pre-install` hook'u da eksik bilgide build'i durdurur. Bu kontrol FCM v1 servis hesabının sunucuda yetkili olduğunu kanıtlamaz.

## 2. Development APK

`eas.json` yalnız `development` profili içerir: developmentClient, internal distribution, Android APK. `app.config.ts`, gerçek env'den projectId/googleServicesFile okur. Expo Notifications plugin ve `payahead-reminders` default channel hazırdır. Android kaynak klasörleri elle oluşturulmadı.

Gerçek kurulum ve giriş tamamlandığında:

```powershell
cd mobile
npm.cmd run push:preflight
npx.cmd eas-cli@latest build --profile development --platform android
# EAS'ten alınan APK'yi telefona kurduktan sonra:
npx.cmd expo start --dev-client
```

Bu teslimde **EAS build gönderilmedi, APK oluşturulmadı**. API bilgisayarda güncel Development helper ile LAN'dan dinlemeli; telefon ve bilgisayar aynı Wi-Fi üzerinde olmalı. Mobil EXPO_PUBLIC_API_BASE_URL bilgisayarın LAN kök adresi olmalı; `/api` eklenmez, telefonda localhost kullanılmaz. Firebase/projectId/plugin değişikliğinden sonra native build yenilenir; yalnız Metro reload yetmez.

Yerel HTTP API için usesCleartextTraffic sadece `EAS_BUILD_PROFILE=development` iken açılır. Expo introspection development manifest=true, production manifest=false doğruladı; APK ağ testi yapılmadı. Production için HTTPS gerekir; production deployment yapılmaz. [Expo build properties](https://docs.expo.dev/versions/v57.0.0/sdk/build-properties/).

## 3. Backend Expo yetkisi ve worker

Backend .NET8/EF8 ve mevcut SQL Server kullanılır; yeni migration yoktur. `/health/ready`200 ve Swagger reminder/device/settings endpoint'lerini doğrula. Expo projesinin enhanced push security access token'ını backend'e gizli süreç environment veya User Secrets ile ver. Üçüncü taraf örnek token veya başka projenin anahtarını kullanma. Kod etkin worker için HTTPS endpoint ve access token gerektirir.

| Backend environment | Anlam |
|---|---|
| Notifications__Enabled | Gerçek provider kurulunca `true`; varsayılan `false` |
| Notifications__AccessToken | Gerçek Expo push access token; yalnız backend secret |
| Notifications__PollSeconds | Varsayılan30, izinli5–300; normalde değiştirmek gerekmez |
| Notifications__SendEndpoint | Varsayılan resmî Expo send HTTPS adresi; değiştirmek gerekmez |
| Notifications__ReceiptEndpoint | Varsayılan resmî Expo receipt HTTPS adresi; değiştirmek gerekmez |

PowerShell'de token'ı komut geçmişine yazmadan süreç environment'a alabilirsin:

```powershell
$expoPushSecret = Read-Host 'Expo push access token' -AsSecureString
$expoPushPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($expoPushSecret)
try { $env:Notifications__AccessToken = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($expoPushPtr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($expoPushPtr) }
$env:Notifications__Enabled = 'true'
# Repository kökünde güncel Development başlatma helper'ını çalıştır:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Development.ps1
```

Gerçek token'ı ekrana dökme; shell kapanınca süreç env kaybolur. İş bitince `Remove-Item Env:Notifications__AccessToken` kullanabilirsin. Phase4 Mailpit akışı gerekiyorsa önceki helper'ın EmailCapture/ResetLinkBase seçeneklerini koru; profesyonel SMTP kurma. Worker disabled logu görüyorsan push bekleme. Uygulamanın bildirim ekranı worker'ın uzaktan etkinliğini okuyamaz; böyle bir status endpoint'i yoktur.

## 4. Telefonda izin ve kayıt

1. Gerçek development APK ile giriş yap; Bildirimler ekranını aç.
2. Açıklamayı okuyup **İzni kontrol et ve cihazı bağla** düğmesine bas. Android channel izin isteminden önce oluşturulur. Android13+ runtime izni sadece bu açık eylemle istenir; otomatik başlangıç istem göstermez.
3. Reddetmek uygulamayı engellemez. canAskAgain=false ise sistem ayarlarına yönel; tekrar tekrar istem gösterilmez.
4. Verilen izin + kullanıcıya bağlı opt-in ile gerçek Expo token alınır, POST /api/devices ile kaydedilir. Token response/ekran/loglarda gösterilmez. SecureStore yalnız cihaz binding userId/id ve opt-in owner bilgisini saklar; push token saklanmaz. Token değişimi native FCM token'ını doğrudan göndermek yerine Expo token'ını yeniden alır; aynı server id güncellenir.
5. Otomatik kayıt yalnız o hesabın opt-in tercihi varsa yapılır. Üstteki bağlantıyı kaldır düğmesi opt-in'i ve kendi server kaydını kaldırır; resume tekrar etkinleştirmez. Hesap cihaz listesinden aynı cihaz silinirse yerel binding de temizlenir.

## 5. Gerçek ödeme/trial teslim kabulü

Ödeme detayından hatırlatma yönetimine gir: 0/1/3/7 veya0–365 gün, HH:mm hesap saati, etkinlik. Free etkin reminder limiti1, toplam plan başına hard cap10; Premium limiti response'dan alınır. Trial kuralları aynı plan kotasını tüketebildiğinden sunucu403 son karardır. Pasif kural kaydı teslim değildir. Hesap notificationsEnabled, OS izni, aktif device, etkin kural, plan/occurrence uygunluğu ve worker gereklidir.

Test için yeni bekleyen bir plan ve gelecekteki yerel saat seç; o saati bekle. Paid/Skipped/pasif plan gönderim üretmemeli. Backend 24 saatlik telafi penceresini korur; geçmişe dönük sınırsız gönderim yapılmaz. Trial saat/günleri özel seçilemez: server09:00 hesap saatinde, Free1 gün önce; yeterli Premium kotasında1/3/7 ve0. Trial preference abonelik düzenlemede EnableTrialReminders ile değişir. İlk ücretli ödeme ile deneme bitişi ayrıdır; iptal/expiry/tercih kapatma sunucuda yeniden doğrulanır. Trial testi için beklenen09:00 saatini bekle; sahte “gönderildi” üretme.

| Zincir | Kanıt ve anlamı |
|---|---|
| Reminder + occurrence | SQL scheduling/dedup testleri ve gerçek API CRUD geçti |
| Delivery + claim | Gerçek SQL Server atomik lease testleri geçti |
| Expo HTTPS adapter | Kontrollü HTTP handler testleri geçti; bu teslim gerçek Expo servisini aramadı |
| TicketAccepted | Sağlayıcı kabulü; telefon teslimi değildir. AwaitingReceipt, 15dk sonra receipt |
| ReceiptAccepted / Sent | Backend mevcut Sent durumu sağlayıcı receipt kabulüdür; ekranda telefon göründü kanıtı değildir |
| Retry/backoff |30sn üstel, en çok3600sn; send en çok5; receipt15dk,24saat/96 deneme sınırı |
| Fiziksel bildirim | Telefonda gerçekten görüp dokunarak ilgili kayda gitmek ayrıca bekliyor |

Güvenli tanıda yalnız kendi test kullanıcının delivery **durumu, deneme sayısı ve zaman damgalarını** incele; DeviceTokens tablosunu veya provider payload/token/receipt ID değerlerini rapora dökme. Status enum:0Pending,1Processing,2Sent(provider receipt),3Failed,4AwaitingReceipt,5Cancelled. Worker health/log ve NotificationDeliveries birlikte kontrol edilir; mobil geçmiş endpoint'i varmış gibi davranılmaz.

## 6. Güvenli navigasyon ve çıkış sınırları

Push data: notificationId, kind(payment/subscription), resourceId; URL/route alınmaz, GUID'ler doğrulanır. Tap login gerekiyorsa önce login; ardından mevcut kullanıcının JWT'siyle ilgili GET üzerinden sahiplik doğrulanıp detay açılır.404 kaldırılmış veya bu hesaba ait olmayan kayıt için aynı nötr mesajdır. Offline lookup detay açmaz. Bildirim metni ad/tutar/e-posta içermez.

Logout önce yerel JWT/cache'i temizler, sonra yakalanmış eski kullanıcı credential'ıyla yalnız kendi device id'sini kaldırır; yeni kullanıcının bearer'ı kullanılmaz. Serial queue token rotasyonu ve yarım kalmış kaydı temizler. Refresh hatasında queue/cleanup deadlock regresyonu testlidir. Çevrimdışı/expired bearer cleanup başarısızsa binding saklanır ve yeni hesaba otomatik transfer engellenir; uyarı görünür. Eski hesapla tekrar giriş yapıp bağlan/kaldır veya Android sistem bildirimlerini kapat. Sağlayıcıya önceden kabul ettirilmiş mesaj geri alınamaz. POST sonucu ağda kaybolduysa sunucu kaydı oluşmuş olabilir; aynı hesapla cihaz listesini kontrol et. Çıkış/uygulama kaldırma koşullarında “kesinlikle artık push gelmez” garantisi verilmez.

## 7. Otomatik kontroller

`npm.cmd run typecheck`, `npm.cmd run lint`, `npm.cmd test -- --silent`, `npx.cmd expo-doctor`, `npx.cmd expo install --check`, Android Hermes export. Yerel API smoke için worker'ı açıkça kapat ve yalnız test sürecinde `$env:PAYAHEAD_NOTIFICATION_SMOKE_WORKER_DISABLED='true'`; `npm.cmd run test:notifications:live`. Bu smoke sentetik token'ı yalnız API kayıt/ownership testi için kullanır ve **gerçek Expo gönderimi değildir**. Script yalnız kendi geçici hesaplarını temizler. Gerçek FCM bilgileri olmadan push:preflight'in başarısız olması doğru ve beklenen sonuçtur.
