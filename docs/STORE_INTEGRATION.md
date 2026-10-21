# Apple / Google mağaza entegrasyonu

Faz10 webhook ingress ortak SQL60/dakika/IP, verify/restore10/dakika/user; outage503 fail-closed. Worker health/metrics ve terminal inbox için guard'lı `ops/retry-webhook.sql` manuel retry eklendi. Lease/poison/retry SQL testleri ve unbound inbox load gerçek provider çağrısı değildir. Silinmiş hesabın evidence'i kaldırılır, yeniden provider sorgusu yapılmaz; geç gelen event retention'ı privacy incelemesine tabidir. [Runbook](OPERATIONS_RUNBOOK.md), [privacy](DATA_PRIVACY.md), [checklist](PRODUCTION_CHECKLIST.md).

Faz 9 adapter'ları Infrastructure katmanındadır. Varsayılan tüm mağaza bayrakları ve worker kapalıdır. Credential yokken Premium verme veya otomatik fake fallback yoktur. **Canlı Apple/Google veya gerçek sandbox satın alması bu makinede doğrulanmadı.** SQL ve HTTP testleri kontrollü test sağlayıcısı kullanır; Google OIDC imza testleri yerel RSA/JWKS ile çalışır.

## Apple

StoreKit transaction JWS önce ES256, x5c zinciri ve Apple leaf/intermediate OID'leriyle doğrulanır. Yalnızca Apple Root CA G3 SHA256 pin'i kabul edilir. İşletim sistemi CustomRootTrust zinciri, ardından online revocation kontrolü kullanılır; sertifika indirme kapalıdır, bilinmeyen/iptal kontrolü başarısız zincir reddedilir. Yalnızca güvenilir Apple zinciri online revocation sorgusuna gider. `signedDate` gelecekte olamaz (5 dakika tolerans).

Bundle/environment/product ve hesap bağı kontrol edilir. Ardından App Store Server API `GET /inApps/v1/subscriptions/{originalTransactionId}` ile güncel durum alınır; response ve signedTransactionInfo/signedRenewalInfo tekrar doğrulanır. Transaction ID denetim kaydı, original transaction üyelik ailesidir. AppAccountToken oturumdaki UserId ile eşleşmelidir. Production appAppleId eşleşir. Yenileme, ürün değişimi, grace, retry, period expiry ve revocation bu güncel cevaptan türetilir. İptal, AutoRenew=false olarak ücretli süre sonuna kadar erişimi korur.

