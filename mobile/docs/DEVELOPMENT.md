# Geliştirme — Authentication ve Android bağlantısı

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
