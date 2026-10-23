# Dashboard ve ödeme planları — Phase 3 entegrasyonu

## Doğrulanan kaynaklar

Backend `PaymentsController`, `OverviewController`, `PremiumController`; Application/Payments Contracts, Validators, CurrencyCodes ve PaymentService; OverviewService/Validators; Domain PaymentPlan.Behavior ve Recurrence/PaymentRecurrenceCalculator incelendi. Backend kaynak kodu değiştirilmedi. PaymentOccurrence durumları projection içinde Pending/Paid/Skipped olarak okunur; bu faz occurrence tahsilat/paid/skip mutation'ı eklemez.

| Uç | Kullanım |
|---|---|
| GET `/api/dashboard/summary` | Sunucunun Today/TimeZoneId/context'i, aktif plan sayısı ve en yakın Pending |
| GET `/api/dashboard/summary?fromDate=...&toDate=...` | Kullanıcı saat dilimine göre bu ayın özeti |
| GET `/api/payments/upcoming` | Pending yaklaşanlar ve bu ayın geciken kayıtları; pageNumber/pageSize/fromDate/toDate/status |
| GET `/api/payments` | Sunucu sayfalaması; search/isActive/currencyCode/category |
| GET `/api/payments/{id}` | Plan detayı ve Base64 RowVersion |
| POST `/api/payments` | CreatePaymentRequest → 201 PaymentResponse |
| PUT `/api/payments/{id}` | Tam UpdatePaymentRequest + rowVersion → 200 |
| DELETE `/api/payments/{id}` | Tek strong `If-Match: "<rowVersion>"` → 204 |
| GET `/api/premium/entitlements` | plan/isPremium/limits.activePaymentPlans |

PaymentResponse: id, name, description, amount, currencyCode, category, frequency, interval, startDate, endDate, anchorDay, isActive, createdAtUtc, updatedAtUtc, rowVersion. **NextDueDate alanı yoktur.** Liste sırası CreatedAtUtc descending + Id; sunucu sıralama parametresi sağlamaz. İstemci eksik sayfaları kendi içinde sıralayıp tüm liste gibi sunmaz. Kategori filtresi tam eşleşme, search ödeme adını arar. Sayfa 20 kayıt; toplam ve totalPages sunucudan gelir.

## Dashboard semantiği

İlk default summary sunucunun hesap saat dilimindeki Today değerini sağlar. Bu tarihin ay başlangıcı/sonu ile ikinci, ayrı cache anahtarlı aylık summary istenir. Aynı rolling sorguyu kullanan ekranlar TanStack Query cache üzerinden tekilleştirilir. Uygulama ön plana geldiğinde veya kullanıcı aşağı çektiğinde veriler yenilenir.

Ana lacivert karttaki tutarlar backend `upcomingTotalsByCurrency`: bugünden ay sonuna kadar Pending kayıtlar. Gecikmiş tutarları kapsıyor gibi etiketlenmez. Ödenenler `paidTotalsByCurrency` içindeki actualAmount bazlı sunucu toplamlarıdır. pendingPaymentCount ay içindeki tüm Pending; upcomingPaymentCount bugün ve sonrasındaki Pending'dir. Sayılar birbirinin eş anlamlısı değildir.

Gecikenler **bu ayın başından düne kadar** Pending aralığıdır; tüm geçmişin gecikmiş toplamı olduğu iddia edilmez. Sonuç totalCount ve ilk 3 kayıt gösterilir; bu alt kümeden toplam hesaplanmaz. Yaklaşan liste default backend aralığında (bugün ve +30 gün dahil), ilk 3 kayıttır; aralık ve toplam kayıt sayısı görünür. En yakın ödeme backend nextUpcomingPayment alanından alınır. Hesap timezone fallback kullanırsa UTC uyarısı gösterilir.

PaymentResponse'ta occurrence next-date olmadığı için plan listesi/detayı **Planlanan** tarihini metadata recurrence takviminden hesaplar. Bu, ödenen/atlanan durumundan bağımsız plan tarihidir; UI bunu açıkça söyler. Gerçek Pending tarihinin kaynağı Dashboard projection'ıdır. Pasif/end-date geçmiş/tek-seferlik geçmiş planda yeni tarih yoktur.

## Para ve tarih güvenliği

Backend decimal(19,4), pozitif ve en fazla 999999999999999.9999. JSON para tokenları `amount`/`actualAmount` alanlarında token-aware tarayıcı ile JSON.parse öncesinde lexical string olarak korunur. Açıklama içindeki kaçışlı metinler değiştirilmez. Runtime Zod şeması gerçek DTO biçimini doğrular. Para Intl currency parçaları ve ondalık metin ile Türkçe biçimlenir; Number toplaması/yuvarlaması yapılmaz. Farklı para birimleri birleştirilmez.

