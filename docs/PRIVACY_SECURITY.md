# Phase 5.4 — Gizlilik ve hesap güvenliği

29 Ekim 2026. Bu belge teknik davranışı açıklar; hukuki uygunluk onayı değildir. Yalnız Phase 5.4 uygulanır. Mevcut avatar, hesap, ödeme ve i18n akışları korunur.

## Ekranlar ve sözleşmeler

Ayarlar → Gizlilik ve Güvenlik; hesap güvenliği, normal parola değişim kotası, gerçek oturum sayısı, sürümlü üç hukuki belge, veri hakları ve mevcut hesap silme akışına bağlantı sunar. Dil, bildirim veya hesap silme için ikinci ayar sistemi yoktur. Kayıt ve kurtarmada parola tekrarı/göster-gizle ve erişilebilir güç tahmini vardır; gösterge güvenlik garantisi değildir. Kayıt formu mevcut tekrar alanını reset ekranından bağımsız doğrular.

- `GET /api/legal`: herkese açık taslak manifesti; `version`, `isDraft`, `requiresTermsAcceptance`, `documentIds`. Çevrilmiş metinler mobil pakettedir; bu endpoint tam belge gövdesi döndürmez.
- `GET /api/users/me/security`: JWT + aktif kullanıcı; yalnız kendi kalan değişim hakkı, 3/30 gün politikası, sonraki UTC hak tarihi, hesaplama zamanı ve aktif refresh aile sayısı. Profile ortak 30/dakika SQL bütçesi, `no-store`.
- Mevcut `POST /api/auth/register` isteğine isteğe bağlı `legalVersion` eklenir. Güncel mobil sürümü gönderir; eski istemciyle uyumluluk için eksik alan kabul edilir ve sunum kanıtlanmış sayılmaz. Eski veya bilinmeyen sürüm 400 doğrulama hatasıdır.
- Mevcut normal değişiklik/reset endpoint'leri aynı kalır. `password_common`/`password_reused`: 400; normal `password_change_limit`: 409 ProblemDetails. Aynı yeni/mevcut parola, mevcut input validator tarafından da 400 alan doğrulamasıyla reddedilebilir. Ham parola veya hash yanıta/loga eklenmez.

Aktif oturum sayısı geçerli, iptal edilmemiş refresh tokenların ayrı `FamilyId` sayısıdır. Çevrimiçi cihaz listesi, cihaz adı, IP/konum takibi veya tek cihaz iptali eklenmedi. Güvenlik değişiklikleri mevcut tüm-oturum-iptal davranışını kullanır. Mevcut logout/refresh politikası değiştirilmedi.

## Şifre kotası ve transaction

Başarı zamanı `now - 30 gün` sınırından **büyük** olan normal değişiklikler sayılır. Tam sınır anında eski hak açılır. Üç başarıdan sonra sonraki normal değişiklik 409 olur; yanlış mevcut parola, reddedilen yeni parola veya rollback kotayı artırmaz. Kalan hak ve ilk açılma tarihi backend'den gelir; istemci saati yetki vermez.

Normal değişiklik ve reset aynı Users satırında SQL `UPDLOCK,HOLDLOCK` transaction'ını paylaşır. Güncel parola doğrulaması, geçmiş denetimi, normal kota, eski hash ekleme, yeni hash/stamp ve oturum iptali aynı transaction içindedir. Birden çok API instance'ı aynı veritabanı kilidini paylaşır. Kısa süreli kötüye kullanım limiti (credentials 5/dakika) ayrıca korunur.

Kurtarma 3/30 gün kotasını ne tüketir ne sıfırlar. Fakat mevcut ve bilinen eski parolayı, kısa token süresini ve tek kullanımı denetler. Başarılı normal/reset değişimi SecurityStamp yeniler; mevcut access tokenlar aktif kullanıcı/stamp kontrolünde reddedilir, refresh aileleri iptal edilir, cihaz push kayıtları devre dışı kalır. Yeni giriş gerekir. Forgot isteğinin genel yanıtı e-posta teslimi iddiası değildir; SMTP davranışı bu fazda değiştirilmedi.

## Geçmiş ve minimizasyon

`PasswordHistory`: kullanıcı, rastgele kayıt kimliği, mevcut Identity PasswordHasher V3 (salt/PBKDF2, 210.000 iterasyon) ile üretilmiş eski hash, yalnız normal başarı varsa nullable UTC zaman. Aday parola her özetle aynı hasher üzerinden doğrulanır; farklı salt'lar nedeniyle hash metinleri karşılaştırılmaz. Düz metin/geri çözülebilir parola yoktur. Bilinmeyen eski hesap geçmişi üretilmez; mevcut parola ilk gelecek başarıda arşivlenir.

Tüm **bilinen** eski parolaların tekrarını engellemek için özetler aktif hesap ömrünce tutulur; hesap silmede kullanıcı kapsamında temizlenir. Geçmişe sayı/zaman kırpması getirmek eski bilinen parolayı yeniden kullanılabilir yapacağından yapılmadı. Başarı zamanları 30 gün geçince kotada sayılmaz; sonraki başarılı normal değişiklik veya mevcut sınırlı bakım partisi bu alanı null yapar. Bakım kapalıyken tam 30. günde fiziksel temizleme garanti edilmez. Hash/satır kalır; gereksiz eski zaman detayı kaldırılır. İşletim yetkileri, backup ve erişim politikası ayrıca yayın öncesi değerlendirilmelidir.

