namespace AgroMarket.Api.Features.Orders;

public sealed class MarketplaceOptions
{
    public const string SectionName = "Marketplace";

    public int DailyCutoffHourLocal { get; set; } = 19;
    public string TimeZoneId { get; set; } = "Asia/Baku";
    public decimal PlatformFeeRate { get; set; } = 0.05m;
    public decimal DefaultDeliveryFee { get; set; } = 10.00m;
    public int OrderConfirmExpiryMinutes { get; set; } = 180;
    public decimal VarianceToleranceRate { get; set; } = 0.10m;
    public int ExpirationSweepIntervalSeconds { get; set; } = 60;
}
