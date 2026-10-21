# Geliştirme — Authentication ve Android bağlantısı

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
3. Backend'in mevcut SQL/connection string/migration kurulumunu root README ve docs/DEVELOPMENT.md üzerinden hazır tut. Bu mobil görev SQL verilerini değiştirmez, migration çalıştırmaz. Bu oturumda Docker komutu bulunamadı; çalışan API de tespit edilmedi.
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
