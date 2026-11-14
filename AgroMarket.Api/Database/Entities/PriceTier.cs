namespace AgroMarket.Api.Database.Entities;

public sealed class PriceTier
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal MinQuantityKg { get; set; }
    public decimal PricePerKg { get; set; }

    public Product Product { get; set; } = null!;
}
