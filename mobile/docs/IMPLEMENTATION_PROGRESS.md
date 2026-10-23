# Frontend Phase 4 — Takvim, geçmiş, reset ve analizler

**23 Ekim 2026.** Kullanıcının Phase 4 attachment talebiyle frontend ve yalnızca reset/read-only analytics için gerekli backend ekleri onaylandı. Başlangıçta Phase 3 değişiklikleri commit edilmemişti; korundu. Kullanıcının API csproj/appsettings.Development değişiklikleri ve production-operations-review.sql dosyası korunmuştur. .NET 8/C#12/EF8 ve Expo57/RN0.86 sürümleri yükseltilmedi. Production deployment ve Frontend Phase 5 yapılmadı.

Gerçek aylık takvim, server Today, ay/gün seçimi, durum işaretleri, geçmiş dönem/durum/pagination, plan ve tek ödeme ayrımı, Paid/Skipped/Pending mutation'ları ve açık 409/uncertain-result yönetimi uygulandı. PaidAtUtc API tarafından atanır; mobil sahte tarih alanı göndermez. Tüm finans cache'i kullanıcı/epoch kökünde invalidate edilir. Başlangıç/bitiş için SDK57 datetimepicker9.1.0, opsiyonel bitiş temizleme ve yerel DateOnly parçaları kullanıldı. Analiz ayrı route'tur; beş tab korunur. Son altı ay aggregation sunucuda; decimal string metinleri, currency seçimi/server filtresi, SVG bar/donut, kategori ve top5 vardır.

Reset: 32 rastgele byte, SQL'de SHA256 hash, 5–60 dakika yapılandırılabilir süre (default15), tek kullanım, kullanıcı kilidi ve refresh-family iptali aynı transaction, auth SQL rate limiti ve 2 dakika hesap cooldown. SMTP bellekte sınırlı kuyrukla HTTP'den ayrıdır; teslim/hata/hesap varlığı sızdırılmaz. Token/parola loglanmaz. Yeni additive `20261023190720_PasswordResetTokens`; mevcut kullanıcı/plan korunumu SQL'de doğrulandı. Geliştirme ve production'a otomatik migration yoktur. Türkçe HTML e-posta, Mailpit compose, Expo Go exp linki ve elle bağlantı/kod fallback hazırdır.

| Son kontrol | Gerçek sonuç |
|---|---|
| TypeScript / ESLint | Başarılı; hata/uyarı yok |
| Jest | **91/91**, 9 suite; atlanan yok |
| Expo Doctor / install --check | **21/21**, SDK uyumlu |
| Android Hermes export | Başarılı; 4011 modül, yaklaşık 7.9 MB |
| Expo startup | 8085 Metro status200/running, HTML200, Android development JS200 |
| .NET8 restore/build | Başarılı; final build0 warning/0error; ayrı root artifacts çıktısı |
| Backend unit | **384/384**, atlanan yok |
| Gerçek SQL integration | **253/253**, atlanan yok; Testcontainers SQL Server2022 ve rastgele test DB'leri |
| Hedefli Phase4 | **11/11** SQL/reset/analytics/migration/SMTP/Kestrel senaryosu; tam253 içinde de geçti |
| E-posta | Gerçek loopback TCP SMTP MIME capture geçti; dış inbox teslimi **doğrulanmadı** |
| HTTP smoke | Gerçek Kestrel/izole SQL: ready, Swagger, register/login, calendar, analytics, forgot/reset, eski refresh401, yeni login200; teslim bu smoke'ta test double |
| Npm audit | Toplam8:5 high/3 moderate; force/SDK downgrade/uyumsuz override yapılmadı |
| Fiziksel Android/görsel | **Çalıştırılmadı**; kabul listesi ANDROID_TESTING.md içinde |

