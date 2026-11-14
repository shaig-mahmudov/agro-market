namespace AgroMarket.Api.Database.Entities;

public sealed class BusinessProfile
{
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? PreferredDeliveryWindow { get; set; }
    public string? TaxId { get; set; }

    public User User { get; set; } = null!;
}
