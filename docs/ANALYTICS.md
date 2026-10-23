# Finansal analiz API'si — Frontend Phase 4

`GET /api/analytics?year=2026&month=10&currencyCode=TRY` JWT gerektirir. Year/month zorunlu; currencyCode isteğe bağlı ISO 4217. Seçilen ay ve önceki 5 ay sunucuda hesaplanır. İlk desteklenen ay Haziran 0001'dir. Status filtresi kabul edilmez; karşılaştırma tüm durumları içerir. Kullanıcı `sub` kimliğinden alınır; istemci sahiplik ID'si alınmaz. Kullanıcı başına ortak SQL kotası 30 istek/dakika; 429/Retry-After ve koruma erişilemiyorsa 503 uygulanır.

Yanıt:

```text
context: fromDate, toDate, today, timeZoneId, usedTimeZoneFallback
currencies[]:
  currencyCode
  months[6]: month, plannedAmount, paidAmount, pendingAmount, overdueAmount,
             paidCount, pendingCount, skippedCount
  paidCategories[]: label, amount, count
  topPlans[0..5]: label, amount, count
```

Paralar JSON decimal sayısıdır; mobil lossless parser bunları string olarak korur. Grafik için yalnızca piksel oranı Number'a dönüştürülür. Toplam, sıralama ve aggregation sunucuda decimal ile yapılır. Farklı para birimleri hiçbir zaman toplanmaz; kur dönüşümü yoktur.

- Tarih ekseni **DueDate**'tir. PaidAtUtc ayına göre nakit akış analizi değildir. Bu ayrım mobil ekranda gösterilir; Dashboard'un aynı due-date dönemindeki paid toplamıyla karşılaştırılabilir.
- Planned, mevcut plan tutarıyla tüm ilgili tekrarların toplamıdır; paid/skipped dahil. Tarihsel fiyat snapshot'ı yoktur. Plan düzenlemesi geçmiş planlanan tutarları etkileyebilir.
- Paid yalnızca Paid kayıtlarının ActualAmount toplamıdır. Skipped ödenmişe katılmaz. Pending gecikenleri de içerir; Overdue, sadece o ay içindeki DueDate < server Today olan Pending alt kümesidir. “Tüm geçmiş borç” değildir.
- Para birimi, kategori ve ad güncel plan metadata'sıdır; tarihsel snapshot saklanmaz. Planın para birimini sonradan değiştirmek eski ödeme tutarlarının görüntülenen para birimini de etkiler. Geçmiş para birimi doğruluğu için plan para birimini değiştirmek yerine yeni plan açılması gerekir. Snapshot migration bu fazın izinli backend kapsamına eklenmedi.
- Aktif plan tekrarları ile saklı Paid/Skipped kayıtları mevcut recurrence/overview kurallarıyla birleştirilir. Pasif planda yalnızca saklı geçmiş görünür. Silinmiş/geçmişte başka tanımlı tüm planlar yeniden oluşturulmaz; muhasebe defteri garantisi yoktur.
- Para birimi için ilgili aralıkta veri varsa altı ayın tamamı hesaplanır; o para birimindeki hareket bulunmayan ay gerçek kapsamda 0 döner. Hiç veri yoksa `currencies: []`; mobil uydurma para birimi/örnek grafik göstermez. Limit/hata durumunda kısmi toplam veya sıfır verilmez.

Sorgu, mevcut `(PaymentPlanId, DueDate)` occurrence unique index'i ve UserId plan index'lerini kullanır. En fazla 1000 ilgili plan/50000 birleşik tekrar; aşımda aralığı/para birimini daraltma hatası döner. Veriler telefona tek tek indirilmez; bounded projection/aggregation sunucudadır. İstek yeni occurrence kaydetmez. Kategori grafiği mobilde en yüksek 8 kategoriyi, plan listesi sunucunun en yüksek 5 planını gösterir. Büyük ölçek için mevcut cap'ler korunur; sınırsız analitik/data warehouse eklenmez.

Mevcut sözleşmeler değişmedi: takvim `GET /api/calendar`, durum bazlı dönem/pagination `GET /api/payments/upcoming`, tek ödeme `GET /api/payments/{id}/occurrences/{dueDate}`, durum değişimi `PUT .../{dueDate}/status`. Durum gövdesi yalnızca `status`, `actualAmount`, `rowVersion`; PaidAtUtc sunucu atamasıdır. Kaydedilmemiş Pending sanal occurrence'dır. Pending'e dönüş saklı kaydı kaldırır; para tahsilatı yapmaz. Geçmiş mobil görünümü bugünden ileri DueDate istemez; mevcut endpoint'in sırasını (DueDate, ad, ID) korur.
