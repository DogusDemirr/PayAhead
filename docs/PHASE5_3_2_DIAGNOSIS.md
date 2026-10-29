## Ek düzeltme — Yükleme başarılı, avatar görünmüyor (29 Ekim 2026)

Canlı fiziksel Android denemesinde yükleme200dönerken hemen ardından görsel GET isteği401aldı; Metro'da avatar.display/image_error görüldü. Native Image doğrudan korumalı URL'yi çağırıyordu ve merkezi API istemcisinin refresh/yeniden okuma akışından geçmiyordu. Önce denenen katman/boyut düzenlemesi tek başına sorunu çözmedi; kullanıcı ikonun kaldığını bildirdi.

Son düzeltmede GET /api/users/me/avatar, session-aware apiClient üzerinden arraybuffer olarak okunur. GET401 için mevcut merkezi refresh/replay politikası kullanılır. JPEG MIME/imza/2MiB sınırı kontrol edilir, Hermes uyumlu kodlayıcıyla yalnız bellekte data URI oluşturulur. Native Image artık HTTP veya Bearer header taşımaz; doğrulanmış JPEG byte'larını gösterir. Görsel boyutları açıktır. Owner+epoch+rowversion cache anahtarı, geç yanıt iptali, logout/cache temizliği ve kaldırmada scoped image-cache temizliği korunur. Son gözlemciden sonra30s boyunca yalnız JS belleğinde tutulan kaynak SecureStore'a/dosyaya persist edilmez. Upload bildirimi authenticated görsel okuması doğrulandıktan sonra gösterilir; write200ama okuma başarısızsa avatar_unconfirmed döner.

**Fiziksel sonuç:** Kullanıcı aynı8085oturumunu tekrar açıp mevcut fotoğrafı yeniden yüklemeden gördüğünü açıkça doğruladı: “Evet, fotoğraf görünüyor”. Bu doğrulama Hesabım görünümü içindir; başka cihaz/OS/geniş dark-RTL kabulü yapılmış sayılmaz.

Dosyalar: mobile/src/features/account/avatar.tsx, yeni avatar-source.ts, avatar.test.tsx, avatar-transport.test.tsx ve yeni avatar-source.test.ts. Yeni testler: merkezi401refresh→JPEGokuma, native Image kaynağında token/HTTPolmaması, base64padding/chunkdoğruluğu, MIME/boyut reddi, eski oturum yanıtının iptali, sürüm değişimi ve okuma hatasında sahte başarı olmaması. Son doğrulama: TypeScript başarılı, ESLint0hata/0uyarı, i18n issue0; mobil325/325 (26suite), skipped0. Hermes export4180modül/9.2MBbaşarılı. Bu ek düzeltmede backend/model/migration değişmedi; önceki420unit/280SQLkanıtı tarihsel olarak korunur, yeniden çalıştırılmış gibi gösterilmez. Production veya yeni faz yok.

# Phase 5.3.2 — Profil fotoğrafı yükleme tanısı

29 Ekim 2026. Önceki Phase 5.2/5.3/5.3.1 çalışmaları korunur. Production, migration, veri sıfırlama veya Phase 6 push çalışması yapılmadı.

## Kanıtlanan durum ve cihaz kanıtının sınırı

Kullanıcı fiziksel Android'de galeri kırpması tamamlandıktan sonra hata gördüğünü bildirdi. Önceki denemenin HTTP kaydı/trace'i mevcut değil. Gösterilen `common:dialog.failed` metni tüm tanınmayan hataların fallback'iydi; bu metinden concurrency veya HTTP409 sonucu çıkarılamaz.

Bu oturumda iki somut Development bağlantı hatası bulundu:

1. Mobil Git dışında kalan `.env` API adresi `192.168.1.155:5080` idi; bilgisayarın gerçek LAN IPv4 adresi `192.168.1.110`. Eski adres erişilemedi. Başlangıçta Docker Engine ve yerel API de çalışmıyordu. Docker Desktop kullanıcı kurulumundan başlatıldı; mevcut SQL volume/container silinmedi.
2. API `appsettings.Development.json` AllowedHosts listesinde de eski `.155` vardı. Güncel `.110` üzerinden gerçek istek **HTTP400, HTML “Invalid Hostname”, ProblemDetails hata kodu yok** ile reddedildi. Bu uygulama endpoint'inden önceki host filtrelemesidir; AvatarException/concurrency/token/storage işlemi değildir. Mobil adres ve yalnız Development açık host listesi `.110` ile hizalandı. Wildcard veya production host güvenliği açılmadı. Düzeltme sonrası LAN `/health/ready`200 ve Metro `/status`200 doğrulandı.

