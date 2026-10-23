# Phase 4 — Şifremi unuttum hata analizi ve düzeltme

23 Ekim 2026. Phase 5 başlatılmadı. Bu kayıt fiziksel Android kabulünün yerine geçmez.

## Kanıtlanan kök neden

Mobil `.env` içindeki LAN API kökü `http://192.168.1.155:5080` idi. Çalışan API süreci PID11796, `src/PayAhead.Api/bin/Debug/net8.0/PayAhead.Api.exe` konumundaydı; DLL son derlemesi ve süreç başlangıcı 18:19'du. Phase4 reset kodu bu süreçte yoktu. Canlı Swagger200 yanıtında forgot-password/reset-password/analytics path'leri bulunmuyordu. Bu nedenle anonymous forgot POST'u yeni AllowAnonymous controller action'ına ulaşamadı; mevcut fallback authorization **401 ProblemDetails / Kimlik doğrulama gerekli.** döndürdü. Kayıtlı, kayıtsız ve biçimi bozuk adresin üçü de 401 aldı. Eski API readiness200, o eski binary'nin bildiği migration'lar bakımından sağlıklıydı; yeni kaynakların çalıştığını kanıtlamıyordu.

İkinci engel: gerçek HTTP üzerinden oluşturulan disposable kullanıcının bulunduğu DB ile yapılandırmadaki SQL hedefi eşleştirildi. Hedef **PayAheadDockerDevelopment**, SQL erişimi başarılı, son uygulanmış migration ProductionOperations, tek pending migration `20261023190720_PasswordResetTokens` idi. Reset tablosu yoktu. Yalnızca backend restart yapmak bu kez SQL aşamasında hata oluşturacaktı.

Üçüncü eksik: dosya/User Secrets/default shell yapılandırmasında Email kapalı, SMTP host/from/credentials ve reset LinkBase tanımsızdı. Bu durum gözlenen eski API401'in nedeni değildi; API/migration düzeltildikten sonra gerçek teslimi engelleyen ayrı kurulum eksikliğiydi.

## Katman kontrolü ve düzeltme

- Mobil forgot formu email'i trim eder; Zod email/max254 ile doğrular. Payload `{email}` backend `ForgotPasswordRequest.Email` ile uyumludur. DTO veya parola alanı eksikliği yoktur. Geçersiz biçim normalde HTTP çağrısından önce reddedilir; doğrudan HTTP de400 döndürür.
- Axios mapper eski endpoint'in401'ini genel kimlik bilgisi hatası gibi gösteriyordu. Forgot/reset endpoint'lerine özel401/404 artık “Şifre yenileme hizmeti şu anda kullanılamıyor” mesajıdır. Reset domain400 için güvenli expired/used/invalid mesajı vardır; sunucunun hassas detail'i ekrana taşınmaz. Login ve protected endpoint davranışı değişmedi.
- `PasswordResetStore` hesap aramasından **önce** reset tablosunu kontrol eder. Eksik migration halinde kayıtlı/kayıtsız adreslerin ikisi de aynı güvenli503 alır, queue'ya iş girmez. Böylece hatalı deployment'ın kayıtlı hesap için500, kayıtsız için202 üretmesi önlenir. Schema/SQL ayrıntıları client'a verilmez. Yeni migration oluşturulmadı.
- Hedef DB, tek pending migration ve mevcut satır sayıları doğrulandıktan sonra yalnız additive reset migration uygulandı. User/PaymentPlan/PaymentOccurrence sayıları korunumu kontrol edildi; pending0. Hiçbir mevcut mali veri silinmedi, down/reset/drop uygulanmadı. Eksik migration regresyon testi yalnız izole Testcontainers DB'sini önceki şemaya indirir.
- Eski Development API kapatıldı. Güncel kaynaklar ayrı `artifacts/development` çıktısına derlenerek aynı5080 portunda başlatıldı. Yeni helper `scripts/Start-Development.ps1` her seferinde güncel kodu derler; otomatik migration yapmaz. Production'a dokunulmadı.
- Ayrı `payahead-reset-mailpit-dev` container'ı oluşturuldu; yalnız loopback1025 SMTP /8025 UI, relay yok. Çalışan API'ye `-EmailCapture` üzerinden process environment verildi; User Secrets veya gerçek sağlayıcı kimlik bilgileri değiştirilmedi. Dosya/default-shell tanısındaki Email=false, **çalışan API'nin process override'ını göstermez**; runtime SMTP başarısı canlı Mailpit capture ile kanıtlandı.

## Gerçek yerel HTTP sonucu

Rastgele disposable test hesabı kullanıldı; parola, reset token'ı, refresh/access token veya connection string yazdırılmadı.

