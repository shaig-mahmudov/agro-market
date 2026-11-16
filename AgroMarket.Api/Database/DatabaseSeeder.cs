using AgroMarket.Api.Common;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgroMarket.Api.Database;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; } = true;
    public bool SeedSampleData { get; set; } = true;
}

public sealed record BootstrapAccountInfo(
    Guid Id,
    string PhoneNumber,
    string FullName,
    UserRole Role,
    bool IsVerified,
    string BusinessName,
    string Region,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    string? PreferredDeliveryWindow,
    string? TaxId);

public static class DatabaseSeeder
{
    // Deterministic GUIDs for idempotent seeding across restarts
    public static readonly Guid AdminUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid FarmerGubaUserId = Guid.Parse("10000000-0000-0000-0000-000000000011");
    public static readonly Guid FarmerLankaranUserId = Guid.Parse("10000000-0000-0000-0000-000000000012");
    public static readonly Guid FarmerZiraUserId = Guid.Parse("10000000-0000-0000-0000-000000000013");
    public static readonly Guid FarmerShekiPendingUserId = Guid.Parse("10000000-0000-0000-0000-000000000014");
    public static readonly Guid RestaurantCaspianUserId = Guid.Parse("10000000-0000-0000-0000-000000000021");
    public static readonly Guid RestaurantIcherisheherUserId = Guid.Parse("10000000-0000-0000-0000-000000000022");
    public static readonly Guid RestaurantPortBakuPendingUserId = Guid.Parse("10000000-0000-0000-0000-000000000023");
    public static readonly Guid DriverElchinUserId = Guid.Parse("10000000-0000-0000-0000-000000000031");

    public static readonly IReadOnlyList<BootstrapAccountInfo> DefaultBootstrapAccounts =
    [
        new(
            AdminUserId,
            "+994500000000",
            "Platform Admin",
            UserRole.Admin,
            true,
            "AgroMarket Operations HQ",
            "Baku",
            "8 November Ave 25, Khatai, Baku",
            40.379200m,
            49.875600m,
            "06:00 - 10:00",
            "1400000001"),
        new(
            FarmerGubaUserId,
            "+994501111111",
            "Rashad Aliyev",
            UserRole.Farmer,
            true,
            "Guba Highland Orchards",
            "Guba-Khachmaz",
            "Nugadi Village Rd 14, Guba",
            41.361100m,
            48.513600m,
            "04:30 - 06:30",
            "1700234501"),
        new(
            FarmerLankaranUserId,
            "+994501111112",
            "Farid Hasanov",
            UserRole.Farmer,
            true,
            "Lankaran Citrus & Subtropics",
            "Lankaran-Astara",
            "Hirkan Highway 8, Lankaran",
            38.752900m,
            48.850900m,
            "04:00 - 06:00",
            "1800543201"),
        new(
            FarmerZiraUserId,
            "+994501111113",
            "Aysel Mammadova",
            UserRole.Farmer,
            true,
            "Zira Eco Greenhouses",
            "Absheron",
            "Zira Agro Park Block C, Khazar, Baku",
            40.365300m,
            50.291700m,
            "05:00 - 07:00",
            "1900876501"),
        new(
            FarmerShekiPendingUserId,
            "+994501111114",
            "Kamran Babayev",
            UserRole.Farmer,
            false,
            "Sheki Walnut & Orchard Co.",
            "Sheki-Zagatala",
            "Kish Valley 12, Sheki",
            41.191900m,
            47.170600m,
            "05:00 - 07:30",
            "2000112201"),
        new(
            RestaurantCaspianUserId,
            "+994502222221",
            "Leyla Mammadova",
            UserRole.Restaurant,
            true,
            "Caspian Bistro & Grill",
            "Baku",
            "Nizami St 42, Sabail, Baku",
            40.372500m,
            49.842800m,
            "07:00 - 09:00",
            "1400998801"),
        new(
            RestaurantIcherisheherUserId,
            "+994502222222",
            "Orkhan Guliyev",
            UserRole.Restaurant,
            true,
            "Icherisheher Courtyard Kitchen",
            "Baku",
            "Boyuk Qala St 18, Icherisheher, Baku",
            40.366100m,
            49.835200m,
            "07:30 - 09:30",
            "1400776601"),
        new(
            RestaurantPortBakuPendingUserId,
            "+994502222223",
            "Nigar Karimova",
            UserRole.Restaurant,
            false,
            "Port Baku Artisan Cafe",
            "Baku",
            "Neftchilar Ave 151, Nasimi, Baku",
            40.375400m,
            49.859800m,
            "08:00 - 10:00",
            "1400554401"),
        new(
            DriverElchinUserId,
            "+994503333331",
            "Elchin Mustafayev",
            UserRole.Driver,
            true,
            "Baku Cold-Chain Fleet",
            "Baku",
            "Heydar Aliyev Ave 108, Narimanov, Baku",
            40.402100m,
            49.868900m,
            "05:00 - 11:00",
            "1500332201")
    ];

