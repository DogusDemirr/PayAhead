# PayAhead Mobile — Frontend Phase 4

Expo 57.0.31 / React Native 0.86.3 / React 19.2.3 üzerinde gerçek takvim, ödeme geçmişi, tek ödeme durumları, native tarih seçici, e-posta ile şifre sıfırlama ve ayrı Analizler ekranı eklendi. Kayıt/giriş, oturum/cache izolasyonu ve ödeme CRUD korundu. Backend'e yalnızca reset ve read-only analytics desteği eklendi; .NET/EF sürümleri yükseltilmedi. Frontend Phase 5 başlatılmadı.

Phase 4 için backend'in yeni migration ve yapılandırmayla çalışması gerekir: [SMTP, migration ve deep link kurulumu](../docs/PASSWORD_RESET.md), [analiz sözleşmesi ve tarih/para semantiği](../docs/ANALYTICS.md). SMTP varsayılan kapalıdır; gerçek posta kutusuna teslim doğrulanmadı. [Android kabul listesi](docs/ANDROID_TESTING.md) fiziksel cihazda tamamlanmalıdır. Önceki faz kayıtları aşağıda tarihsel olarak korunmuştur.

## Çalıştırma

Bu çalışma alanında bağımlılıklar ve `.env` hazırdır:

```powershell
cd mobile
npx.cmd expo start
```

Fiziksel Android için SDK 57 uyumlu Expo Go, aynı Wi-Fi ve bilgisayarın LAN adresi gerekir. Yerel `.env` bu çalışmada `http://192.168.1.155:5080` adresine ayarlandı; IP DHCP ile değişebilir. Backend ayrıca çalışmalıdır. [Adım adım bağlantı](docs/DEVELOPMENT.md).

Yeni checkout: `npm.cmd ci`, ardından `Copy-Item .env.example .env`; API kök adresini bilgisayarının LAN IPv4 adresine göre düzenle. PowerShell execution policy değiştirilmeden `.cmd` komutları kullanılabilir; macOS/Linux'ta ekleri kaldır.

## Gerçek akış

| İşlem | Backend sözleşmesi |
|---|---|
| Register | POST `/api/auth/register`, email/password/displayName → 201 UserResponse; otomatik giriş yok |
| Login | POST `/api/auth/login`, email/password → AuthResponse |
| Refresh | POST `/api/auth/refresh`, refreshToken → yeni token çifti |
| Logout | POST `/api/auth/logout`, refreshToken → 204 |
| Profil | GET `/api/users/me`, Bearer JWT → id/email/displayName |

Login, SecureStore yazımı ve `/me` doğrulaması tamamlanınca korunan Dashboard açılır. Kayıt başarıyla tamamlandığında login ekranı açılır. Loading, çift gönderim koruması, server field validation, invalid credentials, network ve timeout mesajları hazırdır. Token veya parola loglanmaz.

Refresh tekilleştirilir; geç dönen eski-token 401 yanıtı yeni bir rotation başlatmaz. İptal edilmiş istek veya eski oturum isteği replay edilmez. Logout yerel token/cache'i temizler; offline sunucu iptalinin doğrulanamadığını bildirir. Ayrıntılı [oturum mimarisi](docs/ARCHITECTURE.md).

## Gerçek Dashboard ve ödeme yönetimi

Dashboard aylık yaklaşan/ödenen toplamları para birimi bazında sunucudan alır. En yakın ödeme, aktif plan sayısı, ayın Pending/Paid/Skipped sayıları, bu ayın gecikenleri ve yaklaşan ilk kayıtlar gösterilir. Loading/empty/error/retry ve pull-to-refresh desteklenir; başarılı değişikliklerde finans cache'i yenilenir.

Ödemeler sunucu tarafı arama, aktif/pasif, para birimi ve kategori filtreleriyle sayfalanır. Detay, create, edit, onaylı delete/pasifleştirme vardır. Gerçek recurrence alanları, DateOnly, decimal(19,4) precision, RowVersion/If-Match ve entitlement limiti korunur. Tutarlar JSON sayısı olarak gönderilir; uygulama içinde hassasiyet için lexical ondalık metin tutulur. [Sözleşme ve davranış ayrıntıları](docs/PAYMENTS_API.md).

Home/Payments artık tasarım fixture'ı göstermez. Varsayılan preview false; Calendar/Subscriptions önizlemesi ayrı, etiketli ve development + gerçek oturum gerektirir. Bu diğer ekranların gerçek API entegrasyonuna geçilmedi. Premium bağlantısı mevcut bilgilendirme ekranına gider; sahte satın alma yoktur.

SecureStore yalnızca native Android/iOS hedefinde kullanılır. Web gerçek kalıcı oturum desteklemez; güvensiz localStorage/AsyncStorage alternatifi yoktur. Web bundle smoke testi native SecureStore testi yerine geçmez.

## Doğrulama

```powershell
npm.cmd run typecheck
npm.cmd run lint
npm.cmd test
npx.cmd expo-doctor
npx.cmd expo install --check
npx.cmd expo export --platform android
npm.cmd run test:auth:live
npm.cmd run test:finance:live
```

Canlı smoke komutları yalnızca yerel/LAN geliştirme backend'ine gider. Rastgele geçici hesap/planlar oluşturur ve yalnızca kendi oluşturduklarını temizler; şifre/token/finansal yanıt gövdesi yazdırmaz.

23 Ekim 2026: 68/68 Jest testi, strict TypeScript, ESLint, Expo Doctor 21/21 ve Android Hermes export başarılı. Gerçek auth smoke 8 HTTP adımı, finans smoke 24 HTTP kontrolü ve 4 cleanup çağrısıyla geçti. Startup/uygulama HTML'i ve development JS bundle HTTP 200 döndürdü. Bunlar fiziksel Android testi değildir; yeni ekranların cihaz kabulü bekliyor.

Güncel npm audit 7 kayıt (4 high/3 moderate), dev hariç de 7 raporladı. Önceki fazın 58 kaydı güncel registry çıktısında aynı değildir; bunu kod değişikliğiyle giderilmiş açık sayısı olarak sunmuyoruz. Kalan node-forge/Router query decoder zincirleri ve build/test ayrıntıları [ilerleme raporunda](docs/IMPLEMENTATION_PROGRESS.md). SDK'yı bozan major/downgrade/force uygulanmadı.

Belgeler: [Mimari](docs/ARCHITECTURE.md), [tasarım sistemi](docs/DESIGN_SYSTEM.md), [geliştirme](docs/DEVELOPMENT.md), [Android kabul matrisi](docs/ANDROID_TESTING.md), [ilerleme ve açık noktalar](docs/IMPLEMENTATION_PROGRESS.md).
