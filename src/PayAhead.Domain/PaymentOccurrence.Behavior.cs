using PayAhead.Domain.Recurrence;

namespace PayAhead.Domain;

public sealed partial class PaymentOccurrence
{
    public static PaymentOccurrence Create(Guid paymentPlanId, DateOnly dueDate, OccurrenceStatus status,
        decimal? actualAmount, DateTime nowUtc)
    {
        if (paymentPlanId == Guid.Empty) throw new DomainRuleException("Geçerli ödeme planı gereklidir.");
        var occurrence = new PaymentOccurrence
        { Id = Guid.NewGuid(), PaymentPlanId = paymentPlanId, DueDate = dueDate, CreatedAtUtc = nowUtc };
        occurrence.ChangeStatus(status, actualAmount, nowUtc);
        return occurrence;
    }

    public bool ChangeStatus(OccurrenceStatus status, decimal? actualAmount, DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) throw new DomainRuleException("Zaman damgası UTC olmalıdır.");
        if (!Enum.IsDefined(status)) throw new DomainRuleException("Geçersiz ödeme durumu.");
        if (status == OccurrenceStatus.Paid)
        {
            if (actualAmount is null || !PaymentPlanRules.IsValidAmount(actualAmount.Value))
                throw new DomainRuleException("Paid için pozitif decimal(19,4) ActualAmount gereklidir.");
        }
        else if (actualAmount is not null) throw new DomainRuleException("ActualAmount yalnızca Paid durumunda tutulabilir.");

        if (Status == status && ActualAmount == actualAmount) return false;
        Status = status; ActualAmount = actualAmount;
        PaidAtUtc = status == OccurrenceStatus.Paid ? nowUtc : null;
        return true;
    }
}

public static class OccurrenceScheduleRules
{
    public static void EnsureValidDate(PaymentPlan plan, DateOnly dueDate, IPaymentRecurrenceCalculator calculator)
    {
        if (calculator.GetNextOccurrence(plan, dueDate) != dueDate)
            throw new DomainRuleException("Tarih ödeme planının aktif recurrence takvimine ait değildir.");
    }
}
