# Phase5.1.1 — Kategori ve gerçek logo düzeltmeleri

25 Ekim2026. Kullanıcının fiziksel inceleme bulgularına göre mevcut sistem genişletildi; sıfırdan yazılmadı. Phase6/7, yeni push kurulum, SMTP/Resend ve production deployment yapılmadı. Fiziksel kabul bekler.

| İstenen sonuç | Doğrulanan durum |
| --- | --- |
| Kök nedenler | Dar kart hit alanı; yalnız doğrudan kategori hizmet filtresi; Yakıt ara düğümüne marka üyeliği eksikliği; Haber ve diğer ana başlıklarda alt içerik eksikleri; yolu göstermeyen arama;24saat stale cache; boş logo registry. [Analiz](../../docs/PHASE5_1_1_DIAGNOSIS.md) |
| Hiyerarşi | Ana kategori→alt kategori→hizmet→mevcut form. Eski kimlik/parent/ad korunur. Yakıt→Shell, Bakım ve Tamir→servisler, Haber→10alt tür→7yayıncı; doğrudan MTV/muayene ve diğer23araç gideri erişimi |
| Ana kategori |19DB kökü; dijital6başlık kısayoluyla ana ekranda25giriş kartı. Bunlar25ayrıDB kökü diye sayılmaz |
| Alt kategori |97alt düğüm;116toplam. Yeni60alt kategori. [Her ana kategori denetimi](../../docs/CATALOG_AUDIT.md) |
| Hizmetler |104toplam/102aktif;7yeni haber hizmeti.245kategori-hizmet üyeliği. bp/Apple TV pasif durumu önceki fazdan korunur |
| Gerçek logo |5asset/5hizmet: Netflix, Spotify, Opet, Microsoft(Microsoft365 sağlayıcısı), Adobe(Creative Cloud sağlayıcısı). Yerel PNG+orijinalSVG+SHA256/kaynak. Wordmark ve sağlayıcı logosu ürün ikonuyla karıştırılmaz |
| Fallback |99/104toplam,97/102aktif hizmet. Diğer öncelikli markaların hakları varsayılmadı. [Kaynak/telif/marka notları](../assets/brands/NOTICE.md), [hizmet bazında envanter](../../docs/CATALOG_LOGOS.md) |
| Android erişimi | Otomatik yol testi116benzersiz kategori için geçti: Payment115,Subscription50 (kümeler örtüşür). Ana kart sayısı Payment25,Subscription14. Fiziksel Android ekranında bu sayılar henüz doğrulanmadı |
| Migration/seed | `20261025145738_CatalogCoverage`, yalnız INSERT ileri işlemleri. Development `PayAheadDockerDevelopment` hedefi doğrulandı, uygulandı, pending0. Önceki56kategori/97hizmet değerleri ile kullanıcı/plan/occurrence/abonelik değerleri ve rowversion aynı. İdempotent eksik-satır seed'i ve eski sürümden upgrade SQL testlidir. Şema/precision/recurrence/linked plan değişmez |
| Mobil | TS/lint temiz;180/180 Jest,18suite,skipped0. Tüm ağaç yolları, kart hit alanı, Türkçe arama, özel kayıt, Android geri, gerçek asset dosyaları ve fallback içerir |
| Backend unit |398/398,skipped0; parent/kapsam/bağlantı bütünlüğü dahil |
| SQL integration |269/269,skipped0;6dk53sn. SQL Server2022 Testcontainers; SQLite/InMemory kullanılmadı. Katalog upgrade eski metadata ve bağlı özel kaydın tamamını korudu; idempotent seed/hiyerarşi/arama/mod filtreleri geçti |
| Gerçek HTTP | Güncel Development API5086 üzerinde37kontrol+4hesap cleanup geçti. Yakıt→Shell, bakım, haber aboneliği, kapsam115/50, ikinci sayfa, hiyerarşik arama, decimal19,4/212.2457toplam, linked plan, eski PUT,409/404/400 ve kullanıcı izolasyonu |
| Açık eksikler | Fiziksel Android/görsel kabul; kalan99hizmette gerçek marka asset'i yok. Bazı yayıncı/marka sayfalarının erişim sınırlamaları var; abonelik ürününün varlığı Türkiye'de her paket/fiyatın sunulduğu anlamına gelmez. Önceki8audit(5high/3moderate), Phase6 gerçek push ve Phase4 inbox/fiziksel açıkları sürer |

Her ana kategoriye alt seçim eklendi. Hazır hizmeti olmayan örnekler: basılı gazete, spor haberleri, market, çocuk bakımı, seyahat ve çeşitli gider türleri. Bunlar sahte ücretli markayla doldurulmadı; uygun kategori içinde **özel kayıt** sunulur ve [denetim tablosunda](../../docs/CATALOG_AUDIT.md) satır bazında belirtilir. Substack tüm yayıncıların ücretli olduğu anlamına gelmez; kullanıcı kendi ücretli yayınını ve gerçek fatura sıklığını belirlemelidir. Haber seed'i fiyat veya varsayılan sıklık dayatmaz.

## Çalıştırma ve Android kabulü

Çalışmakta olan eski API durdurulmadı; güncel API ayrı5086portunda doğrulandı. Android'i eski süreçle denemeyin. Eski backend terminalinde kontrollü Ctrl+C ardından repository kökünde `./scripts/Start-Development.ps1 -Port 5080` çalıştırın. Bu script güncel kodu ayrı artifacts çıktısında derler; migration startup'ta yapılmaz. Bu makinede migration zaten uygulanmıştır.

Mobile `.env` içindeki mevcut LAN API adresini kontrol edin; telefonda127.0.0.1 bilgisayarı göstermez. `mobile` dizininde `npx.cmd expo start --go --clear` başlatın; aynı Wi-Fi ve güncel bundle ile giriş yapın. Açık seçicide **Kataloğu yenile** veya pull-to-refresh kullanın. [10zorunlu Android kabul adımı](ANDROID_TESTING.md) ve light/dark/geri/arama kontrolleri bekler. Fiziksel sonucu cihaz/OS/tarih ile kaydedin; token veya kişisel bilgi paylaşmayın.

## Doğrulama kaydı

- Backend: `artifacts/phase511-check/test-results/phase511-final_*.trx`; .NET8.0.426/EF8.0.21/C#12; restore başarılı, build0warning0error. Varsayılan output ilk denemede açık eski API DLL kilidinden başarısız; son artifacts build temizdir.
- Mobil: Doctor21/21, SDK check uyumlu; nihai Hermes4106modül/8.2MB ve36asset(beş yeni marka PNG'si dahil). `mobile/artifacts/phase511-android-final` başarıyla üretildi.
- Audit: `mobile/artifacts/phase511-audit.json`;8önceki bulgu, force/SDKupgrade yok.
- Public seed ve lisans envanteri kullanıcı verisi içermez. Asset contact sheet görsel olarak incelendi; bu cihaz ekranı kabulü değildir.
- Test API teslimde kapatıldı; kullanıcının eski API/Metro süreçleri, SQL/Mailpit ve volume korunur. Yeni faza geçilmez.
