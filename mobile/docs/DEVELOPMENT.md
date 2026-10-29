> **Phase 5.3.2:** Native avatar FormData URI parçası Axios XHR ile gönderilir; Content-Type/boundary elle yazılmaz. Telefon API kökü ile backend Development AllowedHosts gerçek LAN IP’siyle eşleşmelidir. DHCP sonrası ikisi birlikte güncellenip API/Metro yeniden başlatılır. [Tanı](../../docs/PHASE5_3_2_DIAGNOSIS.md).

> **Phase 5.3.1:** `expo-image-picker` ve `expo-image-manipulator` SDK57 sürümleri kullanılır. Geniş galeri/kamera/mikrofon izni istenmez. Kare kırpma sonrası512 JPEG backend’e gönderilir; telefon dosya yolu kalıcı storage sayılmaz. [Backend/avatar](../../docs/AVATARS.md), [silme/dialog tanısı](../../docs/PHASE5_3_1_DIAGNOSIS.md). Gerçek loopback smoke: `EXPO_PUBLIC_API_BASE_URL=http://127.0.0.1:5094` ortamıyla `node scripts/phase531-smoke.cjs`; yalnız kendi test hesabını temizler.

## Phase 5.3 — Profil merkezi

Ayarlar → Hesap Bilgileri, `/account` route'unu açar. Güncel backend ve ProfileSecurity migration gerekir; bu makinenin Development migration'ı uygulanmıştır. Dil `/language`, bölge/bildirim `/notifications` üzerinden ortak API'leri kullanır. Parola değişince tüm oturumlar kapanır; yeniden login gerekir. Profil/deletion timeout'u başarılı yazı olarak kabul edilmez.

`npm.cmd run test:account:live` yalnız localhost API'sinde kendi rastgele test hesaplarıyla çalışır. Gerekirse süreç ortamında EXPO_PUBLIC_API_BASE_URL portunu5093 seçin, .env LAN adresini değiştirmeyin. Test kişisel hesabınıza bağlanmaz; token/parola yazdırmaz. [Sonuç](PHASE5_3_REPORT.md), [Android adımları](ANDROID_TESTING.md), [API/komutlar](../../docs/ACCOUNT_MANAGEMENT.md).

## Phase5.1.1 — Kategori ve gerçek logo denemesi

Backend'i güncel kodla yeniden başlatın: repository kökünde `./scripts/Start-Development.ps1 -Port 5080`. Bu makinede CatalogCoverage uygulanmıştır;startup migrate yoktur. Mevcut eski API sürecini önce kendi terminalinden Ctrl+C ile kapatın. Mobile `.env` LAN adresi aynı port/IP'yi göstermeli;telefonda localhost kullanmayın. Mobile dizininde `npx.cmd expo start --go --clear`, ardından yeni bundle ile giriş ve seçicide **Kataloğu yenile**. Her kategori kartının tamamı tıklanır; seçili yol görünür. Dijital altı başlığa ana sayfada kısayol vardır. Payment araç giderlerini gösterir,Subscription göstermez. Haber→Dijital Gazete çalışmalıdır.

5gerçek yerel marka görseli(Netflix/Spotify/Opet/Microsoft/Adobe);99fallback. Güncel Metro bundle olmadan eski0logo ekranını görmeye devam edebilirsiniz. Katalog bellekte5dakika stale/24saatgc;diskofflinegarantisi yoktur. Manuel yenile/pull-to-refresh aktif;logout cache'i temizler. `npm.cmd run test:catalog:live` yerel/LAN API'de37HTTP+4cleanup yapar;geçici hesaplar kullanır. [10fiziksel kabul adımı](ANDROID_TESTING.md), [kaynak/telif notları](../assets/brands/NOTICE.md), [rapor](PHASE5_1_1_REPORT.md). ExpoGo katalogCRUD için yeterlidir;gerçekpush ayrıPhase6kabulüdür.

