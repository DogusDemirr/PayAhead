# Phase 5.4.1 — Kayıt belgeleri ve parola gücü UX

29 Ekim 2026. Phase 5.4.1 kod ve otomatik doğrulama tamamlandı; production veya Phase 6/7 yoktur. Mevcut kullanıcı/finans verileri ve Phase 5.4 parola kotası/geçmiş/oturum güvenliği korunur. Fiziksel Android ve hukuki yayın kabulü ayrı bekler.

## Ekranlar ve teyit akışı

Gizlilik/Güvenlik ekranındaki üç belge menüsü kaldırıldı. Belgeler herkese açık `/legal` ekranında ve yeni kayıt formunun modern kartlarında erişilebilir. Kart nötr, tikli veya eksik teyit durumunu gösterir; eksik kart doğrudan açılır. Hesap Oluştur görünür/tıklanabilir kalır; eksik teyitler ve form hataları gösterilir, HTTP kayıt isteği gönderilmez. Üç teyit tamamlandığında normal kayıt devam eder.

Her belgenin en altında ayrı erişilebilir checkbox vardır. Varsayılanlar işaretsizdir; kaydırma otomatik teyit vermez. KVKK bilgilendirme teyidi, Gizlilik okundu teyidi ve Koşullar sözleşme kabulü ayrı anlam taşır. Checkbox kaldırılabilir. Aynı formda belgeye gidiş/Android geri dönüşte durum ve alanlar korunur. Seçimler yalnız kayıt denemesine bağlı bellektedir; yeni kayıt, çıkış veya başarıdan sonra temizlenir. Persist edilmez, başka hesap/yeni denemeye kopyalanmaz. Geçersiz/eski deneme linki yeni formda teyit oluşturamaz. Herkese açık salt okunur belge ekranı, aktif kayıt denemesi yokken checkbox göstermez.

Parola boşken bar ve güç etiketi render edilmez. İlk karakterde görünür, tamamen silinince gizlenir. Minimum iç alan ayrılarak kayma azaltılır; uzun metinler sabit yüksekliğe kırpılmaz. Güç seviyesi hem görünür hem erişilebilir progressbar değeridir; rehber metin kalır. Parola politikası, göster/gizle, tekrar ve backend denetimleri değişmez. Ortak bileşenin aynı davranışı reset/Hesabım yeni parola alanlarında da geçerlidir.

## Backend sözleşmesi

Yeni endpoint yoktur. Sürümlü `GET /api/legal` manifesti korunur; `isDraft:true`, `requiresTermsAcceptance:true`, `requiresNoticeAcknowledgment:true`, `requiresPrivacyAcknowledgment:true` bildirir. Bu endpoint mevcut manifesttir; tam çok dilli belge gövdesi mobil pakette tutulmaya devam eder.

`POST /api/auth/register` için artık `legal` zorunludur:

```json
{
  "legalVersion": "2026-10-29-draft-1",
  "legal": {
    "noticeVersion": "2026-10-29-draft-1",
    "noticeAcknowledged": true,
    "privacyVersion": "2026-10-29-draft-1",
    "privacyAcknowledged": true,
    "termsVersion": "2026-10-29-draft-1",
    "termsAccepted": true
  }
}
```

Bu parça olağan email/password/displayName alanlarına eklenir; gerçek parola örneği değildir. Eksik/null legal nesnesi, false/eksik boolean, eksik/eski/geçersiz belge sürümü 400 ValidationProblemDetails üretir. Üstteki eski `legalVersion` alanı metadata uyumluluğu için isteğe bağlıdır; gönderilirse güncel olmalıdır, tek başına teyit yerine geçmez. Üç belge sürümü ayrı ayrı zorunludur. İsteğe bağlı pazarlama/açık rıza alanı eklenmedi; böyle bir izni vermek kayıt şartı değildir.