Resmi dayanaklar: [App Store Server API](https://developer.apple.com/documentation/appstoreserverapi), [Apple imzalı veri doğrulayıcısı](https://github.com/apple/app-store-server-library-python/blob/main/appstoreserverlibrary/signed_data_verifier.py), [Apple sertifika otoritesi](https://www.apple.com/certificateauthority/). Eski verifyReceipt varsayılan değildir.

Kaynak ağacındaki `Certificates/AppleRootCA-G3.cer` herkese açık sertifikadır; private key değildir. SHA256: `63343ABFB89A6A03EBB57E9B3F5FA7BE7C4F5C756F3017B3A8C488C3653E9179`. DER dosyasının base64'ü konfigürasyona verilir; farklı root kabul edilmez. Apple root rotasyonu kontrollü kod/config güncellemesi gerektirir. Üretim sunucusundan Apple API ve OCSP/CRL erişimi ayrıca doğrulanmalıdır.

## Google

Service account RS256 assertion ile OAuth androidpublisher scope alınır. Sabit resmi host ve konfigürasyondaki package üzerinden `purchases.subscriptionsv2.get` çağrılır; client'ın package seçmesine izin verilmez. Dönen product allowlist, environment (testPurchase), obfuscatedExternalAccountId, expiry, state, autoRenew ve linkedPurchaseToken denetlenir. Pending purchase erişim vermez. Active/canceled ücretli süre sonuna kadar; grace erişimli; hold/paused BillingRetry; expired erişimsizdir. Gerekli Active pending acknowledgement sunucu tarafından resmi acknowledge endpoint'ine gönderilir; başarısızsa satın alma aktivasyonu tamamlanmaz.

Token SHA256 ile benzersiz bağlanır, yeniden sorgulama için AES-GCM ile şifreli saklanır. Replacement token eski bağlı token'a işaret ediyorsa aynı üyelik kökü kullanılır, farklı kullanıcıya bağlanamaz. Eski token tekrar gönderilince en son saklanan token sorgulanır. Yenileme order ID'leri ayrı unique transaction kaydına eklenir. Ürün değişimleri allowlist içindeyse güncel mağaza ürünüyle kaydedilir.

Resmi kaynaklar: [subscriptionsv2.get](https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.subscriptionsv2/get), [Subscription lifecycle](https://developer.android.com/google/play/billing/lifecycle/subscriptions). Google iade/revocation güncel API'nin erişimsiz/expired durumuna yansır; finansal iade tutarı/chargeback muhasebesi uygulanmaz.

## Webhook ve background reconcile

- Apple: `POST /api/webhooks/apple/subscriptions`, V2 `signedPayload`, dış ve iç transaction JWS/app/environment doğrulaması.
- Google: `POST /api/webhooks/google/subscriptions`, Pub/Sub OIDC RS256 Google JWKS imzası, issuer/audience/lifetime ve tam service account email + email_verified=true; subscription resource ve payload package eşleşmesi. Subscription ve voided purchase bildirimleri işlenir; test notification üyelik oluşturmaz.

[Pub/Sub authenticated push](https://docs.cloud.google.com/pubsub/docs/authenticate-push-subscriptions) için push audience endpoint URL'siyle ve beklenen email ile ayarlanır. Google JWKS 30 dakika önbelleklidir; bilinmeyen key ID sonraki denemede yenileme başlatır.

Endpoint anonimdir; kriptografik kontrol zorunludur. Başarılı doğrulama sonrası şifreli referans ve unique store/event inbox'a yazılır, 202 döner. Aynı event tekrarları tek satır olur. İşlem notification'ın ileri sürdüğü durumu doğrudan uygulamaz; son mağaza durumunu tekrar sorgular. Ters sıralı event eski Premium'u geri açmaz. Authentication/ingress süre sınırı 15 saniye; gövde/cevap boyutları sınırlıdır.

`Stores:WorkerEnabled=true`: 5 saniyede bir iş, SQL atomic claim, 2 dakika lease, en fazla 10 deneme; 30 saniyeden başlayıp 1 saate kadar exponential backoff. Başarısız işler `AttemptCount=10` ve `ReconcileFailed` ile incelemeye kalır. Otomatik dead letter servisi yoktur; operatör credential/network düzeltip ilgili inbox deneme sayısı/zamanını kontrollü tekrar planlamalıdır. Ham hata veya token saklanmaz. Periyodik üyelik yenileme, boş inbox turunda çalışır; yoğun inbox periyodik işi geciktirebilir. Çoklu worker aynı kullanıcı kilidi altında güncel kayıtla reconcile eder.

Unbound Apple original / Google token için kullanıcı tahmini yapılmaz; inbox `Unbound` tamamlanır. Özellikle ilk kez görülen replacement Google token'ın kullanıcı tarafından verify/restore edilmesi gerekir. Bildirim tek başına bilinmeyen kullanıcıya üyelik açmaz. Test doubles gerçek sağlayıcı imzası kanıtı değildir. Gerçek RTDN/V2 yönlendirmesi ve mağaza IAM/ürün ayarları canlı ortam öncesi ayrıca uçtan uca denenmelidir.

## Konfigürasyon ve secret yönetimi

ASP.NET Core configuration secret store veya environment değişkeni kullanın (`:` yerine `__`). Gerçek anahtarı appsettings, `.env.example`, Git veya loglara yazmayın.

| Anahtar | Gereksinim |
| --- | --- |
| Stores:AppleEnabled / GoogleEnabled | Bağımsız etkinleştirme; varsayılan false |
| Stores:WorkerEnabled | Reconcile için true; varsayılan false |
| Stores:AppleEnvironment | Production veya Sandbox; otomatik fallback yok |
| Stores:AppleBundleId / AppleAppId | Uygulamanın gerçek bundle ve numeric App ID'si |
| Stores:AppleIssuerId / AppleKeyId / ApplePrivateKeyPem | App Store Connect Server API imzalama kimliği ve P-256 PEM |
| Stores:AppleProducts:0 (vb.) | Premium product allowlist |
| Stores:AppleRootCertificatesBase64:0 | Pin'e uyan Apple Root CA G3 DER base64 |
| Stores:GoogleEnvironment | Production veya Sandbox (license testPurchase) |
| Stores:GooglePackageName | Play uygulama package adı |
| Stores:GoogleServiceAccountEmail / GooglePrivateKeyPem | Yetkili service account ve RSA PEM |
| Stores:GoogleProducts:0 (vb.) | Premium product allowlist |
| Stores:GooglePushAudience / GooglePushEmail | Beklenen OIDC audience ve doğrulanmış push service account |
| Stores:GooglePushSubscription | Tam projects/.../subscriptions/... resource adı |
| Stores:EncryptionKeyId | Varsayılan v1; nokta içermeyen sabit key kimliği kullanın |
| Stores:EncryptionKeys:v1 | CSPRNG ile üretilmiş 32-byte AES anahtarının base64 değeri |

Enabled store eksik temel config/key ile başlamaz. Yanlış PEM/ürün/IAM runtime doğrulamasında da erişimi reddeder. Google Android Publisher API etkin, Play Console service account izinli, RTDN topic publisher ve authenticated push IAM ayarları doğru olmalıdır. Apple API key yetkileri ve App Store Notifications V2 URL'leri ilgili environment'a ayrı kurulmalıdır. Public HTTPS ve doğru TLS sertifikası gereklidir.

AES-GCM her kayıt için yeni 12-byte nonce ve 16-byte tag üretir; key ID veride saklanır. Yeni anahtarı yeni key ID ile ekleyip `EncryptionKeyId` değiştirin; eski kayıtlar yeniden şifrelenene kadar eski key'i keyring'de tutun. Key kaybı plaintext fallback sağlamaz. Yedek veritabanı ile keyring ayrı korunmalıdır. Üyelik kanıtı ve inbox referansı şifreli; token hash'i SHA256'dır. Store HTTP loggers kapalıdır çünkü Google URL path'i token içerir. Proxy/APM/body/URL logları da deployment'ta redakte edilmelidir.

Inbox/transaction geçmişi otomatik silinmez; unique replay geçmişi korunur. Saklama ve şifreli yedek politikası operasyonel olarak belirlenmelidir. Exactly-once mağaza teslimatı veya anlık refund bildirimi garantisi verilmez. Periyodik sorgu ve tazelik üst sınırı bu kesintileri sınırlar; süresi dolmuş veya çok eski doğrulama hak vermez.

## Test sınırı

Shipping API'de fake provider kaydı veya config ile etkinleştirme yolu yoktur; `Stores:UseFake=true` her environment'ta startup reddidir. PayAhead.Testing kaynakları yalnızca test projesi ve `tests/PayAhead.PremiumSmokeHost` içine derlenir. Ayrı smoke executable yalnızca `ASPNETCORE_ENVIRONMENT=Testing`, `PayAheadSmoke_` adlı izole DB ve en az 32-byte test HMAC anahtarıyla, 127.0.0.1 üzerinde çalışır; Production başlatma testi reddedilmiştir. Test host'u deployment'a dahil etmeyin. Çözüm testleri onu başlatmaz; ayrı restore/build gerekir. Bu sağlayıcının imzalı test proof'u gerçek Apple/Google satın alması değildir.
