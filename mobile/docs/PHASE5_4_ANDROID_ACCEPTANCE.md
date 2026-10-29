# Phase 5.4 — Android kabulü (bekliyor)

Fiziksel cihaz sonucu bu faz için alınmadı. Otomatik render/RTL testleri veya Hermes export cihaz kabulü yerine geçmez. Test hesabı kullanın; kendi günlük hesabınızın şifresini üç kez değiştirerek kotasını gereksiz tüketmeyin. Hiçbir parola/token/istek gövdesini paylaşmayın.

1. Güncel Development API ve Metro ile Expo Go'yu açın; eski bundle yerine Reload yapın. Ayarlar → Gizlilik ve Güvenlik'e girin. Mevcut özel profil fotoğrafının Hesabım/Ayarlar'da görünmeye devam ettiğini kontrol edin.
2. Türkçe, İngilizce, Almanca ve Arapça başta olmak üzere 14 dilde başlıklar, uzun belge metinleri, büyük yazı, dark/light ve kaydırmayı kontrol edin. Arapçada metin/ok yönünü ve Android geri düğmesini deneyin; native yön için mevcut açık yeniden başlatma gereksinimi korunur.
3. Çıkış yapıp kayıt ekranında üç belgeyi ayrı ayrı açın ve geri dönün. Her belgede taslak/sürüm/eksik yayın bilgilerini görün. Genel açık rıza veya pazarlama checkbox'ı bulunmamalıdır; bu taslakta zorunlu sözleşme kabulü yoktur. Belgeye gidişte girilen alanlar korunmalıdır.
4. Parola tekrarı uyuşmazlığında kayıt API'ye gitmemeli. Göster/gizle, güç tahmini ve ekran okuyucu etiketini deneyin. Geçerli yeni test hesabıyla kayıt/giriş yapın.
5. Güvenlik ekranında gerçek kalan 3 hak ve oturum sayısını görün. Cihaz adı veya çevrimiçi garantisi beklemeyin. Uçak modunda hata/tekrar deneme görülmeli, uydurma hak sayısı gösterilmemelidir.
6. Yanlış mevcut parola ve daha önce kullanılmış parola deneyin; hak azalmamalı. Üç farklı geçerli normal değişiklik sonrası her defasında yeniden giriş gerekmesini ve kalan hakkın 2/1/0 olmasını doğrulayın. Eski açık oturumun tekrar yetkili çağrı yapamadığını kontrol edin.
7. Kota sıfırken sonraki hak zamanı UTC gösterilmeli, normal gönderim kapalı olmalı; hesap kurtarma bağlantısı erişilebilir kalmalı. Normal dördüncü değişiklik API'de de engellenir. 30 günlük süreyi geliştirme veritabanını elle değiştirerek kısaltmayın; sınır otomatik SQL testinde sabit saatle doğrulandı.
8. Kurtarma testi için gerçek SMTP veya açıkça yerel yakalama yapılandırması gerekir. Bu fazın API'sinde dış e-posta/push kapalıdır; güvenli genel yanıt teslim kanıtı değildir. Yetkili test bağlantısıyla eski parola reddi, yeni parola/tek kullanım ve yeniden girişi kontrol edin; token ekran görüntüsü paylaşmayın.
9. Hesap silme uyarısı, güncel parola ve ayrı modern onay korunmalı; Premium mağazada ayrıca iptal uyarısını okuyun. Yalnız silinmesi istenen geçici hesapla deneyin. Finansal verisi gereken hesabı silmeyin.
10. Dil, bildirim, üyelik, ödeme, takvim ve geçmiş ekranlarına gidiş/dönüşü kontrol edin. Hesap dışa aktarmada belge metadata'sı bulunabilir; parola geçmişi veya token bulunmamalıdır.

Fiziksel görsel taşma, ekran okuyucu, native RTL, geri navigasyonu ve profesyonel dil/hukuk kabulü açık kalır. Phase 6/7 veya production işlemi bu planın parçası değildir.
