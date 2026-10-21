# Premium üyelik ve entitlement

Faz10 verify/restore tüm instance'larda ortak10/dakika/user SQL bütçesi, limiter outage503 fail-closed kullanır. Hesap silme store auto-renew iptali değildir, store anti-replay bağları pseudonymous tutulur. [DATA_PRIVACY.md](DATA_PRIVACY.md), [OPERATIONS_RUNBOOK.md](OPERATIONS_RUNBOOK.md). Gerçek mağaza doğrulaması yapılmadı.

Faz 9, PayAhead'in kendi `UserSubscription` üyeliğini yönetir. Netflix/Spotify gibi harici hizmetlerin `TrackedSubscription` kayıtları Premium sağlamaz. Eski `UserSubscription.Status=Active` kayıtları da `IsStoreVerified=false` olduğu sürece Free kalır. İstemciden üyelik durumu veya sahiplik kabul edilmez.

## Merkezi kurallar

| Özellik | Free | Premium varsayılanı |
| --- | --- | --- |
| Aktif PaymentPlan | 5 | 1000 |
| Plan başına aktif ReminderRule | 1 | 10 |
| Aktif TrackedSubscription | 3 | 1000 |
| Temel dashboard / calendar | Açık | Açık |
| Gelişmiş abonelik takibi yeteneği | false | true |

Premium değerleri `Membership:PremiumPaymentPlans` (5–1000), `PremiumReminders` (1–10), `PremiumTrackedSubscriptions` (3–1000) ile ayarlanır. Free sınırları tek `MembershipOptions.Free` tanımındadır. `IEntitlementService` karar verir; JWT içinde kalıcı Premium claim bulunmaz. `AdvancedSubscriptionTracking` yetenek bilgisi ve artırılmış limitler sunulur; mevcut Faz 8 liste/summary/history okumaları Free kullanıcıya kapanmaz.

Aktif plan sayımı `IsActive`; hatırlatıcı sayımı silinmemiş, etkin **Payment ve SubscriptionTrial hedeflerinin toplamı** üzerinden yapılır. Harici abonelik sayımı kullanıcının saat dilimindeki bugüne göre Trial/Active, erişimi bitmemiş kayıtlardır. AutoRenew=false bir trial, trial bitiminden sonra sayılmaz. İptal edilmiş/expired harici abonelik tekrar aktifleştirilemez (Faz 8 kuralı); yeni kayıt gerekir. AutoRenew ile bağlı planın yeniden açılması ayrıca plan limitinden geçer.

Her yeni aktif kayıt ve pasiften aktife geçiş, SQL transaction içinde `Users WITH (UPDLOCK,HOLDLOCK)` kilidi altında sayılır. Aynı kullanıcının farklı API instance'larına gelen istekleri de sıralanır. Limit aşımı 403 `application/problem+json`, `code=entitlement_limit`; çakışma 409'dur. Satın alma doğrulaması aynı kullanıcı kilidini kullanır; mağaza çağrısı 25 saniyelik işlem süresine dahildir.

## Lifecycle

| Durum | Premium erişimi |
| --- | --- |
| Free | Yok |
| Active | Doğrulanmış, taze ve erişim bitişi gelecekteyse var |
| GracePeriod | Aynı koşulla tüm Premium hakları var; grace bitişi erişim bitişidir |
| BillingRetry / account hold / pause | Yok |
| Expired | Yok |
| Revoked | Yok |

AutoRenew=false ücretli dönemi hemen sonlandırmaz. Apple status/refund bilgisi ve Google güncel abonelik durumu otoritedir. Süresi geçmiş Active/GracePeriod cevabı entitlement tarafında Expired görünür. En az bir geçerli mağaza üyeliği varsa Premium devam eder; bir mağazadaki iptal diğer geçerli üyeliği iptal etmez.

Doğrulama tazeliği `Membership:VerificationFreshnessHours` varsayılan 24, aralık 1–24 saattir. Worker güncel üyeliği 6 saat sonra yeniden sorgular, hata olursa 15 dakika sonra dener. Mağazaya erişilemezse son doğrulamanın tazelik penceresi ve mevcut dönem bitişinden erken olanı geçince erişim Free olur. Yenileme henüz doğrulanmamışsa kısa süreli Free görünmesi mümkündür. İade/revocation, doğrulama veya reconcile tamamlandığında yansır; bildirim gecikmesi sıfır gecikme garantisi vermez.

## Süre dolması ve eski veriler

