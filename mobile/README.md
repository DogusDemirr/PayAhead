# PayAhead Mobile — Frontend Phase 2

Mevcut Expo 57.0.31 / React Native 0.86.3 / React 19.2.3 uygulamasında gerçek authentication ve session management. Phase 1 lacivert/teal tasarımı, Inter, light/dark teması ve ortak bileşenler korundu. Backend kodu değiştirilmedi. Frontend Phase 3 başlatılmadı.

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

Varsayılan `EXPO_PUBLIC_DESIGN_PREVIEW=false`. Development önizlemesi etkinleştirilse bile **gerçek giriş gerektirir** ve açıkça etiketlenir; production'da kapanır. Finans ekranlarının gerçek API bağlantısı Phase 3'e bırakıldı; bu sürümde veri bağlı olmadığını belirten görünüm kullanılır. Settings profili gerçek `/me` yanıtından gelir; Premium/finans işlemleri yapılmaz.

SecureStore yalnızca native Android/iOS hedefinde kullanılır. Web gerçek kalıcı oturum desteklemez; güvensiz localStorage/AsyncStorage alternatifi yoktur. Web bundle smoke testi native SecureStore testi yerine geçmez.

## Doğrulama

```powershell
npm.cmd run typecheck
npm.cmd run lint
npm.cmd test
npx.cmd expo-doctor
npx.cmd expo install --check
npx.cmd expo export --platform android
node --env-file-if-exists=.env scripts/auth-smoke.cjs
```

Son komut yalnızca yerel/LAN geliştirme backend'ine gider. Rastgele geçici hesabı oluşturur, auth sözleşmesini kontrol eder ve yalnızca oluşturduğu hesabı siler. Şifre/token/yanıt gövdesi yazdırmaz. Backend erişilemiyorsa hata koduyla çıkar; başarılı sayılmaz.

TypeScript/ESLint ve Expo Doctor 21/21 geçti; otomatik testler ve sonuçların ayrıntısı [ilerleme raporunda](docs/IMPLEMENTATION_PROGRESS.md). Android Hermes export ve Metro/web bundle başarılı. Gerçek backend smoke ilk register isteğinde bağlantı kuramadı; bu çalışmada fiziksel Android testi yapılmadı. Kullanıcı Phase 1 tasarımını fiziksel cihazda beğendiğini bildirdi; bu, Phase 2 auth testinin yerine geçmez.

npm audit: 58 kayıt (50 high/8 moderate), `--omit=dev` ile 53 (45 high/8 moderate). Dört temel transitive bildirim ve Expo araç zinciri etkisi [güvenlik değerlendirmesinde](docs/IMPLEMENTATION_PROGRESS.md). Uyumsuz major sürüm, zorlanmış downgrade veya doğrulanmamış override uygulanmadı.

Belgeler: [Mimari](docs/ARCHITECTURE.md), [tasarım sistemi](docs/DESIGN_SYSTEM.md), [geliştirme](docs/DEVELOPMENT.md), [Android kabul matrisi](docs/ANDROID_TESTING.md), [ilerleme ve açık noktalar](docs/IMPLEMENTATION_PROGRESS.md).
