# Veritabanı

SQL Server, EF Core 8 Fluent API ve InitialCreate migration kullanılır. On tablo: Users, PaymentPlans, PaymentOccurrences, ReminderRules, UserSettings, DeviceTokens, UserSubscriptions, RefreshTokens, NotificationDeliveries, AuditLogs.

- Kullanıcı e-postası NormalizedEmail üzerinden benzersizdir. Normalleştirme ve parola hashleme Faz 6'da uygulanacaktır; özel kriptografi kullanılmaz.
- Ödeme tutarları decimal(19,4), takvim tarihleri SQL date, hatırlatma saati time, zaman damgaları datetime2 ve UTC sözleşmesindedir.
- PaymentPlan ve PaymentOccurrence SQL rowversion kullanır.
- PaymentPlanId + DueDate benzersizdir. Para pozitifliği, interval, tarih sırası, anchor ve enum aralıkları check constraint ile korunur.
- ReminderRule sahipliği bileşik foreign key ile PaymentPlan sahibine bağlanır. NotificationDelivery aynı kural/plan/kullanıcı üçlüsüne bağlanır.
- Token hash'i benzersiz ve en fazla 64 ASCII karakterdir; sonraki fazda SHA-256 hex sözleşmesi kullanılacaktır. Ham refresh token saklanmaz.
- Push token ve token hash karşılaştırmaları büyük/küçük harf duyarlıdır.
- Silme ilişkileri NoAction kullanır: SQL Server çoklu cascade yolları engellenir; hesap silme ileride transaction ile açık sırada uygulanmalıdır.
- Kullanıcı/tarih ve iş kuyruğu indeksleri başlangıç sorgularını destekler; sonraki fazlarda gerçek sorgu planları ölçülmelidir.

CurrencyCode veritabanında üç büyük harf olarak sınırlandırılır; ISO 4217 üyeliği Faz 2 Application doğrulamasında SIX 2026-09-17 snapshot'ı ile uygulanır. Para birimleri arasında dönüşüm veya toplama yoktur. Zaman damgalarının UTC olması uygulama sözleşmesidir; SQL datetime2 Kind bilgisi saklamaz. Response mapper UTC Kind bilgisini geri ekleyerek JSON'da Z gösterir.

Faz 2 şema değişikliği gerektirmedi. Name uygulamada 150 karakterle sınırlıdır; mevcut nvarchar(200) kolonu daraltılmadı. İlişkisiz plan hard delete edilir; occurrence/reminder varsa 409 ile korunur. IsActive silinme göstergesi olarak kullanılmaz. Mevcut NoAction FK'lar silme kontrolünden sonra eklenmiş ilişkilere karşı da veriyi korur.

Migration üretme:

```powershell
dotnet ef migrations add Name --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
```

Tasarım zamanı için `ConnectionStrings__PayAhead` zorunludur. Üretim şemasını elle değiştirmeyin; migration SQL çıktısını gözden geçirin. Geri alma veri kaybına yol açabilir; otomatik downgrade yoktur.

## Docker doğrulaması — 21 Ekim 2026

`docker/compose.yml`, SQL Server 2022 Linux imajı, named volume, healthcheck ve `unless-stopped` restart politikası kullanır. SA parolası `.env`/ortamdan gelir; varsayılan host portu 14333 yalnızca loopback'e bağlıdır. Başlatma/durdurma ve parola içermeyen bağlantı kurulum komutları README içindedir.

`PayAheadDockerDevelopment` veritabanında mevcut `20261021084857_InitialCreate` migration'ı başarıyla uygulandı; migration silinmedi veya yeniden oluşturulmadı. SQL Server sürümü 16.0.4305.5 (2022 CU27-GDR), Ubuntu 22.04.5 olarak sorgulandı. Container yeniden başlatıldıktan sonra aynı veritabanı ve tablolar korundu.

Fiziksel şema kontrolü: on iş tablosu ve `__EFMigrationsHistory`; on iş tablosunda PK; on etkin ve trusted FK; `PaymentPlans.Amount decimal(19,4)`; PaymentPlan/PaymentOccurrence üzerinde sekiz baytlık, null kabul etmeyen rowversion; `PaymentPlanId + DueDate` üzerinde unique index. SQL sistem katalogları rowversion için `timestamp` adını gösterebilir; bu SQL Server'da aynı otomatik binary concurrency türüdür, takvim zamanı değildir.

Testcontainers 4.8.1 varsayılan olarak ayrı SQL Server 2022 container'ı oluşturur. Test, `PayAheadTest_<guid>` veritabanına migration uygular ve sadece bu adı doğrulayarak kendi veritabanını temizler. Insert/query/update/delete, FK, unique ihlali, decimal round-trip, rowversion çatışması, check constraint ve transaction rollback gerçek SQL Server üzerinde doğrulanır. SQLite veya EF InMemory kullanılmaz.

## Faz 4 persistence ve transaction politikası

Yeni migration yoktur: başlangıç modelindeki PaymentOccurrence rowversion, decimal(19,4), status/state check constraint, PaymentPlanId FK ve unique(PaymentPlanId,DueDate) kullanılmaya devam eder. Eski migration/snapshot değiştirilmedi. EF model fark kontrolü değişiklik olmadığını doğruladı.

Durum yazarken plan, UserId ve Id ile parametreli SQL üzerinden UPDLOCK/HOLDLOCK ile okunur; occurrence okuma ve SaveChanges aynı Serializable transaction içindedir. Plan değişikliği/silme ve aynı plandaki occurrence yazmaları sıralanır. Mevcut occurrence'ın farklı duruma geçişinde istemcinin rowVersion'ı doğrulanır; EF update/delete kendi concurrency predicate'ini de uygular. Aynı durum/tutar tekrarında SaveChanges çağrılmaz. SQL unique 2601/2627 ve FK 547 güvenli 409'a çevrilir; hata halinde transaction geri alınır. Retry, transaction'ın tamamını kapsar; başarısız denemenin ChangeTracker durumu tekrar kullanılmaz.

Liste sorgusu kayıtlı durumları AsNoTracking ile okur; UserId parent join üzerinden her occurrence sorgusunda sınırlandırılır. Sahiplik kontrolü + count + page sabit üç sorgudur. DueDate DESC/Id ASC sırası ve tarih aralığı filtreleri mevcut plan/tarih unique indeksinden yararlanabilir; status için yeni indeks ölçüm yapılmadan eklenmedi. Count/page farklı sorgular olduğundan eşzamanlı yazmalarda snapshot tutarlılığı iddia edilmez.

Varsayılan Pending satırı eklenmez. Pending'e reset, yalnızca ilgili plan+tarih satırını siler; diğer tarihler değişmez. Eski Pending satırları otomatik topluca temizlenmez; açık ve yetkili reset ile kaldırılabilir. Kaydı silmek önceki Paid/Skipped bilgisini de kaldırır; AuditLog'a değişiklik olayı yazılmaz. Bu tablo immutable işlem defteri değildir. CurrencyCode/plan adı snapshot'ı tutulmaz; geçmiş para birimi yorumunda parent plan değişiklikleri dikkate alınmalıdır. Plan ve kullanıcı silme/export gibi genel yaşam döngüleri sonraki fazların konusudur.
