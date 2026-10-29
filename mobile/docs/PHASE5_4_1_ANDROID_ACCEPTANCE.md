# Phase 5.4.1 — Android kabulü

Fiziksel cihaz kabulü henüz yapılmadı. Güncel LAN `192.168.1.155`; eski `.110` adresi bu oturumda geçersizdi. API Development host listesi ve ignored `mobile/.env` API adresi `.155:5080` ile hizalandı. DHCP değişirse tekrar doğrulayın; production host listesine wildcard eklemeyin.

1. Güncel Metro'yu açın: `cd mobile`, `npx.cmd expo start --go --lan --port 8085`. Expo Go'da yeni gösterilen adresi açıp Reload yapın. API 5080 üzerinde güncel kaynaklarla çalışmalıdır; gerekirse yeniden giriş gerekir.
2. Çıkış yapıp yeni hesap ekranına girin. KVKK, Gizlilik ve Kullanım Koşulları kartlarının üçünün de nötr/teyitsiz olduğunu görün. Kayıt alanlarını doldurup Hesap Oluştur'a basın: buton erişilebilir kalmalı; eksik belgeler vurgulanmalı, kayıt oluşmamalıdır.
3. Her kartı ayrı açın. Belge başlığını, taslak ve sürüm bilgisini, uzun metinleri ve kaydırmayı kontrol edin. En alta kaydırmak kendi başına kutuyu işaretlememelidir.
4. KVKK için “okudum ve bilgilendirildim”, Gizlilik için “okudum”, Koşullar için “kabul ediyorum” kutularını bilinçli olarak işaretleyin. KVKK/gizlilik teyitleri açık rıza değildir. Her birinden Android geri ile dönün; o kart tikli görünmeli, form alanları korunmalıdır.
5. Bir belgeyi yeniden açıp kutusunu kaldırın. Yalnız o kart teyitsiz olmalı; gönderim tekrar engellenmelidir. Üç teyit tamamlandığında geçici yeni hesapla kayıt/giriş yapın. Pazarlama rızası veya başka bir isteğe bağlı veri izni istenmemelidir.
6. Parola boşken güç barı ve seviye etiketi görünmemeli. İlk karakterde görünmeli, yazdıkça değişmeli; tamamen silince tekrar gizlenmelidir. Ayrılan küçük alan nedeniyle kayma sınırlı olmalı. Göster/gizle ve parola tekrarı çalışmalı; güç göstergesi parola politikasının yerine geçmemelidir.
7. TalkBack ile belge kartı durumlarını, işaretsiz/işaretli checkbox'ı ve güç seviyesini dinleyin. Uzun etiketleri, büyük yazıyı, light/dark ve Türkçe/İngilizce/Almanca/Arapça başta olmak üzere 14 dili kontrol edin. Arapça metin/ok yönünü ve native RTL yeniden başlatma sınırını kontrol edin.
8. Kayıt ekranından tamamen çıkıp yeni kayıt başlatın veya uygulamayı kapatıp açın: eski teyitler yeni kayda aktarılmamalıdır. Aynı kayıt içinde belgeye gidiş/dönüşte seçimler korunmalıdır.
9. Mevcut kullanıcıyla giriş, Hesabım/avatar, parola değiştirme, dil/bildirim ayarları ve ödeme ekranlarının çalıştığını kontrol edin. Gizlilik/Güvenlik ekranında üç hukuki belge menüsü artık bulunmamalı; güvenlik/kota/haklar/hesap silme açıklamaları kalmalıdır.

Gerçek kişi hesabını sırf test için silmeyin; kayıt senaryolarında geçici hesap kullanın. Parola/token/reset bağlantısı veya istek gövdesini paylaşmayın. Bu planın sonucu cihazdan gelmeden fiziksel kabul tamamlandı sayılmaz. Hukuki metinler ve çeviriler yayın öncesi uzman incelemesi gerektiren taslaklardır.
