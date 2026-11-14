namespace AgroMarket.Api.Database.Entities;

public sealed class OtpChallenge
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive => ConsumedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
