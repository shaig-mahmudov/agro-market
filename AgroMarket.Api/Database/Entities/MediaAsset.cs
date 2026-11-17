namespace AgroMarket.Api.Database.Entities;

public sealed class MediaAsset
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Folder { get; set; } = "products";
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/webp";
    public int Width { get; set; }
    public int Height { get; set; }
    public int SizeBytes { get; set; }
    public byte[] Data { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }

    public User Owner { get; set; } = null!;
}
