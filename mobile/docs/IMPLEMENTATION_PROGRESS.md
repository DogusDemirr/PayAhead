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
