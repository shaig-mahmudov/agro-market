namespace AgroMarket.Api.Database.Entities;

public enum UserRole
{
    Farmer = 0,
    Restaurant = 1,
    Driver = 2,
    Admin = 3
}

public enum DeliveryRunStatus
{
    Planned = 0,
    PickingUp = 1,
    InTransit = 2,
    Completed = 3
}

public enum FulfillmentType
{
    PlatformCourier = 0,
    FarmerDelivery = 1,
    BuyerPickup = 2
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    PickedUp = 2,
    InTransit = 3,
    DeliveredPendingApproval = 4,
    Delivered = 5,
    Disputed = 6,
    Cancelled = 7
}

public enum PaymentMethod
{
    CashOnDelivery = 0,
    BankTransfer = 1,
    WeeklyInvoice = 2
}

public enum PaymentStatus
{
    Unpaid = 0,
    Paid = 1,
    Disputed = 2
}
