namespace PayAhead.Domain;

public sealed class RateLimitBucket
{
    public string Id { get; set; } = "";
    public int Count { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
