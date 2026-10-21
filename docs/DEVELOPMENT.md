# Geliştirme

Plan: (1) ortam/depo incelemesi; (2) altı proje ve mimari referanslar; (3) model/SQL konfigürasyonu/migration; (4) API/DI/log/hata/sağlık/geliştirme kimliği; (5) gerçek doğrulamalar ve dokümantasyon; (6) Faz 2 onayını bekleme.

VS Code çalışma alanını kök dizinden açın. `global.json` yalnızca .NET 8 SDK 8.0.4xx patch sürümlerine izin verir, C# 12 sabittir. Proje içindeki SDK kaynak kontrolüne alınmaz. .slnx istenen envanter dosyasıdır; .NET 8 uyumluluğu için .sln kullanılır. Yeni proje eklenirse iki dosyayı da güncelleyin.

Restore/build/test ve API başlatma komutları README içindedir. Testler xUnit kullanır. Birim testleri geliştirme kimliği güvenlik sınırını ve SQL model metaverisini doğrular. Entegrasyon testleri HTTP başlangıcı ve gerçek SQL Server migration/constraint/transaction/readiness davranışını doğrular. Model metaverisi testi gerçek veritabanı testi yerine geçmez.

SQL Server testinde SQLite veya InMemory kullanılmaz. Mevcut ortamda Docker bulunmadığı için yerel SQL Server tercih edilmiştir. Testcontainers otomasyonu sonraki sertleştirme fazında eklenebilir. SQL testi rastgele adlandırılmış kendi veritabanını oluşturur ve siler; ortak geliştirme/üretim veritabanı kullanılmaz.

Her faz sonunda `docs/IMPLEMENTATION_PROGRESS.md` gerçek komut ve sonuçlarla güncellenir. Başarısız/atlanmış testler başarılı sayılmaz. SQL bağımlılığı yoksa engel açıkça kaydedilir.