İlk sandbox SQL girişimi Docker named-pipe erişim engeliyle başarısızdı; başarılı sayılmadı, izinli çalıştırmayla gerçek SQL kullanıldı. Ara turlarda yeni tablo için eski schema sayıları ve SMTP test sunucusu/MIME çözümleme beklentisi düzeltildi. Çalışan test çıktısını yeniden derleme denemesi dosya kilidine takıldı; son derleme/test ayrı `artifacts/phase4-release` üzerinden temiz geçti. Son kanıtlar: root `artifacts/phase4-complete-tests.log`, `artifacts/phase4-release-results/*.trx`, additive SQL script `artifacts/phase4-password-reset.sql`; mobil ignored `artifacts/phase4-android`, audit JSON dosyaları. Önceki başarısız loglar silinmedi.

Kalan sınırlar: geçmiş plan tutarı/para birimi/kategori snapshot'ı yok; analiz DueDate ayına göre, PaidAtUtc nakit akışı değildir. Inactive/yeniden düzenlenmiş planların mevcut projection semantiği korunur. Queue process restart'ta kaybolabilir; yeni istek cooldown sonrası gerekir. Eski access JWT kısa süresi dolana kadar geçerli kalabilir; refresh aileleri iptal edilir. Mevcut kullanıcı API'si durdurulmadı; yeni geliştirme migration/SMTP/LinkBase kurulumuyla kontrollü yeniden başlatma gerekir. Mailpit compose sağlandı fakat bu kabulde yerel SMTP transport test aracı kullanıldı. Production domain/App Links ve gerçek provider/inbox/telefon kabulü açık. Ayrıntılı dosya ve 14 maddelik rapor: [PHASE4_REPORT](PHASE4_REPORT.md); [kurulum](../../docs/PASSWORD_RESET.md), [analiz](../../docs/ANALYTICS.md).

**Duruş:** Phase 4 uygulama ve otomatik doğrulamaları tamamlandı. Fiziksel/provider kabulü bekliyor. Phase 5 başlatılmadı; kullanıcı onayı beklenir.

## Önceki Phase 3 kaydı — Premium Dashboard ve ödeme yönetimi

Tarih: **23 Ekim 2026**. Kaynak değişiklikleri yalnızca `mobile/` altında. Görev başlangıcında mevcut olan `src/PayAhead.Api/PayAhead.Api.csproj`, `appsettings.Development.json` değişiklikleri ve `production-operations-review.sql` dosyasına dokunulmadı. Backend kaynakları, production yapılandırması, migration veya mevcut kullanıcı verileri değiştirilmedi. Phase 4 başlatılmadı.

## Ön kabul — Phase 2

Geliştirme öncesi session/SecureStore/current-user testleri **24/24** geçti. Restore, logout/navigation guard, invalid access sonrası single-flight refresh ve refresh failure senaryoları doğrulandı. Bu fazda gerçek QueryClient ile logout/başka kullanıcı geçişinde finans verilerinin temizlendiği ve session epoch ayrımı da test edildi. Kullanıcı fiziksel Android'de gerçek kayıt/girişin çalıştığını bildirdi; kapat/aç, restore, donanım geri tuşu ve yeni finans ekranlarının fiziksel kabulü bu oturumda doğrulanmadı.

## Uygulama

- Gerçek Dashboard: backend saat dilimindeki aylık summary, para birimine göre ayrı upcoming/paid toplamları, aktif plan sayısı, aylık Pending/Paid/Skipped sayıları, en yakın ödeme ve yaklaşan ilk 3 kayıt. Gecikenler bu ayın başından düne kadar; aralık ve totalCount açıkça gösterilir.
- Ödeme listesi: sunucu sayfalaması, arama, aktif/pasif, ISO currency/category filtreleri; CreatedAtUtc descending sabit sunucu sırası. Eksik sayfalar tüm veri gibi gösterilmez.
- Plan create/detail/edit/delete/deactivate; silme ve pasifleştirmede native onay. Form gerçek DTO alanları, Zod/RHF kuralları, Once/Daily/Weekly/Monthly/Yearly, interval, start/end, anchor day, category, description ve isActive içerir.
- Decimal(19,4) JSON tokenları precision kaybetmeden okunur; canonical numeric JSON olarak gönderilir. Tarihler DateOnly; ay sonu/artık yıl ve interval hesapları backend recurrence semantiğiyle eşleşir. Planlanan tarih occurrence durumu olmadığı açık etiketlenir.
- Edit RowVersion snapshot; 409 sonrası açık reload, delete strong If-Match. Geçmiş/hatırlatma/abonelik bağlantıları frontend'den bypass edilmez.
- Entitlement yanıtındaki aktif plan limiti kullanılır; Free 5 sınırı canlı backend'de doğrulandı. Premium için sabit limit varsayımı/sahte satın alma yoktur.
- Finans key'leri userId + sessionEpoch ile ayrılır. Mutation başarıları yalnızca kendi scope'unu invalidate eder; logout eski veriyi temizler. Cancel ve epoch fencing korunur. Yazmadan önce gerekirse token yenilenir; yazmalar otomatik replay/retry edilmez. Belirsiz network/timeout sonucunda form açık kullanıcı teyidine kadar tekrar gönderimi engeller.
- Mevcut Inter/navy/teal, light/dark/system, ortak AppCard/AppInput/AppButton/AppScreen korundu. Dashboard/list pull-to-refresh; multiline isim/tutar ve scroll form düzeni kullanılır. Home/Payments gerçek veridir; preview bandı bu ekranlarda kullanılmaz.

