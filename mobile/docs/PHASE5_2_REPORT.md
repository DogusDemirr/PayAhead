# Phase 5.2 — Sonuç raporu (26 Ekim 2026)

Mevcut yarım değişiklikler korundu ve tamamlandı. Üç paralel yardımcı ajan kullanıcının açık onayıyla 12 hedef dilin mevcut dosyalarını tamamladı; İngilizce ve Türkçe ana kaynaklar ana çalışma içinde düzenlendi. Eksik diller İngilizceyle doldurulmadı. Çeviriler **AI taslağıdır**, profesyonel dilsel kabul değildir.

## Kapsam

- 14 dil: Türkçe, English, Deutsch, Français, Español, Italiano, Português (Brasil), Nederlands, Русский, العربية, 简体中文, 日本語, 한국어, हिन्दी.
- 17 mevcut özellik ekran bileşeni + yeni dil ekranı; ayrıca Premium/Bilgi/Önizleme/404 route arayüzleri, route başlıkları, ortak bileşenler ve servis/validasyon metinleri. Auth ve reset bileşenleri birden fazla akışı paylaşır; route sayısı ekran sayısı diye şişirilmedi.
- 62 mevcut kaynak dosyasında kullanılan **714 namespaced kaynak anahtarı** (674 benzersiz metin hash'i) ve **48 anlamsal ek anahtar**. Çoğul varyantları nedeniyle runtime sayıları dile göre farklıdır.
- 13 namespace; interpolation, CLDR çoğulları, Hermes PluralRules polyfill, dil bazlı tarih ve hassas para gösterimi.
- Eksik çeviri, yinelenen taslak, placeholder, çoğul, kullanılmayan kaynak anahtarı, bundle/JSON ve taranan ham UI metni hatası: **0**. Statik audit tüm olası görsel sorunların kanıtı değildir.

| Dil | Runtime kaynak girdisi | Eksik/hatalı |
|---|---:|---:|
| tr | 786 | 0 |
| en | 786 | 0 |
| de | 786 | 0 |
| fr | 799 | 0 |
| es | 799 | 0 |
| it | 799 | 0 |
| pt-BR | 799 | 0 |
| nl | 786 | 0 |
| ru | 812 | 0 |
| ar | 838 | 0 |
| zh-Hans | 773 | 0 |
| ja | 773 | 0 |
| ko | 773 | 0 |
| hi | 786 | 0 |

## Davranış ve güvenlik

Dil seçimi aramalı, native dil adları ve erişilebilir radio durumuyla light/dark temada çalışır. Manuel cihaz seçimi, hesap tercihi, cihaz dili, İngilizce sırası uygulanır. SecureStore/localStorage anahtarı auth token deposundan ayrıdır; logout/login manuel seçimi silmez. Offline seçim hemen uygulanır, sunucu eşitlemesi öne gelişte yeniden denenir. Revision/credential epoch ve sıralı yazılar geç yanıtları sınırlar. Formlar dil değişiminde key ile remount edilmez.

JWT `GET/PUT /api/users/me/language` yalnız LanguageCode yazar; kullanıcı izolasyonu/rate limit korunur. Para birimi, saat dilimi ve notification ayarları değiştirilmez. Backend hata kodu/status/traceId korunur; güvenilmeyen ProblemDetails başlığı/alan metni doğrudan gösterilmez. Form hata metinleri doğrulama anında çevrilir; görünür eski hata yeni dile eşlenir.

Kategori hiyerarşisinin tamamı **116 × 14 = 1.624** çeviri; genel hizmet şablonları **33 × 14 = 462** çeviri içerir. Marka/kullanıcı adları değişmez. Eski NameTr/NameEn/ID ve TR/EN arama verileri korunur; locale query/cache ayrımı ve kontrollü İngilizce fallback vardır. Logo envanteri önceki fazla aynıdır.

Bildirim worker'ı gönderim uygunluğu kontrolünde güncel hesap dilini taşır; ödeme/deneme genel metinleri 14 dilde hazırlanır. Lease/retry/receipt ve hassas bilgi içermeyen içerik korunur. Gerçek push gönderilmedi.

## Migration ve gerçek backend

.NET SDK **8.0.426**, EF Core **8.0.21**, hedef **net8.0 / C# 12** korunmuştur. Docker Engine **29.9.1** çalışır. Mevcut SQL Server ve Mailpit container'ları silinmedi/durdurulmadı.

`20261025154307_CatalogLocalization` doğrulanmış **127.0.0.1:14333 / PayAheadDockerDevelopment** hedefine açık işlemle uygulandı. Yalnız çeviri tablosu/index/FK ve seed ekler. Users/UserSettings/PaymentPlans/PaymentOccurrences/TrackedSubscriptions/ReminderRules/CatalogCategories/CatalogServices/CatalogServiceCategories öncesi/sonrası satır sayıları ve SQL checksum değerleri aynı kaldı. Otomatik migration eklenmedi.

Ayrı 5088 Development API'sinde gerçek JWT ve geçici test hesaplarıyla **79 yerelleştirme HTTP kontrolü + 2 cleanup**, ayrıca mevcut katalog/ödeme/abonelik smoke'unda **37 HTTP kontrolü + 4 cleanup** geçti. 14 dil tercih PUT/GET, tüm kategoriler, marka/genel hizmet adı, yerel arama, 400/401/409/404 izolasyon ve finansal roundtrip doğrulandı. Yalnız test hesaplarının temizliği yapıldı. SMTP/push bu test API'sinde kapalıydı.

## Test sonuçları

| Kontrol | Sonuç |
|---|---|
| i18n build + source audit | Başarılı; eksik/hata 0 |
| TypeScript | Başarılı |
| ESLint | 0 hata / 0 uyarı |
| Mobil Jest | 216/216; 19 suite; atlanan 0 |
| .NET restore/build | Başarılı; 0 hata / 0 uyarı |
| Backend unit | 407/407; atlanan 0 |
| Gerçek SQL Server 2022 Testcontainers | 271/271; atlanan 0 |
| Expo Doctor | 21/21 |
| Expo SDK dependency check | Uyumlu |
| Android Hermes export | Başarılı; 4.164 modül, 9,1 MB HBC, 36 asset; APK/cihaz kabulü değildir |
| Gerçek yerel API smoke | 116 ana kontrol + 6 cleanup başarılı |

SQL testleri SQLite/InMemory değildir; rastgele ayrı test DB'leri kullanır. Migration upgrade/rollback, tüm kategori dilleri, kullanıcı dil izolasyonu/diğer ayarların korunması, marka/genel hizmet ve worker dili test edildi. Mobil yeni testler 14 dil kaynak/çoğul/validasyon/tarih/decimal, tercih önceliği, offline/persist/race, taslak korunması, hata güvenliği ve light/dark dil seçiciyi kapsar.

İlk çalışmada sandbox NuGet/Docker/npm erişimini engelledi; gerekli komutlar izinle yeniden çalıştırıldı. İlk mobil turdaki iki eski ham backend hata beklentisi yeni güvenli davranışla güncellendi; ek denetimlerde kalan form/Alert metinleri, modül yüklenirken sabitlenen Zod hataları ve RTL okları giderildi. Son sonuçlarda başarısız/atlanmış test yok.

## Açık kabul ve sınırlar

- [14 Android kabul senaryosu](ANDROID_TESTING.md) fiziksel cihazda uygulanmadı; tüm dillerin taşma/font/ekran okuyucu ve native RTL kabulü açık.
- Metin ve JS yönü anında güncellenir. React Native native yönü bazı kontrollerde açık yeniden yükleme gerektirir; otomatik reload yapılmaz. Önce taslağı kaydedin. Expo Go, development build kabulü sayılmaz.
- Android sistem tarih/saat penceresi işletim sistemi dilindedir; uygulama etiketi seçilen locale'i kullanır. iOS seçicisine locale aktarılır.
- AI çevirileri hedef dil uzmanı incelemesi bekler. Otomatik kapsam, dilsel doğruluk garantisi değildir.
- Önceki gerçek push, SMTP posta kutusu ve mağaza/production kabul açıkları kapanmış sayılmadı. Kaynak kontrolüne gizli bilgi eklenmedi; production yayın ve yeni Phase 6/7 geliştirmesi yapılmadı.

Kod/otomatik doğrulama tamamlandı; fiziksel ve dilsel kabul açık olarak duruldu. Son geçici 5088 test API'si kapatıldı; kullanıcının eski API'si ve SQL/Mailpit altyapısı durdurulmadı. Android'de denemeden önce güncel backend'i kendi Development terminalinizden başlatın. Çalıştırma/migration komutları [yerelleştirme rehberinde](../../docs/LOCALIZATION.md) bulunur.