API'nin varsayılan `artifacts/development` binary'si başlangıçta23 Ekim tarihliydi; güncel kaynaklardan yeniden derlendi. Bu dosyanın önceki telefon denemesinde kullanıldığı doğrulanamadı; eski binary olasılığı kesin cihaz nedeni diye sunulmaz.

SDK57'nin kurulu gerçek `expo/src/winter/fetch/convertFormData` kodu native `{uri,type,name}` parçasını `Unsupported FormDataPart implementation` ile HTTP oluşmadan reddeder. Bu davranış otomatik testte yeniden üretildi. Axios native XHR yolunda aynı parça geçerlidir; gerçek Axios XHR adapter testi bunu doğrular. Native avatar upload için XHR açıkça seçildi; Expo fetch fallback'ine bağımlılık kaldırıldı. Önceki fiziksel denemede hangi adapter'ın seçildiği kaydedilmediğinden bu uyumsuzluk o denemenin kesin istisnası olarak ilan edilmez.

**İlk fiziksel tekrar:** Kullanıcı yükleme sonrası ikon kaldığını bildirdi. Yukarıdaki ek düzeltme ve sonraki kullanıcı doğrulaması bu ilk kaydın güncel sonucudur.

## Gerçek HTTP ve backend sonucu

Güncel API ve gerçek Development SQL ile, yalnız oluşturulan geçici hesapta:

| Senaryo | Gerçek yanıt | Hata kodu / katman |
|---|---|---|
| Geçerli PNG |200; hasAvatar=true | Upload/normalize/storage/SQL başarılı |
| Geçerli JPG ile değiştirme |200; yeni profil sürümü | Eski dosya temizlenir |
| JPEG GET |200;512×512 JPEG, no-store, EXIF yok | Authenticated okuma |
| Yeniden giriş/profil okuma |200; hasAvatar=true | Sunucuda kalıcı |
| SVG / geçersiz içerik |400 | avatar_invalid / AvatarException |
|2MiB üstü dosya |413 | avatar_too_large / AvatarException |
| Güncel sürümle kaldırma |200; hasAvatar=false | Sonraki GET404 |
| Stale sürüm |409 | avatar_changed / AvatarException; SQL regresyonu |
| Yanlış LAN host |400 HTML | HostFiltering; code/AvatarException yok |
| Eski LAN IP erişilemez |HTTP yanıtı yok | Bağlantı |

`mobile/scripts/phase532-smoke.cjs`28 HTTP kontrolünü geçti; profil/ödeme regresyonlarını da içerir. Yalnız kendi hesabı ve kayıtları temizlendi. Geçerli upload'ın sunucuya kaydedilip mobil yanıt eşlemesinde hata verme durumu fiziksel cihazda kanıtlanmadı. Bu olasılık için HTTP200+bozuk DTO testinde başarı engellenir ve “sonuç doğrulanamadı” gösterilir; sonraki profil okuması gerçek durumu uzlaştırır.

Mevcut Serilog request middleware'i exception handler yanıtı400'e çevirmeden önce exception yolunu500 olarak kaydedebilir. Bu nedenle tek request log satırı gerçek istemci HTTP yanıtı sayılmaz. Avatar hataları için `ApiExceptionHandler` artık gerçek eşlenen status/code/ExceptionType/TraceId alanlarını ayrıca güvenli biçimde kaydeder. İstek gövdesi, token, parola, URI, fotoğraf ve dosya yolu eklenmez. Beklenen hatalı görsel testinde AvatarException gözlendi; geçerli JPG/PNG için backend decode/storage hatası yeniden üretilemedi.

## Frontend düzeltmeleri

- `features/account/avatar.tsx`: native XHR upload, tek `image` URI parçası, JPEG MIME/ad, quoted If-Match. Multipart Content-Type veya boundary elle atanmaz; RN networking üretir. Web Blob yolu korunur.
- `services/avatar-error.ts`: selection/preparation/multipart/request/response aşamaları ve HTTP status+allowlist code birlikte değerlendirilir. Development'ta yalnız bu güvenli alanlar `avatar.failure` olarak yazılır. Ham istisna mesajı/URI yazılmaz.
- `components/dialogs.tsx`: avatar hataları fotoğrafa özel mesajlara gider. Ağ, boyut, desteklenmeyen görsel, sunucu, oturum, gerçek conflict ve doğrulanamayan başarı ayrı yönetilir. Ödeme409 kodları korunur.
- Yalnız `409 + avatar_changed` güvenli profil refetch'i başlatır. Sahiplik/epoch doğrulanıp profil/current-user cache ve avatar sürümü güncellenir. Yeni rowversion ile otomatik write retry yapılmaz. Başka409 iş kuralı concurrency diye yorumlanmaz.
-200 yanıt DTO'su ve hasAvatar işlem sonucu kontrol edilir. Hatalı yanıt başarıya dönüşmez; eski doğrulanmış cache iyimser biçimde silinmez.401 merkezi refresh/session politikasından geçer; belirsiz yazı otomatik tekrarlanmaz.
- `AccountScreen`: yeni denemede önceki başarı bildirimi temizlenir. Picker iptali hata göstermez; loading/tek işlem kapısı ve baş harf fallback korunur.

