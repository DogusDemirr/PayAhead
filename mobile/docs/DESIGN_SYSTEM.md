# PayAhead tasarım sistemi

## Görsel yön

Sakin ve güven veren lacivert temel, teal vurgu, ferah beyaz kartlar ve özgün ileri yön oku. Marka varlıkları `assets/brand` içindedir; `npm.cmd run brand:generate` SVG kaynağından PNG üretir. Başka finans uygulaması ekranı veya sağlayıcı logosu kopyalanmadı. Kurgusal sağlayıcılar Lucide kategori ikonlarıyla temsil edilir. Inter fontu SIL OFL, Lucide ISC lisanslıdır; paketlerin lisans metinleri korunur.

## Tokenlar

Tek kaynak `src/theme/tokens.ts`.

| Alan | Light | Dark |
|---|---|---|
| Background | #F5F7FA | #0B1220 |
| Surface | #FFFFFF | #151F30 |
| Elevated | #EDF2F7 | #1C2A40 |
| Ana metin | #101D35 | #F8FAFC |
| İkincil metin | #64748B | #94A3B8 |
| Accent | #14B8A6 | #2DD4BF |
| Border | #E5EAF0 | #29374A |

Primary navy #101D35. Açık zemindeki küçük teal metin #087C70; açık temada başarı #15803D, uyarı #926000 ve hata #C53030 okunabilir koyu varyantlardır. Marka semantik başlangıç renkleri #16A34A / #F59E0B / #EF4444 token dosyasında ayrıca tanımlıdır. Koyu temada semantik metinler açık tonlara döner. Ana özet kartı lacivert → koyu teal yumuşak geçiş, ince dekoratif daireler ve beyaz tutar kullanır.

Inter 400/500/600/700 uygulamaya paketlenir; uzaktan font indirilmez. Display 34/42, ekran başlığı 28/36, bölüm 19/27, body 15/23, caption 12/18, tutar 34/44. Finansal tutarlar tabular numeral ve `tr-TR` Intl biçimlendirme kullanır. USD ve TRY ayrı gösterilir; döviz dönüşümü yapılmaz.

Spacing 4/8/12/16/20/24/32/40; ekran yatay boşluğu 20. Kart radius 20, buton/input 14. Kartlar ağır gölge yerine yüzey kontrastıyla ayrılır. Web uygulama çerçevesi en fazla 480 genişliktedir; native ekran kullanılabilir genişliğe oturur. Metinler sabit satır sayısıyla kesilmez, scroll alanı uzayabilir.

## Bileşen kataloğu

`ui.tsx`: AppScreen, AppText, AppButton, AppInput, AppCard, AppHeader, CurrencyAmount, StatusBadge, SectionHeader, LoadingSkeleton, EmptyState, ErrorState; ayrıca IconButton, Brand, Assurance, SettingRow, MotionCard.

`finance.tsx`: SummaryCard, PaymentListItem, SubscriptionCard ve CategoryIcon. Ekranlar aynı tutar, kategori, tarih, durum ve boşluk düzenini paylaşır.

Ana butonlar 54, ikon butonları 48, filtreler 48, liste satırları en az 82 yüksekliğinde. Takvim yedi eşit sütun nedeniyle küçük ekranlarda yatayda 48 dp altında kalabilir; yükseklik 52'dir. Bu özel alanın native erişilebilirlik kabulü beklemektedir. Tab etiketleri 10 pt; büyük font ölçeği cihazda ayrıca denetlenmelidir.

Butonlarda opacity/scale geri bildirimi, kartlarda kısa giriş animasyonu, tab/stack fade ve skeleton pulse vardır. Reanimated reduced-motion bilgisi kart/stack/tab/skeleton hareketini kapatır. Basılı durum geri bildirimi anlıktır. Erişilebilir button/tab/radio rolleri, seçili state, input label, hata alert ve yükleme progressbar kullanılır. Ekran safe-area yönetimi üstte root, yan/alt alanlarda AppScreen tarafından yapılır.

## Ekran kompozisyonu

Dashboard: saat bazlı karşılama, 30 günlük özet, ayrı döviz toplamı, iki hızlı aksiyon, sonraki üç ödeme, abonelik sayıları ve trial uyarısı. Ödemeler: arama + isteğe bağlı para birimi filtreleri + üç durum sekmesi. Takvim: pazartesi başlayan ay görünümü, gün göstergeleri, seçili günün listesi ve para birimi bazlı plan tutarı. Abonelikler: aylık karşılıklar, aktif/trial ayrımı, yenileme bilgisi. Ayarlar: örnek profil, tema, önizleme tercihleri ve hesap bilgilendirmeleri. Auth: özgün marka, değer önerisi, klavyeye uyumlu kaydırılan form ve açık bağlantı durumu.

Tasarım kodda uygulanmıştır; bu oturumda görsel ekran görüntüsü veya native cihaz kontrolü yapılamadı. Premium görsel kabul tamamlanmış olarak işaretlenmez. Kabul listesi ANDROID_TESTING.md içindedir.
