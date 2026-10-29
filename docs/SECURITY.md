# Authentication ve güvenlik kararları — Faz 6–10

## Mobil Phase 5.4 ekleri

[Parola kotası, geçmiş ve hukuki taslak teknik sözleşmesi](PRIVACY_SECURITY.md). Normal değişim son 30 günde en fazla 3 başarıdır; recovery bu kotaya dahil değildir. Geçmiş hash doğrulaması normal/resette, kullanıcı SQL kilidi ve aynı transaction altında yapılır. Mevcut salted Identity hash algoritması, credentials rate limiti ve access/refresh invalidation korunur. Yeni security summary parola/cihaz kimliği döndürmez. Geçmiş aktif hesap ömrünce tutulur, hesap silmede temizlenir; eski normal zaman etiketleri bakımda temizlenir. Küçük yerel yaygın parola listesi kapsamlı breach taraması veya NIST uygunluğu değildir.

## Faz 10 ekleri

Production/Staging TLS doğrulayan SA olmayan SQL, açık AllowedHosts ve güçlü dış JWT key ile fail-fast; fake push/etkin resmi olmayan Expo URL reddi. CORS exact HTTPS allowlist, credentials yok. Genel1MiB ve privacy4096byte sınırı, güvenlik başlıkları ve doğrulanmış CorrelationId eklendi. SQL rate limit tüm instance'larda ortak, kesinti sensitive route503 fail-closed. İki API bütçesi/40bağlantı yarışı, export izolasyonu, reauth deletion/rollback, cleanup ve eski JWT/replay/IDOR testleri vardır. Ayrıntılar [runbook](OPERATIONS_RUNBOOK.md), [privacy](DATA_PRIVACY.md), [deployment](DEPLOYMENT.md).

## Faz 9 — Premium güvenlik sınırı

Premium yalnızca sunucuda doğrulanmış UserSubscription + süre + tazelikten hesaplanır. TrackedSubscription, legacy Active veya istemcinin isPremium alanı hak vermez. Verify/restore JWT ve aktif DB kullanıcısı ister; kullanıcı/IP purchase rate limit 10/dakikadır. Satın alma sahibi store hesabının appAccountToken/obfuscatedAccountId alanıyla ve SQL immutable unique transaction/token binding ile doğrulanır. Hesap aktarımı yoktur. Yeni aktif kaynaklar aynı kullanıcı SQL kilidi altında sayılır; eşzamanlı API instance'ları Free sınırını geçemez.

Apple ES256/pin'li güvenilir CA zinciri + online revocation + uygulama/environment/ürün/hesap kontrolü ve güncel Server API; Google service account OAuth + subscriptionsv2 package/product/account/state kontrolü kullanılır. Apple V2 webhook JWS, Google Pub/Sub OIDC RS256/JWKS, issuer/audience/email/verified/lifetime ve payload package/subscription kontrolünden geçer. Event ID unique inbox replay'i bastırır; event state doğrudan uygulanmaz, mağaza yeniden sorgulanır.

Purchase evidence ve inbox referansları dış 32-byte AES-GCM keyring ile şifrelidir; hash'ler unique binding içindir. Ham token, credential, request body ve Store HTTP URL loglanmaz. Proxy/APM için ayrıca redaction gerekir. Encryption key rotasyonu eski key ID'leri korunarak yapılır; kayıp key veya erişilemeyen mağaza fake/unencrypted fallback oluşturmaz. İşlem/gövde/cevap/teknik kayıt sayısı sınırları [STORE_INTEGRATION.md](STORE_INTEGRATION.md) içindedir.

Shipping API test assembly'lerine referans vermez ve `Stores:UseFake=true` ile her environment'ta başlamaz. Ayrı test host yalnızca Testing, izole PayAheadSmoke_ DB, HMAC key ve loopback ile açılır; Production başlangıcı reddedildi. Gerçek Apple/Google credentials, mağaza IAM ve physical sandbox purchase doğrulanmadı. Tazelik penceresi içindeki mağaza kesintisi ve bildirim gecikmesi anında refund garantisi vermez; en fazla yapılandırılmış 24 saatten sonra erişim fail-closed olur. Varsayılan mağazalar/worker kapalıdır; üretim için kurulumu tamamlayın.

