# Profil fotoğrafları — Phase 5.3.1

## API ve doğrulama

JWT ile yalnız mevcut kullanıcının kaynağı:

| Endpoint | Sözleşme |
|---|---|
| GET `/api/users/me/avatar` | JPEG; Cache-Control no-store; yoksa404 |
| PUT `/api/users/me/avatar` | multipart/form-data içinde tek `image`; güçlü `If-Match: "<rowVersion>"`; güncel ProfileResponse200 |
| DELETE `/api/users/me/avatar` | aynı If-Match; güncel ProfileResponse200, hasAvatar=false |

RowVersion `GET /api/users/me/profile` üzerinden alınır; `hasAvatar` profil ve current-user/auth DTO'larında bulunur. Mobil yalnız doğrulanmış yanıt sonrası kendi kullanıcı/oturum cache'lerini değiştirir. Yeniden giriş User rowversion'ını da değiştirebilir; eski fotoğraf sürümüyle silme409 döner, güncel profil alınmalıdır. GET'e userId/dosya adı verilmez; token URL'ye konmaz.

Upload/delete verified user başına5/dakika SQL rate limit kullanır (429+Retry-After). Kimlik, dosya veya kullanıcı ID'siyle atlatılamaz. Ek form alanı veya çok dosya reddedilir. Eksik/stale sürüm409; geçersiz görsel400; fazla boyut413; erişilemeyen storage503. Kullanıcı metni mobilde hata kodundan yerelleştirilir.

En fazla2 MiB dosya ve2048×2048 kaynak piksel. SkiaSharp gerçek içerikten yalnız JPEG/PNG/WebP, tek kare ve başarılı decode kabul eder; uzantı/MIME güvenilir sayılmaz. SVG/executable reddedilir. Merkezden kare kırpılır, 512×512 JPEG olarak yeniden kodlanır; EXIF/GPS ve kaynak metaveri kopyalanmaz. İşlem sınırları decode öncesinde uygulanır. Mobil SDK57 picker/manipulator önce kırpıp optimize eder; sunucu yine bağımsız doğrular.

## Development depolama

`IAvatarStorage` Application sözleşmesidir. Infrastructure `LocalAvatarStorage` varsayılan olarak API content root altındaki `.data/avatars/<db-scope>/` özel klasörünü kullanır. `.data/` Git dışında; `wwwroot` değildir ve static file endpoint'i yoktur. Db-scope SQL server+database adının hash'idir; test DB'si Development dosyalarını temizleyemez. Sunucu rastgele32hex.jpg adı üretir, CreateNew kullanır ve anahtar regex'i traversal yolunu reddeder. Dosya adı/yolu response'da paylaşılmaz.

Özel konum için environment `Avatars__Root` veya User Secrets `Avatars:Root` **mutlak özel klasör** yolunu ayarlayın. Örnek:

```powershell
$env:Avatars__Root = 'C:\PayAheadPrivate\avatars'
```

Klasör API işletim sistemi hesabına özel olmalı; SQL verisiyle birlikte yedeklenmeli. Konumu değiştirmek mevcut dosyaları otomatik taşımaz; önce kontrollü dosya taşıma gerekir. Telefonu yeniden kurmak dosyayı silmez; aynı backend'e başka cihazdan giriş fotoğrafı geri getirir. Backend diskini silmek veya ephemeral container dosya sistemi kullanmak kalıcılık sağlamaz.

Development dışında açık storage root olmadan fotoğraf işlemi503 verir. Bu bir production deployment değildir. Gelecekte IAvatarStorage nesne depolama adaptörüyle değiştirilebilir; ücretli servis veya kimlik bilgisi eklenmedi. Çok instance production için ortak kalıcı storage/adaptör, erişim politikası, yedek/retention ve işletim kabulü ayrıca gerekir. API Linux container imajına uyumlu Skia native assets paketi eklenmiştir; Linux production runtime kabulü çalıştırılmadı.

## Tutarlılık ve temizlik

Yeni dosya yazılır, authenticated kullanıcı satırı kilitlenip beklenen rowversion ile DB referansı değiştirilir. DB commit sonrası eski dosya temizlenir. DB güncellenmezse yeni dosya yalnız referanssız olduğu doğrulanınca kaldırılır. Commit cevabı belirsiz veya DB erişilemezse dosya korunur; referans verilen fotoğraf silinmez. İki sistem arasında dağıtık atomiklik iddiası yoktur.

AvatarCleanupWorker15 dakikada bir yalnız kendi DB storage alanında bir saatten eski referanssız JPEG'leri temizler; en fazla1000 aday/koşu. Kullanılan dosyalar DB kontrolüyle korunur. Silme başarısızlığı güvenli tip veya genel uyarı olarak loglanır; dosya yolu/token/görsel loglanmaz. Hesap silme AvatarKey'i temizleyip commit sonrası dosyayı kaldırır; başarısız fiziksel temizlik worker'a bırakılır. Var olan finansal silme/session invalidation politikasını değiştirmez.

## Migration ve çalıştırma

`20261028210901_ProfileAvatar` yalnız Users.AvatarKey nullable varchar(36) sütununu ekler. ProfileSecurity üzerine gelir. Doğrulanan `PayAheadDockerDevelopment` hedefine29 Ekim2026 uygulandı; eski kullanıcı/finansal/session değerleri karşılaştırıldı ve değişmedi; pending0. Uygulama startup'ında otomatik migration eklenmedi.

Başka Development makinede önce hedef bağlantı ve migration listesi incelenmeli, ardından `dotnet ef database update` mevcut proje yönergeleriyle açıkça çalıştırılmalı. Production'a otomatik uygulamayın; eski migration'ları silmeyin veya DB/volume sıfırlamayın.

Güncel API'yi `scripts/Start-Development.ps1` ile yeniden derleyip başlatın. Telefonun mobil `.env` dosyası aynı bilgisayarın erişilebilir LAN API köküne işaret etmeli; loopback telefonu bilgisayara bağlamaz. Metro için `npm.cmd start -- --clear` kullanılabilir. Fotoğraf native ayarlarının tam kabulü için SDK57 development build gerekebilir; Expo Go ile picker/optimizasyon fiziksel kabulü ayrıca yapılmalıdır.
