# Abonelik sözleşmesi ve kullanım — Frontend Phase 5

24 Ekim 2026. Mevcut backend Phase 8 sözleşmesi kullanılır; backend, paket ve migration değişikliği yoktur. `TrackedSubscription` dış hizmetin takip kaydıdır; `UserSubscription` PayAhead'in kendi mağaza üyeliğidir. Oluşturma, backend transaction'ı içinde yalnızca bir bağlı `PaymentPlan` oluşturur. Mobil ikinci `/api/payments` POST'u yapmaz.

## Kullanılan endpoint'ler

| Endpoint | Kullanım |
|---|---|
| GET `/api/subscriptions` | status, search (ad/sağlayıcı), currencyCode; pageNumber/pageSize=20. Backend ayrıca tam providerName filtresi destekler; mobil tek arama kutusu kullanır. Sabit sıra: CreatedAtUtc azalan, ardından Id. |
| POST `/api/subscriptions` | Yeni takip ve tek bağlı plan, 201. |
| GET `/api/subscriptions/{id}` | Sunucu lifecycle, nextBillingDate, trialDaysRemaining, rowVersion ve hesap saat dilimi. |
| PUT `/api/subscriptions/{id}` | Tam düzenleme gövdesi ve yüklenen rowVersion. |
| POST `/api/subscriptions/{id}/cancel` | Kullanıcının seçtiği accessEndsOn ve rowVersion. |
| DELETE `/api/subscriptions/{id}` | Güçlü, tırnaklı If-Match; soft-delete, geçmiş korunur. |
| GET `/api/subscriptions/summary` | Tüm kayıtların sayısı/durumu, aylık tahmini karşılık, en yakın yenileme ve 7 gün içinde bitecek deneme sayısı. Liste filtrelerinden bağımsızdır. |
| GET `/api/subscriptions/trials/expiring?withinDays=1|3|7` | Başlamış, halen Trial durumundaki denemeler; bitiş günü dahil. Sonuçlar istemcide 20 satır halinde gösterilir. |
| GET `/api/premium/entitlements` | Free/Premium gerçek limits değerleri. |
| GET `/api/dashboard/summary` | Hesap tarihi ve aktif bağlı plan kotası. |

Tüm abonelik endpoint'leri JWT gerektirir. Sahiplik backend'de doğrulanmış UserId ile sınırlandırılır; mobil gövdede userId/paymentPlanId göndermez. 401/refresh ve oturum değişimi mevcut auth altyapısıyla yönetilir.

## Form ve tarihler

Alanlar: name, providerName, description, amount, currencyCode, billingFrequency, billingInterval, startDate, firstBillingDate, anchorDay, trialStartDate/trialEndDate, status (Active/Trial), autoRenew, accessEndsOn, enableTrialReminders. Sıklık Daily/Weekly/Monthly/Yearly; Once abonelik sözleşmesinde yoktur. anchorDay yalnız aylık/yıllık tekrar için gönderilir. Sunucu nextBillingDate'i hesaplar; formda “İlk ücretli ödeme tarihi” kullanılır, hesaplanan sonraki tarih ayrı alandır.

DateOnly `YYYY-MM-DD` olarak taşınır. Mevcut native DateField yerel yıl/ay/gün parçalarını kullanır; UTC dönüşümüyle gün kaydırılmaz. Deneme tarihleri çift halinde, startDate'ten önce olmayacak şekilde; firstBillingDate kesinlikle trialEndDate'ten sonra olmalıdır. AutoRenew kapalı Active kayıt accessEndsOn ister. İptal tarihinin alt sınırı sunucu Today ve startDate'in büyük olanıdır. Erişim bitiş günü dahildir.

Kalan gün/lifecycle istemcide yeniden hesaplanmaz: server context.today/timeZoneId/usedTimeZoneFallback ve trialDaysRemaining gösterilir. 0 “son gün”, 1/3/7 aralıkları belirgin gösterilir. Geçmiş deneme Active kayıtta “Deneme süresi bitti”; Cancelled deneme iptal; Expired erişim sona erdi olarak sunulur. İptal erişimi hemen bitirmek zorunda değildir. Cancelled/Expired düzenlenemez, yeniden etkinleştirilemez; gerekirse yeni takip açılır.

## İptal, kaldırma ve eşzamanlılık

İptalden önce dış sağlayıcıda iptal yapılmadığı açıkça bildirilir. Kullanıcı gerçek accessEndsOn tarihini seçer ve onaylar. Backend yenilemeyi durdurur, bağlı planı pasifleştirir; belirlenen tarihe kadar Cancelled/erişim devam ediyor, sonrasında Expired/erişim sona erdi görünür. Kaldırma ayrı onayla listeyi gizler, ödeme geçmişini korur; dış hizmeti iptal etmez.