## Phase5.1 — Kategori ve katalog kabulü

Backend'e CatalogFoundation migration'ı açıkça uygulanmış olmalıdır; otomatik migration yoktur. Development hedefi/pending kontrolü [backend rehberindedir](../../docs/DEVELOPMENT.md). Bu makinede hedef doğrulanarak uygulandı. Katalog endpoint'leri JWT ister. Ödeme/abonelik ekle tam ekran ortak seçiciyi açar; özel kayıt için ilgili kategori veya Diğer seçilir. Tutar/tarih kullanıcıya aittir. 0 marka logosu, mevcut kategori ikonları fallback'tir; gerçek logo beklemeyin.

```powershell
# mobile dizininde; .env mevcut LAN adresini içermeli, credential içermez
npm.cmd run typecheck
npm.cmd run lint
npm.cmd test
npx.cmd expo-doctor
npx.cmd expo install --check
npx.cmd expo export --platform android --output-dir artifacts/phase51-android
npm.cmd run test:catalog:live
npx.cmd expo start --go --clear
```

Canlı smoke local/LAN hedefleri kabul eder; iki geçici kendi hesabını oluşturup cleanup yapar; gerçek kullanıcı verisiyle çalışmaz. API'yi önce Development'ta başlatın. Fiziksel cihazda localhost bilgisayar değildir; mevcut yerel IP, aynı Wi-Fi ve güvenlik duvarı erişimini doğrulayın. Bu işlem production veya push kurulumu yapmaz. Phase5.1 CRUD Expo Go'da denenebilir; gerçek Phase6 push için ayrı development APK/kabul gerekir.

Cache24saat yalnız bellektedir; ilk açılış offline ise katalog gelmez, tekrar dene gerekir. Önceden alınmış veri aynı oturumda kullanılabilir; logout temizler. Katalog hatası eski finansal kayıtları gizlemez. [20 adım](ANDROID_TESTING.md), [rapor](PHASE5_1_REPORT.md), [logo envanteri](../../docs/CATALOG_LOGOS.md). Önceki Phase6/Phase4 açık kabulleri sürer.

# Geliştirme — Authentication ve Android bağlantısı

## Frontend Phase 6

[PUSH_SETUP](PUSH_SETUP.md) EAS development build, Firebase/FCM v1, backend Expo access token, worker, gerçek teslim ve çıkış sınırları için esas belgedir. `npm.cmd run push:preflight` gerçek config yoksa exit1 verir; credential olmadan APK/push başarı raporu yok. `npx.cmd expo start --go` UI/CRUD, `--dev-client` gerçek APK içindir. Mevcut Android package com.payahead.mobile; native plugin/FCM değişimi yeni build gerektirir. Yerel HTTP izni yalnız development profilinde açılır; production introspection false doğrulandı. SMTP/Resend bu fazda yapılmaz.

Gerçek API smoke için test backend'ini Notifications__Enabled=false ile başlat; yalnız o test sürecinde PAYAHEAD_NOTIFICATION_SMOKE_WORKER_DISABLED=true ve gerekirse EXPO_PUBLIC_API_BASE_URL=loopback kullan. `npm.cmd run test:notifications:live` kendi geçici hesapları ve sentetik cihaz token'larıyla API ownership/CRUD doğrular; Expo'ya göndermez. Gerçek cihaz token'ı/deployment testi yerine geçmez. .NET restore/build/test için üç komutta aynı `--artifacts-path artifacts/phase6-check`; Docker gerçek SQL gereklidir.

## Frontend Phase 5

