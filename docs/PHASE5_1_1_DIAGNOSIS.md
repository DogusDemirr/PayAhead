# Phase5.1.1 — Kök neden ve düzeltme

25 Ekim2026. Kod değişikliğinden önce kullanıcıya kısa bulgu raporu verildi. Fiziksel cihaz hatası bu oturumda cihaz üzerinde yeniden üretilmedi; aşağıdaki nedenler repository ve gerçek SQL/API ile doğrulandı.

| Katman | Önce | Düzeltme |
| --- | --- | --- |
| Seed/veri |23araç gideri zaten vardı; ParentId doğruydu. Yakıt'ın markaları yalnız Benzin/Motorin/LPG üyeliğine sahipti | Eski kimlik/parent korunarak Yakıt ve yeni Bakım ve Tamir'e ek üyelikler |
| API | Hizmet categoryId filtresi yalnız doğrudan üyeliğe bakıyordu; ara düğüm boş dönebiliyordu | Aktif alt ağacı kapsayan bounded scope; hizmetler DISTINCT kaynak kümesinden sayfalanır, link çoğalması duplicate sonuç yapmaz |
| Arama | Hizmet ad/keyword ve birincil kategori dışında üst hiyerarşi hizmet aramasına dahil değildi | Eşleşen kategori ve aktif alt ağacındaki hizmetler; mobilde tam kategori yolu |
| UI | Kart dışı View, yalnız içerideki metin düğmesi tıklanabilirdi | Tüm kart Pressable ve tek erişilebilir button; testID ile kart alanına basma regresyonu |
| Gezinme | Aramadan alt kategoriye girince gerçek üst yol kurulmazdı | ParentId'den tam yol, breadcrumb, ana kategorilere dön; Android geri gerçek ebeveyne gider |
| Kapsam | Araç Payment olarak abonelikten doğru gizleniyordu | Korundu; tüm kategoriler için parent/kapsam uyumu testi. Araç aboneliğe zorlanmaz |
| İçerik | Haber0alt/0hizmet; birçok ana başlığın alt kategorisi yoktu | Her ana başlığa anlamlı alt seçimler; Haber10alt/7yayıncı. Hazır hizmet olmayan yaprakta özel kayıt |
| Cache |24saat stale süre aynı oturumda eski veriyi gösterebilirdi |5dakika stale,24saat bellek gc; yeni kategori/hizmet query key revizyonu ve her zaman manuel yenile/pull-to-refresh |
| Logo | Registry boştu |5kaynak/telif durumu incelenmiş yerel gerçek marka görseli;99fallback, lisans/marka notları ayrı |

Gerçek Development DB başlangıçta yalnız `20261025145738_CatalogCoverage` migration'ını bekliyordu. İleri yönde60kategori,7hizmet ve113üyelik eklenir; önceki56kategori/97hizmet/132üyelik silinmez veya güncellenmez. Son116/104/245,102aktif; eski finansal ve katalog değerleri aynı, pending0. Startup migration yoktur.

Build'in ilk varsayılan çıktı denemesi açık eski API'nin DLL kilidi nedeniyle başarısız oldu; o süreç durdurulmadı, ayrı Phase511/artifacts build kullanıldı. EF tasarım factory'si environment connection beklediğinden migration üretiminde bağlanılmayan design-only bağlantı kullanıldı; bu Development veritabanına erişim kanıtı sayılmadı. Hedef bağlantı ayrıca secret kaynaklarıyla doğrulandı. İlk mobil mock'larda isRefetching bulunmadığından RefreshControl booleana normalize edildi; son testler yeniden çalıştırıldı. İlk SVG indirmesinde Spotify429 verdi, tek kontrollü yeniden deneme başarılı oldu. Audit çıktısının PowerShell UTF16 formatı doğru decoder ile okundu; başarısız JSON okuma audit başarısı sayılmadı.

[Kategori denetim tablosu](CATALOG_AUDIT.md), [sonuç raporu](../mobile/docs/PHASE5_1_1_REPORT.md), [logo notları](../mobile/assets/brands/NOTICE.md).
