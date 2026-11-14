using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AgroMarket.Api.Infrastructure.Auth;

public sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _jwt = options.Value;

    public TokenPair IssueTokenPair(User user)
    {
        var now = DateTimeOffset.UtcNow;
        var accessExpires = now.AddMinutes(_jwt.AccessTokenMinutes);
        var refreshExpires = now.AddDays(_jwt.RefreshTokenDays);

        var keyBytes = Encoding.UTF8.GetBytes(_jwt.SigningKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("phone_number", user.PhoneNumber),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("is_verified", user.IsVerified ? "true" : "false")
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: accessExpires.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateSecureToken(48);

        return new TokenPair(accessToken, accessExpires, refreshToken, refreshExpires);
    }

    public string CreateTelegramLinkToken(Guid userId, TimeSpan ttl)
    {
        var expiresUnix = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds();
        var payload = $"{userId:N}.{expiresUnix}";
        var sig = ComputeHmacBase64Url(payload);
        return $"{payload}.{sig}";
    }

    public bool TryValidateTelegramLinkToken(string token, out Guid userId)
    {
        userId = Guid.Empty;
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        if (!Guid.TryParseExact(parts[0], "N", out userId)
            || !long.TryParse(parts[1], out var expiresUnix))
        {
            return false;
        }

        if (DateTimeOffset.FromUnixTimeSeconds(expiresUnix) <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        var expectedSig = ComputeHmacBase64Url($"{parts[0]}.{parts[1]}");
        var actualBytes = Encoding.UTF8.GetBytes(parts[2]);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSig);

        return actualBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }

    private string ComputeHmacBase64Url(string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_jwt.SigningKey);
        var hash = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(hash);
    }

    private static string GenerateSecureToken(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
