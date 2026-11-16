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

    public string[] BootstrapAdminPhones { get; set; } = ["+994500000000"];
    public string[] BootstrapFarmerPhones { get; set; } = ["+994501111111", "+994501111112", "+994501111113", "+994501111114"];
    public string[] BootstrapRestaurantPhones { get; set; } = ["+994502222221", "+994502222222", "+994502222223"];
    public string[] BootstrapDriverPhones { get; set; } = ["+994503333331"];
    public bool EnableBootstrapDemoOtp { get; set; } = true;
    public string BootstrapOtpCode { get; set; } = "123456";

    public bool IsBootstrapPhone(string normalizedPhone) =>
        BootstrapAdminPhones.Concat(BootstrapFarmerPhones)
            .Concat(BootstrapRestaurantPhones)
            .Concat(BootstrapDriverPhones)
            .Select(AgroMarket.Api.Common.PhoneNumberNormalizer.Normalize)
            .Contains(normalizedPhone, StringComparer.Ordinal);
}
