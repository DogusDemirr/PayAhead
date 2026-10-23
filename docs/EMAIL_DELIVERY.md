# Şifre sıfırlama e-postası teslim zinciri

23 Ekim 2026, çalışan Development API üzerinde tekrar doğrulandı. **Şu an dış e-posta sağlayıcısı kullanılmıyor; mesajlar Mailpit'te yakalanıyor.** Bu nedenle kişisel inbox/spam klasöründe görünmemesi beklenen sonuçtur. Uygulamanın202 yanıtı teslim onayı değildir.

## Mevcut durum — yedi sorunun yanıtı

| Soru | Doğrulanan sonuç |
|---|---|
| SMTP host/port | `127.0.0.1:1025` |
| Gönderim etkin mi? | Çalışan API process ortamında `Email__Enabled=true` |
| Background worker | Çalışıyor; yeni HTTP isteğinden sonra gerçek SMTP mesajı Mailpit'te yakalandı |
| Yerel capture mı? | Evet. `payahead-reset-mailpit-dev`, UI `http://127.0.0.1:8025`; relay/forwarding yapılandırılmamış, custom mount yok |
| Dış gönderimde eksik | Gerçek SMTP hesabı/host/587 STARTTLS, yetkili gönderici, kullanıcı ve uygulama parolası veya sağlayıcının desteklediği kimlik doğrulama |
| Kod gerekli mi? | Mevcut hatanın nedeni capture yapılandırması. Kullanıcı/parola veya uygulama parolası kabul eden STARTTLS sağlayıcısına geçiş yapılandırmadır. Outlook/OAuth2 için mevcut transport'a ek OAuth desteği gerekir; yalnız parola ayarı yeterli sayılmaz |
| Gerçek inbox test edildi mi? | Hayır. Bu oturumda gerçek sağlayıcı kimlik bilgisi yok; dış gönderim/teslim yapılmadı |

Mevcut capture bağlantısında `Email__EnableSsl=false`, kullanıcı/parola yoktur. Bu yalnızca loopback geliştirme aracına uygundur. TLS veya SMTP AUTH başarısızlığı gözlenmedi; bu iki protokol yerel capture modunda kullanılmıyor. Dış sağlayıcının bağlantı/TLS/auth davranışı henüz test edilmiş değildir. Gerçek gönderimde `Email__From=payahead@example.test` kullanılamaz; kendi yetkili adresinle değiştirilmelidir.

## HTTP → SQL → kuyruk → SMTP kanıtı

Rastgele yeni disposable hesapla gerçek LAN API'sine istek atıldı. Kayıtlı ve kayıtsız forgot istekleri **202**, yanıtları aynı; geçersiz biçim400. SQL'de yalnız test kullanıcısına ait1 reset kaydı,64 karakter SHA256 hash,15dk ömür ve henüz tüketilmemiş durum doğrulandı. Yeni mesajın Mailpit'te görünmesi hem enqueue hem worker tüketimi hem de SMTP kabulünü doğrular. API202 tek başına bu adımların tamamlandığını kanıtlamaz; aynı hesapta2dk cooldown da vardır.

Mailpit container'ı Docker Engine üzerinden salt okunur incelendi: çalışıyor; SMTP1025 ve UI8025 yalnız127.0.0.1'e bağlı; dış relay/forwarding environment ayarı ve custom config mount yok. Yalnız test hesabı ve ona ait yakalanan mesaj temizlendi. Diğer mesajlar/hesaplar incelenmedi veya silinmedi. Parola, reset/access/refresh token ve connection string yazdırılmadı.

## Yapılandırma nereden geliyor?

`SmtpPasswordResetEmail`, DI'dan `IConfiguration` okur. `PayAheadApi.Build`, `WebApplication.CreateBuilder` kullanır. Öncelik: command line → process environment → Development User Secrets → appsettings.Development.json → appsettings.json. [ASP.NET Core yapılandırma sırası](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0).

Çalışan API `scripts/Start-Development.ps1 -EmailCapture` ile başlatıldı. Bu switch **API'nin üst PowerShell process environment'ına**127.0.0.1/1025/Enabled=true/TLS=false/From ve Expo LinkBase yazar; child API bunları devralır. Başka terminalde yalnız dosyalar/UserSecrets okunursa Email=false görünmesi çalışan process'i yansıtmaz. User Secrets'e gerçek sağlayıcı yazıp yeniden `-EmailCapture` ile başlatmak da capture override'ını korur. API environment değişiklikleri için yeniden başlatılmalıdır.

