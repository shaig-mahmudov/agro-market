namespace AgroMarket.Api.Database.Entities;

public sealed class Order
{
    public Guid Id { get; set; }
    public Guid CheckoutGroupId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid FarmerId { get; set; }
    public Guid? DeliveryRunId { get; set; }
    public FulfillmentType FulfillmentType { get; set; } = FulfillmentType.PlatformCourier;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal EstimatedTotalAmount { get; set; }
    public decimal PlatformFeeAmount { get; set; }
    public decimal DeliveryFeeAmount { get; set; }
    public decimal FinalTotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public string? DisputeNote { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public CheckoutGroup CheckoutGroup { get; set; } = null!;
    public User Restaurant { get; set; } = null!;
    public User Farmer { get; set; } = null!;
    public DeliveryRun? DeliveryRun { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];
}