Gerçek abonelik listesi, form/detay/iptal/kaldırma ve denemeler için [sözleşme ve semantik](SUBSCRIPTIONS.md), [sonuç raporu](PHASE5_REPORT.md), [Android kabul listesi](ANDROID_TESTING.md). Yeni dependency/backend/migration yoktur. Normal mobil başlangıç ve API LAN ayarı aşağıdakiyle aynıdır. `npm.cmd run test:subscriptions:live` yalnız geçici hesaplarla yerel gerçek API'yi test eder; mevcut kişisel veritabanını temizlemez. Hedef API hazır değilse önce mevcut Development kurulumunu başlat, SMTP'yi bu faz için değiştirme. Mobil API kökünü süreç içinde loopback'e çevirmek için `$env:EXPO_PUBLIC_API_BASE_URL='http://127.0.0.1:5080'`; fiziksel telefonda LAN adresini kullan.

Kontroller: `npm.cmd run typecheck`, `npm.cmd run lint`, `npm.cmd test -- --silent`, `npx.cmd expo-doctor`, `npx.cmd expo install --check`, `npx.cmd expo export --platform android --output-dir artifacts/phase5-android`. Root AGENTS gereği .NET restore/build/test de yapıldı; çalışan API dosyalarını kilitlememek için üç komutta da aynı `--artifacts-path artifacts/phase5-check` kullanıldı. İlk restore yalnız varsayılan obj'yi oluşturur; ayrı artifacts ile no-restore build için o dizinde restore da gerekir. SQL entegrasyon Testcontainers ve gerçek SQL Server2022 kullanır; Docker erişimi olmadan geçmiş sonuç başarı olarak taşınmaz.

E-posta202 sonrası inbox'a gelmiyorsa mevcut kurulum Mailpit capture modundadır. [Teslim zinciri ve gerçek SMTP/Gmail/User Secrets/Outlook kurulum rehberi](../../docs/EMAIL_DELIVERY.md). Mobil public env içine SMTP secret eklenmez; gerçek SMTP için backend `-EmailCapture` olmadan başlatılır.

Şifremi unuttum401 düzeltmesi ve güncel başlatma/Android adımları: [hata analizi](../../docs/PASSWORD_RESET_DIAGNOSIS.md). Güncel helper `scripts/Start-Development.ps1`; yerel Mailpit kullanırken `-EmailCapture -ResetLinkBase exp://IP:8081/--/reset-password` gereklidir. SMTP capture kişisel inbox değildir. Sadece readiness200 eski backend sürümünü elemez; Swagger path'lerini de kontrol et.

## Frontend Phase 4 kurulumu

