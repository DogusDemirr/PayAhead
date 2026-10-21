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

CurrencyCode veritabanında üç büyük harf olarak sınırlandırılır; ISO 4217 üyeliği Faz 2 doğrulamasında uygulanacaktır. Para birimleri arasında dönüşüm veya toplama yoktur. Zaman damgalarının UTC olması uygulama sözleşmesidir; SQL datetime2 Kind bilgisi saklamaz.

Migration üretme:

```powershell
dotnet ef migrations add Name --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
```

Tasarım zamanı için `ConnectionStrings__PayAhead` zorunludur. Üretim şemasını elle değiştirmeyin; migration SQL çıktısını gözden geçirin. Geri alma veri kaybına yol açabilir; otomatik downgrade yoktur.
