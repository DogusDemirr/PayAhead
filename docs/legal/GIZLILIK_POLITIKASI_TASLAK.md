# Gizlilik Politikası — taslak

Sürüm: `2026-10-29-draft-1`. Yayına hazır veya mevzuata tam uyumlu belge değildir. Veri sorumlusu, iletişim, somut işleme/aktarım dayanakları ve production alıcıları **Yayın öncesi doldurulacak**. [Aydınlatma taslağı](KVKK_AYDINLATMA_TASLAK.md) ayrıca sunulur.

## Veriler ve güvenlik

Hesap, ödeme/abonelik kayıtları, tercihler ve seçilirse fotoğraf/bildirim/mağaza verileri ilgili özellik için işlenir. Parolalar mevcut Identity PasswordHasher ile salt içeren tek yönlü özetler olarak tutulur; uygulama geçmiş parolayı geri çözmez. Aktif kullanıcı ve kaynak sahipliği API'de doğrulanır. Fotoğraf yalnız yetkili hesap erişimiyle okunur, mobil özel cache hesap/oturum sınırına bağlıdır. Bu kontroller mutlak güvenlik, uçtan uca şifreleme veya sertifikasyon garantisi değildir.

Parola değişikliği ve başarılı kurtarma oturumları iptal eder; yeniden giriş gerekir. Aktif oturum sayısı geçerli refresh ailelerinden hesaplanır, cihaz adı veya çevrimiçi olma kanıtı değildir. İstemciye parola geçmişi/özetleri verilmez. Küçük yerel yaygın parola listesi vardır; tam sızıntı veritabanı taraması yapılmaz, parola üçüncü tarafa sorgulatılmaz.

## Mevcut teknik saklama

Bu süreler kod davranışıdır; hukuki saklama gerekçesi veya kesin silinme SLA'sı değildir:

- Bilinen önceki parola özetleri aktif hesap ömrünce tutulur; doğrulamalı hesap silmede kaldırılır. Tüm bilinen eski parolaları reddetmek bu saklamayı gerektirir; eski özetler zamanla otomatik düşürülmez. Normal değişiklik başarı zamanları 30 günlük pencere dışına çıkınca kotada sayılmaz; sonraki başarılı normal değişiklikte veya etkin, sınırlı partilerle çalışan bakımda zaman alanı boşaltılır. Bakım kapalıysa tam 30. günde fiziksel temizleme garantisi yoktur.
- Kayıt belge sürümü ve kayıt zamanı hesap silinene kadar tutulur; mevcut kayıtta istemci sürüm bildirmemişse sunulma/sürüm alanı boştur. İstemci tarafından bildirilen sunum bilgisi açık rıza veya okuma kanıtı değildir. Eski hesaplara geçmiş onay uydurulmaz.
- Refresh özetleri için varsayılan bakım eşiği 120 gündür; son kullanma/iptal zamanı ve ailedeki diğer kayıtlar dikkate alınır. Bu, giriş oturumunun 120 gün geçerli olduğu anlamına gelmez.
- Süresi geçmiş reset token kayıtları, aynı kullanıcı için sonraki üretim sırasında bir günlük ek eşikten sonra temizlenir; token geçerliliği ayrıca kısa süre ve tek kullanım kontrolüyle sınırlıdır. Bir gün sonra kesin toplu silme iddiası yoktur.
- Bildirim teslim metadatasında son durum ve vade üzerinden 400 günlük teknik temizlik eşiği vardır; worker çalışmasına bağlıdır. Canlı hesap için genel finansal kayıt yaşına göre otomatik silme yoktur.
- Değiştirilen/kaldırılan avatar silinir; başarısız fiziksel silmeler için 15 dakikada bir orphan taraması ve bir saatlik güvenlik aralığı vardır. Backup ve operatör kopyaları bu taramaya dahil değildir.

## Erişim, dışa aktarma ve silme

Kullanıcı kendi verilerini hesap ekranından dışa aktarabilir; büyük hesaplarda mevcut boyut sınırı nedeniyle doğrulanmış destek süreci gerekebilir. Çıktıda parola, token veya parola geçmişi yoktur. Hesap silme güncel parola ve ayrı onay ister; finansal kayıtlar, tercihler, fotoğraf ve güvenlik geçmişi bu kullanıcı için kaldırılır/scrub edilir, oturumlar geçersizleşir. İşlem geri alınamaz.

Mevcut mağaza anti-replay bağları ve pasif takma kimlik kalabilir; bunlar anonim kabul edilmez. Bu bağların amacı, erişim yetkisi, hukuki dayanağı ve nihai temizleme süresi **Yayın öncesi doldurulacak**. Mağaza aboneliğinin yenilenmesi hesap silmeyle iptal edilmez; mağazada ayrıca kapatılmalıdır. Kabul edilmiş push bildirimi geri çağrılamayabilir.

Backup saklama/silme süreleri, geri yüklemede silme taleplerinin tekrar uygulanması, production erişim/log saklama ve yurt dışı aktarım ayrıntıları **Yayın öncesi doldurulacak**. Tüm kopyalardan anında ve kesin silinme iddiası yoktur. Hak başvurusu için doğrulanmış iletişim adresi **Yayın öncesi doldurulacak**; ayrıca [teknik veri rehberi](../DATA_PRIVACY.md) bulunur.