Formda virgül veya nokta ondalık ayracı kabul edilir, binlik ayraç kabul edilmez. JSON isteğinde amount **tırnaklı string olarak gönderilmez**: doğrulanmış canonical ondalık, sayısal JSON tokenı olarak serileştirilir. Testler en büyük (19,4) değerin gidiş/dönüşünü ve ondalık kaybı olmadığını doğrular.

Tarihler YYYY-AA-GG DateOnly'dir; cihaz saat dilimi dönüşümü uygulanmaz. Gregorian ay uzunluğu/artık yıl doğrulanır. İlk ödeme her zaman StartDate'tir. Monthly/Yearly AnchorDay sonraki dönemlere uygulanır, olmayan gün ay sonuna kırpılır; Mart tekrar 31'e, sonraki artık yıl 29 Şubat'a döner. Once/Daily/Weekly/Monthly/Yearly gerçek enum değerleridir. Interval 1–Int32.MaxValue; EndDate başlangıçtan önce olamaz. CurrencyCodes backend snapshot'ıyla eşleştirilmiştir. Category zorunlu, 50; Name 150; Description 2000 karakter üst sınırıdır.

## Mutation, concurrency ve hata politikası

Finans key'leri `['finance', userId, sessionEpoch, ...]`. Önceki oturumun isteği, yeni hesabın tokenıyla gönderilmez. Query AbortSignal aktarılır; kullanıcı değişiminde iptal/cache temizliği Phase 2 altyapısıyla korunur. Başarılı create/update/delete sonrası yalnızca o kullanıcı/oturumun finans key'leri invalidate edilir. Cache yenilemesi sırasında kullanıcı değişirse eski mutation devamı iptal edilir; eski planın yeni oturumda açılması engellenir. Düzenleme/silme sonrası mevcut hedef ekrana dismissTo ile dönülür, geri yığınında kopya ekran oluşturulmaz. Tamamlanmış mutation girdileri gözlemci kalkınca tutulmaz (`gcTime: 0`). Finans verisi veya token loglanmaz, kalıcı query cache eklenmez.

Yazmadan önce access süresi kontrol edilir ve gerekirse merkezi single-flight refresh yapılır. Yazma 401 dönerse rotation yapılabilir ancak POST/PUT/DELETE otomatik replay edilmez; kullanıcı açıkça tekrar denemelidir. Network timeout'ta işlem sunucuda tamamlanmış olabilir: form listeyi kontrol etmeyi ister ve tekrar gönderimi açık kullanıcı onayına kadar kapatır. Otomatik mutation retry yoktur.

Düzenlenen RowVersion form açılışında sabitlenir; arka plan refetch'i sürümü sessizce değiştirmez. 409 sonrası kaydetme kapanır; güncel kaydı açıkça yüklemek form değişikliklerini kaldırır. Silme ve pasifleştirme native Alert onayı ister. Silme güçlü ETag kullanır. İlişkili history/reminder veya tracked subscription bağlantısını backend reddeder; istemci geçmişi silmez veya sahipliği aşmaya çalışmaz.

403 `entitlement_limit` ayrı mesaj ve mevcut Premium tanıtımına bağlantı verir. Gerçek limit entitlement yanıtından gelir; Premium için sabit üst sınır varsayılmaz. Sunucu son otoritedir. Limit aktif planları kapsadığı için pasif plan oluşturma backend'in desteklediği normal davranıştır. Satın alma/entitlement uydurma akışı eklenmedi.

401 oturum, 403 izin/limit, 404 bulunamadı/erişilemiyor, 409 sürüm/ilişki çatışması, 429 bekleme, network/timeout ayrı ele alınır. Ortak ProblemDetails mapper auth dışındaki 409'u artık e-posta hatası olarak etiketlemez.

## Gerçek ve mock test ayrımı

`npm.cmd test`: adapter/storage/component testleri; gerçek backend veya Android cihaz testi değildir. `npm.cmd run test:auth:live` ve `npm.cmd run test:finance:live`: yalnızca private LAN/localhost geliştirme API'sinde rastgele geçici hesaplar kullanır. Oluşturmadığı kullanıcı/planı silmez, gerçek finansal işlem yapmaz. İkinci hesapla 404 izolasyonu, Free limit, ETag/409 ve decimal roundtrip gerçek HTTP ile kontrol edilir. Cleanup doğrulanamazsa script hata verir. Production için kullanılmaz.
