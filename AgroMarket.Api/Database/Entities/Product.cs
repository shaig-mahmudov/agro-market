namespace AgroMarket.Api.Database.Entities;

public sealed class Product
{
    public Guid Id { get; set; }
    public Guid FarmerId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal AvailableStockKg { get; set; }
    public decimal MinOrderKg { get; set; }
    public decimal BasePricePerKg { get; set; }
    public ProductMetadata Metadata { get; set; } = new();
    public string[] ImageUrls { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optimistic concurrency token backed by PostgreSQL <c>xmin</c> system column.
    /// </summary>
    public uint RowVersion { get; set; }

    public User Farmer { get; set; } = null!;
    public ICollection<PriceTier> PriceTiers { get; set; } = [];
}

public sealed class ProductMetadata
{
    public string? Variety { get; set; }
    public DateOnly? HarvestDate { get; set; }
    public string? Grade { get; set; }
    public string? Packaging { get; set; }
    public bool IsOrganic { get; set; }
}