    // Deterministic Product IDs
    public static readonly Guid ProductZiraPinkTomatoId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid ProductCherryTomatoId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid ProductCulinaryHerbsId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    public static readonly Guid ProductBabyCucumberId = Guid.Parse("20000000-0000-0000-0000-000000000004");
    public static readonly Guid ProductGubaGoldenAppleId = Guid.Parse("20000000-0000-0000-0000-000000000005");
    public static readonly Guid ProductGubaHoneycrispId = Guid.Parse("20000000-0000-0000-0000-000000000006");
    public static readonly Guid ProductKhachmazPotatoId = Guid.Parse("20000000-0000-0000-0000-000000000007");
    public static readonly Guid ProductLankaranMandarinId = Guid.Parse("20000000-0000-0000-0000-000000000008");
    public static readonly Guid ProductAstaraLemonId = Guid.Parse("20000000-0000-0000-0000-000000000009");

    // Deterministic DeliveryRun, CheckoutGroup, and Order IDs
    private static readonly Guid RunCompletedId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid RunInTransitId = Guid.Parse("30000000-0000-0000-0000-000000000002");

    private static readonly Guid CheckoutGroupCompletedId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid CheckoutGroupInTransitId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid CheckoutGroupUpcomingId = Guid.Parse("40000000-0000-0000-0000-000000000003");

    private static readonly Guid OrderDeliveredPaidId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid OrderDeliveredInvoiceId = Guid.Parse("50000000-0000-0000-0000-000000000002");
    private static readonly Guid OrderInTransitId = Guid.Parse("50000000-0000-0000-0000-000000000003");
    private static readonly Guid OrderPendingApprovalId = Guid.Parse("50000000-0000-0000-0000-000000000004");
    private static readonly Guid OrderConfirmedReadyId = Guid.Parse("50000000-0000-0000-0000-000000000005");
    private static readonly Guid OrderPendingFarmerId = Guid.Parse("50000000-0000-0000-0000-000000000006");

    public sealed record SeedResult(
        int UsersCreated,
        int ProfilesCreated,
        int ProductsCreated,
        int OrdersCreated,
        int DeliveryRunsCreated);