Yeni ekranlar güncel backend gerektirir. [Reset/SMTP/Mailpit/migration/deep link rehberi](../../docs/PASSWORD_RESET.md) ve [analytics API](../../docs/ANALYTICS.md) esas alınmalıdır. `npx.cmd expo install` ile kurulan datetimepicker **9.1.0**, Expo Go SDK 57 içindedir; mevcut SVG **15.15.4** ile grafik çizilir. Sürüm yükseltilmedi. [Expo tarih seçici](https://docs.expo.dev/versions/v57.0.0/sdk/date-time-picker/) ve [SVG](https://docs.expo.dev/versions/v57.0.0/sdk/svg/) uyumluluğu kontrol edildi. Web için DateOnly metin fallback vardır; native Android modalı jest mock testi gerçek cihaz kabulü sayılmaz.

Takvim aylık veri; geçmiş aylık ve bugüne kadar, 20 kayıt/sayfa ve tek durum filtresi kullanır. Analiz son altı ayı sunucuda toplar. Grafikler para string'ini yalnızca boyut oranı için Number'a çevirir; gösterilen para lexical decimal olarak korunur. Yeni finans sorguları aynı kullanıcı/epoch cache altında; logout temizliği devam eder. Development e-posta linki Expo Go `exp://IP:PORT/--/reset-password` adresini kullanır; production domain/App Links burada kurulmadı. Link açılmazsa uygulamadaki kod/bağlantı alanı kullanılabilir.

Önceki fazdaki “backend değişmedi / migration yok” kayıtları kendi tarihsel kapsamını anlatır; Phase 4 yeni additive migration gerektirir. Çalışan kullanıcı API'si bu görevde durdurulmadı. Normal build DLL kilidine takılırsa kullanıcı kendi development API terminalini kontrollü kapatıp yeni kodla başlatabilir; production süreci için bu adımı otomatik uygulama.

## Mobil uygulama

Node 24.22.0/npm 11.19.1 ile doğrulandı; Expo 57.0.31, RN 0.86.3, React 19.2.3 sürümleri korundu. Yeni projede `npm.cmd ci`; mevcut workspace'te kurulum hazır. Windows execution policy değiştirilmez; `.cmd` kullanılır.

```powershell
cd mobile
npx.cmd expo start
```

`EXPO_PUBLIC_API_BASE_URL` **API kök adresi** olmalı, `/api` ekleme. Telefonun localhost adresi bilgisayarı göstermez. `EXPO_PUBLIC_DESIGN_PREVIEW=false` normal auth akışıdır. `.env.example` yalnızca örnek LAN adresi içerir. Public env uygulamaya gömülür; JWT signing key, SQL şifresi, refresh token veya parola koyma. `.env` Git tarafından izlenmez.

## Fiziksel Android → bilgisayar backend'i

1. Telefonu ve bilgisayarı aynı güvenilir Wi-Fi ağına bağla. Guest Wi-Fi/client isolation ve VPN erişimi engelleyebilir.
2. Bilgisayar IPv4 adresini `ipconfig` ile bul. Bu çalışmada etkin Wi-Fi IPv4 `192.168.1.155`; DHCP değiştirirse `.env` de değişmelidir.
3. Backend'in mevcut SQL/connection string/migration kurulumunu root README ve docs/DEVELOPMENT.md üzerinden hazır tut. Bu mobil görev SQL verilerini değiştirmez, migration çalıştırmaz. 23 Ekim doğrulamasında yapılandırılmış backend ready endpoint HTTP 200 döndürdü; ayrı bir SQL/Docker kurulumu yapılmadı.
4. Repository **kökündeki ayrı terminalde**, mevcut SQL ortam ayarlarını koruyarak Kestrel'i LAN'dan dinlet:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:DevelopmentIdentity__Enabled='false'
$env:DOTNET_ROOT=(Resolve-Path .tools/dotnet).Path
$env:DOTNET_CLI_HOME="$PWD/.tools/cli"
& ./.tools/dotnet/dotnet.exe run --project src/PayAhead.Api --no-launch-profile --urls 'http://0.0.0.0:5080'
```

Sistem .NET 8 SDK kuruluysa `dotnet run ...` da kullanılabilir. SQL bağlantısı ayrıca mevcut güvenli ortamdan sağlanmalıdır; şifreyi mobil `.env` dosyasına yazma. Yalnızca `localhost:5080` binding telefonu kabul etmez. AllowedHosts sınırlandırılmışsa kullanılan LAN IP'sini development allowlist'e dahil et; production ayarlarını gevşetme.

5. Windows firewall inbound TCP 5080'i yalnızca **Private** profil ve yerel alt ağ için kabul etmeli. Gerekirse yönetici terminalinde dar kural örneği (bu oturumda uygulanmadı):

```powershell
New-NetFirewallRule -DisplayName 'PayAhead Dev API 5080' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5080 -Profile Private -RemoteAddress LocalSubnet
```

Firewall'u kapatma; router port forwarding/public exposure yapma. Test bitince artık gerekmeyen development kuralını kaldır. LAN'daki diğer cihazların da bu API'ye erişebileceğini hesaba kat.

6. Telefon tarayıcısında `http://192.168.1.155:5080/health/live` aç. Bu yalnızca erişim/liveness kontrolüdür; SQL için `/health/ready` ve auth akışı ayrıca doğrulanmalı.
7. `mobile/.env` içine `EXPO_PUBLIC_API_BASE_URL=http://192.168.1.155:5080` yaz. Metro'yu yeniden başlat, Expo Go'da yeniden yükle. SDK 57 uyumlu Expo Go ile QR okut. Android Studio gerekmez.
8. Gerçek kayıt oluştur, ardından login yap. Kayıt otomatik oturum açmaz. Ayarlar'daki ad/email gerçek `/me` yanıtından gelmeli.

## HTTPS ve güvenlik sınırı

Production transport HTTP'yi kod seviyesinde reddeder. HTTPS sertifika doğrulamasını atlayan kod yoktur. Development HTTP yalnızca güvenilir LAN ve test hesabı içindir: parola ve bearer/refresh token ağ üzerinde şifrelenmez. Gerçek hesapla veya açık Wi-Fi üzerinde kullanma.

ASP.NET development HTTPS sertifikası telefonda otomatik güvenilir değildir; localhost adına düzenlenmiş sertifika LAN IP'siyle de eşleşmeyebilir. Güvenilir CA zinciri ve kullanılan host/IP'yi kapsayan sertifika gerekir. `TrustAll`, `rejectUnauthorized=false` veya sertifika kontrolünü kapatma ekleme. Güvenilir geliştirme TLS setup'ı yoksa yerel test hesabıyla development HTTP kullanılabilir; production HTTPS şartı değişmez.

Native Expo Go istekleri browser CORS kuralına tabi değildir. Web preview için backend exact HTTPS CORS allowlist'i ayrıca gerekir; bu görevde backend CORS gevşetilmedi. `npx.cmd expo start --tunnel` yalnızca Metro bağlantısını taşır, yerel API'yi internete otomatik açmaz. Tunnel bu oturumda başlatılmadı.

## Kontroller

```powershell
npm.cmd run typecheck
npm.cmd run lint
npm.cmd test
npx.cmd expo-doctor
npx.cmd expo install --check
npx.cmd expo config --type public
npx.cmd expo export --platform android
node --env-file-if-exists=.env scripts/auth-smoke.cjs
```

Smoke script yalnızca localhost/private IPv4 backend kabul eder; geçici `mobile-smoke-*` hesabı oluşturup register/login/me/rotation/logout ve iptal edilmiş refresh'i doğrular. Yalnızca bu hesabı temizler. Backend erişilemiyorsa nonzero exit verir. Token/şifre/yanıt gövdesi çıktısı yoktur. Cleanup doğrulanamazsa bunu ayrıca bildirir; script üretim veritabanına karşı çalıştırılmamalıdır.

Jest testleri mock adapter/storage kullanır; gerçek backend veya SecureStore donanımı kanıtı değildir. Native cihaz matrisi ANDROID_TESTING.md içindedir. Web oturumu desteklenmez; native SecureStore yerine güvensiz saklama eklenmez.

## Troubleshooting

| Sorun | Çözüm/kontrol |
|---|---|
| Network error | Backend çalışıyor mu, Kestrel 0.0.0.0 binding, doğru LAN IP/port, Private firewall, aynı Wi-Fi? |
| Timeout | İstek 15 saniyede sonlanır. Refresh timeout'unda eski token kör tekrar edilmez; yeniden login gerekir. |
| 401 | Yanlış/kilitli/pasif hesap aynı genel hata döner; refresh gerekiyorsa merkezi servis tek kez dener. |
| 429 | Backend auth bütçesi IP başına 20/dakika; biraz bekle. Beş yanlış parola 15 dakika hesap kilidi oluşturabilir. |
| Restore offline | Oturum doğrulanamadığından local session kapanır; bağlantı geldiğinde tekrar login yap. |
| Logout offline | Local kayıt/cache temizlenir, server iptali doğrulanamadığı açıkça gösterilir. |
| SecureStore hatası | Authentication fail-closed kalır. Native depolama sorunu çözülmeden oturum açılmış sayılmaz. |
| Port dolu | Mevcut servisi durdurmadan Metro için `--port 8082` gibi boş port seç. |
| Metro/cache | Ctrl+C, ardından `npx.cmd expo start --clear`. Env değişince yeniden yükle. |
| npm audit | Major/downgrade önerilerini kör uygulama; ilerleme raporundaki direct/transitive ayrımını incele. |

## Frontend Phase 3 doğrulama ve kullanım

Normal akışta preview kapalı kalır. Gerçek login sonrası Dashboard ve Ödemeler backend verisini gösterir. DateOnly alanları YYYY-AA-GG, tutar alanı virgül/nokta ondalık ile girilir; binlik ayraç kullanılmaz. Pull-to-refresh ve edit/create sonrası otomatik cache yenilemesi vardır. Backend'de doğrudan yapılan değişiklikleri refresh ile al.

`npm.cmd run test:finance:live` yalnızca LAN/localhost geliştirme API'sinde rastgele geçici hesap/planlarla CRUD, 403 limit, 409 concurrency, 404 izolasyon ve decimal roundtrip kontrolü yapar. Kendi test hesaplarını temizler; mevcut kullanıcıların verisini kullanmaz. 23 Ekim'de bu script ve auth smoke gerçek backend üzerinde geçti. Bunlar fiziksel Android testi değildir.

Backend dev sunucusu çalışırken standart .NET build dosya kilidi alabilir. Servisi durdurmadan doğrulama için repository kökünde `.tools/dotnet/dotnet.exe build PayAhead.sln --artifacts-path artifacts/phase3-backend-check` kullanılabilir. Backend testinin ops SQL dosyası için sabit göreli yolu olduğundan artifact kökü `mobile/artifacts` altında olmamalıdır. Bu, backend kaynak değişikliği gerektirmez. Ayrıntılı komut sonuçları IMPLEMENTATION_PROGRESS.md içindedir.

Güncel güvenlik denetimi 7 kayıt (4 high/3 moderate) bildiriyor; önceki 58 kaydın artık aynı olmaması bu fazda düzeltilmiş açık sayısı değildir. node-forge ve Router query decoder zincirleri için SDK uyumlu upstream düzeltme beklenir; major/downgrade veya force uygulanmadı.
## Phase 5.2 — 14 dil geliştirme

Ayarlar → Uygulama Dili seçimi anında metin/JS yönünü günceller; form taslakları yeniden mount edilmez. Dil tercihi token deposundan ayrıdır, logout sonrası korunur. Offline seçim bekleyen hesap eşitlemesi olarak gösterilir; uygulamayı öne getirmek yeniden dener. Arapça native kontrolleri yeniden yükleme uyarısı gösterebilir; önce taslağı kaydedin. Android sistem tarih/saat penceresi cihaz dilini kullanır. [Ayrıntılı rehber](../../docs/LOCALIZATION.md).

```powershell
npm.cmd run i18n:audit
npm.cmd run typecheck
npm.cmd run lint
npm.cmd test -- --silent
npx.cmd expo-doctor
npx.cmd expo install --check
npx.cmd expo export --platform android --output-dir artifacts/phase52-export
# Ayrı terminalde güncel Development API çalışırken, .env LAN adresini değiştirmeden:
$env:EXPO_PUBLIC_API_BASE_URL='http://127.0.0.1:5088'
npm.cmd run test:localization:live
Remove-Item Env:EXPO_PUBLIC_API_BASE_URL
```

Live yerelleştirme testi yalnız localhost kabul eder ve kendi rastgele hesaplarını kullanır. Telefonda `127.0.0.1` yerine bilgisayarın LAN adresi gerekir; mevcut `mobile/.env` adresi korunmuştur. Expo Go'yu kapat-açıp güncel Metro bundle'ına bağlanın. Hermes export bir APK, fiziksel cihaz testi veya production yayın değildir.
