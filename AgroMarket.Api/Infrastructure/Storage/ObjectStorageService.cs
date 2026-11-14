using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace AgroMarket.Api.Infrastructure.Storage;

public sealed class R2Options
{
    public const string SectionName = "R2";

    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "agromarket-media";
    public string PublicBaseUrl { get; set; } = string.Empty;
    public int PresignedUrlMinutes { get; set; } = 15;
}

public sealed record PresignedUploadResult(
    string ObjectKey,
    string UploadUrl,
    string PublicUrl,
    DateTimeOffset ExpiresAt,
    bool IsLocalStub);

public interface IObjectStorageService
{
    PresignedUploadResult CreatePresignedPutUrl(
        Guid ownerId,
        string folder,
        string fileName,
        string contentType);
}

public sealed class R2ObjectStorageService(
    IOptions<R2Options> options,
    ILogger<R2ObjectStorageService> logger) : IObjectStorageService
{
    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/webp"] = ".webp",
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png"
    };

    private readonly R2Options _r2 = options.Value;

    public PresignedUploadResult CreatePresignedPutUrl(
        Guid ownerId,
        string folder,
        string fileName,
        string contentType)
    {
        if (!ExtensionByContentType.TryGetValue(contentType, out var extension))
        {
            throw new ArgumentException($"Unsupported content type '{contentType}'. Allowed: image/webp, image/jpeg, image/png.");
        }

        var safeFolder = folder.Trim('/').ToLowerInvariant();
        var objectKey = $"{safeFolder}/{ownerId:N}/{Guid.NewGuid():N}{extension}";
        var now = DateTimeOffset.UtcNow;
        var ttlSeconds = Math.Clamp(_r2.PresignedUrlMinutes * 60, 60, 3600);
        var expiresAt = now.AddSeconds(ttlSeconds);

        var publicBase = string.IsNullOrWhiteSpace(_r2.PublicBaseUrl)
            ? $"https://media.agromarket.local/{_r2.BucketName}"
            : _r2.PublicBaseUrl.TrimEnd('/');
        var publicUrl = $"{publicBase}/{objectKey}";

        if (string.IsNullOrWhiteSpace(_r2.AccountId)
            || string.IsNullOrWhiteSpace(_r2.AccessKeyId)
            || string.IsNullOrWhiteSpace(_r2.SecretAccessKey))
        {
            logger.LogInformation(
                "R2 credentials not configured; returning local stub presigned URL for {ObjectKey}",
                objectKey);

            return new PresignedUploadResult(
                objectKey,
                $"https://r2-stub.agromarket.local/{_r2.BucketName}/{objectKey}?expires={expiresAt.ToUnixTimeSeconds()}",
                publicUrl,
                expiresAt,
                IsLocalStub: true);
        }

        var host = $"{_r2.AccountId}.r2.cloudflarestorage.com";
        var canonicalUri = $"/{_r2.BucketName}/{objectKey}";
        var uploadUrl = SignS3PresignedPut(host, canonicalUri, ttlSeconds, now);

        return new PresignedUploadResult(
            objectKey,
            uploadUrl,
            publicUrl,
            expiresAt,
            IsLocalStub: false);
    }

    private string SignS3PresignedPut(
        string host,
        string canonicalUri,
        int expiresSeconds,
        DateTimeOffset now)
    {
        const string region = "auto";
        const string service = "s3";

        var amzDate = now.UtcDateTime.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var dateStamp = now.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var credentialScope = $"{dateStamp}/{region}/{service}/aws4_request";
        var credential = Uri.EscapeDataString($"{_r2.AccessKeyId}/{credentialScope}");

        var canonicalQuery = string.Join('&',
            "X-Amz-Algorithm=AWS4-HMAC-SHA256",
            $"X-Amz-Credential={credential}",
            $"X-Amz-Date={amzDate}",
            $"X-Amz-Expires={expiresSeconds}",
            "X-Amz-SignedHeaders=host");

        var canonicalHeaders = $"host:{host}\n";
        const string signedHeaders = "host";
        const string payloadHash = "UNSIGNED-PAYLOAD";

        var canonicalRequest = string.Join('\n',
            "PUT",
            canonicalUri,
            canonicalQuery,
            canonicalHeaders,
            signedHeaders,
            payloadHash);

        var canonicalRequestHash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));

        var stringToSign = string.Join('\n',
            "AWS4-HMAC-SHA256",
            amzDate,
            credentialScope,
            canonicalRequestHash);

        var signingKey = GetSignatureKey(_r2.SecretAccessKey, dateStamp, region, service);
        var signature = Convert.ToHexStringLower(
            HMACSHA256.HashData(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        return $"https://{host}{canonicalUri}?{canonicalQuery}&X-Amz-Signature={signature}";
    }

    private static byte[] GetSignatureKey(string secretKey, string dateStamp, string region, string service)
    {
        var kSecret = Encoding.UTF8.GetBytes($"AWS4{secretKey}");
        var kDate = HMACSHA256.HashData(kSecret, Encoding.UTF8.GetBytes(dateStamp));
        var kRegion = HMACSHA256.HashData(kDate, Encoding.UTF8.GetBytes(region));
        var kService = HMACSHA256.HashData(kRegion, Encoding.UTF8.GetBytes(service));
        return HMACSHA256.HashData(kService, Encoding.UTF8.GetBytes("aws4_request"));
    }
}
