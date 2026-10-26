# Payments render hatası — BigInt / Hermes (26 Ekim 2026)

## Kesin kök neden

Phase 5.2 sırasında `mobile/src/features/payments/money.ts` içindeki `financialMoney` fonksiyonuna eklenen şu çağrı hatalıydı:

```ts
new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(BigInt(integer))
```

Çağrı yolu `PaymentsScreen → PlanRow → FinancialAmount → financialMoney` idi. Hermes JavaScript BigInt desteği, Hermes Intl API'sinin BigInt kabul ettiği anlamına gelmez. Hermes'in NumberFormat `format` ve `formatToParts` uygulamaları `toNumber_RJS` kullanır; BigInt girdisi TypeError üretir. Bu sınır [Hermes kaynak kodunda](https://github.com/facebook/hermes/blob/main/lib/VM/JSLib/Intl.cpp#L1203-L1278) açıkça görülür. [Platform Intl dokümanı](https://github.com/facebook/hermes/blob/static_h/doc/IntlAPIs.md) platform/işletim sistemi farklarını da belirtir.

Önceki testler Node Intl üzerinde BigInt kabul edildiği için geçti; Hermes bundle üretimi runtime render kontrolü değildir. Aynı kısıtı test ortamına ekledikten sonra eski fonksiyonda **aynı `Cannot convert bigint to number` hatası yeniden üretildi**. Düzeltme sonrası bu regresyon ve tüm mobil testler geçti. Fiziksel telefon bu çalışma sırasında uzaktan çalıştırılmadı.

## İnceleme sonucu

- Payments listesi/PlanRow ve ortak FinancialAmount doğrudan hata yolundaydı. Ayrı bir PaymentCard dönüşümü yoktu.
- API `amount` alanları decimal lexeme string olarak korunur. `parseFinancialJson` bu tutarları float'a çevirmeden işler; burada BigInt oluşturulmaz.
- Payment/Category/Service kimlikleri UUID string'dir. Kimlikler Number'a çevrilmez; üst bitleri büyük UUID'lerle mapping ve gezinme testi eklendi. Sayısal ID sözleşmesi eklenmedi.
- `utils/format.ts` içindeki Number tabanlı preview biçimleyicisi, tarih biçimleyicileri, locale store ve katalog bileşenleri BigInt üretmez.
- Date picker, animasyon ve grafik sayısal prop'larına BigInt taşınan başka yol bulunmadı. Grafik oranları için mevcut Number kullanımı bu render hatasının kaynağı değildi ve değiştirilmedi.
- Dashboard, Calendar, History, abonelikler ve analizler de ortak biçimleyiciyi kullanır; düzeltme bu ekranları da kapsar.

## Düzeltme

Gerçek tam tutar veya tam sayı kısmı Number'a dönüştürülmez. BigInt oluşturulmaz ve Intl'ye büyük string geçirilerek örtük Number dönüşümüne güvenilmez.

Intl yalnız küçük, kesin Number örnekleriyle kullanılır: `±1` para simgesi/işaret/ondalık yerleşimi; `123456789` grup boyları; `1000` minimum gruplama eşiği; `0…9` yerel rakamlar. Gerçek tutarın tam ve kesir kısmı string üzerinde yerelleştirilir. Böylece Hint 3/2 gruplaması, İspanyolcada 1000/10000 farkı, Arapça rakamlar, negatif işaret/bidi karakterleri ve dört ondalık basamak korunur. Dil başına kurallar cache'lenir. Para birimi ve finansal hesaplama değişmez.

Try/catch ile gizleme, genel BigInt→Number dönüşümü, veri değişikliği, yeni paket, Expo/React Native yükseltmesi veya backend değişikliği yapılmadı.

## Regresyon ve sonuçlar

`mobile/test-support/number-only-intl.ts` tüm Jest render testlerinde BigInt kabul etmeyen Intl sınırını uygular; tam Hermes emülatörü değildir. `money-hermes.test.ts` 17 test ve `finance-screens.test.tsx` 4 yeni Payments render testi ekler.

- Eski kodda hedeflenen büyük tutar testi: aynı TypeError ile başarısız (beklenen kırmızı regresyon kanıtı).
- Son tüm mobil testler: **237/237, 20 suite, atlanan 0**.
- TypeScript: başarılı. ESLint: **0 hata / 0 uyarı**.
- i18n audit: 14 dil, eksik/placeholder/plural/source audit hatası **0**.
- Android Hermes export: **başarılı**, 4.164 modül / 9,1 MB HBC / 36 asset; `mobile/artifacts/payments-bigint-fix`. Fiziksel cihaz kabulü değildir.
- Backend/DTO/veritabanı değişmediği için bu düzeltmede backend/SQL testleri yeniden çalıştırılmadı. Önceki Phase 5.2 SQL sonuçları bu düzeltmenin yeni çalıştırması diye raporlanmaz.

Yeni testler: Türkçe/İngilizce/Almanca/Arapça Payments açılışı, TRY/USD/EUR listesi, UUID ile detay gezinmesi; 14 dil × TRY/USD/EUR/JPY/KWD güvenli Number Intl karşılaştırması; `999999999999999.9999`, güvenli tam sayı aralığını aşan toplam `9007199254740993.0001`, negatif `-0.0001`, baştaki/sontaki sıfırlar ve API mapping değişmezliği. Tüm mevcut dashboard/para birimi ayrımı, oluşturma/düzenleme, Calendar/History/native date picker ve abonelik testleri de BigInt reddeden test ortamında geçti.

## Android'de yeniden deneme

1. Güncel kaynaklarla Metro'yu `cd mobile; npx.cmd expo start --clear` komutuyla başlatın. Expo Go'da uygulamayı kapatıp güncel bundle'ı yeniden açın; eski indirilmiş bundle ile test etmeyin.
2. Payments ekranını sırayla Türkçe, English, Deutsch ve العربية seçerek açın. TRY/USD/EUR satırları ve detay gezinmesi hata vermemeli; Arapça rakamlar/yön korunmalı.
3. Test hesabında dört ondalıklı bir ödeme oluşturup düzenleyin; tutar ve para birimi sunucuya aynı gitmeli. Büyük tutar sınırını mevcut gerçek kayıtları değiştirmeden yalnız test hesabında deneyin.
4. Dashboard para birimi toplamları, Calendar, History, abonelik listesi/detayı ve analiz tutarlarını kontrol edin. Aynı kaydın tutarı dil değiştirmekle değişmemeli.

Fiziksel Android kabulü bekler. Phase 5.2 çalışmaları korundu; Phase 6/7 veya production çalışması yapılmadı.
