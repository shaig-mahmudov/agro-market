using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace AgroMarket.Api.Infrastructure.Storage;

public sealed record MediaUploadResult(
    Guid Id,
    string FileName,
    string ContentType,
    int SizeBytes,
    int Width,
    int Height,
    string Url);

public interface IImageStorageService
{
    Task<MediaUploadResult> ConvertToWebpAndStoreAsync(
        AppDbContext db,
        Guid ownerId,
        string folder,
        IFormFile file,
        string publicBaseUrl,
        CancellationToken cancellationToken = default);
}

public sealed class DatabaseImageStorageService : IImageStorageService
{
    public const long MaxUploadSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxDimensionPx = 1600;
    private const int WebpQuality = 80;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp"
    };

    public async Task<MediaUploadResult> ConvertToWebpAndStoreAsync(
        AppDbContext db,
        Guid ownerId,
        string folder,
        IFormFile file,
        string publicBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0)
        {
            throw new ArgumentException("Uploaded image file cannot be empty.");
        }

        if (file.Length > MaxUploadSizeBytes)
        {
            throw new ArgumentException($"Image file exceeds maximum allowed size of {MaxUploadSizeBytes / (1024 * 1024)} MB.");
        }

        var originalExtension = Path.GetExtension(file.FileName ?? string.Empty);
        var contentType = file.ContentType?.Trim() ?? string.Empty;

        if (!AllowedContentTypes.Contains(contentType) && !AllowedExtensions.Contains(originalExtension))
        {
            throw new ArgumentException("Unsupported file format. Allowed formats: PNG, JPG, JPEG, WebP.");
        }

        await using var inputStream = file.OpenReadStream();
        using var image = await TryLoadImageAsync(inputStream, cancellationToken);

        image.Mutate(ctx =>
        {
            ctx.AutoOrient();

            if (image.Width > MaxDimensionPx || image.Height > MaxDimensionPx)
            {
                ctx.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxDimensionPx, MaxDimensionPx)
                });
            }
        });

        using var outputStream = new MemoryStream();
        var encoder = new WebpEncoder
        {
            Quality = WebpQuality,
            FileFormat = WebpFileFormatType.Lossy
        };

        await image.SaveAsWebpAsync(outputStream, encoder, cancellationToken);
        var webpBytes = outputStream.ToArray();

        var baseFileName = Path.GetFileNameWithoutExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(baseFileName))
        {
            baseFileName = "image";
        }

        var safeBaseName = new string(baseFileName
            .Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            .Take(80)
            .ToArray());

        if (string.IsNullOrWhiteSpace(safeBaseName))
        {
            safeBaseName = "image";
        }

        var webpFileName = $"{safeBaseName}.webp";
        var safeFolder = string.IsNullOrWhiteSpace(folder)
            ? "products"
            : folder.Trim('/').ToLowerInvariant();

        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Folder = safeFolder,
            FileName = webpFileName,
            ContentType = "image/webp",
            Width = image.Width,
            Height = image.Height,
            SizeBytes = webpBytes.Length,
            Data = webpBytes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.MediaAssets.Add(asset);
        await db.SaveChangesAsync(cancellationToken);

        var trimmedBaseUrl = publicBaseUrl.TrimEnd('/');
        var url = $"{trimmedBaseUrl}/api/media/{asset.Id:N}.webp";

        return new MediaUploadResult(
            asset.Id,
            asset.FileName,
            asset.ContentType,
            asset.SizeBytes,
            asset.Width,
            asset.Height,
            url);
    }

    private static async Task<Image> TryLoadImageAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            return await Image.LoadAsync(stream, cancellationToken);
        }
        catch (UnknownImageFormatException ex)
        {
            throw new ArgumentException("Invalid or unsupported image file. Please upload a valid PNG, JPG, JPEG, or WebP image.", ex);
        }
        catch (InvalidImageContentException ex)
        {
            throw new ArgumentException("Corrupted image content.", ex);
        }
    }
}
