# Android kabul matrisi — Frontend Phase 4

Phase 4 aşağıdaki kontrolleri fiziksel Android/Expo Go üzerinde bekliyor. Kullanıcı Phase 1–3 kayıt/giriş/CRUD başarısını bildirdi; bu bildirim yeni Phase 4 native akışlarının kanıtı değildir. Ön koşul: güncel backend, additive reset migration, çalışan Metro ve [SMTP/deep link kurulumu](../../docs/PASSWORD_RESET.md).

| Senaryo | Kabul ölçütü | Durum |
|---|---|---|
| 1. Takvim günleri | Ay ileri/geri, bugün, ödemeli gün noktaları; geciken/bekleyen/ödenen/atlanan metinleri | Bekliyor |
| 2. Tek ödeme | Doğru plan, DueDate, güncel plan tutarı, actualAmount ve PaidAtUtc ayrı | Bekliyor |
| 3. Ödendi | Gerçek tutar gir; tek gönderim; sunucu onayı sonrası takvim/Dashboard/analiz yenilensin | Bekliyor |
| 4. Atlandı/bekliyor | Atlandı paid toplamını artırmasın; bekliyor kaydı kaldırıp plan tekrarını göstersin; eski sürüm 409 | Bekliyor |
| 5. Geçmiş | Ay/durum değişimi, pagination; gelecek vade listelenmesin; empty/error retry | Bekliyor |
| 6. Başlangıç seçici | Mevcut tarih seçili; Android geri/iptal değiştirmesin; 31 Ocak ve 29 Şubat doğru | Bekliyor |
| 7. Bitiş seçici | Başlangıç alt sınırı; boşaltma; end-before-start hatası; düzenlemede mevcut tarih | Bekliyor |
| 8. Analiz | Dashboard bağlantısı; ayrı ekran; currency seçimi, 6 ay barları, durum donut, kategori ve top 5 | Bekliyor |
| 9. Tutar karşılaştırma | Aynı ay/currency paid Dashboard ile eşleşsin; yaklaşan ile tüm pending farkı doğru anlaşılsın | Bekliyor |
| 10. Reset isteği | Kayıtlı/kayıtsız aynı yanıt; Mailpit gerçek capture veya gerçek SMTP provider | Bekliyor |
| 11. Linkten reset | Expo Go açık/kapalı; LAN ve Metro portu; link fallback yapıştır; expired/replayed kod reddi | Bekliyor |
| 12. Yeni giriş | Eski parola reddi; yeni parola kabulü; eski refresh iptali; logout sonrası eski finans cache'i yok | Bekliyor |
| Görsel/erişilebilirlik | Light/dark/system, büyük font, küçük ekran, TalkBack, offline/loading ve klavye/geri davranışı | Bekliyor |

Takvim yedi sütun olduğundan dar ekranda her gün 48 dp genişliğinde olamaz; günler metinsel erişilebilir etiket ve 58 dp yükseklik taşır. Native okunabilirlik/taşma kabulü yapılmalıdır. Jest native picker/SVG testleri mock'tur; Hermes bundle, dokunmatik veya gerçek cihaz performansını doğrulamaz. Yerel SMTP capture dış inbox teslimi değildir. Production verified App Links/domain olmadan production deep link hazır denmez.

## Önceki Phase 3 kaydı

Kullanıcı Phase 1 tasarımı ve Phase 2 gerçek kayıt/giriş akışını fiziksel Android/Expo Go'da başarıyla denediğini bildirdi. Bu çalışma sırasında Phase 3 için bağlı telefon/emülatör veya kullanılabilir browser/app kontrol yüzeyi bulunmadı; aşağıdaki native kontroller **çalıştırılmadı**. RNTL input/switch/Alert testleri gerçek dokunmatik/klavye veya geri tuşu testi değildir.

| Kontrol | Beklenen / cihaz durumu |
|---|---|
| Tam kapat/aç, restore | Gerçek oturum geri gelir, restore sırasında yanlış ekran görünmez. Bekliyor; mock restore geçti. |
| Logout / başka kullanıcı | Eski Dashboard/list/detail görünmez; query verileri temizlenir, geri tuşuyla açılamaz. Bekliyor; cache/epoch testleri geçti. |
| İlk kullanıcı Dashboard | Örnek tutar yok; kaliteli boş görünüm ve ödeme ekleme CTA. Bekliyor; RNTL geçti. |
| Gerçek Dashboard | Aylık tutarlar para birimi bazında ayrı; timezone/context, bu ay geciken, en yakın ve yaklaşan kayıtlar doğru. Bekliyor; gerçek HTTP smoke geçti. |
| Pull-to-refresh | Ana Sayfa ve Ödemeler aşağı çekilince yenilenir; spinner ve hata/retry düzgün. Bekliyor. |
| Liste / filtre / sayfa | Arama, aktif/pasif, ISO para birimi ve kategori sunucuda uygulanır; toplam/sayfa bilgisi görünür. Bekliyor; API pagination smoke geçti. |
| Create | Tarih YYYY-AA-GG, virgüllü ondalık, kategori ve enum seçenekleri; çift basma tek mutation. Bekliyor; form/API testleri geçti. |
| Ay sonu / 29 Şubat | Başlangıç ilk tarihtir; sonraki tarihler ay sonuna kırpılır, sonraki Mart/artık yılda geri döner. Bekliyor; otomatik tarih testleri geçti. |
| Edit / 409 | Mevcut değerler yüklenir; eski sürüm yeni veriyi ezmez; açık reload değişiklikleri sıfırlar. Bekliyor; mock ve HTTP 409 geçti. |
| Silme / pasifleştirme | Onay diyaloğu iptal edilebilir; güçlü ETag kullanılır; geçmiş/ilişki varsa backend reddi anlaşılır. Bekliyor; Alert ve canlı CRUD testleri geçti. |
| Free limit | Yanıttaki activePaymentPlans limiti ve premium bilgi bağlantısı; sahte satın alma yok. Bekliyor; canlı 403 entitlement_limit geçti. |
| Network/timeout | Belirsiz write sonucu kullanıcıya açıklanır, otomatik çift create olmaz. Bekliyor; otomatik test geçti. |
| Klavye ve safe area | Uzun formun tüm alan/CTA'sı erişilebilir; decimal-pad ve Android resize; gesture ve 3-button nav çakışmaz. Bekliyor. |
| Küçük ekran/uzun isim/büyük tutar | 320/360 dp, uzun 150 karakter isim ve (19,4) tutarda kesilme/taşma yok. Bekliyor; tutar lexical testleri geçti. |
| Light/dark/system | Mevcut Inter/navy/teal sistemi ve semantik kontrast korunur. Bekliyor. |
| Geri / deep link | Edit/detail normal geri; logout sonrası eski korunan route açılamaz. Bekliyor. |

