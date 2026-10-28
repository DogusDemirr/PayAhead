# Phase 5.3 — Premium profil ve hesap yönetimi (28 Ekim 2026)

## Sonuç ve ekranlar

Ayarlar'daki Hesap Bilgileri bilgi placeholder'ı gerçek `/account` ekranına bağlandı. Avatar/ad/e-posta, kişisel bilgiler, güvenlik, dil/bölge, bildirimler, üyelik ve hesap işlemleri eklendi. Ad soyad güncellenir; e-posta salt okunurdur. Doğrulanmış e-posta rozeti uydurulmadı. Premium/Free, gerçek entitlement, bitiş tarihi ve server limitlerinden gelir; sorgu doğrulanamazsa üyelik bilinmiyor gösterilir. Mevcut dil/bildirim/saat dilimi ekranları tekrar kullanılır. Mağaza aboneliği kullanıcı tarafından mağazada yönetilir.

35 yeni metin ×14 dil =490 AI taslak çeviri tamamlandı. Eksik/duplicate/placeholder/plural/JSON/source audit hatası0. Türkçe, İngilizce, Almanca, Fransızca, İspanyolca, İtalyanca, pt-BR, Hollandaca, Rusça, Arapça, zh-Hans, Japonca, Korece ve Hintçe kapsamındadır. Dil değişimi profil taslağını silmez. Çeviriler profesyonel dil kabulü değildir.

## Backend ve migration

Yeni `GET/PUT /api/users/me/profile` ve `POST /api/users/me/change-password`; mevcut `/api/users/me`, settings, language, entitlement ve DELETE korunur. DTO ek alanları reddeder, sahiplik doğrulanmış UserId'den gelir. Profil 1–100 karakter ve SQL rowversion concurrency kullanır. Parola 12–128, farklılık/tekrar/mevcut hash doğrulaması; credential5/dakika ve profile30/dakika SQL rate limit vardır.

`ProfileSecurity` migration'ı User'a iki sütun ekler: RowVersion ve SecurityStamp. Doğrulanmış Docker Development DB'ye uygulandı. Eski kullanıcı kolonlarının ve PaymentPlans/PaymentOccurrences/TrackedSubscriptions/UserSettings/RefreshTokens/ReminderRules/DeviceTokens/CatalogCategories/CatalogServices/CatalogCategoryTranslations tablolarının checksum ve satır sayıları aynı kaldı. Mevcut kullanıcı/finansal veriler değiştirilmedi; DB/volume sıfırlanmadı. SDK8.0.426/net8.0/C#12/EF8.0.21 korundu.

## Güvenlik

Şifre değişimi tüm refresh'leri revoke eder, eski access token'ları security stamp ile reddeder, reset token'larını tüketir ve cihazları pasifleştirir. Yeni parola ile yeniden giriş gerekir. Eşzamanlı eski login/session ekleme yarışı engellendi. JWT doğrulama ve refresh reuse davranışı korunur. Validation mesajları parola karşılaştırma değerlerini içermez; şifre/token loglanmaz ve yeni parolalar SecureStore'a yazılmaz. Mutation retry kapalı, değişken cache süresi0 ve reset kullanılır. Oturum epoch/owner kontrolü eski yanıtın başka hesaba yazılmasını engeller.

Silme için mevcut backend kullanılır: parola + ayrı açık onay, 204 doğrulanması ve session invalidation. Kullanıcının finansal kayıtları silinir; takma kimlikli sahip ve sınırlı mağaza anti-replay bağları tutulur. Tam anonimlik/tüm verinin fiziksel yok oluşu iddiası yoktur. Hesabı silmek mağaza aboneliğini iptal etmez. Offline/timeout sonucu belirsiz credential işlemi başarı gibi sunulmaz.

## Test ve araç sonuçları

| Kontrol | Sonuç |
|---|---|
| .NET restore/build | Başarılı, 0 hata/0 uyarı |
| Backend unit | 413/413, atlanan0 |
| Gerçek SQL Server 2022 integration | 274/274, atlanan0 |
| TypeScript / ESLint | Temiz |
| Mobil Jest | 266/266, 22 suite, atlanan0 |
| 14 dil i18n audit | Eksik/hata0 |
| Expo Doctor | 21/21 |
| Expo install --check | SDK57 uyumlu |
| Hermes Android export | Başarılı; 4.167 modül, 9,2 MB HBC, 36 asset; APK/cihaz kabulü değildir |
| Gerçek localhost HTTP | 31 ana kontrol +3 cleanup başarılı |

SQL kanıtları `artifacts/phase53-verified/test-results/phase53-verified_net8.0_20261028231226.trx` ve `phase53-verified_net8.0_20261028232122.trx` dosyalarındadır. SQLite/InMemory kullanılmadı. İlk test turlarında üç tarihsel migration testi, güncel User modeliyle eski şemaya kayıt yazdığı için kırıldı; tarihi şemaya uygun parametreli insert/plan seed ile düzeltildi. Son tam tur274/274geçti. Yeni endpointlerde actual JWT/access/refresh invalidation, rowversion, ek alan/kimlik/e-posta engeli, rate limit, parola sızdırmayan hata ve finansal kayıtların değişmezliği doğrulandı.

Mobilde 14 dil profil görüntüsü, geçersiz ad, başarılı güncelleme/cache/session refresh, stale response/owner fence, yanlış parola/alan temizliği, şifre mismatch, offline belirsizlik, silme onayı, Free/Premium/unknown/expiry, light/dark, geri gezinme ve dil değişiminde taslak korunması test edildi. Mevcut Payments BigInt regresyon sınırı tüm testlerde korunur.

Canlı test ayrı5093API üzerinde gerçek JWT ile profil okuma/yazma/409/400, diğer kullanıcı izolasyonu, yanlış mevcut parola, bütün eski token'ların reddi, yeni parola login, gerçek Free entitlement, dil/saat dilimi settings ve hesap silme/oturum reddini doğruladı. İlk smoke'ta yalnız test scriptinin entitlement URL'si yanlıştı; `/api/premium/entitlements` ile düzeltilip geçti, ilk turun kendi hesapları da temizlendi. Push/SMTP kapalıydı, provider teslimi yapılmadı.

## Açık kabul ve duruş

[10 Android senaryosu](ANDROID_TESTING.md) fiziksel cihazda uygulanmadı. Güncel backend'in yeniden başlatılması ve Metro bundle'ının yenilenmesi gerekir. Native RTL/karakter/taşma ve hedef dil uzmanı kabulü açıktır. E-posta değişikliği/doğrulama veya gerçek mağaza satın alma altyapısı bu fazda eklenmedi. Önceki gerçek push/inbox kabul açıkları kapatılmış sayılmaz.

[Model, saklama politikası, komutlar ve endpoint ayrıntıları](../../docs/ACCOUNT_MANAGEMENT.md). Production deployment, gerçek kullanıcı hesabı silme veya yeni Phase6/7 çalışması yapılmadı. Phase5.3 kodu ve otomatik doğrulama tamamlandı; fiziksel/dilsel kabul açık olarak duruldu. Geçici5093API ve8091Metro süreçleri kapatıldı; kullanıcının eski API/SQL/Mailpit süreçleri korunur.
