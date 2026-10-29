# KVKK Aydınlatma Metni — taslak

Sürüm: `2026-10-29-draft-1`. Hukuki inceleme ve yayın onayı tamamlanmamıştır. Uygulamadaki 14 dil metni de AI taslağıdır. Bu belge açık rıza formu değildir; hizmet kullanımına ilişkin genel veri işleme izni vermez.

## Veri sorumlusu ve başvuru

Veri sorumlusunun gerçek kişi/tüzel kişi kimliği, adresi, yetkili iletişim kanalı ve doğrulanmış başvuru usulü: **Yayın öncesi doldurulacak**. Depodaki proje adı bir şirket kimliği olarak kabul edilmemiştir.

## Sistemden doğrulanan veri ve amaçlar

Hesap için ad, e-posta, güvenli parola özeti ve kimlik doğrulama kayıtları; kullanıcı tarafından girilen ödeme planları, ödeme oluşumları/geçmişi, takip edilen abonelikler ve hatırlatıcılar işlenir. Dil, saat dilimi ve bildirim tercihleri hizmetin kişiselleştirilmesi için kullanılır. Kullanıcı isterse profil fotoğrafı ekler; fotoğraf eklemek hesap açma şartı değildir. Bildirim özelliği kullanıldığında cihaz/push bilgileri, gerçek mağaza doğrulaması kullanıldığında üyelik ve satın alma kanıtları işlenir. Telefon, adres veya doğum tarihi bu fazda istenmez.

Parola değişikliği güvenliği için güncel ve sistem tarafından bilinen önceki parola özetleri denetlenir. Yeni parola hiçbir zaman düz metin olarak geçmişe yazılmaz. Geçmişte kaydedilmemiş eski parolalar bilinmez. Normal değişiklik kotası için başarı zamanları UTC tutulur. Kayıt isteğinde istemcinin bildirdiği belge sürümü ve kayıt zamanı saklanır; bu, kişinin belgeyi okuduğuna veya açık rıza verdiğine ilişkin kanıt değildir.

Her veri kategorisinin amacıyla eşleştirilmiş somut KVKK işleme şartı, zorunluluk değerlendirmesi ve gerekiyorsa ayrı açık rıza süreci: **Yayın öncesi doldurulacak**. Sistemde pazarlama rızası toplanmaz; gelecekte böyle bir amaç eklenirse ayrı, isteğe bağlı ve geri alınabilir süreç gerekir.

## Toplama ve aktarım

Veriler mobil uygulama/API istekleri, kullanıcının tercih ettiği özellikler ve etkinleştirilmiş mağaza/bildirim entegrasyonları üzerinden alınır. Geliştirme ortamında SQL Server, özel avatar dosyaları ve yapılandırılırsa Mailpit vardır; Mailpit dış posta kutusuna teslim etmez. Expo push, Apple/Google mağaza ve SMTP sağlayıcısı yalnız ilgili gerçek entegrasyon etkinse aktarım tarafı olabilir. Bu fazda yeni dış gönderim yapılmadı.

Production barındırma, gerçek SMTP ve diğer veri işleyenlerin kimliği, alıcı grupları, ülkeleri, aktarım kapsamı ve uygulanacak yurt dışı aktarım mekanizması: **Yayın öncesi doldurulacak**. Depoda entegrasyon kodu bulunması production hizmetinin kullanıldığı anlamına gelmez.

## Saklama ve haklar

Mevcut teknik saklama/silme davranışları [Gizlilik Politikası taslağında](GIZLILIK_POLITIKASI_TASLAK.md) açıklanır. Nihai hukuki saklama takvimi ve backup politikası: **Yayın öncesi doldurulacak**.

İlgili kişi; verilerinin işlenip işlenmediğini öğrenme, bilgi isteme, amaç ve amaca uygun kullanımı öğrenme, aktarılan tarafları bilme, düzeltme/silme talepleri ve koşulları oluştuğunda diğer kanuni haklarını kullanabilir. Kimliği doğrulanmış talepler için yetkili başvuru adresi/usulü **Yayın öncesi doldurulacak**. Uygulamadaki hesap dışa aktarma ve doğrulamalı silme araçları tüm hukuki başvuru sürecinin yerine geçtiği iddiasını taşımaz.

Resmi dayanaklar: [KVKK aydınlatma açıklaması](https://www.kvkk.gov.tr/Icerik/2033/Aydinlatma-Yukumlulugu-), [Aydınlatma Tebliği](https://www.kvkk.gov.tr/Icerik/4132/aydinlatma-yukumlulugunun-yerine-getirilmesinde-uyulacak-usul-ve-esaslar-hakkinda-teblig). Somut yayına uygunluk hukuk incelemesi gerektirir.
