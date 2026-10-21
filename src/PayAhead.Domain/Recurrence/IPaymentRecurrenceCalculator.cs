namespace PayAhead.Domain.Recurrence;

public interface IPaymentRecurrenceCalculator
{
    /// <summary>İki sınır da dahildir. Geçersiz girdi veya sonuç sınırının aşılması DomainRuleException üretir.</summary>
    IReadOnlyList<DateOnly> CalculateOccurrences(PaymentPlan paymentPlan, DateOnly fromDate, DateOnly toDate);

    /// <summary>Referans tarihinde veya sonrasında, plan sınırları içindeki ilk tarih; yoksa null.</summary>
    DateOnly? GetNextOccurrence(PaymentPlan paymentPlan, DateOnly referenceDate);
}