## Doğrulama — gerçek sonuçlar

| Kontrol | Sonuç |
|---|---|
| TypeScript strict | Geçti; uygulama ve testler dahil. |
| ESLint | Geçti; sıfır hata/uyarı. |
| Jest / RNTL | **8 suite, 68/68 geçti, 0 atlandı.** Son çalıştırma exit 0; açık handle uyarısı yok. |
| Expo Doctor | **21/21 geçti.** |
| Expo install --check | Dependencies are up to date; SDK 57 / RN 0.86.3 / React 19.2.3 korundu. |
| Android Hermes export | Başarılı. APK veya fiziksel telefon testi değildir. |
| Expo startup | localhost:8085 başlatıldı; /status running, HTML HTTP 200, development JS bundle HTTP 200. |
| Gerçek auth smoke | **8 HTTP adımı geçti:** register/login/me/rotation/logout/revoked-refresh ve kendi test hesabının temizliği. |
| Gerçek finans smoke | **24 HTTP kontrolü +4 cleanup çağrısı geçti:** CRUD, pasifleştirme, pagination/filter, summary/upcoming, entitlement, 403, 409, cross-user404, yetkisiz401, decimal roundtrip. |
| .NET 8 restore | NuGet.Config sandbox erişim hatası izinli tekrar ile giderildi; restore başarılı. SDK 8.0.426 kullanıldı. |
| Backend build | Normal output çalışan PayAhead.Api dosya kilidine takıldı; servis durdurulmadı. Ayrı `artifacts/phase3-backend-check` output'unda **0 hata/0 uyarı** ile başarılı. |
| Backend unit | **379/379 geçti**, 0 atlandı. |
| Backend SQL integration | İlk tam tur **241/242 geçti**, tek hata özel artifact konumundan ops SQL dosyasının bulunamamasıydı. Çıktı kök `artifacts/phase3-backend-check` altına alınarak yalnızca o test **1/1 yeniden geçti**. Böylece 242 farklı senaryonun her biri başarıyla doğrulandı; ilk tam turun exit 1 olduğu gizlenmedi. |
| Fiziksel Android / görsel kabul | Bu oturumda telefon/emülatör ve kullanılabilir browser/app yüzeyi yok. Yeni ekranların native/görsel kabulü bekliyor. |

Jest kapsamı: gerçek DTO mapping ve numeric token koruma, Dashboard loading/empty/error/success, liste sayfalama, create/edit form mutation'ları, negatif/sıfır/precision/tarih/ISO/interval kuralları, 5 recurrence enum'u, 31 Ocak/29 Şubat/end-date/once/inactive, concurrency, silme onayı, Free limit hatası, farklı para birimleri, kendi scope'unda invalidation, kullanıcı değişiminde cache temizliği, 401 write replay yasağı, Android uyumlu TextInput/Switch etkileşimleri ve önceki auth/tema regresyonları. RNTL testleri native cihaz kanıtı olarak sayılmadı.

