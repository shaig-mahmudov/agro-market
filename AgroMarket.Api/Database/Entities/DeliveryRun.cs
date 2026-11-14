namespace AgroMarket.Api.Database.Entities;

public sealed class DeliveryRun
{
    public Guid Id { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string DriverPhone { get; set; } = string.Empty;
    public string? VehicleNote { get; set; }
    public DateOnly RunDate { get; set; }
    public DeliveryRunStatus Status { get; set; } = DeliveryRunStatus.Planned;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Order> Orders { get; set; } = [];
}