Yazmalar otomatik tekrar edilmez. Busy ref çift dokunmayı engeller. 409 veya sonucu belirsiz network/timeout hatasında aynı gövdeyi körlemesine yeniden göndermek engellenir; güncel kaydı yüklemek gerekir. Yeni kayıtta belirsiz sonuç sonrası listeye dönüp oluşturulup oluşturulmadığı kontrol edilir. Backend field errors form alanlarına eşlenir. `entitlement_limit` (403) uyarı ve mevcut üyelik seçeneklerine bağlantı verir; satın alma/success taklidi yoktur.

## Para ve finansal tek kaynak

JSON decimal token'ları parse edilmeden string'e alınır (`amount`, `estimatedAmount` dahil). Giden amount tırnaksız tam decimal token; JavaScript Number üzerinden para yuvarlama/toplama yoktur. Pozitif decimal(19,4), en çok 15 tam/4 ondalık basamak ve ISO4217 doğrulanır.

Sunucu aylık eşdeğeri yalnız otomatik yenilenen effective Active/Trial için toplar: Monthly amount/interval, Yearly amount/(12×interval), Weekly amount×52/(12×interval), Daily amount×365/(12×interval). Para birimleri ayrı; grup sonucu 4 basamakta ToEven yuvarlanır. Deneme gelecekteki ücretini tahmine katabilir; gerçek tahsilat değildir.

**Mevcut sözleşme sınırı:** aboneliklere özel “bu ay kesin ne ödeyeceğim/ödedim” aggregate endpoint'i yoktur. Sayfalı listeden eksik toplam üretmek veya tahmini gerçek aylık harcama diye sunmak yerine aylık tahmini karşılık açıkça etiketlenir. Gerçek vade/durumlar bağlı plan üzerinden mevcut takvim/geçmiş/analizde görülebilir. Aboneliklere özel ay/durum aggregate'i istenirse ayrıca onaylı backend genişletmesi gerekir; bu fazda endpoint uydurulmadı.

Dashboard abonelik sayıları, en yakın yenileme ve yaklaşan denemeye erişim sağlar. Finansal toplamına ikinci bir abonelik tutarı eklenmez. Detaydan bağlı plan salt okunur açılır. Genel ödeme listesinde planın aboneliğe bağlı olduğu DTO'da belirtilmez; oradan yapılan değişiklikleri backend 409 ile korur.

## Limit ve cache

Doğrulanan Free: 3 aktif/deneme takip kaydı, 5 aktif ödeme planı. Premium limitler konfigürasyona bağlıdır ve entitlement response'dan okunur; sınırsız varsayılmaz. Oluşturma iki farklı kotayı tüketebilir. Ön kontrol yardımcıdır, sunucu transaction'ında gerçek limit uygulanır. Bildirim tercihi teslim kanıtı değildir; gerçek push/SMTP kurulumu bu fazın kapsamına girmez.

Query kökü `['finance', userId, sessionEpoch]`; altında subscriptions/list/detail/summary/trials. Create/update/cancel/delete başarısı yalnız bu kökü invalidate eder: plan, Dashboard, takvim, geçmiş, analiz, entitlement ve abonelikler birlikte yenilenir. Dashboard pull-refresh abonelik sorgularını da yeniler. Eski epoch yanıtları ve mutation devamı iptal edilir; logout mevcut QueryClient temizliğini kullanır. Başka kullanıcı cache'ine yazılmaz. Finansal gövdeler/parolalar/token'lar loglanmaz.

## Çalıştırma

Backend migration eklenmedi; mevcut Phase 4 migration'ları uygulanmış Development API gerekir. `GET /health/ready` ve Swagger'da subscription/reset path'leri kontrol edilir. Normal başlatma `scripts/Start-Development.ps1`; önceki Mailpit akışı gerekiyorsa root [reset tanısı](../../docs/PASSWORD_RESET_DIAGNOSIS.md) adımlarındaki `-EmailCapture -ResetLinkBase exp://IP:PORT/--/reset-password` korunur. Profesyonel SMTP/Resend bu fazda kurulmaz.

Mobil: `npx.cmd expo start`; fiziksel cihaz için .env API kökü bilgisayarın LAN IPv4:5080 adresidir. `npm.cmd run test:subscriptions:live` .env'yi okur, yalnız yerel/LAN hedef kabul eder, iki geçici hesapla çalışıp yalnız onları temizler. Varsayılan test hesabı Free3 olmalıdır; gerçek kullanıcı hesabı kullanmaz. Docker/SQL olmadan başarılı SQL smoke iddiasında bulunulmaz.