Çalıştırma: `cd mobile`, `npx.cmd expo start`, SDK 57 uyumlu Expo Go'da QR okut. Aynı güvenilir Wi-Fi, doğru LAN API adresi ve backend erişimi gerekir. [DEVELOPMENT.md](DEVELOPMENT.md) bağlantı adımlarını, [PAYMENTS_API.md](PAYMENTS_API.md) tarih/precision/sürüm kurallarını açıklar.

Cihaz sonucu kaydedilirken model, Android/Expo Go sürümü, ekran/font ölçeği, tema ve her satırın gözlenen sonucu yazılmalı. Token/parola/gerçek finansal veri ekran görüntüsüne veya loga konmamalı. Otomatik testler ve canlı Node HTTP smoke fiziksel Expo Go auth/CRUD kanıtı olarak sunulmaz.

---

## Önceki fazın tarihsel kontrol listesi

# Android kabul matrisi — Frontend Phase 2

Kullanıcı Phase 1 premium tasarımını fiziksel Android/Expo Go üzerinden beğendiğini bildirdi. Bu oturumda Phase 2 için fiziksel telefon/emülatör erişimi yoktur; aşağıdaki native kabul adımları **bekliyor**. Jest navigation-policy testleri Android donanım geri tuşunun gerçek cihaz testi değildir. Hermes export bir APK veya cihaz testi sayılmaz.

## Hazırlık

SDK 57 uyumlu [Expo Go](https://expo.dev/go), aynı Wi-Fi, çalışan SQL/backend ve bilgisayarın LAN IP'si gerekir. [Bağlantı kılavuzundaki](DEVELOPMENT.md) Kestrel/firewall/TLS adımlarını izle. Proje hazır; `cd mobile`, `npx.cmd expo start`, ardından QR okut. İlk hedef Android Studio gerektirmeyen fiziksel Expo Go'dur.

## Auth senaryoları — gerçek cihazda çalıştırılmadı

| Adım | Beklenen |
|---|---|
| İlk açılış | Restore tamamlanana kadar yükleme; sonra login. Demo giriş yok. |
| Geçersiz form | Email/boş parola, register 12 karakter/ad kuralları; field error okunabilir. |
| Register | 201 sonrası login ve kayıt başarı mesajı; otomatik oturum yok. |
| Login | Loading/disabled CTA, çift tıklamada tek giriş; yanlış kimlik genel mesaj. |
| Başarılı login | SecureStore yazımı ve /me tamamlanınca Dashboard; Ayarlar'da gerçek ad/email. |
| Uygulamayı kapat/aç | Güvenli kayıt okunur; gerekiyorsa rotation, /me doğrulaması; yanlış ekrana ani geçiş yok. |
| Token süresi | Access dolunca tek refresh; paralel istekler eski token'ı tekrar consume etmiyor. |
| Network/timeout | Kullanıcıya anlaşılır hata; token/parola console'a basılmıyor. |
| Arka plan/ön plan | /me tekrar doğrulanır; pasif/silinmiş hesap anonymous olur. |
| Logout | Local token ve query verileri temizlenir; login açılır. |
| Logout offline | Local çıkış tamam; server iptali belirsiz notice'u görünür. |
| Android geri | Login sonrası auth'a dönmez; detail/modal normal geri; logout sonrası eski protected ekran açılamaz. |
| Deep link | Oturumsuz tabs/detail/modal açılamaz; restore bitmeden protected render yok. |
| Klavye/küçük ekran | 320/360 dp'de Register son input/CTA kaydırılabilir; şifre göster/gizle çalışır. |
| Safe area | Gesture ve 3-button navigation içerik/buton kapatmaz. |
| Light/dark/font | Inter/Türkçe karakter, yüksek font ölçeği, badge ve hata kontrastı korunur. |
| Reduced motion/TalkBack | Hareket tercihi ve label/state/alert'ler doğru. |

Production HTTP reddi otomatik testte doğrulandı; gerçek cihaz production build testi yapılmadı. Web SecureStore oturumu desteklenmez. Örnek finans preview flag'i açılsa bile auth guard atlanamaz; varsayılan kapalıdır.

Sonuç kaydı: cihaz modeli, Android ve Expo Go sürümü, boyut/font ölçeği, API erişim yolu, test satırı ve gözlenen sonucu yaz. JWT/parola/refresh body ekran görüntüsü veya loga koyma. Bu faz push cihazı kaydetmez ve gerçek finans verisini bağlamaz; bunları auth başarısı olarak test etme.