| Environment anahtarı | User Secrets anahtarı | Gerçek gönderim değeri |
|---|---|---|
| `Email__Enabled` | `Email:Enabled` | `true` |
| `Email__Host` | `Email:Host` | Sağlayıcının SMTP host'u |
| `Email__Port` | `Email:Port` | STARTTLS için sağlayıcının portu, aşağıdaki örnekte587 |
| `Email__EnableSsl` | `Email:EnableSsl` | `true` — bu transport'ta STARTTLS |
| `Email__From` | `Email:From` | Yetkili/doğrulanmış gönderici adresi |
| `Email__Username` | `Email:Username` | SMTP hesabı veya sağlayıcı kullanıcı adı |
| `Email__Password` | `Email:Password` | Uygulama parolası veya sağlayıcının SMTP parolası; normal hesap parolası varsayılmaz |
| `PasswordReset__LinkBase` | `PasswordReset:LinkBase` | Development Expo Go LAN/Metro reset route'u; production için güvenilir HTTPS |
| `PasswordReset__LifetimeMinutes` | `PasswordReset:LifetimeMinutes` | Varsayılan15; izin verilen5–60 |

SMTP anahtarları `mobile/.env` veya `EXPO_PUBLIC_*` içine konmaz. Backend `.env` dosyasını kendiliğinden yüklemez. Environment anahtarlarının ayarlandığı terminalden API başlatılmalı veya API'nin User Secrets deposu kullanılmalıdır. User Secrets geliştirme içindir; şifreli bir secret vault değildir. [Microsoft rehberi](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0).

## Gmail ile kişisel posta kutusuna Development testi

