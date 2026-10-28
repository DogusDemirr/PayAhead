namespace PayAhead.Application.Payments;

public sealed class PaymentDeletionRestrictedException(string code) : Exception
{
    public string Code { get; } = code;
}
