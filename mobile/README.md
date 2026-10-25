> **Phase5.1.1:** Tüm kategori kartı tıklanır; tam yol/geri, hiyerarşik arama, alt kategori ve özel kayıt korunur.116kategori,102aktif hizmet;Netflix/Spotify/Opet/Microsoft/Adobe gerçek yerel görselleri,99fallback. [Sonuç](docs/PHASE5_1_1_REPORT.md), [10Android kabulü](docs/ANDROID_TESTING.md), [lisans/marka notları](assets/brands/NOTICE.md). Güncel backend'i yeniden başlatıp Metro cache'ini temizleyin. Fiziksel kabul, önceki push/inbox açıkları ayrı kalır.

> **Phase5.1 — 25 Ekim 2026:** Ödeme/abonelik için ortak kategori/hizmet seçici, arama, popülerler, araç giderleri ve özel kayıt geliştirildi. 0 gerçek marka logosu; kategori ikonları fallback olarak kullanılır. TS/lint temiz, Jest173/173, Doctor21/21, Hermes4099modül/8.2MB. [Rapor](docs/PHASE5_1_REPORT.md), [Android20adım](docs/ANDROID_TESTING.md), [merkezi sözleşme](../docs/CATALOG.md). Fiziksel Phase5.1 kabulü ve mevcut Phase6 gerçek push kabulü bekler. Bu faz SMTP/production/Phase7 içermez.

# PayAhead Mobile — Frontend Phase 6

Gerçek ödeme hatırlatma CRUD, native saat seçimi, hesap bildirim/saat dilimi tercihleri, cihaz kaydı/rotation/çıkış temizliği ve güvenli bildirim tap navigasyonu eklendi. [Phase6 raporu](docs/PHASE6_REPORT.md), [gerçek EAS/FCM/push kurulum adımları](docs/PUSH_SETUP.md), [Android kabulü](docs/ANDROID_TESTING.md).

**Gerçek push henüz doğrulanmadı:** EAS projectId, Firebase Android dosyası, EAS FCM v1 yetkisi ve backend Expo access token kurulumu gerekiyor. Development APK oluşturulmadı; Expo Go remote push kanıtı değildir. Yalnız reminder/ayar UI akışı için `npx.cmd expo start --go`; gerçek kurulum sonrası `npx.cmd expo start --dev-client`. `npm.cmd run push:preflight` eksik gerçek ayarlarda bilerek başarısız olur. İzin, kayıt, Expo ticket/receipt ve telefon teslimi ayrı durumlardır. Phase7 başlatılmadı, SMTP/Resend ve production deployment yapılmadı.

Son testler: mobil161/161; backend unit386/386, gerçek SQL256/256; TS/lint/build temiz, Doctor21/21. Kullanıcı Phase5 fiziksel kabulünü başarılı bildirdi; Phase4 açık fiziksel/inbox maddeleri korunur. Aşağıdaki Phase5 açıklaması önceki fazın işlevlerini anlatır.

## Frontend Phase 5

Expo 57.0.31 / React Native 0.86.3 / React 19.2.3 üzerinde abonelik listesi, oluşturma/düzenleme, detay, iptal/kaldırma ve yaklaşan denemeler gerçek API'ye bağlandı. Dashboard abonelik özeti eklendi. Kayıt/giriş, takvim/geçmiş/analiz ve oturum izolasyonu korundu; backend ve migration değiştirilmedi. [Phase 5 raporu](docs/PHASE5_REPORT.md), [API ve finansal semantik](docs/SUBSCRIPTIONS.md). Phase 6 başlatılmadı.

Aylık tahmini karşılık para birimlerine göre ayrıdır; bu ayın kesin harcaması değildir. İptal işlemi yalnızca PayAhead takibini durdurur; dış sağlayıcıda ayrıca iptal gerekir. Bağlı tek PaymentPlan takvim ve analizin finansal kaynağıdır. Free limitleri güncel entitlement yanıtından okunur; sunucu ayrıca uygular. Gerçek satın alma yapılmaz.

Phase 5 kalite komutları: `npm.cmd run typecheck`, `npm.cmd run lint`, `npm.cmd test -- --silent`, `npx.cmd expo-doctor`, `npx.cmd expo export --platform android --output-dir artifacts/phase5-android`, çalışan yerel backend için `npm.cmd run test:subscriptions:live`. Smoke yalnızca kendi geçici hesaplarını oluşturup temizler; gerçek kişisel aboneliklerini kullanmaz. [Android Phase 5 kabul listesi](docs/ANDROID_TESTING.md) fiziksel cihazda bekliyor.

Phase 4 için backend'in yeni migration ve yapılandırmayla çalışması gerekir: [SMTP, migration ve deep link kurulumu](../docs/PASSWORD_RESET.md), [analiz sözleşmesi ve tarih/para semantiği](../docs/ANALYTICS.md). Development e-postası Mailpit yakalamasıdır; gerçek sağlayıcı/inbox kabulü yayın öncesine ertelenmiştir. Bu fazda SMTP/Resend kurulmadı. Phase 4'ün açık fiziksel kabulü sürer. Önceki faz kayıtları aşağıda tarihsel olarak korunmuştur.

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
