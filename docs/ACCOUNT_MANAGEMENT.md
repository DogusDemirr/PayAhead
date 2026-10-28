# Phase 5.3 — Profil ve hesap yönetimi

## Mevcut altyapı ve ekran

Ayarlar → Hesap Bilgileri artık korumalı `/account` route'una gider. Avatar baş harflerinden üretilir; ad soyad ve e-posta API'den gelir. Kişisel Bilgiler, Güvenlik, Dil ve Bölge, Bildirimler, Üyelik ve Hesap İşlemleri aynı hesap merkezindedir. Lacivert/teal ve light/dark tema korunur. E-posta doğrulaması desteklenmediğinden doğrulanmış hesap rozeti gösterilmez; e-posta düzenlenemez. Telefon/adres/doğum tarihi eklenmedi.

Üyelik `/api/premium/entitlements` verisidir; bilinmeyen/hatalı sorgu Free/Premium diye sunulmaz. Gerçek bitiş tarihi ve server limitleri gösterilir. Mağaza yönetimi cihaz platformuna göre Google Play/App Store abonelik sayfasını açar; satın alma veya otomatik iptal yapmaz. Başka mağazadan alınmış üyelik, satın alındığı mağazada yönetilmelidir.

Dil mevcut `/language`, saat dilimi ve bildirimler mevcut `/notifications` ekranı ve User Settings API'si üzerinden yönetilir. Yeni paralel ayar deposu eklenmedi. 35 yeni profil metni × 14 dil = 490 AI taslak çeviri mevcuttur; profesyonel dilsel kabul değildir. `i18n:audit` kaynak/placeholder/çoğul/JSON ve kullanım denetimini sürdürür.

## Endpoint sözleşmesi

Tüm endpoint'ler JWT, aktif kullanıcı ve `no-store` gerektirir. Sahiplik body'den alınmaz.

| Endpoint | Davranış |
|---|---|
| `GET /api/users/me/profile` | `id,email,displayName,rowVersion` döner |
| `PUT /api/users/me/profile` | Yalnız `displayName,rowVersion`; güncel profili döner |
| `POST /api/users/me/change-password` | `currentPassword,newPassword,confirmation`; başarı 204 |
| `GET /api/users/me` | Mevcut sözleşme korunur; güncel profil adı görünür |
| `DELETE /api/users/me` | Önceden var olan, `currentPassword` ile yeniden doğrulanan silme |

Profil adı 1–100 karakter, boş/control karakter içeremez; baş/son boşluklar temizlenir. SQL rowversion stale ise 409, hatalı giriş/ek alan 400. Kullanıcı ID, email ve securityStamp güncellemesi kabul edilmez. Güncelleme sahip satırı `UPDLOCK,HOLDLOCK` ile kilitler, ardından rowversion kontrol eder. Güncel `/me` ve profil cache'i doğru kullanıcı/oturum kapsamında yenilenir. SessionManager public kullanıcı adını token deposuna yeniden yazmadan günceller. Geç yanıtlar epoch/owner kontrolüyle engellenir.

Profil GET/PUT için kullanıcı başına 30/dakika, password için 5/dakika paylaşılan SQL rate limit vardır; hesap silme mevcut 5/dakika privacy bütçesini korur. Yanlış mevcut parola 400 + `current_password_invalid` döner; bu hatada oturum gereksiz yere kapatılmaz. Başka alanlardaki hata mesajı ham backend metnini göstermez. Karşılaştırma validasyon mesajları parola değerlerini içermez.

## Şifre ve session politikası

Yeni parola mevcut politikayla 12–128 karakter, mevcut paroladan farklı ve tekrar alanıyla eşit olmalıdır. Mevcut parola backend Identity hash doğrulamasıyla kontrol edilir. Parolalar loglanmaz, profil/şifre isteği response'a yansıtılmaz, SecureStore'a yazılmaz. Mutation retry kapalıdır; `gcTime:0` ve form sonunda `reset` ile credential değişkenleri cache'de tutulmaz. Alanlar istek sonunda temizlenir.

Başarıda bütün refresh token'lar revoke edilir, bekleyen reset token'ları tüketilir, cihaz kayıtları pasifleştirilir ve `Users.SecurityStamp` değiştirilir. JWT artık `security_stamp` claim'i taşır; her doğrulamada aktif kullanıcı ve mevcut stamp kontrol edilir. Eski access token'lar da reddedilir. Login doğrulamasıyla session ekleme arasındaki yarış, owner lock ve beklenen stamp kontrolüyle engellenir. Kullanıcı yeni parolayla tekrar giriş yapar.