Gerçek smoke testleri yalnızca rastgele oluşturulan `mobile-smoke-*` / `finance-smoke-*` test hesaplarını kullandı. Mevcut hesaba/planlara dokunulmadı. Cleanup, doğrulanmış kendi kullanıcı kimliği ve kendi oluşturduğu parola ile yapıldı. Banka tahsilatı veya store satın alması yapılmadı. Backend SQL testleri kendi izole fixture veritabanlarını kullanır; geliştirme veritabanı test için temizlenmez.

## Dosyalar

Yeni `src/features/payments/`: api, contracts, currencies, dates, money, validation, queries, components, PaymentFormScreen ve PaymentDetailScreen. Yeni `src/app/edit-payment.tsx`; gerçek HomeScreen/PaymentsScreen ve add-payment/payment route bağlantıları. Ortak AppScreen refresh, scoped preview banner, foreground invalidation, session write hazırlığı, ProblemDetails code/409 mapping ve DateOnly biçimlendirmesi güncellendi. `scripts/finance-smoke.cjs`, dört yeni test/fixture dosyası ve session testleri eklendi/genişletildi. README, ARCHITECTURE, DEVELOPMENT, ANDROID_TESTING ve bu rapor güncellendi; PAYMENTS_API.md eklendi. Paket kurulmadı, native klasör oluşturulmadı. Build/test çıktıları ignore edilen artifact klasörlerindedir.

## Güncel npm güvenlik değerlendirmesi

23 Ekim registry denetimi: **7 kayıt (4 high, 3 moderate, 0 critical)**; `--omit=dev` de 7. Önceki 58 kayıt ile fark, bu fazda düzeltilmiş açık sayısı olarak raporlanmaz: package-lock/paket sürümleri değiştirilmedi, güncel advisory çıktısı değişti.

