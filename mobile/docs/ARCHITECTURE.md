# Mobil mimari — Phase 4

Phase 4: `features/occurrences` takvim/history/tek ödeme akışlarını, `features/analytics` sunucu aggregation verisinden SVG grafikleri taşır. DateField native picker'ı DateOnly ile yerel takvim parçaları üzerinden eşler. Finance API/schemas/hooks aynı kullanıcı + epoch query kökünü kullanır; plan ve occurrence mutation'ları Dashboard/calendar/history/analytics dahil kökü invalidate eder. Reset anonymous API client'ı kullanır; token form belleğinde/route parametresindedir, başarıda temizlenir; kalıcı storage eklenmedi. Reset route'u oturumdan bağımsızdır; analytics/history/occurrence korumalıdır. Beş tab sayısı korunur. E-posta backend bounded memory queue + SMTP soyutlamasıyla gönderilir; ayrıntılar root PASSWORD_RESET/ANALYTICS belgelerindedir.

## Sorumluluklar

`src/app` yalnızca Router route/layout dosyaları; `features` ekranlar/form kuralları; `components` ortak premium UI; `theme` merkezi tokenlar; `store` UI ve public session snapshot; `services` auth API, transport, session ve SecureStore. Testler route ağacının dışındadır. Phase 1 görsel sistemi korunur. Backend AuthController (UsersController aynı dosyada), Application/Auth Contracts/Validators/AuthService, Infrastructure/AuthStore, TokenService ve backend API/SECURITY belgeleri kaynak alındı.

## API ve hata modeli

Auth API anonim, bearer eklemeyen bir Axios instance kullanır. Register UserResponse döndürür; login varsayılmaz. Login/refresh AuthResponse döndürür. `/me` ayrı korunan instance kullanır ve TanStack Query ile alınır. Request alanları backend ile aynı: email/password/displayName, refreshToken. Login parola 1–128, register 12–128; email en fazla 254, displayName trim sonrası 1–100. Şifre trim edilmez. Ön doğrulama backend kurallarını veya backend doğrulamasını devre dışı bırakmaz.

15 saniye timeout; ApiError HTTP status, traceId ve field errors taşır. Network/timeout/configuration/session hataları ayrılır; ham Axios config, token gövdesi veya stack kullanıcıya verilmez. 401/409/429 için uygun genel mesajlar vardır. Yalnızca yapılandırılmış base URL altında göreli `/api/` istekleri kabul edilir; harici absolute URL'lere bearer gönderilmez. Production HTTPS şarttır, development HTTP LAN testi kabul edilir.

## Oturum yaşam döngüsü

SessionManager tokenları private bellekte tutar. Zustand yalnızca status (`restoring`, `anonymous`, `authenticating`, `authenticated`), public user ve güvenli notice içerir. Parola veya JWT persist middleware/AsyncStorage/console içine yazılmaz.

SecureStore access/refresh token ve iki UTC son kullanım zamanını **tek JSON kaydında** tutar. `WHEN_UNLOCKED_THIS_DEVICE_ONLY` kullanılır; bozuk/eski şema kayıtları silinir. Silme başarısızsa token içermeyen `null` ile overwrite denenir. Her iki native işlem de başarısızsa UI anonymous kalır ve depolama temizliğinin başarısız olduğunu bildirir; cihazda kalıcı silmenin sağlandığı iddia edilmez. Web login güvenli kalıcı saklama hatasıyla kapalı kalır; localStorage fallback yoktur.

Login önce eski cache/kayıtları temizler; sunucudan aldığı çifti güvenli saklar, sonra `/me` doğrulanmadan protected erişim vermez. Restore tek kez/tek promise ile SecureStore okur. Refresh süresi bittiyse temizler; access süresine 30 saniyeden az kaldıysa önce refresh eder. Ardından `/me` sorgulanır. Restore network/storage/kimlik hatasında fail-closed davranır; offline oturum açılmaz, yeniden giriş gerekir.

## Rotation ve yarış güvenliği

