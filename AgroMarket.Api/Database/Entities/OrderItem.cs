namespace AgroMarket.Api.Database.Entities;

public sealed class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductTitleSnapshot { get; set; } = string.Empty;
    public decimal UnitPriceApplied { get; set; }
    public decimal EstimatedWeightKg { get; set; }
    public decimal? ActualDeliveredWeightKg { get; set; }
    public decimal? RejectedWeightKg { get; set; }
    public string? ProofPhotoUrl { get; set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
