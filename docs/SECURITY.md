# Faz 6 — Authentication ve güvenlik kararları

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

Logout refresh token sahipliğine dayanır, access JWT istemez. Eski aile üyesiyle logout güncel üyeyi de iptal eder. Tekrarlanan/bilinmeyen doğru biçimli token logout 204 döner; yanlış biçim 400. Ayrı login ailesi etkilenmez. **Logout/reuse access token değerini anında iptal etmez:** geçerli JWT kısa ömrü + skew boyunca kullanılabilir. Kullanıcı pasif/silinmiş ise her istekte SQL kontrolü hemen engeller. Otomatik token temizliği ve oturum yönetimi UI bu fazda yoktur; hash satırları reuse tespiti için tutulur.

## Brute force, proxy ve dağıtım

Auth uçları ortak IP bütçesi: 20 istek/dakika, kuyruk yok, 429 ProblemDetails ve Retry-After: 60. Beş yanlış parola SQL üzerinde 15 dakikalık hesap kilidi yaratır; doğru parola kilit süresince de 401 alır. Başarılı login sayacı sıfırlar. Hız sınırı süreç başınadır; yatay ölçekli dağıtımda edge/distributed limit gerekir. SQL hesap kilidi örnekler arasında ortaktır; hesap kilitleme saldırıları için edge izleme/koruma gereklidir.

TrustedProxies:0 vb. ile açık IP allowlist verilmedikçe forwarded header işlenmez. Verildiğinde KnownNetworks boş, ForwardLimit=1, yalnızca izinli proxy için X-Forwarded-For/Proto işlenir. Rastgele istemci başlıkları IP sınırını aşamaz. Production HTTPS redirection ve HSTS aktiftir; doğrudan TLS veya güvenilir TLS terminasyonu gerekir. CORS açılmadı; native istemci için gerekmez. Auth ve /me yanıtları no-store/no-cache taşır. İstek gövdeleri/query/token loglanmaz.

DevelopmentIdentity shipping API içinde authentication şeması değildir; Development bayrağı kimlik sağlamaz, yanlış JWT ile erişim açılamaz. Eski bayrak Development dışında açıksa başlangıç reddedilir. Regresyonlar test assembly içindeki özel handler kullanır; yeni auth testleri gerçek JWT ile çalışır.

## Mobil istemci

React Native için iOS Keychain / Android Keystore destekli güvenli cihaz deposu kullanın; AsyncStorage, log veya analytics içinde token tutmayın. Access token Authorization: Bearer başlığında gönderilir. Refresh tek bir işlem olarak çalışmalı, dönen iki token birlikte güvenli kaydedilmelidir. Refresh 401 sonrası yerel oturumu temizleyip login isteyin. Logout sonrasında yerel değerleri silin. Frontend kodu bu fazda eklenmedi.

Kaynaklar: [Microsoft JWT Bearer](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-8.0), [rate limiting](https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-8.0).

HTTPS yönlendirme hedefi varsayılan 443 olarak açıkça ayarlıdır; farklı TLS portu için HttpsPort kullanılır. Production HTTP→HTTPS yönlendirmesi gerçek API host testiyle kontrol edilir. Forwarded proto ancak izinli proxy üzerinden kabul edilir.
