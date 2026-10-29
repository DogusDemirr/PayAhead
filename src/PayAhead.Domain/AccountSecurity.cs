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
