namespace PayAhead.Domain;

public sealed class PasswordHistoryEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = "";
    public DateTime? NormalChangedAtUtc { get; set; }
}
public sealed class LegalNoticeReceipt
{
    public Guid UserId { get; set; }
    public string? NoticeVersion { get; set; }
    public DateTime? PresentedAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
public enum LegalConfirmationKind { NoticeAcknowledgment, PrivacyReadAcknowledgment, TermsAcceptance }
public sealed class RegistrationLegalConfirmation
{
    public Guid UserId { get; set; }
    public string DocumentId { get; set; } = "";
    public LegalConfirmationKind Kind { get; set; }
    public string DocumentVersion { get; set; } = "";
    public DateTime ConfirmedAtUtc { get; set; }
}