## Kimlik ve parola

Register otomatik giriş yapmaz; kullanıcı ve varsayılan UserSettings aynı SQL transaction içinde oluşturulur. Email Trim/ToUpperInvariant ile normalize edilir; NormalizedEmail unique index eşzamanlı kayıtta tek kazanan sağlar. Duplicate 409 tercihi kayıt uçunda email varlığının çıkarılabilmesine izin verir. Login yanlış email/parola, pasif veya kilitli hesap için aynı genel 401 döner.

Parola 12–128 karakter, trim edilmez; uzun passphrase kabul edilir. Identity PasswordHasher V3 (PBKDF2, 210.000 iteration, kütüphane tarafından üretilen salt) kullanılır. SuccessRehashNeeded halinde hash yenilenir. Bilinmeyen hesap için dummy hash verification yapılır; uç için tam sabit zaman garantisi yoktur. Eski geliştirme hash işaretleriyle giriş yapılamaz. Parola, hash ve token loglanmaz; hassas EF logging kapalıdır.

## JWT

HS256; imza, izinli algoritma, issuer, audience ve lifetime zorunlu doğrulanır. Varsayılan access ömrü 15 dakika (yapılandırılabilir 1–30), clock skew 15 saniye. Claim: sub (UserId), iss, aud, iat, nbf, exp, jti; ad/email/parola içermez. Her doğrulanmış JWT için SQL üzerinde mevcut aktif kullanıcı kontrol edilir; cache yoktur, istek başına ek 1 sorgu gerekir. Diğer kullanıcı kaynakları 404 döner; UserId istemci girdisinden alınmaz.

Jwt:SigningKey Base64 ile kodlanmış en az 32 rastgele byte olmalıdır. Eksik, bozuk veya belirgin zayıf anahtar Production/Staging başlangıcını durdurur; uzunluk/çeşitlilik kontrolü gerçek entropi ölçümü değildir, anahtar mutlaka CSPRNG ile üretilmelidir. Development içinde anahtar verilmezse süreç ömrüyle sınırlı 64 rastgele byte oluşturulur; yeniden başlatma önceki access token imzalarını geçersiz kılar. Kalıcı yerel denemelerde de dışarıdan anahtar verin. Jwt:Issuer varsayılan PayAhead, Jwt:Audience PayAhead.Mobile. Üretimde secret manager/environment ile sağlayın, repoya veya paylaşılan komut çıktısına koymayın. Anahtar değişimi bütün access token imzalarını etkiler; bu fazda çok anahtarlı kid/key ring yoktur.

## Refresh, reuse ve logout

48 CSPRNG byte → 64 base64url karakter; SQL yalnızca SHA-256 hex hash tutar. Her login yeni FamilyId açar. Refresh eski token için RevokedAtUtc/ReplacedByTokenId yazar ve aynı ailede yeni token üretir. Varsayılan 30 gün (1–90 yapılandırılabilir), her rotasyon yeni kayan süre açar; mutlak aile ömrü yoktur.

SQL transaction önce kullanıcı satırında UPDLOCK/HOLDLOCK alır, token durumunu tekrar okur, child insert ve parent update işlemlerini atomik commit eder. Bütün API örnekleri aynı kullanıcıda bu kilidi paylaşır; farklı cihaz yenilemeleri de kısa süre sıraya girer. Retry stratejisi transaction bütününü kapsar; belirsiz commit durumunda güvenlik lehine yeniden giriş gerekebilir.

İptal edilmiş token tekrar kullanılırsa ailedeki bütün aktif refresh token üyeleri iptal edilir. Expired token da ailesini iptal eder. İptal commit edildikten sonra servis 401 üretir. Aynı refresh token ile yarışta biri 200, diğeri 401 alır; ikinci istek reuse olduğundan kazananın child refresh token değeri de iptal edilir. İstemci refresh işlemlerini tekilleştirmelidir. Ağ hatasında kör tekrar oturumu kapatabilir; refresh idempotent değildir.

