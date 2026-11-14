namespace AgroMarket.Api.Database.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public long? TelegramChatId { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public BusinessProfile? BusinessProfile { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}
