> **Phase5.1.1 güncel durum:** `20261025145738_CatalogCoverage` yalnız60kategori,7hizmet,113bağlantı ekler. Önceki56kategori/97hizmet/132bağlantı ve özel finansal kayıtlar değiştirilmez. Development hedefi doğrulanıp uygulandı, pending0. [Kök neden](PHASE5_1_1_DIAGNOSIS.md), [kategori denetimi](CATALOG_AUDIT.md), [rapor](../mobile/docs/PHASE5_1_1_REPORT.md). Aşağıdaki CatalogFoundation migration kaydı önceki faza ait tarihsel ilk uygulamadır; güncel sayılar yukarıdaki yeni faza aittir.

# Phase5.1.1 — Merkezi kategori ve hizmet kataloğu

25 Ekim 2026. Bu faz katalog metadatası ve mobil seçim deneyimini ekler. Yeni harcama motoru, otomatik ödeme, sağlayıcı aboneliği veya vergi takvimi oluşturmaz.

## Model ve kimlik

`CatalogCategories`: Id, kalıcı Code, ParentId, NameTr/NameEn, IconKey, SortOrder, IsActive, ApplicableTo ve normalize SearchText.

`CatalogServices`: Id, kalıcı Code, DisplayName, birincil CategoryId, nullable LogoKey, SearchKeywords/SearchText, SortOrder, IsPopular/IsActive, ApplicableTo, nullable DefaultRecurrenceType/CountryCode.

`CatalogServiceCategories`: `(ServiceId, CategoryId)` composite PK ile çoklu kategori bağlantısı. Örneğin Shell Benzin/Motorin/LPG'de görünür; Amazon Prime alışveriş/video, iCloud+ bulut/video/oyun kategorilerine bağlanır. Bu bağlantı ayrı ücret veya ayrı ödeme planı anlamına gelmez. Kullanıcı aynı paket için iki kayıt açmamalıdır.

116 kategori:19DB kökü ve97alt düğüm. Dijital altı başlığa kısayolla ana ekranda25giriş kartı vardır.104hizmet,102aktif,2pasif,245kategori-hizmet bağlantısı. Her ana başlığın anlamlı alt seçimleri ve her yaprağın özel kayıt yolu vardır. Tam envanter: [kategoriler](CATALOG_INVENTORY.md), [logo/lisans durumu](CATALOG_LOGOS.md), [kaynaklar](CATALOG_SOURCES.md).

Kalıcı GUID, SHA-256(`payahead.catalog.v1:` + code) ilk 16 byte'ından .NET Guid düzeninde üretilir; version/variant bitleri sabittir. Hizmet girdisi `service.` öneki kullanır. Code ve namespace değiştirilemez; yeniden adlandırma mevcut Id'yi korumalıdır. Operatör etiketleri ve kullanıcı kayıtları seed tarafından yeniden yazılmaz. EF HasData migration'ı deterministiktir; açık bakım yardımcısı `CatalogSeed.EnsureMissingAsync` yalnız eksik ortak satırları ekler, startup'ta çağrılmaz. Ardışık iki çalıştırma izole SQL'de test edildi.

## API

JWT gereklidir. Ortak SQL rate limiter katalog okumalarını kullanıcı, anonim durumda IP anahtarıyla 60/dakika sınırlar. Yazılabilir katalog endpoint'i yoktur; özel kullanıcı adı ortak kataloğa eklenmez.

| GET | Yanıt |
| --- | --- |
| `/api/catalog/categories` | Aktif kategori dizisi, en fazla 250 |
| `/api/catalog/categories/{id}` | Tek kategori; eski referans için pasif de okunabilir |
| `/api/catalog/services` | `items`, `totalCount`, `pageNumber`, `pageSize` |
| `/api/catalog/services/{id}` | Tek hizmet ve `categoryIds`; pasif de okunabilir |

Filtreler: `categoryId`, `applicableTo=Payment\|Subscription\|Both`, `search`, `popular`, `countryCode=TR`, `pageNumber`, `pageSize`. Hizmet kategori filtresi aktif alt ağacı da kapsar; mobilde seçili kategori hizmetin doğrudan üyeliği değilse geçerli birincil kategoriyle forma ilerlenir. Kategori endpoint categoryId filtresi doğrudan çocukları döndürür. Arama eşleşen kategori ve tüm aktif alt hiyerarşideki hizmetleri kapsar. Hizmet filtreleri üyelik köprü tablosunu kullanır. `Both` veya boş kapsam tüm kapsamları gösterir; Payment/Subscription kendi kapsamı ve Both kaynaklarını içerir. Ülke filtresi global (null) hizmetleri de içerir. Kategoriler sayfalanmaz; hizmetler varsayılan 30, en fazla 100/sayfa; sayfa 1–10000, arama en fazla 100 karakter, ülke iki büyük Latin harfi. Geçersiz filtre 400 ProblemDetails, bulunamayan Id 404, anonim 401 döner.

