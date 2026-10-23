# Şifre sıfırlama — Frontend Phase 4

**23 Ekim hata düzeltmesi:** Eski çalışan API401'i, eksik Development migration ve SMTP kurulumu tespit edilip düzeltildi. Güncel gerçek HTTP/SMTP sonuçları ve Android yeniden deneme adımları [PASSWORD_RESET_DIAGNOSIS](PASSWORD_RESET_DIAGNOSIS.md) içindedir. Yeni `scripts/Start-Development.ps1` eski binary'ye dönmek yerine güncel kaynakları derleyerek başlatır. Bu düzeltmede hedef Development DB'ye additive migration elle uygulandı; önceki fazın “çalışan API durdurulmadı/migration uygulanmadı” kayıtları kendi tarihsel kapsamıdır.

Bu özellik backend'in mevcut JWT, rotating refresh ve SQL Server mimarisini kullanır. Production'a dağıtım veya otomatik migration eklenmedi. Yeni migration yalnızca `PasswordResetTokens` tablosunu, hash unique index'ini, kullanıcı/tarih index'ini ve User FK'sını ekler. Finansal tabloları değiştirmez.

## Sözleşmeler

- `POST /api/auth/forgot-password` → `{ "email": "adres@example.test" }`. Geçerli formatta kayıtlı/kayıtsız adres için aynı **202** ve `message`. Yanıt e-posta teslimini doğrulamaz. Geçersiz format 400; auth kotası aşılırsa 429; SQL koruması erişilemezse 503.
- `POST /api/auth/reset-password` → `{ "token": "epostadan-alinan-kod", "password": "yeni-parola" }`. Başarılı **204**; geçersiz/süresi dolmuş/kullanılmış token aynı genel 400. Parola kayıt akışıyla aynı 12–128 karakter sınırındadır. Token tam 43 base64url karakteridir.

32 kriptografik rastgele byte üretilir; SQL'de yalnızca SHA-256 hash saklanır. Varsayılan süre 15 dakika, yapılandırılabilir sınır 5–60 dakikadır. Kullanıcı başına 2 dakika bekleme, tüm auth endpoint'leriyle ortak IP başına 20 istek/dakika SQL kotası vardır. Yeni token eskilerini geçersizleştirir. SMTP ağı HTTP yanıtını bekletmez; hesap varlığına bağlı SMTP zaman farkı oluşturulmaz. SQL sorguları için mutlak sabit süre garantisi verilmez.

Tüketim, kullanıcı satırı kilidi altında tek SQL transaction içinde parola değişimi, tüm kullanılmamış reset token'larının tüketimi ve bütün refresh ailelerinin iptalini yapar. Eşzamanlı sıfırlamada yalnızca biri kazanır. Reset öncesinde doğrulanıp sonrasında session oluşturmaya çalışan login de kullanıcı kilidiyle sınırlandırılır. Mevcut access JWT'ler kendi kısa süreleri dolana kadar geçerli kalabilir (varsayılan 15 dakika + 15 saniye tolerans); reset akışı access token blacklist eklemez. Mobil başarıdan sonra yerel oturumu/cache'i temizler ve yeniden giriş ister.

## Geliştirme SMTP yakalama