## Güvenlik ve veri

User rowversion ve güçlü If-Match kaldırılmadı. Tek dosya, gerçek JPEG/PNG/WebP decode,2MiB/2048piksel sınırı,512 JPEG reencode/EXIF-GPS temizliği, özel storage, kullanıcı izolasyonu,5/dakika verified-user limiti, eski dosya/account cleanup ve session güvenliği korunur. Domain/finansal model değişmedi. Yeni migration yok; mevcut ProfileAvatar şeması gerçek HTTP/SQL testleriyle kullanıldı. Gerçek kullanıcı finansal kayıtları üzerinde test silmesi yapılmadı.

Altı hata metni14 dilde güncellendi/eklendi: failed/invalid/large/network/retry/unconfirmed. Üçü yeni semantic anahtar. TR/EN/DE/FR/ES/IT/pt-BR/NL/RU/AR/zh-Hans/JA/KO/HI kaynaklarında missing/duplicate/placeholder/JSON/raw UI hatası0. AI taslakları profesyonel dil kabulü değildir.

## Testler

- Native RN FormData alanları, gerçek Axios XHR Authorization/If-Match ve otomatik boundary sözleşmesi.
- SDK57 gerçek fetch encoder'ının eski URI parçasını HTTP öncesi reddetmesi.
- Gerçek409 recovery, otomatik write retry olmaması, yanlış409/500 kodlarının concurrency sayılmaması.
- HTTP200+bozuk DTO'nun başarı olmaması; cache ve eski fotoğrafın korunması.
-14 dilde hata mesajları; logların URI/ham istisna sızdırmaması.
- Önceki profil, şifre, hesap silme, ödeme, abonelik ve diğer mobil regresyonlar.

Mobil319/319 (25suite), TypeScript ve ESLint başarılı; i18n issue0; Doctor21/21; Hermes4179modül/9.2MB başarılı. İlk backend restore/build0hata0uyarı, unit420/420 ve gerçek SQL Server2022 Testcontainers280/280; skipped0. Son güvenli backend log/config değişikliği sonrası tam tekrar da420/420 unit ve280/280 gerçek SQL ile geçti; skipped0. Kanıt dosyaları [IMPLEMENTATION_PROGRESS.md](IMPLEMENTATION_PROGRESS.md) içindedir. Test altyapısındaki Jest browser FormData/XHR otomatik mock farkları düzeltilmiştir; ilk başarısız testler başarı diye sayılmamıştır.

## Android tekrar adımları

1. Aynı Wi-Fi'da `exp://192.168.1.110:8085` Expo Go oturumunu açın. Eski Metro bundle'ını yeniden kullanmayın.
2. Gerekirse tekrar giriş yapın; Hesabım'dan fotoğraf seçip kare kırpın. Spinner sonrası fotoğraf ve başarı bildirimi beklenir.
3. Ayarlar avatarını, kapat/aç veya yeniden giriş sonrası kalıcılığı ve kaldırınca baş harflerin gelmesini kontrol edin.
4. Hata varsa yalnız yeni kullanıcı mesajını ve `avatar.failure` stage/status/code alanlarını paylaşın; token/URI/şifre paylaşmayın.
5. DHCP ile IP tekrar değişirse mobil API kökü ve Development AllowedHosts birlikte güncellenip API/Metro yeniden başlatılmalı. Production host listesine wildcard eklemeyin.

PowerShell bu makinede Start-Development.ps1 çalıştırmasını execution policy nedeniyle reddetti. Politika değiştirilmedi; onaylı normal .NET CLI build ve derlenmiş Development DLL çalıştırması kullanıldı. Script politikası açıksa mevcut script güncel kaynakları derler; aksi halde aynı standart CLI işlemleri kullanılabilir. Production deployment yoktur.