Arama Türkçe `ı→i`, Unicode FormD diakritik temizleme ve invariant küçük harf kullanır; `net`, `spot`, `dogalgaz`, `arac`/`ARAÇ`, `turk`, `Gemini`, `Max` test edildi. JSON enumları metindir; gerçek HTTP kontrolünde sayısal ApplicableTo hatası yakalanıp düzeltildi, ham JSON regresyonu eklendi.

## Eski veriler ve finansal tek kaynak

PaymentPlan'a yalnız nullable `CatalogCategoryId`, `CatalogServiceId` eklenir. Eski `Name` ve `Category` silinmez. Backfill ve ada göre otomatik eşleştirme yoktur. Yeni kategori seçiminde Türkçe ad eski Category alanına snapshot olur (max50); sonradan katalog adı değişmesi geçmiş finansal kayıtları güncellemez. Özel kullanıcı adı korunur.

TrackedSubscription katalog bilgisini mevcut tek bağlı PaymentPlan'dan okur; ikinci plan veya tutar oluşturmaz. Response ve Dashboard/takvim projeksiyonlarına yalnız nullable kimlikler eklenir. Decimal(19,4), DateOnly, UTC, rowversion, döviz ayrımı, recurrence, lifecycle, Free/Premium limitleri ve UserId filtreleri korunur.

Eski istemcinin katalog alanlarını **hiç göndermediği PUT**, mevcut referansları korur. Açık `null/null` referansı kaldırır. Yeni seçimde aktiflik, Payment/Subscription kapsamı ve kategori-hizmet eşleşmesi sunucuda doğrulanır. Aynı mevcut pasif seçimle düzenleme mümkündür; yeni pasif seçim reddedilir. ServiceId için CategoryId gerekir. SQL composite FK geçersiz çiftleri doğrudan SQL yazımında da engeller. Tüm katalog FK'ları NoAction; kullanıcı geçmişi cascade ile silinmez.

## Migration

`20261025134913_CatalogFoundation`: üç yeni tablo, iki nullable Plan kolonu ve katalog index/FK'ları. Eski migration'lar değişmedi. Model toplam 19 tablo/PK ve 22 FK içerir. Finansal kolon, precision, timestamp veya rowversion değişmez. Startup otomatik migrate etmez.

Development hedefi `PayAheadDockerDevelopment` ve loopback SQL olarak doğrulandı; önce tek pending migration'ın CatalogFoundation olduğu kontrol edildi. Migration açıkça uygulandı. Users/PaymentPlans/PaymentOccurrences/TrackedSubscriptions eski kolon ve rowversion değerlerinin önce/sonra bellek içi fingerprint'leri eşleşti; değerler loglanmadı. Eski referanslar null kaldı, pending migration 0. İzole gerçek SQL testleri önceki migration'dan dolu kayıtla upgrade, rollback/re-upgrade ve model snapshot uyumunu doğrular. Down katalog referanslarını kaldırır; sonradan yapılmış katalog ilişkilendirmeleri kaybolacağından gerçek kullanım sonrası rollback öncesi yedek ve inceleme gerekir.

## Mobil davranış ve cache

Ortak tam ekran Modal/FlatList seçici: popülerler, kategori ağacı, alt kategori, hizmetler, arama, pagination ve özel kayıt. 300ms debounce, 20 hizmet/sayfa; Android geri önce arama/alt gezinmeyi kapatır. Özel kayıt katalogda seçilen kategori veya Diğer'e bağlanır; kullanıcı adı paylaşılmaz. Normal seçim ad/kategori ve varsa sıklığı doldurur; tutar, para birimi, başlangıç ve fatura tarihlerini kullanıcı belirler. Shell/MTV tek seferlik önerilir; abonelik seçicisinde araç giderleri görünmez. Udemy tek kurs ve üyelik modelleri nedeniyle sıklık dayatmaz. Varsayılan sıklık her zaman değiştirilebilir.

TanStack Query katalog cache'i5dakika stale/24saat gc süresiyle yalnız bellektedir; disk offline cache yoktur. Aynı oturumdaki son başarılı veri ağ hatasında kullanılabilir; ilk açılış offline ise hata/tekrar dene görünür. Logout tüm cache'i temizler; mevcut auth epoch koruması sürer. Katalog hatası finansal listeyi gizlemez. GUID'siz eski satırlar katalog isteği yapmadan eski kategori + ikon gösterir. Liste/detay/Dashboard/takvim kimliği ortak CatalogIdentity ile görünür.

5gerçek yerel marka asset'i vardır (Netflix/Spotify/Opet/Microsoft/Adobe);99hizmette kategori ikonu fallback'i sürer. Kaynak/telif/marka notları ve orijinalSVG/optimizePNG korunur. Marka taklidi ve hotlink yoktur. Gerçek logo ekleme süreci [lisans envanterinde](CATALOG_LOGOS.md) belgelenir. Fiziksel görsel kabul [Android matrisinde](../mobile/docs/ANDROID_TESTING.md) bekler.