| Aşama | Gerçek sonuç |
|---|---|
| Ready / Swagger |200 /200; üç Phase4 endpoint'i mevcut |
| Test register / login |201 /200 |
| Forgot kayıtlı / kayıtsız |202 /202; gövde birebir aynı |
| Direkt HTTP geçersiz email |400 ValidationProblemDetails |
| SQL token |1 kayıt; SHA256 hash64 karakter, süre15dk |
| Queue → SMTP → Mailpit |Gerçek mesaj yakalandı; mock değil |
| Expo link |Yapılandırılmış LAN8081/--/reset-password path'i e-postada mevcut; fiziksel açılış bekliyor |
| Reset / replay |204 /400 |
| Eski parola / eski refresh |401 /401 |
| Yeni parola login |200 |
| Temizlik |Yalnız disposable account DELETE204 ve ona ait Mailpit mesajı kaldırıldı |

Rate limiting, token süresi/tek kullanım, SMTP failure ve eksik migration için SQL entegrasyon testleri tekrar çalıştırılır. Güncel sonuçlar IMPLEMENTATION_PROGRESS içinde kayıtlıdır. SMTP hata senaryosunda aynı202 kabul yanıtı, güvenli hata kaydı ve token iptali korunur;202 teslim garantisi değildir.

Son doğrulama: mobil **94/94**, backend unit **384/384**, gerçek SQL integration **254/254**, skipped0; restore/build ve TS/lint temiz. Güncel gerçek Development API'de ayrıca **24 finans HTTP kontrolü +4 cleanup** geçti: ödeme CRUD, free limit403, cross-user404, stale PUT/DELETE409 ve yalnız disposable hesap temizliği. Kanıtlar ignored `artifacts/reset-fix-tests.log` ve `artifacts/reset-fix-results/*.trx` içindedir. Yeni backend kaynakları test edilmiştir; fiziksel cihazdaki Expo Go ekranı veya kişisel e-posta teslimi test edilmiş sayılmaz.

## Android'de yeniden deneme

1. Güncel Development API5080 ve Mailpit şu an çalışır durumdadır. Root readiness `http://192.168.1.155:5080/health/ready`200 olmalı. Swagger'da `/api/auth/forgot-password` ve `/api/auth/reset-password` bulunmalı.
2. Mobil klasöründe `npx.cmd expo start --port 8081` çalıştır, Expo Go'da projeyi yeniden yükle. Public API kökü mevcut LAN adresi olmalı. Metro kapalıyken forgot POST çalışabilir; fakat Expo Go reset linki projeyi yükleyemez.
3. “Şifremi unuttum” ekranında adresini gir. Kayıtlı/kayıtsız için aynı genel yanıt beklenir; hiçbirinde yanlış parola uyarısı olmamalı. Aynı hesapta yeni bağlantı için en az2dk,429 halinde en az1dk bekle.
4. **Kişisel inbox yerine** bilgisayardan `http://127.0.0.1:8025` Mailpit ekranını aç. Mesaj yakalanmışsa SMTP katmanı çalışmıştır. E-postadaki exp bağlantısını telefonda aç; masaüstünde tıklamak Expo Go'yu telefonda açmaz. Gerekirse bağlantıyı güvenli biçimde telefona aktar ve uygulamadaki “Bağlantım var” alanına yapıştır. Token'ı sohbet/log/screenshot üzerinden paylaşma.
5. Yeni parolayı belirle; eski parolayla girişin reddini, yeni parolayla girişin kabulünü ve aynı linkin tekrar reddini dene. Link15dk sonra da reddedilmeli. Kayıt/giriş/ödemeler regresyonunu kontrol et.
6. Bilgisayar IP/Metro portu değişirse API'yi doğru `-ResetLinkBase` ile yeniden başlat. Aynı portta iki API başlatma; eski development API terminalini kontrollü kapat.

Kalıcı yeniden başlatma komutu, repository kökünde:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Development.ps1 -EmailCapture -ResetLinkBase 'exp://192.168.1.155:8081/--/reset-password'
```

Bu komutta ExecutionPolicy yalnızca açılan PowerShell süreci içindir; makinenin genel politikası değiştirilmez. Mailpit'in tekrar kurulması gerekirse `docker compose -f docker/compose.email.yml up -d`; mevcut named capture container bu düzeltmede Docker Engine API üzerinden başlatıldı (Docker CLI PATH'te yoktu). Aynı1025/8025 portlarına ikinci container başlatma. Mevcut capture'ı durdurmak için Docker Desktop'ta yalnız `payahead-reset-mailpit-dev` seçilebilir; SQL container/volume silinmez.

Gerçek kişisel e-posta kutusuna teslim için [SMTP sağlayıcı ayarları](PASSWORD_RESET.md) gerekir. Bu durumda `-EmailCapture` kullanılmaz; gerçek host/TLS/from/credential/HTTPS üretim linki environment/User Secrets'ten sağlanır. Bu oturum kişisel inbox teslimi, fiziksel Android link açılışı veya production App Links doğrulaması yapmadı. Phase4 bu kabul işleri kapanmadan tümüyle tamamlanmış sayılmaz.