Bu yol yalnız Google hesabının uygulama parolasına izin vermesi halinde geçerlidir. Google hesabında2adımlı doğrulama açık olmalı; hesabına özel uygulama parolası oluştur. Kurum politikası/Advanced Protection nedeniyle bu seçenek yoksa güvenliği gevşetme; desteklenen bir sağlayıcı veya ayrıca OAuth entegrasyonu kullan. [Google uygulama parolaları](https://support.google.com/accounts/answer/185833?hl=en). SMTP host `smtp.gmail.com`, port587 ve TLS ayarı Google'ın [SMTP kurulumunda](https://support.google.com/a/answer/176600?hl=en) belgelenir.

1. Mevcut5080 Development API'sini kontrollü kapat; SQL container/volume veya başka process'leri durdurma. Eski API açık kalırsa yeni environment ayarları o process'e uygulanmaz; ikinci API de aynı portu kullanamaz.
2. Repository kökünde yeni PowerShell aç. Aşağıdaki değerleri aynı terminalde ayarla. Parola etkileşimli ve gizli girilir; kaynak dosyaya, komut satırı literaline veya bu sohbete yazılmaz.

```powershell
$env:Email__Enabled = 'true'
$env:Email__Host = 'smtp.gmail.com'
$env:Email__Port = '587'
$env:Email__EnableSsl = 'true'
$env:Email__Username = Read-Host 'Gönderici Gmail adresi'
$env:Email__From = $env:Email__Username
$mailSecret = Read-Host 'Google uygulama parolası' -AsSecureString
$env:Email__Password = [Net.NetworkCredential]::new('', $mailSecret).Password
$env:PasswordReset__LifetimeMinutes = '15'
$env:PasswordReset__LinkBase = 'exp://192.168.1.155:8081/--/reset-password'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Development.ps1
```

**`-EmailCapture` ekleme.** Expo IP ve portunu güncel Metro adresinle eşleştir. Bu örnek yalnız Development içindir; doğrulanmamış production domain eklemez. TLS sertifikası doğrulamasını kapatma; SmtpClient için implicit TLS465 yerine bu STARTTLS587 yolunu kullan.

3. `mobile` klasöründe `npx.cmd expo start --port 8081` başlat, Expo Go'yu yükle. Önce gerçek alıcı adresinin PayAhead'de kayıtlı olduğundan emin ol; unknown email de202 alır fakat mesaj üretilmez. Önceki reset isteğinden2dk bekleyip bir kez bağlantı iste.
4. API202'yi yalnız kabul yanıtı olarak kaydet. Güvenli worker loglarında failure bulunup bulunmadığını incele. Genel “transport failed” mesajı bağlantı/auth/TLS nedenini tek başına ayırmaz; ayrıntılı SMTP exception veya protocol dump'ını loglama. Gmail hesap güvenlik etkinliği/gönderilmiş posta kontrolü; SMTP hizmeti kullanılıyorsa sağlayıcının accepted/rejected/bounced/delivered kayıtlarıyla kontrol et. SPF/DKIM/DMARC/sender doğrulaması sağlayıcıya aittir.
5. Alıcının inbox ve spam klasöründe yeni mesajı gerçekten görerek **posta kutusuna teslimi** ayrıca doğrula. SMTP kabulü, provider delivered olayı veya gönderilmiş klasörü tek başına inbox görünürlüğü garantisi değildir. Bağlantıyı açıp yeni parola belirle; aynı bağlantının yeniden kullanımını dene. Bu son adımlar bu oturumda yapılmadı.

Yeni process'i kapattıktan sonra terminaldeki `Email__Password` değişkenini kaldırabilir veya terminali kapatabilirsin. Ortam değişkenlerini/secret listesini debug çıktısına dökme. Gerçek sağlayıcıya geçişte Mailpit'i internete açmak veya Mailpit relay özelliğini rastgele etkinleştirmek gerekli değildir.

## User Secrets alternatifi

Mevcut API projesinde UserSecretsId vardır; yeniden init gerekmez. Yukarıdaki key tablosu kullanılır. Şifreyi komut argümanına yazmadan, gizli girdiyi JSON stdin üzerinden kaydetme örneği:

```powershell
$mailUser = Read-Host 'Gönderici Gmail adresi'
$mailSecret = Read-Host 'Google uygulama parolası' -AsSecureString
$mailSettings = @{
  'Email:Enabled' = 'true'
  'Email:Host' = 'smtp.gmail.com'
  'Email:Port' = '587'
  'Email:EnableSsl' = 'true'
  'Email:From' = $mailUser
  'Email:Username' = $mailUser
  'Email:Password' = [Net.NetworkCredential]::new('', $mailSecret).Password
  'PasswordReset:LinkBase' = 'exp://192.168.1.155:8081/--/reset-password'
}
$mailSettings | ConvertTo-Json -Compress | & ./.tools/dotnet/dotnet.exe user-secrets set --project src/PayAhead.Api
Remove-Variable mailSettings,mailSecret,mailUser
```

Bu komut diğer SQL/JWT secret'larını silmez, verilen anahtarları günceller. `--verbose`, `user-secrets list` veya JSON'u konsola basma kullanılmaz. Yeni terminalde aynı SMTP anahtarları environment'ta tanımlıysa User Secrets'i ezer; eski capture environment değerlerini kaldır veya doğru değerlerle değiştir. API'yi **EmailCapture switch'i olmadan** başlat. User Secrets örneği burada belgelenmiştir; bu oturum gerçek kimlik bilgisi kaydetmedi.

## Outlook ve diğer sağlayıcılar

Outlook.com resmî ayarları `smtp-mail.outlook.com:587`, STARTTLS ve **OAuth2/Modern Auth** gerektirir. Mevcut `SmtpPasswordResetEmail`, `System.Net.Mail.SmtpClient` + `NetworkCredential` kullanır; OAuth access token edinme/SASL XOAUTH2 akışı yoktur. Bu nedenle yalnız Outlook parolası girerek çalışacağı iddia edilemez. Kullanıcının bu sağlayıcıyı seçmesi halinde ayrıca güvenli OAuth transport/provider entegrasyonu gerekir. [Outlook ayarları](https://support.microsoft.com/en-US/Outlook/pop-imap-and-smtp-settings-for-outlook-com), [Microsoft OAuth SMTP](https://learn.microsoft.com/en-us/exchange/client-developer/legacy-protocols/how-to-authenticate-an-imap-pop-smtp-application-by-using-oauth/). Kurumun güvenlik varsayılanlarını kapatma veya temel kimlik doğrulamayı zorla açma önerilmez.

Başka bir SMTP sağlayıcı seçilirse aynı key'ler kullanılır; host/from/user/password/TLS yöntemini sağlayıcının konsolundan doğrula. Sandbox sağlayıcılarda alıcının da önceden doğrulanması gerekebilir. Gerçek hesap bilgileri olmadan dış gönderim yapılmış gibi davranılmaz. Bu incelemede auth/token/rate-limit kodu değiştirilmedi; production deployment veya Phase5 yapılmadı.