Eski token'larda claim yoksa yalnız DB stamp'i Empty olan hesaplarda geriye uyumluluk vardır. Güvenlik değişimi olmuş hesaplarda eski/missing stamp kabul edilmez. JWT issuer/audience/signature/lifetime, refresh rotation/reuse ve DevelopmentIdentity kuralları korunur. Başlamış provider teslimatı geri alınmış sayılmaz; gerçek push kabulü bu fazda yapılmadı.

Offline/timeout ile credential yazısının sonucu belirsizse yerel oturum güvenli biçimde kapatılır ve belirsizlik mesajı gösterilir; başarılı şifre değişimi/silme iddiası yapılmaz. Profilde belirsiz yazı taslağı korur, yeniden yükleme olmadan kör retry engellenir.

## Hesap silme ve saklama

Yeni silme altyapısı icat edilmedi. Mevcut AccountStore aktif kullanıcı/parola doğrulaması ve transaction ile uygulanır. Mobilde parola girişi ve ayrı destructive Alert onayı gerekir. Sunucudan 204 doğrulanınca local session/cache temizlenir; hata/timeout başarı göstermez.

Planlar, occurrence geçmişi, hatırlatıcılar, takip abonelikleri, bildirim işleri, cihazlar, ayarlar, reset/refresh token'ları ve kullanıcı audit kayıtları kullanıcı kapsamında silinir. User satırı aktif olmayan, e-posta/ad/parola temizlenmiş takma kimlikli sahip olarak kalır. Mağaza anti-replay/hesap transferini önleyen sınırlı purchase/subscription bağları saklanır; bunların tamamen anonim veya tüm verinin fiziksel olarak yok edildiği iddia edilmez. Mağaza protected evidence ve tekrar doğrulama kaldırılır. Hesap silme mağaza aboneliğini iptal etmez; bu uyarı 14 dilde onaydan önce görünür.

Bu teknik saklama davranışı hukukî uyumluluk sertifikası değildir. Production deployment veya saklama politikasında yeni bir değişiklik yapılmadı.

## Migration ve Development

`20261028195511_ProfileSecurity` yalnız Users'a `RowVersion (rowversion)` ve `SecurityStamp (uniqueidentifier, başlangıç Empty)` ekler. User ID, email, parola hash'i ve finansal tutarlar değiştirilmez. Otomatik migration eklenmedi, var olan migration'lar silinmedi. Development `127.0.0.1:14333 / PayAheadDockerDevelopment` hedefi doğrulanıp açıkça uygulandı; eski kullanıcı kolonları ve 10 diğer tablonun checksum/satır sayısı korundu.

Yeni makinede önce User Secrets/environment ile hedefi ve pending migration listesini doğrulayın. SQL yedeği ve bakım planı doğrulanmadan başka hedefe uygulamayın. Ardından proje .NET 8 SDK'sıyla:

```powershell
$env:DOTNET_CLI_HOME="$PWD/.tools/cli"
.tools/dotnet/dotnet.exe ef database update 20261028195511_ProfileSecurity --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
.\scripts\Start-Development.ps1 -Port 5080
```

Eski API binary'si yeni endpoint ve security stamp kontrolünü sağlamaz. Android öncesi eski Development terminalini kontrollü kapatıp güncel API'yi başlatın. Alternatif port seçilirse `mobile/.env` içindeki LAN adresinin portuyla eşleştirin. Bu çalışmada kullanıcıya ait eski API süreçleri durdurulmadı; doğrulama için ayrı 5093 API kullanıldı. Production ve eski stamp kontrolü olmayan binary'ye otomatik rollback yapılmadı.

## Doğrulama

```powershell
.tools/dotnet/dotnet.exe restore PayAhead.sln --artifacts-path artifacts/phase53-check
.tools/dotnet/dotnet.exe build PayAhead.sln --no-restore --artifacts-path artifacts/phase53-check
.tools/dotnet/dotnet.exe test PayAhead.sln --no-build --artifacts-path artifacts/phase53-check
cd mobile
npm.cmd run i18n:audit
npm.cmd run typecheck
npm.cmd run lint
npm.cmd test -- --silent
npx.cmd expo-doctor
npx.cmd expo export --platform android --output-dir artifacts/phase53-export
# Yalnız güncel localhost test API'si için (.env dosyası değiştirilmez):
$env:EXPO_PUBLIC_API_BASE_URL='http://127.0.0.1:5093'
npm.cmd run test:account:live
Remove-Item Env:EXPO_PUBLIC_API_BASE_URL
```

Live smoke kendi rastgele test hesaplarını oluşturur/temizler; mevcut kullanıcıyla çalışmaz, parolaları/token'ları yazdırmaz. Testcontainers gerçek SQL Server 2022 ve rastgele izole test DB kullanır. [Sonuç raporu](../mobile/docs/PHASE5_3_REPORT.md) ve [Android kabulü](../mobile/docs/ANDROID_TESTING.md) ayrı tutulur.