**Eski kayıt istemcileri yeni teyit alanları olmadan artık kayıt açamaz.** Bu, istenen zorunlu backend kontrolüdür; gizli legacy bypass yoktur. Mevcut hesapların login/refresh/hesap yönetimi için yeniden teyit şartı getirilmez. Otomatik test fixture'ları ve mevcut smoke script'leri açık test teyitleriyle güncellendi; uygulama test helper'ını kullanmaz ve checkbox'ları otomatik doldurmaz.

## Veritabanı, sürüm ve saklama

Eklemeli migration `20261029192237_RegistrationLegalConfirmations` yalnız yeni tablo/PK/FK oluşturur. Model 23 tablo/PK, 26 FK. Kullanıcı/parola/finans veya eski LegalNoticeReceipts/PasswordHistory satırlarını değiştiren migration SQL'i yoktur; eski migration'lar korunur, backfill veya startup migration eklenmez.

Yeni tabloda kullanıcı + belge composite PK; belge ID, `Kind`, `DocumentVersion`, server UTC `ConfirmedAtUtc` bulunur. Türler: `NoticeAcknowledgment`, `PrivacyReadAcknowledgment`, `TermsAcceptance`. İlk ikisi açık rıza kaydı değildir. Üç satır User, UserSettings ve eski presentation receipt ile aynı EF SaveChanges transaction'ında yazılır; duplicate kayıtta kısmi teyit kalmaz. UTC zaman server'ın kayıt teyitlerini aldığı andır; kullanıcının gerçek okuma süresini veya cihazdaki tıklama anını ölçmez. Client beyanını kaydeder; fiilen okuduğunu veya hukuken geçerli sözleşme kurulduğunu ispatladığı ileri sürülmez.

Eski kullanıcı/receipt satırları korunur; geçmişte bilinmeyen teyitler üretilmez. Kendi export'una üç ayrı kayıt eklenir, başka kullanıcıya/secret'a erişim verilmez. Mevcut doğrulamalı hesap silme bu kullanıcının yeni kayıtlarını da kaldırır. Saklama aktif hesap ömrüdür; sözleşme kayıtları için farklı hukuki saklama gerekip gerekmediği yayın öncesi hukuk sahibi tarafından belirlenmelidir. Backup ve mağaza anti-replay açıkları önceki fazdaki gibidir.

Development hedefi `127.0.0.1,14333 / PayAheadDockerDevelopment` doğrulandı, yalnız beklenen migration pending iken açıkça uygulandı. Mevcut 22 tablonun tüm kolonlarıyla önce/sonra JSON fingerprint aynı; kullanıcı hash'leri ve geçmiş kayıtlar korunuyor, pending0. Bağlantı/fingerprint değerleri raporlanmadı. Down, reset, volume silme veya production işlemi yapılmadı. net8.0/C#12/EF8 korundu.

## Hukuki metinler ve 14 dil

Belge sürümü `2026-10-29-draft-1` ve metin gövdeleri aynı. HEAD karşılaştırmasıyla 14 dilde 196 mevcut hukuki belge alanı değişmedi. Yalnız kayıt ekranının eski “zorunlu kabul alınmaz” UX açıklaması, kullanıcının bu fazda istediği zorunlu akışla uyumlu değişti; belge gövdesi değildir. 8 yeni anlamsal anahtar ×14 dil ve 1 mevcut kayıt UX açıklaması ×14 dil hazır. Audit eksik/placeholder/unused/ham taranan UI hatası0.