Yerel Mailpit, dış posta kutusuna teslim yapmaz. [Resmî Docker kurulumu](https://mailpit.axllent.org/docs/install/docker/) temel alınmıştır. Yalnızca bilgisayarın loopback adresine bağlanır; relay/forwarding yoktur. SMTP varsayılan 1025, web arayüzü 8025'tir. Çakışırsa port değişkenlerini ayarla; başka servis durdurma.

```powershell
docker compose -f docker/compose.email.yml up -d
# Mesajları bilgisayar tarayıcısında http://127.0.0.1:8025 üzerinden görüntüle.
docker compose -f docker/compose.email.yml stop
```

API'yi başlatacağın terminalde (örnek LAN adresini kendi bilgisayarına göre değiştir):

```powershell
$env:Email__Enabled='true'
$env:Email__Host='127.0.0.1'
$env:Email__Port='1025'
$env:Email__EnableSsl='false'
$env:Email__From='PayAhead <payahead@example.test>'
$env:PasswordReset__LifetimeMinutes='15'
$env:PasswordReset__LinkBase='exp://192.168.1.155:8081/--/reset-password'
```

Expo terminalindeki gerçek LAN adresi ve portu kullan. `npx.cmd expo start` başka port seçerse LinkBase'i güncelle ve API'yi yeniden başlat. `localhost` telefon üzerinde bilgisayarı göstermez. Mobil `.env` API kökünü, örneğin `http://192.168.1.155:5080`, göstermelidir. `Email:Enabled` varsayılan kapalıdır. Kapalı/başarısız transport güvenli uyarı üretir; e-posta gönderilmiş sayılmaz.

Geliştirme API'sini kontrollü durdurduktan sonra, mevcut connection string ortamında migration'ı **elle** uygula:

```powershell
$env:DOTNET_CLI_HOME="$PWD/.tools/cli"
& ./.tools/dotnet/dotnet.exe tool restore
& ./.tools/dotnet/dotnet.exe ef database update --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Infrastructure
# ConnectionStrings__PayAhead mevcut development SQL bağlantısı olmalıdır; terminal çıktısına yazdırma.
```

Çalışan API DLL kilitliyken normal build/update başarısız olabilir. Bu çalışmada kullanıcı API'si durdurulmadı; testler ayrı artifacts çıktısı ve yalnızca rastgele `PayAheadTest_<guid>` veritabanlarında çalıştırıldı. Production veritabanına migration uygulanmadı. Mevcut geliştirme API'si yeni kod/migration/SMTP ayarlarıyla yeniden başlatılmadan bu yeni endpoint'leri sunmaz.

## Gerçek sağlayıcı

`Email__Host`, `Email__Port` (STARTTLS genellikle 587), `Email__EnableSsl=true`, `Email__From`, `Email__Username`, `Email__Password` değerlerini environment veya User Secrets ile tanımla. Kimlik bilgilerini `.env.example`, mobile `EXPO_PUBLIC_*`, log veya Git'e koyma. `SmtpClient` STARTTLS kullanır; implicit TLS 465 bu yapılandırmanın doğrulanmış yolu değildir. Gönderici domain doğrulaması/SPF/DKIM/DMARC sağlayıcı tarafındadır. SMTP kabulü dış posta kutusu tesliminin kanıtı değildir. Gerçek inbox teslim testi bu çalışmada yapılmadı.

Development dışında açık e-posta için TLS ve güvenilir **HTTPS** reset LinkBase zorunludur. Doğrulanmış production domain/App Links kurulumu yapılmadı; örnek domain gerçekmiş gibi eklenmedi. Yayın öncesi domain sahipliği, HTTPS landing/fallback, Android assetlinks ve iOS association ayrı kurulmalıdır.

## Mobil bağlantı ve güvenli alternatif

Expo Go bağlantısı `exp://BILGISAYAR_IP:METRO_PORT/--/reset-password?token=...` biçimindedir. Metro açık ve telefon aynı ağda olmalıdır. Custom `payahead://` scheme native build içindir; Expo Go'da çalıştığı varsayılmaz. E-posta istemcisi exp bağlantısını açmazsa login → Şifremi unuttum → “Bağlantım var, yeni şifre belirle” ekranında bağlantının tamamını veya kodu yapıştır. Kod güvenli alanda gösterilir, SecureStore/AsyncStorage'ye yazılmaz; başarıda form ve route parametresi temizlenir. Linki ekran görüntüsünde, analytics veya hata raporunda paylaşma. Birden fazla kod geldiyse en son bağlantıyı kullan.

E-posta Türkçe, PayAhead lacivert/teal markalı ve mobil uyumlu HTML içerir. Gönderim kuyruğu process başına en fazla 100 öğedir ve yalnızca bellektedir. Yeniden başlatmada kuyruk kaybolabilir; aynı kullanıcı 2 dakika sonra yeni bağlantı isteyebilir. Dolu kuyrukta token iptal edilir. Gönderim hatası token'ı geçersizleştirir; süre aşmış kuyruk öğesi gönderilmez. Otomatik tekrar yoktur. Kuyruk, dayanıklı kurumsal e-posta servisi teslim garantisi sunmaz.

## Doğrulama ayrımı

SQL/Testcontainers testleri hash, expiry, replay, yarış, session iptali, generic yanıt, rate limit ve additive migration/veri korumayı doğrular. SMTP transport testi gerçek loopback TCP SMTP sunucusunda MIME mesajını yakalar; mail sınıfı mock'u değildir. API servis testleri teslim sağlayıcısını kontrollü test double ile değiştirir. Kestrel smoke gerçek HTTP + SQL kullanır; o senaryoda teslim test double'dır. Mailpit Compose yapılandırması sağlandı; dış inbox ve fiziksel telefondan deep link kabulü ayrı bekler.
