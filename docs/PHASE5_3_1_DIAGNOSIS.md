# Phase 5.3.1 — Plan silme, dialog ve profil fotoğrafı

29 Ekim 2026. Mevcut Phase 5.2/5.3 çalışma ağacı korunmuştur. Production deployment veya Phase 6 push çalışması yapılmamıştır.

## Kesin silme nedeni

Doğrulanan yerel SQL hedefi `127.0.0.1:14333 / PayAheadDockerDevelopment` üzerinde salt okunur incelemede bir pasif plan bulundu: occurrence geçmişi yok, fakat hem reminder hem tracked subscription bağlantısı var. Bu planın silinmesi mevcut iş kuralları gereği yasak. Kayıt değiştirilmedi veya zorla silinmedi.

Backend bu iş kuralını da gerçek concurrency çatışması gibi HTTP **409** ile bildiriyordu. Mobil genel 409 eşlemesi kullanıcıya “güncel kaydı yükle” diyordu; yenilemek bağlantılı kayıt yasağını çözmez. İkinci sorun, pasife alma yanıtındaki yeni rowVersion'ın detay cache'ine hemen yazılmamasıydı. `If-Match` sözleşmesi hatalı değildi: sekiz SQL rowversion baytının 12 karakter Base64 değeri çift tırnaklı header ile gönderilir.

## Değişiklikler

- `PaymentPlanStore` ve `PaymentDeletionRestrictedException`: `payment_linked_subscription` ve `payment_delete_related` ayrı kodları; HTTP409 korunur. FK ve rowversion kontrolleri devam eder. Hatırlatıcı/geçmiş/abonelik zorla veya cascade ile silinmez.
- `ApiExceptionHandler`: gerçek DELETE concurrency için `payment_changed`; güvenli ProblemDetails kodları. Ham SQL/istisna metni, token ve dosya yolları yeni yanıtlara eklenmez.
- Mobil `payments/queries.ts`: başarılı create/update yanıtı güncel detay cache'ine hemen yazılır. Doğrulanmış delete sonrasında kendi detay cache'i kaldırılır, listeden kayıt çıkarılır ve finans sorguları geçersizleştirilir. Başarısız delete listeyi silmez. Oturum epoch sınırı korunur.
- `PaymentDetailScreen`: normal akış tek onay → DELETE → liste. Gerçek çatışmada sessiz refetch yapılır, kullanıcı güncel planı inceleyip yeniden onaylar. Otomatik yeni sürümle tekrar DELETE gönderilmez. Zorunlu “güncel kaydı yükle” ara adımı kaldırıldı.

## Ortak dialog sistemi

`mobile/src/components/dialogs.tsx`: ConfirmDialog, DestructiveConfirmDialog, InfoDialog, ErrorDialog, SuccessFeedback ve useAppDialog. Temalı lacivert/teal kart, kırmızı yıkıcı işlem, kaydırılabilir uzun içerik, RTL, erişilebilir başlık/duyuru, loading ve senkron çift tıklama kapısı vardır. İşlem sürerken geri/backdrop/kapat iptali engellenir; başlamadan vazgeçilebilir. Ağda sonucu belirsiz işlem başarılı veya kesin değişmemiş gibi sunulmaz.

Ödeme detayı, abonelik detayı, hatırlatıcı silme, cihaz kaydı kaldırma, hesap silme ve native RTL yeniden başlatma uyarısı ortak tasarımı kullanır. Hesap silmenin parola doğrulaması ve ayrı açık onayı korunur. Küçük başarılar inline gösterilir.

20 yeni semantic metin ×14 dil: TR, EN, DE, FR, ES, IT, pt-BR, NL, RU, AR, zh-Hans, JA, KO, HI. Eksik/duplicate/placeholder/JSON ve taranan hardcoded UI hatası0. Eski genel silme mesajı extraction manifestinden silinmedi; gerekçeli `retired-source.json` kaydıyla tarihsel kaynak korunur. Çeviriler AI taslağıdır; profesyonel dil kabulü yapılmadı.

## Profil fotoğrafı

Hesabım avatarı kamera simgesiyle açılan ortak action sheet üzerinden seçilir/kaldırılır. Fotoğraf varsa kaldırma görünür. SDK57 sistem galeri seçicisi geniş galeri/kamera/mikrofon izni istemez; seçilen fotoğrafa sistem erişimi kullanılır. Kare kırpma, 512×512 JPEG ve %85 kalite optimizasyonu yapılır. İptal mevcut fotoğrafı korur; hatada iyimser fotoğraf değişikliği yapılmaz, sunucunun güncel durumu yeniden doğrulanır. Ayarlar avatarı da aynı authenticated fotoğrafı gösterir; hata halinde baş harfler kalır.

[Depolama, endpoint, limit ve migration ayrıntıları](AVATARS.md).

## Doğrulama ve kabul sınırı

Güncel kesin test sayıları [uygulama ilerlemesi](IMPLEMENTATION_PROGRESS.md) içinde kaydedilir. İlk tam koşu unit418/418 ve gerçek SQL277/277 geçti; yeni son testlerle sayılar artar. Son mobil kapsam: dialog14 dil/tema/geri/busy, silmede cancel/cache/gerçek conflict, picker iptali/izin/optimizasyon, upload/remove cache ve hata koruması. Gerçek loopback HTTP smoke 26 kontrol geçti; yalnız kendi geçici hesabı ve kayıtları temizlendi.

SQL testleri gerçek SQL Server2022 Testcontainers ve rastgele `PayAheadTest_<guid>` DB kullanır; SQLite/InMemory kanıtı değildir. Üç önceki SQL/avatar testi ve son ek rate-limit testi; sahiplik, gerçek JPEG pikseli/EXIF temizliği, boyut, stale version, kalıcılık, eski dosya ve hesap silme temizliğini doğrular. Unit test commit sonucu belirsizken DB'nin referans verdiği dosyanın silinmediğini de doğrular.

Fiziksel Android/Expo Go galeri kırpması, TalkBack, küçük ekran/uzun metin, dark tema ve native RTL kabulü bu bilgisayar testleriyle kanıtlanmış değildir. [Android kontrol listesi](../mobile/docs/ANDROID_TESTING.md) izlenmelidir. İş kuralı gereği silinemeyen mevcut plan bu sürümden sonra da silinmez; açıklaması düzelir.

Bu kapsam sonunda durulur; Phase 6/7 başlatılmaz.