Downgrade sırasında planlar, reminder kuralları, harici abonelikler ve geçmiş silinmez veya topluca pasifleştirilmez. Mevcut etkin kayıtlar ve bildirimleri korunur; metadata değişiklikleri ve okumalar devam eder. Yeni aktif kayıt / yeniden etkinleştirme güncel Free limitine tabidir. Böylece eski limit üstü kullanıcının verisi kaybolmaz. Yeniden Premium olduğunda mevcut veri kullanılabilir kalır.

Trial reminder üretimi de aynı slotları kullanır. Öncelik 1, 3, 7, 0 gün, saat 09:00'dır; Free yeni trial için yalnızca 1 gün önce kuralı etkinleşir. Diğer üç kural pasif saklanır. Premium, kendi limitine kadar etkinleştirir. Önceden etkin eski kurallar downgrade sırasında korunur; kapatılan kural tekrar açılırken güncel limit uygulanır. Tek slot trial tarafından kullanılıyorsa ayrıca ödeme reminder açmak için önce trial reminders kapatılmalıdır.

## API ve React Native sözleşmesi

| Method / yol | Kimlik |
| --- | --- |
| GET `/api/premium/plans` | Anonim; limitler, ürün kimlikleri, etkin mağaza bayrakları |
| GET `/api/premium/me` | JWT |
| GET `/api/premium/entitlements` | JWT; me ile aynı cevap |
| POST `/api/premium/verify-purchase` | JWT; kullanıcı/IP başına 10/dakika |
| POST `/api/premium/restore` | JWT; aynı doğrulama ve rate limit |

Satın alma öncesinde `me` cevabından `appAccountToken` (PayAhead UserId UUID) ve `googleObfuscatedAccountId` alınır. Apple StoreKit satın almaya appAccountToken; Google Billing akışına obfuscatedAccountId bu değerlerle bağlanmalıdır. Eski satın almalarda bu bağ yoksa otomatik hesap eşleştirme yapılmaz. Hesap aktarımı desteklenmez.

```json
{"store":"Apple","productId":"configured.product.id","evidence":"StoreKit signedTransactionInfo JWS"}
```

```json
{"store":"Google","productId":"configured.product.id","evidence":"Google purchaseToken"}
```

Restore aynı gövdeyi kullanır; cihazdaki her satın alma için ayrı istek gönderilir. Backend güncel mağaza durumunu yeniden sorgular. Aynı transaction/token tekrarları idempotenttir. Başka kullanıcıya bağlı satın alma 409; yanlış mağaza hesabı/product/proof 400; eksik credential veya geçici servis hatası 503; JWT yoksa 401; rate limit 429. `isPremium` gibi bilinmeyen satın alma alanları 400'dür.

Cevap alanları: `plan`, `state`, `isPremium`, `limits`, `accessEndsAtUtc`, `autoRenew`, `appAccountToken`, `googleObfuscatedAccountId`, `basicDashboard`, `calendar`, `advancedSubscriptionTracking`. Durumlar string enum, zamanlar UTC'dir. Ürün fiyatı, para birimi ve lokalize satın alma metni mağaza SDK'sından alınır; katalog endpoint'i fiyat kaynağı değildir. İstemci hak vermek için satın alma callback'ini kullanmamalı; doğrulama cevabını ve sonraki me sorgularını kullanmalıdır. Frontend uygulanmadı.

## Veri modeli ve teknik sınırlar

`UserSubscriptions` güncel doğrulanmış durum + rowversion; `StorePurchases` değişmez token/ilk transaction bağları; `StoreTransactions` yenilemeler dahil benzersiz transaction denetim kaydı; `StoreWebhookInbox` doğrulanmış bildirim iş kuyruğudur. `(Store,TransactionId)`, `(Store,TokenHash)` ve `(Store,EventId)` unique indexleri replay ve hesaplar arası reuse'u önler. Satın alma/transaction composite FK üyeliğin UserId'sini de doğrular.

En fazla 100 üyelik kökü/kullanıcı; ürün ve transaction 200 karakter; kanıt 100000 karakter; satın alma HTTP gövdesi 150000 byte; webhook 262144 byte; mağaza HTTP cevabı 512 KiB. Teknik limitler kaldırılmamalıdır. [Mağaza entegrasyonu](STORE_INTEGRATION.md), [güvenlik](SECURITY.md), [doğrulama kanıtı](IMPLEMENTATION_PROGRESS.md) ayrıntıları tamamlar.
