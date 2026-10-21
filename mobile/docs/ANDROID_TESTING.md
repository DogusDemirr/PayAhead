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
