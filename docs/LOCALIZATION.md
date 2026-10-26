# Phase 5.2 — Yerelleştirme

## Kaynaklar ve kapsam

Desteklenen diller: `tr`, `en`, `de`, `fr`, `es`, `it`, `pt-BR`, `nl`, `ru`, `ar`, `zh-Hans`, `ja`, `ko`, `hi`. Türkçe dışındaki kaynaklar kullanıcı onayıyla AI taslağı olarak hazırlanmıştır; profesyonel dilsel kabul değildir.

Mobil kaynaklar `mobile/src/i18n` altındadır. `source-manifest.json` ekran bağlamlarını ve sabit anahtarları, `extra-source.json` anlamsal ekleri, `drafts/*.tsv` çeviri kaynaklarını içerir. `locales/<dil>/<namespace>.json` ve `resources.json` üretim çıktılarıdır. Marka, API enum ve kullanıcı tarafından girilmiş finansal metinler çevrilmez.

```powershell
cd mobile
npm.cmd run i18n:audit
npm.cmd run typecheck
npm.cmd run lint
npm.cmd test -- --silent
npx.cmd expo-doctor
```

`i18n:build` eksik anahtar, yinelenen taslak, placeholder ve CLDR çoğul uyuşmazlığında başarısız olur. `i18n-audit.cjs` kaynak anahtar kullanımı, ham JSX metni ve JSON bundle tutarlılığını denetler. Bu statik tarama fiziksel ekran ve dil uzmanı incelemesinin yerine geçmez. `i18n-extract.cjs` ve `i18n-complete-source.cjs` bir defalık dönüşüm araçlarıdır; günlük üretim komutu değildir, yeniden çalıştırılmamalıdır.

## Tercih ve senkronizasyon

Öncelik: cihazda manuel seçim → doğrulanmış hesabın `UserSettings.LanguageCode` değeri → desteklenen cihaz dili → İngilizce. Önceden saklanmış `tr` hesap tercihi korunur. `pt-*` Brezilya Portekizcesine eşlenir; desteklenmeyen Geleneksel Çince İngilizceye düşer. Dil değişimi para birimini ve saat dilimini değiştirmez.

Manuel tercih `payahead.language.v1` anahtarıyla native SecureStore / web localStorage içinde token deposundan ayrı saklanır; logout temizlemez. Hesap değişimi ve eski HTTP yanıtları revision/oturum epoch kontrolüyle sınırlandırılır. Depolama yazıları ve hesap güncellemeleri sıralıdır. Offline seçim hemen uygulanır; hesap senkronizasyonu uygulama tekrar öne geldiğinde yeniden denenir. Depolama başarısızlığı ekranda bildirilir.

JWT gerektiren `GET/PUT /api/users/me/language` yalnız dil alanını değiştirir; kullanıcı ID'si body'den kabul edilmez, diğer ayarlar korunur. Kullanıcı başına rate limit devam eder. Genel User Settings API'si de 14 kodu kabul eder.

## Katalog ve migration

`20261025154307_CatalogLocalization` yalnız `CatalogCategoryTranslations` tablosunu, birleşik PK, FK ve arama indexini ekler; 116 kategori/alt kategori × 14 dil = 1.624 çeviri satırı içerir. Eski `NameTr/NameEn`, kimlikler ve finansal satırlar korunur. Uygulama başlangıcında otomatik migration yoktur.

`/api/catalog/categories`, kategori detayı, `/api/catalog/services` ve hizmet detayı açık `locale` query parametresi alır. Kategori `displayName/cultureCode/usedLanguageFallback` alanları eklenmiştir. Çeviri eksikse İngilizce kontrollü fallback kullanılır. Türkçe/İngilizce arama verileri korunmuştur. Mobil cache anahtarları locale içerir. Genel hizmet şablonları embedded `Localization/ServiceNames.json` kaynağından çevrilir; gerçek marka isimleri korunur.

Migration öncesi User Secrets/environment ile çözülen hedef host/veritabanını, uygulanmış migration listesini ve yedeği doğrulayın. Connection string veya parola çıktısı paylaşmayın. Ardından açıkça seçilmiş Development hedefinde EF Core 8 aracıyla `database update` çalıştırın; mevcut migration'ları silmeyin, volume kaldırmayın. Sadece yeni çeviri tablosunun kaldırılması bile yerel çeviri düzenlemelerini kaybettirebileceğinden rollback otomatik uygulanmaz.

## RTL, tarihler ve para

Metin ve JS yerleşim yönü dil seçimiyle güncellenir; form bileşenleri dil anahtarıyla yeniden yaratılmaz. Arapça RTL'dir. Native navigation/Android sistem kontrollerinin tam yön değişimi React Native nedeniyle yeniden yükleme gerektirebilir; ekrandaki açık uyarıdan önce taslakları kaydedin. Otomatik reload yapılmaz. Expo Go davranışı development build ile aynı kabul edilmez.

Tarih etiketleri seçilen Intl locale'iyle biçimlenir; DateOnly için yerel öğlen kullanılarak gün kayması önlenir. iOS tarih seçicisine locale aktarılır. Android sistem tarih/saat penceresinin dili işletim sistemine bağlıdır; uygulama içi etiket seçilen dildedir. Native pencerenin 14 dilde anlık değiştiği iddia edilmez.

`decimal(19,4)` tutarlar string üzerinden biçimlenir; büyük tutarlar Number'a dönüştürülmez. Hermes Intl BigInt kabul etmediğinden yalnız küçük kesin Number örneklerinden yerel gruplama/rakam/para yerleşimi alınır; gerçek tutar string olarak korunur. [Payments render düzeltmesi](PAYMENTS_RENDER_ERROR.md). Ondalık girdisi nokta/virgül ve Arapça rakamları destekler; karma binlik/ondalık ayracı reddedilir. Dil seçimi döviz dönüşümü yapmaz. Bildirim worker'ı ödeme/deneme için genel mesajı kullanıcının güncel hesap dilinden seçer; retry/lease/receipt davranışı aynıdır. Bu, gerçek push teslim kanıtı değildir.

## Fiziksel Android kabul listesi

Her 14 dil için ayrı ayrı: login/register/reset ekranları; dashboard; ödeme/abonelik formu; 116 kategoride gezinme ve yerel arama; genel hizmet/marka ayrımı; takvim/geçmiş/analiz; hatırlatıcı/ayar/premium; alert/validation/empty/loading; tarih ve USD/TRY tutarları; büyük font/ekran okuyucu; dark/light; uygulamayı kapat-aç ve logout/login; offline seçim/yeniden bağlantı; açık taslakta dil değişimi kontrol edilmelidir. Arapçada ayrıca yön, geri oku, modal, klavye ve native yeniden yükleme kontrol edilir.

Fiziksel Android, dil uzmanı kabulü, gerçek push teslimi ve native build bu çalışma sırasında ayrıca kanıtlanmadıkça açık kalır. Son komut sonuçları `IMPLEMENTATION_PROGRESS.md` içine kaydedilir.
