namespace AgroMarket.Api.Database.Entities;

public sealed class CheckoutGroup
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public DateOnly ScheduledDeliveryDate { get; set; }
    public DateTimeOffset CutoffAt { get; set; }
    public decimal TotalDeliveryFee { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User Restaurant { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = [];
}