Bir oturum epoch'u içinde tek shared refresh promise vardır. Aynı anda gelen 401'ler bunu bekler. Her istek gönderilen access token ve epoch'u taşır: rotation sonrası geç gelen eski 401 yeni rotation başlatmadan güncel tokenla devam eder. Her protected GET/HEAD/OPTIONS isteği en fazla bir kez replay edilir. İkinci 401 oturumu kapatır. Finans mutation'ları otomatik replay edilmez. Yazma öncesi access süresi kontrol edilir; write 401 durumunda merkezi refresh sonrası açık kullanıcı tekrar gönderimi istenir.

Login/refresh/logout public client'tadır; recursive interceptor veya otomatik refresh retry yoktur. Timeout'ta eski refresh tekrar gönderilmez: sunucu rotation'ı commit etmiş olabilir. Refresh hatasında yerel oturum kapanır ve eski aile üyesiyle best-effort logout denenir. Backend eski aile üyesinin logout'unu güncel child'a da uygular.

İstek iptali shared rotation'ı iptal etmez; yalnızca ilgili isteğin replay'ini durdurur. Logout/yeni login epoch'u değiştirir. Geç dönen eski response, restore veya rotation başka hesabı/oturumu açamaz. SecureStore yazma/silme işlemleri sıraya alınır; logout, başlamış yazmanın ardından temizler. Protected response'lar epoch kontrolünden geçmeden query cache'e ulaşamaz.

## Current user, cache ve navigation

`useCurrentUser` TanStack Query anahtarını session epoch ile ayırır; authenticated değilse sorgu çalışmaz. Login/restore `/me` yanıtı aynı query cache'i doldurur. Settings bu gerçek profili gösterir. Uygulama ön plana geldiğinde profil yeniden doğrulanır. `/me` için 401/403/404 güvenli logout sağlar; ağ kesintisi mevcut oturumu kendiliğinden sahte kullanıcıya dönüştürmez ve Settings retry sunar.

SessionBootstrap root QueryClientProvider altındadır. Restore bitmeden Stack yerine yükleme görünümü vardır. Root `Stack.Protected` auth/protected gruplarını karşılıklı kapatır ve guard değişiminde eski history kayıtlarını kaldırır. Detay/modal rotaları da koruma altındadır. Index authenticated kullanıcıyı Dashboard'a yönlendirir. Tab backBehavior `history`; native Android geri davranışı cihaz kabulünde ayrıca kontrol edilmelidir.

Logout önce yerel erişimi kapatır, aktif query'leri iptal eder, kullanıcıya özel cache'i temizler; sonra server logout'u bekler. Network başarısızlığı login ekranında notice olur. Başarılı server logout bile access JWT'yi anında iptal etmez: backend varsayılan access ömrü 15 dakika +15 saniye skew; refresh ailesi iptal edilir. Pasif/silinmiş kullanıcı sunucunun aktif kullanıcı kontrolüyle engellenir. Bu faz push device kaydetmediği için device DELETE akışı eklenmedi; gelecekte push bağlantısı logout öncesi unregister gerektirir.

## Finans entegrasyonu ve Phase 4 sınırı

`features/payments` API/runtime DTO şemaları, precision-safe JSON, DateOnly/recurrence yardımcıları, query/mutation hook'ları ve form/detail bileşenlerini tutar. Home ve Payments gerçek API kullanır; finans query key'leri userId + sessionEpoch ile ayrılır. Foreground'da finans sorguları invalidate edilir. Create/update/delete yalnızca kendi scope'unu yeniler. Ayrıntılar [PAYMENTS_API.md](PAYMENTS_API.md).

Backend summary/upcoming hesapları finansal otoritedir. Sayfalı plan listesinden toplam veya tamamlanmış liste üretilmez. Para stringleri dahili lexical temsildir; wire'da numeric decimal olarak gönderilir. Form RowVersion snapshot'ı background refetch ile değişmez. Belirsiz mutation timeout'unda otomatik retry yoktur.

Preview yalnızca development + explicit flag + gerçek authenticated session ile Calendar/Subscriptions gibi henüz bağlanmayan ekranlarda görünür. Home/Payments/detail/form fixture kullanmaz. Premium tanıtımı satın alma sağlamaz. Gerçek Calendar/Subscriptions ve occurrence mutation'larına, yani Phase 4'e geçilmedi. Backend ve production kaynakları değiştirilmedi.
