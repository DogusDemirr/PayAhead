namespace PayAhead.Domain.Recurrence;

/// <summary>Saat, veritabanı veya global durum kullanmayan Gregorian takvim hesaplayıcısı.</summary>
public sealed class PaymentRecurrenceCalculator : IPaymentRecurrenceCalculator
{
    public const int MaximumOccurrences = 10_000;

    public IReadOnlyList<DateOnly> CalculateOccurrences(PaymentPlan paymentPlan, DateOnly fromDate, DateOnly toDate)
    {
        var schedule = ReadSchedule(paymentPlan);
        if (fromDate > toDate) throw new DomainRuleException("Başlangıç aralığı bitiş aralığından sonra olamaz.");
        if (!schedule.IsActive) return Array.Empty<DateOnly>();
        var lower = fromDate < schedule.StartDate ? schedule.StartDate : fromDate;
        var upper = toDate < schedule.EndDate ? toDate : schedule.EndDate;
        if (lower > upper) return Array.Empty<DateOnly>();

        var index = FirstIndex(schedule, lower);
        var dates = new List<DateOnly>();
        while (AtIndex(schedule, index) is { } date && date <= upper)
        {
            if (dates.Count == MaximumOccurrences)
                throw new DomainRuleException($"Tek hesaplamada en fazla {MaximumOccurrences} ödeme tarihi döndürülebilir; tarih aralığını daraltın.");
            dates.Add(date);
            index++;
        }
        return dates.AsReadOnly();
    }

    public DateOnly? GetNextOccurrence(PaymentPlan paymentPlan, DateOnly referenceDate)
    {
        var schedule = ReadSchedule(paymentPlan);
        if (!schedule.IsActive) return null;
        var lower = referenceDate < schedule.StartDate ? schedule.StartDate : referenceDate;
        if (lower > schedule.EndDate) return null;
        var date = AtIndex(schedule, FirstIndex(schedule, lower));
        return date <= schedule.EndDate ? date : null;
    }

    private static Schedule ReadSchedule(PaymentPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var schedule = new Schedule(plan.StartDate, plan.EndDate ?? DateOnly.MaxValue, plan.Frequency,
            plan.Interval, plan.AnchorDay ?? plan.StartDate.Day, plan.IsActive);
        if (schedule.Interval < 1) throw new DomainRuleException("Tekrar aralığı en az 1 olmalıdır.");
        if (!Enum.IsDefined(schedule.Frequency)) throw new DomainRuleException("Tanımsız tekrar sıklığı.");
        if (schedule.AnchorDay is < 1 or > 31) throw new DomainRuleException("AnchorDay 1 ile 31 arasında olmalıdır.");
        if (schedule.EndDate < schedule.StartDate) throw new DomainRuleException("Plan bitiş tarihi başlangıçtan önce olamaz.");
        return schedule;
    }

    // Önceki bütün tekrarları dolaşmadan yaklaşık ilk indeks bulunur.
    // Ay/yıl hesabında aynı dönemdeki ödeme geçmişse yalnızca bir düzeltme gerekir.
    private static long FirstIndex(Schedule schedule, DateOnly lower)
    {
        if (lower <= schedule.StartDate) return 0;
        var index = schedule.Frequency switch
        {
            PaymentFrequency.Once => 1,
            PaymentFrequency.Daily => CeilingDivide(lower.DayNumber - schedule.StartDate.DayNumber, schedule.Interval),
            PaymentFrequency.Weekly => CeilingDivide(lower.DayNumber - schedule.StartDate.DayNumber, 7L * schedule.Interval),
            PaymentFrequency.Monthly => Math.Max(1L, (MonthIndex(lower) - MonthIndex(schedule.StartDate)) / (long)schedule.Interval),
            PaymentFrequency.Yearly => Math.Max(1L, (lower.Year - schedule.StartDate.Year) / (long)schedule.Interval),
            _ => throw new DomainRuleException("Tanımsız tekrar sıklığı.")
        };
        if (AtIndex(schedule, index) is { } candidate && candidate < lower) index++;
        return index;
    }

    private static DateOnly? AtIndex(Schedule schedule, long index)
    {
        // StartDate her sıklıkta ilk ödemedir; açık AnchorDay sonraki ay/yıllara uygulanır.
        if (index == 0) return schedule.StartDate;
        if (schedule.Frequency == PaymentFrequency.Once) return null;
        if (schedule.Frequency is PaymentFrequency.Daily or PaymentFrequency.Weekly)
        {
            var step = (schedule.Frequency == PaymentFrequency.Weekly ? 7L : 1L) * schedule.Interval;
            if (index > (DateOnly.MaxValue.DayNumber - schedule.StartDate.DayNumber) / step) return null;
            return DateOnly.FromDayNumber((int)(schedule.StartDate.DayNumber + index * step));
        }
        if (schedule.Frequency == PaymentFrequency.Monthly)
        {
            var startMonth = MonthIndex(schedule.StartDate);
            if (index > (MonthIndex(DateOnly.MaxValue) - startMonth) / (long)schedule.Interval) return null;
            var monthIndex = startMonth + index * schedule.Interval;
            return AnchoredDate((int)(monthIndex / 12 + 1), (int)(monthIndex % 12 + 1), schedule.AnchorDay);
        }
        if (index > (9999L - schedule.StartDate.Year) / schedule.Interval) return null;
        return AnchoredDate((int)(schedule.StartDate.Year + index * schedule.Interval), schedule.StartDate.Month, schedule.AnchorDay);
    }

    private static DateOnly AnchoredDate(int year, int month, int anchorDay)
        => new(year, month, Math.Min(anchorDay, DateTime.DaysInMonth(year, month)));

    private static int MonthIndex(DateOnly date) => (date.Year - 1) * 12 + date.Month - 1;
    private static long CeilingDivide(long value, long divisor) => value / divisor + (value % divisor == 0 ? 0 : 1);
    private readonly record struct Schedule(DateOnly StartDate, DateOnly EndDate, PaymentFrequency Frequency,
        int Interval, int AnchorDay, bool IsActive);
}