Küçük yerel, harf büyüklüğünden bağımsız yaygın parola listesi kayıt/normal/resette kullanılır. Parola veya hash üçüncü tarafa gönderilmez. Bu kapsamlı sızıntı taraması değildir; yönetilen daha geniş çevrimdışı liste sonraki güvenlik değerlendirmesi konusudur. Zorunlu periyodik değişiklik yoktur. Mevcut 12–128 karakter sınırı korunur; [NIST SP800-63B](https://pages.nist.gov/800-63-4/sp800-63b.html) tek faktörlü parolada 15 karakter ister. Bu proje NIST uyumlu diye ilan edilmez; politika değişikliği ayrı değerlendirilmelidir.

## Hukuki kayıt ve taslaklar

Sürüm `2026-10-29-draft-1`: [Aydınlatma](legal/KVKK_AYDINLATMA_TASLAK.md), [Gizlilik](legal/GIZLILIK_POLITIKASI_TASLAK.md), [Koşullar](legal/KULLANIM_KOSULLARI_TASLAK.md). Mobilde 14 dil özet taslak ve ortak saklama/aktarım/hak açıklamaları vardır. Türkçe Markdown metinleri teknik ayrıntıyı genişletir; ikisi de nihai hukuki belge değildir.

Kayıtta üç belge ayrı açılır. Bilgilendirme açık rıza sayılmaz. Taslakta zorunlu sözleşme kabulü veya pazarlama/isteğe bağlı analiz rızası yoktur. Dolayısıyla rıza reddiyle kaydı engelleyen bir alan veya sahte rıza kaydı yoktur. Nihai sözleşme kabulü veya ayrı amaçla açık rıza gerekirse hukuk sahibi gereksinimi belirlemeli; o zaman amaç/sürüm/tarih/geri çekme ayrı modellenmelidir. Mevcut genel belge sürümü böyle bir onay yerine kullanılamaz.

`LegalNoticeReceipts`: yeni kullanıcı başına tek satır, istemcinin bildirdiği sürüm, kayıt anı UTC ve sürüm bildirilmişse aynı anda sunum zamanı. Bu kayıt istemcinin sunum beyanıdır; belgeyi açma/okuma veya hukuken geçerli kabul kanıtı değildir. Sürüm gelmezse sunum/sürüm null; eski hesaplara backfill yoktur. Hesap export'unda bu metadata vardır, parola geçmişi yoktur. Hesap silmede kaldırılır. Mevcut kullanıcılara yeni metin tebliği/audit geçmişi bu fazın kayıt akışı değildir.

Veri sorumlusu kimliği, iletişim/başvuru, somut amaç-hukuki sebep eşleştirmesi, production işlemciler/alıcılar/ülkeler ve aktarım mekanizması, nihai retention/backup, mağaza anti-replay saklaması ve sözleşme kabul gereği **Yayın öncesi doldurulacak**. Profesyonel 14 dil ve hukuk incelemesi bekler. [KVKK resmi Tebliği](https://www.kvkk.gov.tr/Icerik/4132/aydinlatma-yukumlulugunun-yerine-getirilmesinde-uyulacak-usul-ve-esaslar-hakkinda-teblig) bilgilendirme ve açık rızanın ayrımına ilişkin temel kaynaktır.

## Migration ve yerel çalıştırma

`20261029131024_AccountPrivacySecurity`: yalnız PasswordHistory ve LegalNoticeReceipts + PK/FK/index oluşturur. User hash/ID/stamp, avatar veya finansal şema/veriye update/backfill yoktur. Model 22 tablo/PK, 25 FK; para decimal(19,4), rowversion ve net8/EF8 korunur. Startup migration eklenmez. Development hedefi `127.0.0.1,14333 / PayAheadDockerDevelopment` doğrulandı; yalnız bu migration pending iken açıkça uygulandı. Öncesi/sonrası mevcut kullanıcı/finans/session/katalog JSON fingerprint karşılaştırması aynı, pending 0. Bağlantı ve fingerprint değeri loglanmadı. Down veya veri reseti çalıştırılmadı.

Migration komutundan önce hedefi ve pending listesini doğrulayın; secrets mevcut User Secrets/environment kaynağında kalır. PowerShell'de repo kökünden:

```powershell
$env:DOTNET_CLI_HOME="$PWD/.tools/cli"
$env:ASPNETCORE_ENVIRONMENT='Development'
.tools/dotnet/dotnet.exe tool run dotnet-ef migrations list --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
# Yalnız doğrulanmış Development hedefinde, migration incelendikten sonra:
.tools/dotnet/dotnet.exe tool run dotnet-ef database update 20261029131024_AccountPrivacySecurity --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
```

Testler rastgele `PayAheadTest_<guid>` gerçek SQL veritabanları kullanır; yalnız kendileri temizlenir. Yerel HTTP smoke yalnız kendi geçici hesabını siler:

```powershell
cd mobile
$env:EXPO_PUBLIC_API_BASE_URL='http://127.0.0.1:5080'
node scripts/phase54-smoke.cjs
```

Smoke genel amaçlı production testi değildir. SMTP/push bu doğrulamada kapalıdır. [Sonuçlar ve cihaz kabulü](PHASE5_4_REPORT.md).