Phase 5.4 Markdown taslakları önceki akışın tarihsel açıklamalarını da içerir; bu fazda metinleri sessizce değiştirmemek için korunmuştur. Özellikle koşullar taslağının eski “kabul toplanmıyor” açıklaması yeni uygulama akışını tarif etmez. Yayın öncesi hukuk sahibi nihai belgeyi yeni sürümle açıkça hazırlamalıdır. Mevcut zorunlu checkbox hukuken kesinleşmiş sözleşme veya KVKK uyumu iddiası değildir. Veri sorumlusu/iletişim, hukuki sebepler, production aktarım tarafları/ülkeleri, nihai saklama/backup ve dilsel inceleme açık kalır. [Resmi KVKK Tebliği](https://www.kvkk.gov.tr/Icerik/4132/aydinlatma-yukumlulugunun-yerine-getirilmesinde-uyulacak-usul-ve-esaslar-hakkinda-teblig) bilgilendirme ve açık rıza ayrımı için kaynaktır.

## Doğrulama

| Kontrol | Sonuç |
| --- | --- |
| Restore/build | Başarılı, 0 hata/uyarı |
| Backend unit | 431/431, atlanan0 |
| Gerçek SQL integration | 293/293, atlanan0; gerçek SQL Server 2022 Testcontainers |
| Mobil Jest | 370/370, 28 suite, atlanan0 |
| TypeScript / ESLint | Başarılı, lint0hata0uyarı |
| Expo Doctor | 21/21 |
| Hermes Android export | 4189 modül, 9.4MB `.hbc`, başarılı |
| 14 dil i18n audit | issue0, taranan ham UI0 |
| Development HTTP kayıt | 12 kontrol + 2 cleanup kontrolü başarılı |
| Mevcut ödeme/avatar HTTP regresyonu | 28 kontrol + yalnız geçici hesap temizliği başarılı |

SQL testleri rastgele ayrı gerçek SQL Server 2022 Testcontainers veritabanlarında çalışır; InMemory/SQLite kanıtı yoktur. İlk koşuda Docker Desktop `Paused (manual)` olduğundan SQL fixture'ları başarısız oldu (288 başarısız,5 saf kontrol geçti); başarılı sayılmadı. Docker devam ettirildi, Engine29.9.1/Linux ve mevcut SQL/Mailpit healthy doğrulandı; volume/veri sıfırlanmadı. Son tam test koşusu ayrı artefakta kaydedilir. Başlangıçta bir yeni unit testte constructor tür belirsizliği düzeltildi; son build başarılıdır.

Kanıtlar: `artifacts/phase541/verified-tests/LENOVO_LAPTOP-2DC8BFEH_2026-10-29_22_26_59.trx` (unit) ve `LENOVO_LAPTOP-2DC8BFEH_2026-10-29_22_27_14.trx` (SQL), `artifacts/phase541/backend-verified.log`, `mobile/artifacts/phase541-mobile-results.json`, `mobile/artifacts/phase541-hermes`. Son SQL koşusu 9 dakika 17 saniye sürdü; başarısız/atlanan0. HTTP script'i `mobile/scripts/phase541-smoke.cjs`: yalnız loopback ve kendi rastgele hesabı; yanıt gövdesi/parola/token loglamaz. SMTP/push/store worker kapalıdır, dış teslim/mağaza işlemi yapılmadı.

## Android ve kapsam

[Android kabul adımları](../mobile/docs/PHASE5_4_1_ANDROID_ACCEPTANCE.md). Fiziksel cihazda kaydırma/taşma, checkbox/geri dönüş, TalkBack, büyük yazı, light/dark ve RTL kabulü henüz yapılmadı. Otomatik render veya Hermes export cihaz kabulü sayılmaz. LAN bu oturumda `.155` olmuştu; ignored mobile/.env ve yalnız Development AllowedHosts düzeltildi. Güncel `http://192.168.1.155:5080/health/ready` gerçek LAN isteği HTTP200 döndü. Production host listesi aynı.

Değişenler: AuthScreen, PrivacyScreen, LegalScreen, PasswordStrength; yeni RegistrationDocuments ve kayıt denemesi bellek store'u; auth DTO/validator/service/store, legal manifest, yeni Domain/config/migration, export/deletion; i18n kaynakları; unit/SQL/mobil/HTTP testleri ve eski kayıt fixture'ları; bu rapor, Android yönergesi ve ana rehberler. Yeni paket veya framework yükseltmesi yoktur. Phase 5.4.1 sonunda durulur; Phase 6/7 başlatılmaz.