- High: node-forge → @expo/code-signing-certificates → @expo/cli → expo. Kök [RSA verification advisory](https://github.com/advisories/GHSA-86w9-cpqp-85rv); registry latest node-forge 1.4.0 hâlâ raporlanan aralıkta. Bu CLI/sertifika araç zinciridir; uygulama kendi RSA algoritmasını yazmaz.
- Moderate: decode-uri-component → query-string → expo-router. [Malformed query decoding DoS](https://github.com/advisories/GHSA-vcc3-ghjq-m6fr) runtime/deep-link değerlendirmesi gerektirir; sadece geliştirme aracı olarak yok sayılmaz.
- Doğrudan manifest kayıtları expo/expo-router; kök açıklar transitive'dir. `--omit=dev` sonuçları cihaz bundle'ındaki çalışma yüzeyiyle birebir aynı değildir; Expo CLI production manifest zincirinde de bulunur.
- Audit'in Expo 44 downgrade / Router 58 major önerileri SDK 57/Expo Go uyumunu bozacağından uygulanmadı. Kör `--force`, legacy-peer-deps veya doğrulanmamış CJS→ESM override yoktur. Upstream SDK uyumlu düzeltme ve yeniden audit release öncesi gereklidir.

## Açık sınırlar ve Phase 4

1. Fiziksel Android'de yeni CRUD/refresh/klavye/safe-area/küçük ekran/geri tuşu/tema kabul matrisi bekliyor; ANDROID_TESTING.md.
2. Gecikenler tüm geçmiş değil **bu ay**; yaklaşan liste belirtilen aralığın ilk 3 kaydı. Backend pending-total alanı sağlamadığından tüm ayın gecikmiş+yaklaşan tutarı uydurulmadı.
3. Planlanan tarih schedule metadata'sıdır; Paid/Skipped kayıtlarını çıkaran gerçek Pending listesi değildir. Bu ayrım UI ve PAYMENTS_API.md içinde belirtilir.
4. Calendar/Subscriptions ve occurrence durum mutation'ları bu fazın dışında kaldı. Premium bağlantısı tanıtım ekranıdır; satın alma yoktur.
5. Kalan 7 npm kaydı release riskidir. Normal backend output dosyaları çalışan API nedeniyle kilitli kalabilir; ayrı artifact output doğrulandı, servis durdurulmadı.
6. Phase 4 için typed API/query/precision/CRUD temeli hazırdır; otomatik geçilmedi. Bu teslim sonunda durulur.

---

# Önceki fazların tarihsel kayıtları

Aşağıdaki Phase 1/2 ifadeleri önceki tarihlerdeki durumu anlatır; güncel sonuç yukarıdaki Phase 3 raporudur.

# Frontend Phase 2 — Authentication & Session Management

Tarih: 21 Ekim 2026. Yalnızca mevcut `mobile/` projesi değişti; backend kodu ve verileri değiştirilmedi. Phase 3 başlatılmadı. Kullanıcı Phase 1 tasarımını fiziksel Android'de beğendiğini bildirdi; bu oturumda yeni auth akışının fiziksel cihaz testi yapılmadı.

## Uygulananlar

- Gerçek kaynak sözleşmesi: POST `/api/auth/register` (201 UserResponse), `/login` ve `/refresh` (AuthResponse), `/logout` (204), GET `/api/users/me` (UserResponse). Register otomatik giriş yapmaz.
- Premium Login/Register tasarımı korunarak RHF/Zod doğrulaması, server field errors, generic invalid credentials, network/timeout, loading/disabled CTA ve eşzamanlı submit koruması eklendi. Login şifresi zorunlu/128 max; kayıt 12–128. Parola trim edilmez.
- Native SecureStore'da tek atomik JSON token çifti + UTC expiry; Zustand yalnızca public status/user/notice. Şifre/token loglanmaz, AsyncStorage/localStorage kullanılmaz. Web güvenli kalıcı oturum desteklemez.
- Single-flight rotating refresh; geç eski 401, ikinci 401, request cancellation ve logout/refresh yarışı için epoch fencing. Public auth instance recursive interceptor kullanmaz. Rotation timeout'u otomatik retry edilmez.
- Restore boyunca navigation bekler; login/restore `/me` doğrulamasından sonra protected state açılır. `Stack.Protected` auth ve tüm tab/detail/modal history'sini sınırlar. Logout cache/token temizler; sunucu iptali belirsizse notice gösterir.
- `/me` TanStack Query ile bağlı; Settings gerçek kullanıcı bilgisi gösterir. Ön plana dönüşte current user tekrar doğrulanır. Pasif/silinmiş/geçersiz hesap yanıtı oturumu kapatır.
- Development preview auth guard'ı aşamaz; varsayılan flag false. Finans ekranları Phase 3'e kadar bağlı olmadığını açıkça gösterir. Premium/ödeme işlemleri eklenmedi.
- Yerel `.env` etkin Wi-Fi LAN IP'si `192.168.1.155:5080` ile hazırlandı; Git tarafından ignore ediliyor. DHCP değişebilir. Kestrel binding, dar firewall kuralı, HTTP/TLS ve Expo Go adımları DEVELOPMENT.md içinde.

## Doğrulama sonuçları

| Kontrol | Gerçek sonuç |
|---|---|
| TypeScript strict | Geçti; uygulama ve test dosyaları dahil. |
| ESLint | Geçti; sıfır hata/uyarı. |
| Jest/RNTL | **5 suite, 45/45 geçti, 0 atlandı.** Mock/service/component testleri. |
| Açık test handle kontrolü | `--detectOpenHandles` geçti; query testindeki GC timer/unmount düzenlendi, son standart test süreci exit 0. |
| Expo Doctor | **21/21 geçti.** |
| Expo paket uyumu | Dependencies are up to date. Expo/RN/React SDK sürümleri korundu. |
| Android export | Metro + Hermes bytecode başarılı; native APK/telefon testi değildir. |
| Expo startup | localhost:8082 başlatıldı; `/status` running; uygulama HTML HTTP 200; development web JS bundle HTTP 200. |
| Gerçek backend auth smoke | **Başarısız/engelli:** ilk POST `/api/auth/register` bağlantı kuramadı; exit 1. Register/login/me/refresh/logout gerçek başarı olarak sayılmadı. |
| Backend ortamı | localhost 5080/5000 health erişilemiyor; Docker PATH'te ve standart Docker Desktop binary yolunda bulunamadı. SQL connection env'i yok. Backend veya SQL yeniden kurulmadı/değiştirilmedi. |
| Fiziksel Android / native geri tuşu | Bu oturumda erişim yok; test edilmiş sayılmadı. |

45 test; login/register validation, başarılı login ve current-user admission, invalid credentials, server validation gösterimi, UI/service çift gönderim koruması, SecureStore çift yazımı/okuma/bozuk kayıt/silme fallback/web yasağı, restore/expired token, refresh success/failure, concurrent ve geç 401 single-flight, ikinci 401 loop engeli, cancellation, logout-refresh yarışı, offline logout, current user query, protected navigation policy, HTTPS/harici origin sınırı, timeout, ProblemDetails ve Phase 1 görsel state regresyonlarını kapsar. Native animasyon/Android back fiziksel test değildir. Token/endpoint test fixture'ları yalnızca test dosyalarındadır; uygulama sahte kullanıcı ile oturum açmaz.

Gerçek test komutu: `npm.cmd run test:auth:live`. Script yalnızca LAN/localhost geliştirme API'sine gider; rastgele geçici hesap, register/login/me/rotation/logout ve revoked-refresh kontrolü yapar; yalnızca kendi oluşturduğu hesabı temizler. Gerçek backend hazır olmadan otomatik kontrollerin geçmesi gerçek entegrasyon kanıtı sayılmaz.

## npm audit — direct/transitive ve etki

Güncel registry üzerinden `npm audit`, `npm audit --omit=dev`, `npm outdated` ve kök paket sürümleri incelendi. Toplam **58 kayıt: 50 high, 8 moderate, 0 critical**. Dev dependency hariç **53 kayıt: 45 high, 8 moderate**. Önceki fazdaki toplam değişmedi. Bu sayılar bağımlılık zinciri üzerinden yayılan kayıtları da içerir; 58 ayrı kök açık değildir.

| Kök transitive paket | Severity / etki |
|---|---|
| [braces](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm) | High; micromatch/Jest/Metro/build girdileri üzerinden DoS. Registry latest 3.0.3 hâlâ etkilenen aralıkta. |
| [node-forge](https://github.com/advisories/GHSA-86w9-cpqp-85rv) | High; Expo CLI/code-signing sertifika araç zinciri RSA doğrulama bildirimi. Registry latest 1.4.0 etkilenen aralıkta. Mobil auth kendi RSA doğrulamasını yapmaz. |
| [decode-uri-component](https://github.com/advisories/GHSA-vcc3-ghjq-m6fr) | Moderate; Router → query-string → decoder. Bozuk percent-encoded query runtime DoS riski; bu yalnızca test aracı sayılmaz. |
| [sprintf-js](https://github.com/advisories/GHSA-hp3w-g68c-fv3c) | Moderate; Jest/coverage/js-yaml/argparse araç zincirinde precision DoS; latest 1.1.3 etkilenen aralıkta. |

Doğrudan manifest bağımlılıkları içinde audit tarafından zincir etkisiyle işaretlenenler: Expo, Expo Router, React Native, Reanimated, Worklets; dev tarafında Jest, jest-expo, @types/jest ve RNTL. Auth'ın doğrudan Axios/TanStack Query/Zustand/RHF/Zod/SecureStore paketleri bu çıktıda doğrudan kök advisory kaynağı olarak raporlanmadı. Bu durum platform veya diğer tüm güvenlik risklerinin kapandığı anlamına gelmez.

`--omit=dev` Node kurulum ağacını sınıflandırır: Expo CLI bazı production manifest zincirlerinde bulunduğundan 53 kaydın tamamı cihaz JS runtime açığı değildir. Router query decoder ise runtime değerlendirmesi gerektirir. Metro/CLI açıklarını release binary'de doğrudan çalışıyor varsaymadık; bunları yok saymadık.

Mevcut SDK 57 manifest aralıklarında `npm outdated` wanted sürümleri current ile aynı. Güvenli, uyumlu yayınlanmış patch belirlenemedi; bu yüzden paket sürümleri değiştirilmedi. Üç kök paketin latest sürümü hâlâ etkilenen aralıkta. Decoder 0.5.0 ESM, query-string 7 eski CJS kullanımına doğrulanmadan override edilmedi; registry'de 0.4.3 yok. Audit'in Expo 44/RN 0.72 downgrade veya Expo/Router/Jest major yükseltme önerileri SDK/Expo Go uyumunu bozacağı için uygulanmadı. `npm audit fix --force`, legacy-peer-deps veya güvenlik uyarısını susturan override yok.

Kalan bağımlılık riskleri release öncesi upstream uyumlu düzeltme ve yeniden audit gerektirir. Bu görev production dağıtımı yapmadı.

## Açık kabul / Phase 3 sınırı

1. Çalışan gerçek SQL/backend ile live auth smoke yeniden çalıştırılmalı; bu oturumda başarı doğrulanamadı.
2. Phase 2 fiziksel Android matrisi (restore, secure storage, klavye, geri/deep link, offline logout, tema) ANDROID_TESTING.md içinde bekliyor.
3. Offline veya native storage hatası politikası fail-closed; disk silme ve overwrite ikisi de başarısızsa kalıcı silme garanti edilemez, UI bunu bildirir. Sunucu logout hatasında refresh ailesi sunucuda geçerli kalabilir; access JWT başarılı logout'tan sonra da kısa ömrü + skew süresince geçerli olabilir.
4. Single-flight bu uygulamanın tek JS runtime'ı içindir; aynı SecureStore kaydını eşzamanlı yöneten birden fazla runtime/process desteklenmez. Web oturumu yoktur.
5. Phase 3 başlatılmadı. Finans Query ekran bağlantıları, mutation idempotency, kesin decimal sunumu ve Premium/notification entegrasyonu uygulanmadı.

---

# Phase 1 tarihsel kayıt

Aşağıdaki bölüm önceki teslimin kaydıdır; güncel durum yukarıdaki Phase 2 raporudur. Özellikle “Phase 2 başlatılmadı” cümleleri önceki faza aittir.

# Frontend Phase 1 ilerleme raporu

Tarih: 21 Ekim 2026. Kapsam yalnızca `mobile/`. Backend Phase 1–10 dosyaları değiştirilmedi. Phase 2'ye geçilmedi.

## Uygulananlar

- Expo SDK 57 / React Native 0.86.3 / React 19.2.3 / strict TypeScript 6.0.3; npm kilidi ve özgün marka varlıkları.
- Light/dark/sistem teması, Inter, merkezi tokenlar, 16 talep edilen ortak UI bileşeni ve kategori ikonları.
- Dashboard, Payments, Calendar, Subscriptions, Settings; ayrı Login/Register; yardımcı detay, önizleme, bilgilendirme ve Premium ekranları.
- Beş sekme, safe-area, klavye için scroll/resize, hafif reduced-motion uyumlu animasyonlar.
- Açık etiketli development preview ve ayrı empty/loading/error görünümleri. Gerçek giriş, ödeme ve satın alma taklidi yok.
- TanStack Query, Axios, Zustand, RHF/Zod ve native SecureStore abstraction; ekranlardan gerçek API çağrısı yok.
- İstenen altı mobil belge ve 16 otomatik test.

## Gerçek komut sonuçları

| Kontrol | Sonuç |
|---|---|
| Node / npm | 24.22.0 / 11.19.1 |
| npm install | Başarılı; resolver peer uyumsuzluğu sürüm 5.2.2 seçilerek giderildi. |
| TypeScript | Geçti, strict; testler dahil. |
| ESLint | Geçti, sıfır hata/uyarı. |
| Jest / RNTL | 2 suite, 16 geçti, 0 atlandı. Gerçek API/native test değildir. |
| Expo paket uyumu | `expo install --check`: Dependencies are up to date. |
| Expo Doctor | 21/21 geçti. |
| Expo public config | Başarılı; SDK 57, Android/iOS/web, doğru plugin/asset yolları. |
| Android export | Metro ve Hermes bytecode üretimi başarılı. İlk sandbox izin hatası izinli tekrar ile giderildi. APK oluşturulmadı. |
| Web export | Production bundle başarılı. |
| Metro başlangıç | localhost:8081 çalıştı; `/status` packager-status:running, uygulama HTML HTTP 200. |
| DevTools | dotslash cache yazma izni reddedildi; fallback bildirimi var. DevTools indirmesi doğrulanmadı. |
| UI görsel kabul | Çalıştırılamadı: kullanılabilir browser inventory boş. Screenshot/taşma/kontrast kontrolü doğrulanmadı. |
| Android telefon/emülatör | Erişim yok; test edilmiş sayılmadı. |

RNTL testleri örnek ve boş dashboard'u, yükleme/hata-retry'ı, form doğrulamayı ve sahte oturum oluşmamasını, ödeme arama/filtre/geçmişini, takvim ay geçişini, tema state'ini, boş abonelikleri kapsar. Ek testler önizlemenin production'da kapanmasını, ayrı döviz toplamlarını, Türkçe tarih/tutar biçimini ve hazırlanmış Axios davranışını doğrular. Testlerde native animasyonlar mock olduğundan animasyon performansı ölçülmedi.

## Bağımlılık güvenliği — açık

Kurulum sonrası `npm audit --json` toplam **58 etkilenen paket kaydı** verdi: 50 high, 8 moderate, 0 critical. Zincirler aynı dört kök bildirimi üst paketlere taşır. Bu kayıtların tamamını bağımsız uygulama açığı olarak yorumlamak doğru değildir; yine de sıfır açık veya production-ready raporu verilemez.

| Kaynak paket | Zincir / durum |
|---|---|
| [braces](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm) | Jest/micromatch ve araç zincirinde stack-exhaustion DoS; registry kontrolünde latest 3.0.3 etkilenen aralıkta. |
| [decode-uri-component](https://github.com/advisories/GHSA-vcc3-ghjq-m6fr) | Router → query-string 7 → 0.2.2; bozuk query percent encoding DoS. Latest 0.5.0 ESM; eski CJS bağımlılığına körlemesine override uygulanmadı. |
| [node-forge](https://github.com/advisories/GHSA-86w9-cpqp-85rv) | Expo CLI/code-signing zincirinde RSA doğrulama bildirimi; registry latest 1.4.0 etkilenen aralıkta. Uygulama kendi kriptografisini yazmaz. |
| [sprintf-js](https://github.com/advisories/GHSA-hp3w-g68c-fv3c) | Jest coverage/js-yaml/argparse araç zincirinde precision DoS; registry latest 1.1.3 etkilenen aralıkta. |

Güvensiz `--force`, `legacy-peer-deps`, SDK downgrade veya doğrulanmamış transitive override kullanılmadı. Özellikle Router query decoder için uyumlu upstream düzeltme ve yeniden audit gereklidir. Yerel smoke test loopback üzerinde, gerçek kullanıcı verisi olmadan yapıldı. Güvenlik kayıtları ve fiziksel/görsel kabul kapanmadan mağaza yayınına hazır kabul edilmemeli.

## Kalan kabul ve sonraki faz sınırı

Fiziksel Android: Expo Go açılışı, küçük ekran/font ölçeği, safe-area, TalkBack, klavye, geri tuşu, dark-mode kontrastı ve animasyonlar bekliyor. Takvim sütunları dar ekranlarda yatayda 48 dp hedefinin altında kalabilir; native kullanım kabulü gerekli. Detaylı matris ANDROID_TESTING.md içinde.

Phase 2 için hazır altyapı: typed API client, gerçek DTO başlangıç tipleri, SecureStore ve QueryClient. Gerçek login/register, refresh/logout, oturum korumalı route, server validation ve ekranları gerçek DTO verisine bağlama **uygulanmadı**. Kesin decimal aritmetiği ve form kurallarının tamamının backend ile eşleştirilmesi o entegrasyonun parçasıdır. Bu teslim Phase 1 kodunu ve yapılabilen doğrulamaları kapsar; görsel/native kabul ve upstream bağımlılık güvenliği açık kalır.
