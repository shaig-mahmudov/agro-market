using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AgroMarket.Api.Database.Entities;

namespace AgroMarket.Api.Common;

public static class AuthorizationPolicies
{
    public const string Admin = "RequireAdmin";
    public const string VerifiedUser = "RequireVerifiedUser";
    public const string VerifiedFarmer = "RequireVerifiedFarmer";
    public const string VerifiedRestaurant = "RequireVerifiedRestaurant";
}

public static class PhoneNumberNormalizer
{
    public static string Normalize(string rawPhone)
    {
        var trimmed = rawPhone.Trim();
        var sb = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            if (char.IsDigit(ch))
            {
                sb.Append(ch);
            }
            else if (ch == '+' && sb.Length == 0)
            {
                sb.Append(ch);
            }
        }

        var cleaned = sb.ToString();
        return cleaned.StartsWith('+') ? cleaned : $"+{cleaned}";
    }

    public static bool IsValidE164(string normalizedPhone)
    {
        if (string.IsNullOrWhiteSpace(normalizedPhone) || normalizedPhone[0] != '+')
        {
            return false;
        }

        var digits = normalizedPhone[1..];
        return digits.Length is >= 8 and <= 15
            && digits[0] != '0'
            && digits.All(char.IsAsciiDigit);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(raw, out var id)
            ? id
            : throw new UnauthorizedAccessException("Authenticated user ID claim is missing.");
    }

    public static UserRole GetUserRole(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.Role)
            ?? user.FindFirstValue("role");

        return Enum.TryParse<UserRole>(raw, ignoreCase: true, out var role)
            ? role
            : throw new UnauthorizedAccessException("Authenticated user role claim is missing.");
    }

    public static bool IsVerifiedUser(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("is_verified");
        return bool.TryParse(raw, out var verified) && verified;
    }
}

public static class SecretHasher
{
    public static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
