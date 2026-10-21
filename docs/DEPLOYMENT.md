# Dağıtım hazırlığı

Bu faz üretim dağıtımı değildir. İşlevler ve güvenlik incelemesi tamamlanmadan internete açmayın. .NET 8 desteği 10 Kasım 2026'da sona erer: [Microsoft duyurusu](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/). Üretime geçişten önce açık onayla desteklenen .NET LTS sürümüne geçiş planlanmalıdır. Otomatik sürüm yükseltmesi yapılmadı.

Yükseltme yolu: ayrı dalda SDK/TargetFramework/EF paketleri birlikte güncellenir; breaking changes incelenir; tüm birim, gerçek SQL Server ve kimlik testleri çalıştırılır; migration farkı ve container imajı doğrulanır; staging yük ve geri dönüş denemesi sonrasında yayınlanır.

Üretimde bağlantı secret store üzerinden `ConnectionStrings__PayAhead` ile verilir. TLS ve geçerli SQL sertifikası zorunludur; `TrustServerCertificate=False` kullanın. API en az yetkili veritabanı hesabıyla çalışır, migration ayrı yetkili hesapla bakım adımı olarak uygulanır. `ASPNETCORE_ENVIRONMENT=Production`, geliştirme kimliği kapalı, AllowedHosts gerçek alan adına ayarlı olmalıdır. HTTPS reverse proxy, forwarded headers ve HSTS topolojisi ayrıca gözden geçirilmelidir.

```powershell
dotnet publish src/PayAhead.Api -c Release -o artifacts/publish
```

`docker/Dockerfile` API için başlangıç paketlemesidir. Compose yalnızca yerel SQL Server içindir; parola ortamdan gelir. API imajı üretimde HTTPS reverse proxy arkasında çalıştırılmalıdır. 21 Ekim 2026 ek doğrulamasında SQL Compose healthcheck, migration ve kalıcılık doğrulandı. API bu aşamada host üzerinde çalıştırıldı; API Docker imajının build/run doğrulaması bu çalışmanın kapsamında yapılmadı.

Yedekleme: RPO/RTO belirleyin; şifreli tam ve işlem günlüğü yedekleri alın; ayrı erişim alanında saklayın; staging ortamına düzenli restore denemesi yapın. Migration öncesi doğrulanmış yedek gereklidir. Saklama süreleri veri envanteri ve hukuki incelemeye göre belirlenmelidir. Faz 1 kapsamında yedek/geri yükleme denemesi yapılmadı.

Türkiye ve uluslararası kullanım için KVKK/GDPR kapsamı, veri sorumluluğu, saklama, aktarım ve silme süreçleri hukuk uzmanıyla incelenmelidir. Bu doküman mevzuata uygunluk iddiası değildir. Export ve hesap silme Faz 10'da uygulanacaktır.