Logout refresh token sahipliğine dayanır, access JWT istemez. Eski aile üyesiyle logout güncel üyeyi de iptal eder. Tekrarlanan/bilinmeyen doğru biçimli token logout204; yanlış biçim400. Ayrı login ailesi etkilenmez. **Logout/reuse access JWT'yi anında iptal etmez:** kısa ömrü+skew boyunca kullanılabilir. Pasif/silinmiş kullanıcı SQL kontrolüyle engellenir. Faz10 bounded maintenance retention120gün ve aile bütünlüğüyle reuse geçmişini koruyarak eski hash'leri temizler; aktif aile silinmez. Oturum UI yoktur.

## Brute force, proxy ve dağıtım

Auth ortak IP bütçesi20/dakika, kuyruk yok,429 ProblemDetails/Retry-After60. Faz10 SQL distributed sayaç ve transaction application lock tüm instance'larda bütçeyi korur; local limiter ek koruma. Beş yanlış parola SQL15dk hesap kilidi; doğru parola kilit süresinde401. Başarılı login sayacı sıfırlar. DDoS/counter cardinality ve hesap kilitleme saldırıları için edge WAF/izleme gerekir.

TrustedProxies açık IP allowlist yoksa forwarded header işlenmez. ForwardLimit1 ve yalnızca izinli proxy için X-Forwarded-For/Proto; rastgele client header sınırı aşamaz. Production HTTPS/HSTS, iç health probe redirect dışında. CORS boş varsayılan exact HTTPS allowlist; native istemci için zorunlu değildir. Auth/me/export no-store; body/query/token loglanmaz.

DevelopmentIdentity shipping API içinde authentication şeması değildir; Development bayrağı kimlik sağlamaz, yanlış JWT ile erişim açılamaz. Eski bayrak Development dışında açıksa başlangıç reddedilir. Regresyonlar test assembly içindeki özel handler kullanır; yeni auth testleri gerçek JWT ile çalışır.

## Mobil istemci

React Native için iOS Keychain / Android Keystore destekli güvenli cihaz deposu kullanın; AsyncStorage, log veya analytics içinde token tutmayın. Access token Authorization: Bearer başlığında gönderilir. Refresh tek bir işlem olarak çalışmalı, dönen iki token birlikte güvenli kaydedilmelidir. Refresh 401 sonrası yerel oturumu temizleyip login isteyin. Logout sonrasında yerel değerleri silin. Frontend kodu bu fazda eklenmedi.

Kaynaklar: [Microsoft JWT Bearer](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-8.0), [rate limiting](https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-8.0).

HTTPS yönlendirme hedefi varsayılan 443 olarak açıkça ayarlıdır; farklı TLS portu için HttpsPort kullanılır. Production HTTP→HTTPS yönlendirmesi gerçek API host testiyle kontrol edilir. Forwarded proto ancak izinli proxy üzerinden kabul edilir.

## Faz 7 — Push güvenlik sınırları

Reminder/device/settings uçları gerçek JWT + aktif kullanıcı kontrolü ve server-owned UserId kullanır. Push token endpoint response veya loglarda görünmez; DELETE token alanını scrub eder. Aynı token başka hesaba otomatik taşınmaz. Provider secret yalnızca güvenilir dış konfigürasyondadır; enabled worker eksik credential ile başlamaz, disabled adapter sahte başarı döndürmez. Payload yalnızca genel hatırlatma ve opaque notificationId taşır; ödeme tutarı/adı/email içermez.

Logout ile device arasında otomatik ilişki yoktur: mobil istemci logout öncesi device DELETE yapmalıdır; offline logout kalıcı aboneliği tek başına kapatmaz. NotificationsEnabled=false genel kapatma sağlar. DB tokenları gönderim için geri okunabilir olmalıdır; en az yetki/TLS/TDE ve şifreli backup deployment sorumluluğudur. Queue completion lease fence uygular; dış provider yarışları nedeniyle tam exactly-once yoktur. Token/notification retention ve operasyonel sınırlar [NOTIFICATIONS.md](NOTIFICATIONS.md) içindedir.
