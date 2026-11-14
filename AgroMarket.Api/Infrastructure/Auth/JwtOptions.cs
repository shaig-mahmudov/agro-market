namespace AgroMarket.Api.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "AgroMarket.Api";
    public string Audience { get; set; } = "AgroMarket.Clients";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}

public sealed class AuthBootstrapOptions
{
    public const string SectionName = "Auth";

    public string[] BootstrapAdminPhones { get; set; } = [];
}