    public static async Task<SeedResult> SeedAsync(
        AppDbContext db,
        AuthBootstrapOptions bootstrapOptions,
        SeedOptions seedOptions,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var usersCreated = 0;
        var profilesCreated = 0;
        var productsCreated = 0;
        var ordersCreated = 0;
        var runsCreated = 0;

        // 1. Seed canonical bootstrap accounts + any extra phones configured in AuthBootstrapOptions
        var userByPhone = await db.Users
            .Include(u => u.BusinessProfile)
            .ToDictionaryAsync(u => u.PhoneNumber, StringComparer.Ordinal, cancellationToken);

        foreach (var account in DefaultBootstrapAccounts)
        {
            var phone = PhoneNumberNormalizer.Normalize(account.PhoneNumber);
            if (!userByPhone.TryGetValue(phone, out var user))
            {
                user = new User
                {
                    Id = account.Id,
                    PhoneNumber = phone,
                    FullName = account.FullName,
                    Role = account.Role,
                    IsVerified = account.IsVerified,
                    CreatedAt = now.AddDays(-14)
                };
                db.Users.Add(user);
                userByPhone[phone] = user;
                usersCreated++;
            }

            if (user.BusinessProfile is null)
            {
                var profile = new BusinessProfile
                {
                    UserId = user.Id,
                    BusinessName = account.BusinessName,
                    Region = account.Region,
                    Address = account.Address,
                    Latitude = account.Latitude,
                    Longitude = account.Longitude,
                    PreferredDeliveryWindow = account.PreferredDeliveryWindow,
                    TaxId = account.TaxId
                };
                db.BusinessProfiles.Add(profile);
                user.BusinessProfile = profile;
                profilesCreated++;
            }
        }

        // Also ensure any extra custom bootstrap admin phones from config exist
        foreach (var rawAdminPhone in bootstrapOptions.BootstrapAdminPhones)
        {
            var phone = PhoneNumberNormalizer.Normalize(rawAdminPhone);
            if (string.IsNullOrWhiteSpace(phone) || !PhoneNumberNormalizer.IsValidE164(phone))
            {
                continue;
            }

            if (!userByPhone.TryGetValue(phone, out var adminUser))
            {
                adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    PhoneNumber = phone,
                    FullName = "Platform Admin",
                    Role = UserRole.Admin,
                    IsVerified = true,
                    CreatedAt = now
                };
                db.Users.Add(adminUser);
                userByPhone[phone] = adminUser;
                usersCreated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (!seedOptions.SeedSampleData)
        {
            logger.LogInformation(
                "Bootstrap accounts verified ({UsersCreated} new users, {ProfilesCreated} new profiles). Sample data seeding disabled.",
                usersCreated,
                profilesCreated);
            return new SeedResult(usersCreated, profilesCreated, 0, 0, 0);
        }

        var farmerGuba = userByPhone["+994501111111"];
        var farmerLankaran = userByPhone["+994501111112"];
        var farmerZira = userByPhone["+994501111113"];
        var restCaspian = userByPhone["+994502222221"];
        var restIcherisheher = userByPhone["+994502222222"];

        // 2. Seed sample products with volume price tiers & JSONB metadata
        var existingProductIds = await db.Products
            .Select(p => p.Id)
            .ToHashSetAsync(cancellationToken);

        var sampleProducts = BuildSampleProducts(farmerZira.Id, farmerGuba.Id, farmerLankaran.Id, today, now);
        foreach (var product in sampleProducts)
        {
            if (!existingProductIds.Contains(product.Id))
            {
                db.Products.Add(product);
                productsCreated++;
            }
        }

        if (productsCreated > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        // 3. Seed sample DeliveryRuns, CheckoutGroups, Orders & OrderItems
        var existingRunIds = await db.DeliveryRuns
            .Select(r => r.Id)
            .ToHashSetAsync(cancellationToken);

        if (!existingRunIds.Contains(RunCompletedId))
        {
            db.DeliveryRuns.Add(new DeliveryRun
            {
                Id = RunCompletedId,
                DriverName = "Elchin Mustafayev",
                DriverPhone = "+994503333331",
                VehicleNote = "Mercedes Sprinter Refrigerated #90-AM-401",
                RunDate = today.AddDays(-1),
                Status = DeliveryRunStatus.Completed,
                CreatedAt = now.AddDays(-1).AddHours(-4)
            });
            runsCreated++;
        }

        if (!existingRunIds.Contains(RunInTransitId))
        {
            db.DeliveryRuns.Add(new DeliveryRun
            {
                Id = RunInTransitId,
                DriverName = "Elchin Mustafayev",
                DriverPhone = "+994503333331",
                VehicleNote = "Isuzu Cold-Chain Reefer #99-AG-102",
                RunDate = today,
                Status = DeliveryRunStatus.InTransit,
                CreatedAt = now.AddHours(-3)
            });
            runsCreated++;
        }

        if (runsCreated > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        var existingGroupIds = await db.CheckoutGroups
            .Select(cg => cg.Id)
            .ToHashSetAsync(cancellationToken);

        // Checkout Group 1: Yesterday's completed multi-farmer order for Caspian Bistro & Grill
        if (!existingGroupIds.Contains(CheckoutGroupCompletedId))
        {
            var cg1CreatedAt = now.AddDays(-2);
            var cg1 = new CheckoutGroup
            {
                Id = CheckoutGroupCompletedId,
                RestaurantId = restCaspian.Id,
                DeliveryAddress = "Nizami St 42, Sabail, Baku",
                ScheduledDeliveryDate = today.AddDays(-1),
                CutoffAt = cg1CreatedAt.AddHours(4),
                TotalDeliveryFee = 10.00m,
                CreatedAt = cg1CreatedAt,
                Orders =
                [
                    new Order
                    {
                        Id = OrderDeliveredPaidId,
                        CheckoutGroupId = CheckoutGroupCompletedId,
                        RestaurantId = restCaspian.Id,
                        FarmerId = farmerZira.Id,
                        DeliveryRunId = RunCompletedId,
                        FulfillmentType = FulfillmentType.PlatformCourier,
                        Status = OrderStatus.Delivered,
                        // Ordered 50 kg @ $1.85/kg = $92.50; delivered 49.50 kg @ $1.85/kg = $91.58 + $4.58 fee + $5.00 delivery = $101.16
                        EstimatedTotalAmount = 92.50m,
                        PlatformFeeAmount = 4.58m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 101.16m,
                        PaymentMethod = PaymentMethod.CashOnDelivery,
                        PaymentStatus = PaymentStatus.Paid,
                        ExpiresAt = cg1CreatedAt.AddHours(3),
                        ConfirmedAt = cg1CreatedAt.AddMinutes(25),
                        DeliveredAt = now.AddDays(-1).AddHours(2),
                        CreatedAt = cg1CreatedAt,
                        Items =
                        [
                            new OrderItem
                            {
                                Id = Guid.Parse("60000000-0000-0000-0000-000000000001"),
                                OrderId = OrderDeliveredPaidId,
                                ProductId = ProductZiraPinkTomatoId,
                                ProductTitleSnapshot = "Zira Pink Beefsteak Tomatoes",
                                UnitPriceApplied = 1.85m,
                                EstimatedWeightKg = 50.00m,
                                ActualDeliveredWeightKg = 49.50m,
                                RejectedWeightKg = 0.00m,
                                ProofPhotoUrl = "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=800&q=80"
                            }
                        ]
                    },
                    new Order
                    {
                        Id = OrderDeliveredInvoiceId,
                        CheckoutGroupId = CheckoutGroupCompletedId,
                        RestaurantId = restCaspian.Id,
                        FarmerId = farmerLankaran.Id,
                        DeliveryRunId = RunCompletedId,
                        FulfillmentType = FulfillmentType.PlatformCourier,
                        Status = OrderStatus.Delivered,
                        // Ordered 30 kg @ $2.25/kg = $67.50 + $3.38 fee + $5.00 delivery = $75.88
                        EstimatedTotalAmount = 67.50m,
                        PlatformFeeAmount = 3.38m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 75.88m,
                        PaymentMethod = PaymentMethod.WeeklyInvoice,
                        PaymentStatus = PaymentStatus.Unpaid,
                        ExpiresAt = cg1CreatedAt.AddHours(3),
                        ConfirmedAt = cg1CreatedAt.AddMinutes(40),
                        DeliveredAt = now.AddDays(-1).AddHours(2),
                        CreatedAt = cg1CreatedAt,
                        Items =
                        [
                            new OrderItem
                            {
                                Id = Guid.Parse("60000000-0000-0000-0000-000000000002"),
                                OrderId = OrderDeliveredInvoiceId,
                                ProductId = ProductLankaranMandarinId,
                                ProductTitleSnapshot = "Lankaran Satsuma Mandarins",
                                UnitPriceApplied = 2.25m,
                                EstimatedWeightKg = 30.00m,
                                ActualDeliveredWeightKg = 30.00m,
                                RejectedWeightKg = 0.00m
                            }
                        ]
                    }
                ]
            };

            db.CheckoutGroups.Add(cg1);
            ordersCreated += 2;
        }

        // Checkout Group 2: Today's active Milk-Run for Icherisheher Courtyard Kitchen (InTransit + DeliveredPendingApproval)
        if (!existingGroupIds.Contains(CheckoutGroupInTransitId))
        {
            var cg2CreatedAt = now.AddHours(-16);
            var cg2 = new CheckoutGroup
            {
                Id = CheckoutGroupInTransitId,
                RestaurantId = restIcherisheher.Id,
                DeliveryAddress = "Boyuk Qala St 18, Icherisheher, Baku",
                ScheduledDeliveryDate = today,
                CutoffAt = cg2CreatedAt.AddHours(3),
                TotalDeliveryFee = 10.00m,
                CreatedAt = cg2CreatedAt,
                Orders =
                [
                    new Order
                    {
                        Id = OrderInTransitId,
                        CheckoutGroupId = CheckoutGroupInTransitId,
                        RestaurantId = restIcherisheher.Id,
                        FarmerId = farmerZira.Id,
                        DeliveryRunId = RunInTransitId,
                        FulfillmentType = FulfillmentType.PlatformCourier,
                        Status = OrderStatus.InTransit,
                        // 10 kg Herbs @ $3.90 ($39.00) + 25 kg Cucumbers @ $1.65 ($41.25) = $80.25 + $4.01 fee + $5.00 delivery = $89.26
                        EstimatedTotalAmount = 80.25m,
                        PlatformFeeAmount = 4.01m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 89.26m,
                        PaymentMethod = PaymentMethod.CashOnDelivery,
                        PaymentStatus = PaymentStatus.Unpaid,
                        ExpiresAt = cg2CreatedAt.AddHours(3),
                        ConfirmedAt = cg2CreatedAt.AddMinutes(30),
                        CreatedAt = cg2CreatedAt,
                        Items =
                        [
                            new OrderItem
                            {
                                Id = Guid.Parse("60000000-0000-0000-0000-000000000003"),
                                OrderId = OrderInTransitId,
                                ProductId = ProductCulinaryHerbsId,
                                ProductTitleSnapshot = "Absheron Fresh Culinary Herbs Bundle",
                                UnitPriceApplied = 3.90m,
                                EstimatedWeightKg = 10.00m
                            },
                            new OrderItem
                            {
                                Id = Guid.Parse("60000000-0000-0000-0000-000000000004"),
                                OrderId = OrderInTransitId,
                                ProductId = ProductBabyCucumberId,
                                ProductTitleSnapshot = "Zira Crispy Baby Cucumbers",
                                UnitPriceApplied = 1.65m,
                                EstimatedWeightKg = 25.00m
                            }
                        ]
                    },
                    new Order
                    {
                        Id = OrderPendingApprovalId,
                        CheckoutGroupId = CheckoutGroupInTransitId,
                        RestaurantId = restIcherisheher.Id,
                        FarmerId = farmerGuba.Id,
                        DeliveryRunId = RunInTransitId,
                        FulfillmentType = FulfillmentType.PlatformCourier,
                        Status = OrderStatus.DeliveredPendingApproval,
                        // Ordered 50 kg @ $1.50 = $75.00; delivered 56 kg with 3 kg rejected -> 53 chargeable kg @ $1.50 = $79.50 + $3.98 fee + $5.00 delivery = $88.48
                        EstimatedTotalAmount = 75.00m,
                        PlatformFeeAmount = 3.98m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 88.48m,
                        PaymentMethod = PaymentMethod.WeeklyInvoice,
                        PaymentStatus = PaymentStatus.Unpaid,
                        ExpiresAt = cg2CreatedAt.AddHours(3),
                        ConfirmedAt = cg2CreatedAt.AddMinutes(20),
                        CreatedAt = cg2CreatedAt,
                        Items =
                        [
                            new OrderItem
                            {
                                Id = Guid.Parse("60000000-0000-0000-0000-000000000005"),
                                OrderId = OrderPendingApprovalId,
                                ProductId = ProductGubaGoldenAppleId,
                                ProductTitleSnapshot = "Guba Golden Delicious Apples",
                                UnitPriceApplied = 1.50m,
                                EstimatedWeightKg = 50.00m,
                                ActualDeliveredWeightKg = 56.00m,
                                RejectedWeightKg = 3.00m,
                                ProofPhotoUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?auto=format&fit=crop&w=800&q=80"
                            }
                        ]
                    }
                ]
            };

            db.CheckoutGroups.Add(cg2);
            ordersCreated += 2;
        }

        // Checkout Group 3: Tomorrow's upcoming orders for Caspian Bistro & Grill (1 Confirmed ready for dispatch, 1 Pending farmer response)
        if (!existingGroupIds.Contains(CheckoutGroupUpcomingId))
        {
            var cg3CreatedAt = now.AddMinutes(-35);
            var cg3 = new CheckoutGroup
            {
                Id = CheckoutGroupUpcomingId,
                RestaurantId = restCaspian.Id,
                DeliveryAddress = "Nizami St 42, Sabail, Baku",
                ScheduledDeliveryDate = today.AddDays(1),
                CutoffAt = now.AddHours(6),
                TotalDeliveryFee = 10.00m,
                CreatedAt = cg3CreatedAt,
                Orders =
                [
                    new Order
                    {
                        Id = OrderConfirmedReadyId,
                        CheckoutGroupId = CheckoutGroupUpcomingId,
                        RestaurantId = restCaspian.Id,
                        FarmerId = farmerGuba.Id,
                        DeliveryRunId = null,
                        FulfillmentType = FulfillmentType.PlatformCourier,
                        Status = OrderStatus.Confirmed,
                        // 60 kg Potatoes @ $0.82 ($49.20) + 50 kg Honeycrisp Apples @ $1.95 ($97.50) = $146.70 + $7.34 fee + $5.00 delivery = $159.04
                        EstimatedTotalAmount = 146.70m,
                        PlatformFeeAmount = 7.34m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 159.04m,
                        PaymentMethod = PaymentMethod.BankTransfer,
                        PaymentStatus = PaymentStatus.Unpaid,
                        ExpiresAt = now.AddHours(2.5),
                        ConfirmedAt = now.AddMinutes(-15),
                        CreatedAt = cg3CreatedAt,
                        Items =
                        [
                            new OrderItem
                            {
                                Id = Guid.Parse("60000000-0000-0000-0000-000000000006"),
                                OrderId = OrderConfirmedReadyId,
                                ProductId = ProductKhachmazPotatoId,
                                ProductTitleSnapshot = "Khachmaz Agria Roasting Potatoes",
                                UnitPriceApplied = 0.82m,
                                EstimatedWeightKg = 60.00m
                            },
                            new OrderItem
                            {
                                Id = Guid.Parse("60000000-0000-0000-0000-000000000007"),
                                OrderId = OrderConfirmedReadyId,
                                ProductId = ProductGubaHoneycrispId,
                                ProductTitleSnapshot = "Guba Honeycrisp Orchard Apples",
                                UnitPriceApplied = 1.95m,
                                EstimatedWeightKg = 50.00m
                            }
                        ]
                    },
                    new Order
                    {
                        Id = OrderPendingFarmerId,
                        CheckoutGroupId = CheckoutGroupUpcomingId,
                        RestaurantId = restCaspian.Id,
                        FarmerId = farmerLankaran.Id,
                        DeliveryRunId = null,
                        FulfillmentType = FulfillmentType.PlatformCourier,
                        Status = OrderStatus.Pending,
                        // 25 kg Meyer Lemons @ $2.75 ($68.75) + $3.44 fee + $5.00 delivery = $77.19
                        EstimatedTotalAmount = 68.75m,
                        PlatformFeeAmount = 3.44m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 77.19m,
                        PaymentMethod = PaymentMethod.BankTransfer,
                        PaymentStatus = PaymentStatus.Unpaid,
                        ExpiresAt = now.AddHours(2.5),
                        CreatedAt = cg3CreatedAt,
                        Items =
                        [
                            new OrderItem
                            {
                                Id = Guid.Parse("60000000-0000-0000-0000-000000000008"),
                                OrderId = OrderPendingFarmerId,
                                ProductId = ProductAstaraLemonId,
                                ProductTitleSnapshot = "Astara Aromatic Meyer Lemons",
                                UnitPriceApplied = 2.75m,
                                EstimatedWeightKg = 25.00m
                            }
                        ]
                    }
                ]
            };

            db.CheckoutGroups.Add(cg3);
            ordersCreated += 2;
        }

        if (ordersCreated > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Database seed completed: {UsersCreated} users, {ProfilesCreated} profiles, {ProductsCreated} products, {OrdersCreated} orders, {RunsCreated} delivery runs.",
            usersCreated,
            profilesCreated,
            productsCreated,
            ordersCreated,
            runsCreated);

        return new SeedResult(usersCreated, profilesCreated, productsCreated, ordersCreated, runsCreated);
    }

    private static IReadOnlyList<Product> BuildSampleProducts(
        Guid farmerZiraId,
        Guid farmerGubaId,
        Guid farmerLankaranId,
        DateOnly today,
        DateTimeOffset now) =>
    [
        new Product
        {
            Id = ProductZiraPinkTomatoId,
            FarmerId = farmerZiraId,
            Category = "Tomatoes",
            Title = "Zira Pink Beefsteak Tomatoes",
            Description = "Vine-ripened Absheron pink beefsteak tomatoes harvested at peak Brix sweetness for salads and grill.",
            AvailableStockKg = 450.00m,
            MinOrderKg = 10.00m,
            BasePricePerKg = 2.40m,
            Metadata = new ProductMetadata
            {
                Variety = "Zira Pink Heirloom",
                HarvestDate = today,
                Grade = "Grade A (Extra)",
                Packaging = "10 kg wooden crate",
                IsOrganic = true
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-2),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000001"),
                    ProductId = ProductZiraPinkTomatoId,
                    MinQuantityKg = 25.00m,
                    PricePerKg = 2.10m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000002"),
                    ProductId = ProductZiraPinkTomatoId,
                    MinQuantityKg = 50.00m,
                    PricePerKg = 1.85m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000003"),
                    ProductId = ProductZiraPinkTomatoId,
                    MinQuantityKg = 100.00m,
                    PricePerKg = 1.65m
                }
            ]
        },
        new Product
        {
            Id = ProductCherryTomatoId,
            FarmerId = farmerZiraId,
            Category = "Tomatoes",
            Title = "Sweet Cluster Cherry Tomatoes",
            Description = "Hothouse cherry tomatoes on the vine with vibrant red skin and balanced acidity for fine dining plating.",
            AvailableStockKg = 180.00m,
            MinOrderKg = 5.00m,
            BasePricePerKg = 3.20m,
            Metadata = new ProductMetadata
            {
                Variety = "Piccolo Truss",
                HarvestDate = today,
                Grade = "Grade A",
                Packaging = "5 kg ventilated carton",
                IsOrganic = false
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1546094096-0df4bcaaa337?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-3),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000004"),
                    ProductId = ProductCherryTomatoId,
                    MinQuantityKg = 20.00m,
                    PricePerKg = 2.85m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000005"),
                    ProductId = ProductCherryTomatoId,
                    MinQuantityKg = 50.00m,
                    PricePerKg = 2.50m
                }
            ]
        },
        new Product
        {
            Id = ProductCulinaryHerbsId,
            FarmerId = farmerZiraId,
            Category = "Greens",
            Title = "Absheron Fresh Culinary Herbs Bundle",
            Description = "Morning-cut coriander, dill, purple reyhan (basil), and tarragon washed and hydro-cooled for kitchen prep.",
            AvailableStockKg = 120.00m,
            MinOrderKg = 3.00m,
            BasePricePerKg = 4.50m,
            Metadata = new ProductMetadata
            {
                Variety = "Mixed Aromatic Greens",
                HarvestDate = today,
                Grade = "Grade A (Extra)",
                Packaging = "3 kg chilled crate",
                IsOrganic = true
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-1),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000006"),
                    ProductId = ProductCulinaryHerbsId,
                    MinQuantityKg = 10.00m,
                    PricePerKg = 3.90m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000007"),
                    ProductId = ProductCulinaryHerbsId,
                    MinQuantityKg = 25.00m,
                    PricePerKg = 3.40m
                }
            ]
        },
        new Product
        {
            Id = ProductBabyCucumberId,
            FarmerId = farmerZiraId,
            Category = "Vegetables",
            Title = "Zira Crispy Baby Cucumbers",
            Description = "Crunchy, thin-skinned greenhouse cucumbers ideal for fresh table mezzes and pickling.",
            AvailableStockKg = 320.00m,
            MinOrderKg = 8.00m,
            BasePricePerKg = 1.90m,
            Metadata = new ProductMetadata
            {
                Variety = "Melita Prickly",
                HarvestDate = today,
                Grade = "Grade A",
                Packaging = "8 kg plastic crate",
                IsOrganic = false
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1449300079323-02e209d9d3a6?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-4),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000008"),
                    ProductId = ProductBabyCucumberId,
                    MinQuantityKg = 25.00m,
                    PricePerKg = 1.65m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000009"),
                    ProductId = ProductBabyCucumberId,
                    MinQuantityKg = 60.00m,
                    PricePerKg = 1.45m
                }
            ]
        },
        new Product
        {
            Id = ProductGubaGoldenAppleId,
            FarmerId = farmerGubaId,
            Category = "Fruits",
            Title = "Guba Golden Delicious Apples",
            Description = "High-altitude Guba orchard apples with crisp texture and natural honey aroma.",
            AvailableStockKg = 600.00m,
            MinOrderKg = 15.00m,
            BasePricePerKg = 1.80m,
            Metadata = new ProductMetadata
            {
                Variety = "Guba Golden",
                HarvestDate = today.AddDays(-2),
                Grade = "Grade A",
                Packaging = "15 kg wooden crate",
                IsOrganic = true
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-5),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000010"),
                    ProductId = ProductGubaGoldenAppleId,
                    MinQuantityKg = 50.00m,
                    PricePerKg = 1.50m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000011"),
                    ProductId = ProductGubaGoldenAppleId,
                    MinQuantityKg = 120.00m,
                    PricePerKg = 1.30m
                }
            ]
        },
        new Product
        {
            Id = ProductGubaHoneycrispId,
            FarmerId = farmerGubaId,
            Category = "Fruits",
            Title = "Guba Honeycrisp Orchard Apples",
            Description = "Premium bi-color red and gold apples suited for pastry, charcuterie boards, and fresh pressing.",
            AvailableStockKg = 380.00m,
            MinOrderKg = 10.00m,
            BasePricePerKg = 2.25m,
            Metadata = new ProductMetadata
            {
                Variety = "Honeycrisp",
                HarvestDate = today.AddDays(-1),
                Grade = "Grade A (Extra)",
                Packaging = "12 kg layered carton",
                IsOrganic = true
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1567306226416-28f0efdc88ce?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-6),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000012"),
                    ProductId = ProductGubaHoneycrispId,
                    MinQuantityKg = 40.00m,
                    PricePerKg = 1.95m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000013"),
                    ProductId = ProductGubaHoneycrispId,
                    MinQuantityKg = 100.00m,
                    PricePerKg = 1.75m
                }
            ]
        },
        new Product
        {
            Id = ProductKhachmazPotatoId,
            FarmerId = farmerGubaId,
            Category = "Root Vegetables",
            Title = "Khachmaz Agria Roasting Potatoes",
            Description = "High-starch yellow-flesh potatoes brushed and graded for French fries, roasting, and mash.",
            AvailableStockKg = 950.00m,
            MinOrderKg = 25.00m,
            BasePricePerKg = 0.95m,
            Metadata = new ProductMetadata
            {
                Variety = "Agria Gold",
                HarvestDate = today.AddDays(-3),
                Grade = "Grade A (55-75mm)",
                Packaging = "25 kg breathable mesh sack",
                IsOrganic = false
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1518977676601-b53f82aba655?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-7),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000014"),
                    ProductId = ProductKhachmazPotatoId,
                    MinQuantityKg = 50.00m,
                    PricePerKg = 0.82m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000015"),
                    ProductId = ProductKhachmazPotatoId,
                    MinQuantityKg = 150.00m,
                    PricePerKg = 0.70m
                }
            ]
        },
        new Product
        {
            Id = ProductLankaranMandarinId,
            FarmerId = farmerLankaranId,
            Category = "Citrus",
            Title = "Lankaran Satsuma Mandarins",
            Description = "Seedless, easy-peel subtropical mandarins from Lankaran groves with intense citrus aroma.",
            AvailableStockKg = 420.00m,
            MinOrderKg = 10.00m,
            BasePricePerKg = 2.60m,
            Metadata = new ProductMetadata
            {
                Variety = "Lankaran Satsuma Unshiu",
                HarvestDate = today.AddDays(-1),
                Grade = "Grade A (Sweet)",
                Packaging = "12 kg wooden crate",
                IsOrganic = true
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1547514701-42782101795e?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-3),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000016"),
                    ProductId = ProductLankaranMandarinId,
                    MinQuantityKg = 30.00m,
                    PricePerKg = 2.25m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000017"),
                    ProductId = ProductLankaranMandarinId,
                    MinQuantityKg = 80.00m,
                    PricePerKg = 1.95m
                }
            ]
        },
        new Product
        {
            Id = ProductAstaraLemonId,
            FarmerId = farmerLankaranId,
            Category = "Citrus",
            Title = "Astara Aromatic Meyer Lemons",
            Description = "Thin-rinded, deep yellow Meyer lemons prized for seafood dressing, tea service, and bar craft.",
            AvailableStockKg = 260.00m,
            MinOrderKg = 5.00m,
            BasePricePerKg = 3.10m,
            Metadata = new ProductMetadata
            {
                Variety = "Astara Meyer",
                HarvestDate = today.AddDays(-1),
                Grade = "Grade A (Extra)",
                Packaging = "10 kg carton",
                IsOrganic = true
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1590502593747-42a996133562?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-4),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000018"),
                    ProductId = ProductAstaraLemonId,
                    MinQuantityKg = 25.00m,
                    PricePerKg = 2.75m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000019"),
                    ProductId = ProductAstaraLemonId,
                    MinQuantityKg = 60.00m,
                    PricePerKg = 2.45m
                }
            ]
        }
    ];
}

public sealed class DatabaseSeedHostedService(
    IServiceProvider serviceProvider,
    IOptions<SeedOptions> seedOptions,
    IOptions<AuthBootstrapOptions> bootstrapOptions,
    ILogger<DatabaseSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!seedOptions.Value.Enabled)
        {
            logger.LogInformation("Database seeding is disabled via configuration (Seed:Enabled=false).");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DatabaseSeeder.SeedAsync(
            db,
            bootstrapOptions.Value,
            seedOptions.Value,
            logger,
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
