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
    public bool AutoMigrateOnStartup { get; set; } = true;
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
    public static readonly Guid ProductGoychayPomegranateId = Guid.Parse("20000000-0000-0000-0000-000000000010");
    public static readonly Guid ProductBardaOnionId = Guid.Parse("20000000-0000-0000-0000-000000000011");
    public static readonly Guid ProductZiraEggplantId = Guid.Parse("20000000-0000-0000-0000-000000000012");

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

        // 2. Seed or synchronize sample products with real farm-gate volume price tiers & JSONB metadata
        var sampleProducts = BuildSampleProducts(farmerZira.Id, farmerGuba.Id, farmerLankaran.Id, today, now);
        var sampleIds = sampleProducts.Select(p => p.Id).ToList();
        var existingSampleProducts = await db.Products
            .Include(p => p.PriceTiers)
            .Where(p => sampleIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var productsUpdated = false;
        foreach (var product in sampleProducts)
        {
            if (!existingSampleProducts.TryGetValue(product.Id, out var existing))
            {
                db.Products.Add(product);
                productsCreated++;
            }
            else if (existing.BasePricePerKg != product.BasePricePerKg || existing.MinOrderKg != product.MinOrderKg)
            {
                existing.Category = product.Category;
                existing.Title = product.Title;
                existing.Description = product.Description;
                existing.MinOrderKg = product.MinOrderKg;
                existing.BasePricePerKg = product.BasePricePerKg;
                existing.Metadata = product.Metadata;

                db.PriceTiers.RemoveRange(existing.PriceTiers);
                existing.PriceTiers.Clear();
                foreach (var tier in product.PriceTiers)
                {
                    existing.PriceTiers.Add(new PriceTier
                    {
                        Id = tier.Id,
                        ProductId = existing.Id,
                        MinQuantityKg = tier.MinQuantityKg,
                        PricePerKg = tier.PricePerKg
                    });
                }

                productsUpdated = true;
            }
        }

        if (productsCreated > 0 || productsUpdated)
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

        // If previously seeded orders still carry legacy retail unit prices, remove and re-seed them with farm-gate prices
        var legacySeedOrder = await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == OrderDeliveredPaidId && o.EstimatedTotalAmount > 40.00m, cancellationToken);
        if (legacySeedOrder is not null)
        {
            var seedGroupIds = new[] { CheckoutGroupCompletedId, CheckoutGroupInTransitId, CheckoutGroupUpcomingId };
            var oldGroups = await db.CheckoutGroups
                .Include(cg => cg.Orders)
                    .ThenInclude(o => o.Items)
                .Where(cg => seedGroupIds.Contains(cg.Id))
                .ToListAsync(cancellationToken);
            db.CheckoutGroups.RemoveRange(oldGroups);
            await db.SaveChangesAsync(cancellationToken);
            existingGroupIds.ExceptWith(seedGroupIds);
        }

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
                        // Farm-gate: 50 kg @ $0.44/kg (0.75 AZN/kg) = $22.00; delivered 49.50 kg @ $0.44 = $21.78 + $1.09 fee + $5.00 delivery = $27.87
                        EstimatedTotalAmount = 22.00m,
                        PlatformFeeAmount = 1.09m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 27.87m,
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
                                UnitPriceApplied = 0.44m,
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
                        // Farm-gate: 60 kg @ $0.40/kg (0.68 AZN/kg) = $24.00 + $1.20 fee + $5.00 delivery = $30.20
                        EstimatedTotalAmount = 24.00m,
                        PlatformFeeAmount = 1.20m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 30.20m,
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
                                UnitPriceApplied = 0.40m,
                                EstimatedWeightKg = 60.00m,
                                ActualDeliveredWeightKg = 60.00m,
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
                        // Farm-gate: 15 kg Herbs @ $0.65 ($9.75) + 50 kg Cucumbers @ $0.29 ($14.50) = $24.25 + $1.21 fee + $5.00 delivery = $30.46
                        EstimatedTotalAmount = 24.25m,
                        PlatformFeeAmount = 1.21m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 30.46m,
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
                                UnitPriceApplied = 0.65m,
                                EstimatedWeightKg = 15.00m
                            },
                            new OrderItem
                            {
                                Id = Guid.Parse("60000000-0000-0000-0000-000000000004"),
                                OrderId = OrderInTransitId,
                                ProductId = ProductBabyCucumberId,
                                ProductTitleSnapshot = "Zira Crispy Baby Cucumbers",
                                UnitPriceApplied = 0.29m,
                                EstimatedWeightKg = 50.00m
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
                        // Farm-gate: Ordered 80 kg @ $0.32 (0.54 AZN/kg) = $25.60; delivered 90 kg with 4 kg rejected -> 86 chargeable kg @ $0.32 = $27.52 + $1.38 fee + $5.00 delivery = $33.90
                        EstimatedTotalAmount = 25.60m,
                        PlatformFeeAmount = 1.38m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 33.90m,
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
                                UnitPriceApplied = 0.32m,
                                EstimatedWeightKg = 80.00m,
                                ActualDeliveredWeightKg = 90.00m,
                                RejectedWeightKg = 4.00m,
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
                        // Farm-gate: 100 kg Potatoes @ $0.20 ($20.00) + 60 kg Honeycrisp Apples @ $0.44 ($26.40) = $46.40 + $2.32 fee + $5.00 delivery = $53.72
                        EstimatedTotalAmount = 46.40m,
                        PlatformFeeAmount = 2.32m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 53.72m,
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
                                UnitPriceApplied = 0.20m,
                                EstimatedWeightKg = 100.00m
                            },
                            new OrderItem
                            {
                                Id = Guid.Parse("60000000-0000-0000-0000-000000000007"),
                                OrderId = OrderConfirmedReadyId,
                                ProductId = ProductGubaHoneycrispId,
                                ProductTitleSnapshot = "Guba Honeycrisp Orchard Apples",
                                UnitPriceApplied = 0.44m,
                                EstimatedWeightKg = 60.00m
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
                        // Farm-gate: 40 kg Meyer Lemons @ $0.59 (1.00 AZN/kg) = $23.60 + $1.18 fee + $5.00 delivery = $29.78
                        EstimatedTotalAmount = 23.60m,
                        PlatformFeeAmount = 1.18m,
                        DeliveryFeeAmount = 5.00m,
                        FinalTotalAmount = 29.78m,
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
                                UnitPriceApplied = 0.59m,
                                EstimatedWeightKg = 40.00m
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
            Description = "Direct greenhouse-gate wholesale price (first-hand farmer rate, no middleman markup). Vine-ripened Absheron pink beefsteak tomatoes harvested at dawn.",
            AvailableStockKg = 650.00m,
            MinOrderKg = 15.00m,
            // 0.85 AZN/kg ($0.50/kg) farm-gate price
            BasePricePerKg = 0.50m,
            Metadata = new ProductMetadata
            {
                Variety = "Zira Pink Heirloom",
                HarvestDate = today,
                Grade = "Grade A (Extra)",
                Packaging = "15 kg wooden crate",
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
                    MinQuantityKg = 50.00m,
                    // 0.75 AZN/kg
                    PricePerKg = 0.44m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000002"),
                    ProductId = ProductZiraPinkTomatoId,
                    MinQuantityKg = 150.00m,
                    // 0.65 AZN/kg
                    PricePerKg = 0.38m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000003"),
                    ProductId = ProductZiraPinkTomatoId,
                    MinQuantityKg = 300.00m,
                    // 0.58 AZN/kg
                    PricePerKg = 0.34m
                }
            ]
        },
        new Product
        {
            Id = ProductCherryTomatoId,
            FarmerId = farmerZiraId,
            Category = "Tomatoes",
            Title = "Sweet Cluster Cherry Tomatoes",
            Description = "First-hand greenhouse producer rate. Cluster cherry tomatoes on the vine with vibrant red skin and balanced acidity for HoReCa kitchens.",
            AvailableStockKg = 240.00m,
            MinOrderKg = 10.00m,
            // 1.19 AZN/kg ($0.70/kg) farm-gate price
            BasePricePerKg = 0.70m,
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
                    MinQuantityKg = 30.00m,
                    // 1.05 AZN/kg
                    PricePerKg = 0.62m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000005"),
                    ProductId = ProductCherryTomatoId,
                    MinQuantityKg = 80.00m,
                    // 0.92 AZN/kg
                    PricePerKg = 0.54m
                }
            ]
        },
        new Product
        {
            Id = ProductCulinaryHerbsId,
            FarmerId = farmerZiraId,
            Category = "Greens",
            Title = "Absheron Fresh Culinary Herbs Bundle",
            Description = "Field-gate wholesale bundle rate (~0.13 AZN/bundle equivalent). Morning-cut coriander, dill, purple reyhan, and tarragon hydro-cooled at the farm.",
            AvailableStockKg = 180.00m,
            MinOrderKg = 5.00m,
            // 1.29 AZN/kg ($0.76/kg) farm-gate price
            BasePricePerKg = 0.76m,
            Metadata = new ProductMetadata
            {
                Variety = "Mixed Aromatic Greens",
                HarvestDate = today,
                Grade = "Grade A (Extra)",
                Packaging = "5 kg chilled crate",
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
                    MinQuantityKg = 15.00m,
                    // 1.11 AZN/kg
                    PricePerKg = 0.65m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000007"),
                    ProductId = ProductCulinaryHerbsId,
                    MinQuantityKg = 40.00m,
                    // 0.95 AZN/kg
                    PricePerKg = 0.56m
                }
            ]
        },
        new Product
        {
            Id = ProductBabyCucumberId,
            FarmerId = farmerZiraId,
            Category = "Cucumbers",
            Title = "Zira Crispy Baby Cucumbers",
            Description = "Direct farm-gate price before Meyveli wholesale markup. Crunchy, thin-skinned Melita greenhouse cucumbers picked same morning.",
            AvailableStockKg = 500.00m,
            MinOrderKg = 15.00m,
            // 0.60 AZN/kg ($0.35/kg) farm-gate price
            BasePricePerKg = 0.35m,
            Metadata = new ProductMetadata
            {
                Variety = "Melita Prickly",
                HarvestDate = today,
                Grade = "Grade A",
                Packaging = "15 kg plastic crate",
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
                    MinQuantityKg = 50.00m,
                    // 0.49 AZN/kg
                    PricePerKg = 0.29m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000009"),
                    ProductId = ProductBabyCucumberId,
                    MinQuantityKg = 120.00m,
                    // 0.43 AZN/kg
                    PricePerKg = 0.25m
                }
            ]
        },
        new Product
        {
            Id = ProductGubaGoldenAppleId,
            FarmerId = farmerGubaId,
            Category = "Fruits",
            Title = "Guba Golden Delicious Apples",
            Description = "Orchard-gate first-hand price in Guba (sold to middlemen at 0.48–0.65 AZN/kg). High-altitude Golden apples with crisp texture and honey aroma.",
            AvailableStockKg = 850.00m,
            MinOrderKg = 20.00m,
            // 0.65 AZN/kg ($0.38/kg) farm-gate price
            BasePricePerKg = 0.38m,
            Metadata = new ProductMetadata
            {
                Variety = "Guba Golden",
                HarvestDate = today.AddDays(-2),
                Grade = "Grade A (70mm+)",
                Packaging = "20 kg wooden crate",
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
                    MinQuantityKg = 80.00m,
                    // 0.54 AZN/kg
                    PricePerKg = 0.32m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000011"),
                    ProductId = ProductGubaGoldenAppleId,
                    MinQuantityKg = 200.00m,
                    // 0.48 AZN/kg
                    PricePerKg = 0.28m
                }
            ]
        },
        new Product
        {
            Id = ProductGubaHoneycrispId,
            FarmerId = farmerGubaId,
            Category = "Fruits",
            Title = "Guba Honeycrisp Orchard Apples",
            Description = "Direct Guba cold-storage orchard price. Premium bi-color red and gold apples suited for desserts, fresh juice pressing, and table service.",
            AvailableStockKg = 520.00m,
            MinOrderKg = 15.00m,
            // 0.85 AZN/kg ($0.50/kg) farm-gate price
            BasePricePerKg = 0.50m,
            Metadata = new ProductMetadata
            {
                Variety = "Honeycrisp / Fuji",
                HarvestDate = today.AddDays(-1),
                Grade = "Grade A (Extra)",
                Packaging = "15 kg layered carton",
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
                    MinQuantityKg = 60.00m,
                    // 0.75 AZN/kg
                    PricePerKg = 0.44m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000013"),
                    ProductId = ProductGubaHoneycrispId,
                    MinQuantityKg = 150.00m,
                    // 0.65 AZN/kg
                    PricePerKg = 0.38m
                }
            ]
        },
        new Product
        {
            Id = ProductKhachmazPotatoId,
            FarmerId = farmerGubaId,
            Category = "Potatoes",
            Title = "Khachmaz Agria Roasting Potatoes",
            Description = "First-hand field-gate sack price (0.29–0.41 AZN/kg at farm gate). High-starch yellow-flesh potatoes graded for fries, roasting, and mash.",
            AvailableStockKg = 1500.00m,
            MinOrderKg = 25.00m,
            // 0.41 AZN/kg ($0.24/kg) farm-gate price
            BasePricePerKg = 0.24m,
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
                    MinQuantityKg = 100.00m,
                    // 0.34 AZN/kg
                    PricePerKg = 0.20m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000015"),
                    ProductId = ProductKhachmazPotatoId,
                    MinQuantityKg = 300.00m,
                    // 0.29 AZN/kg
                    PricePerKg = 0.17m
                }
            ]
        },
        new Product
        {
            Id = ProductLankaranMandarinId,
            FarmerId = farmerLankaranId,
            Category = "Fruits",
            Title = "Lankaran Satsuma Mandarins",
            Description = "Direct Lankaran grove-gate price (sold to middleman trucks at 0.58–0.80 AZN/kg). Seedless, easy-peel subtropical mandarins.",
            AvailableStockKg = 680.00m,
            MinOrderKg = 15.00m,
            // 0.80 AZN/kg ($0.47/kg) farm-gate price
            BasePricePerKg = 0.47m,
            Metadata = new ProductMetadata
            {
                Variety = "Lankaran Satsuma Unshiu",
                HarvestDate = today.AddDays(-1),
                Grade = "Grade A (Sweet)",
                Packaging = "15 kg wooden crate",
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
                    MinQuantityKg = 60.00m,
                    // 0.68 AZN/kg
                    PricePerKg = 0.40m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000017"),
                    ProductId = ProductLankaranMandarinId,
                    MinQuantityKg = 150.00m,
                    // 0.58 AZN/kg
                    PricePerKg = 0.34m
                }
            ]
        },
        new Product
        {
            Id = ProductAstaraLemonId,
            FarmerId = farmerLankaranId,
            Category = "Fruits",
            Title = "Astara Aromatic Meyer Lemons",
            Description = "First-hand Astara citrus grove rate (0.88–1.16 AZN/kg). Thin-rinded, deep yellow Meyer lemons prized for seafood, tea service, and bar craft.",
            AvailableStockKg = 340.00m,
            MinOrderKg = 10.00m,
            // 1.16 AZN/kg ($0.68/kg) farm-gate price
            BasePricePerKg = 0.68m,
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
                    MinQuantityKg = 40.00m,
                    // 1.00 AZN/kg
                    PricePerKg = 0.59m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000019"),
                    ProductId = ProductAstaraLemonId,
                    MinQuantityKg = 100.00m,
                    // 0.88 AZN/kg
                    PricePerKg = 0.52m
                }
            ]
        },
        new Product
        {
            Id = ProductGoychayPomegranateId,
            FarmerId = farmerLankaranId,
            Category = "Fruits",
            Title = "Goychay Guloysha Pomegranates",
            Description = "Direct orchard-gate wholesale price (0.68–0.90 AZN/kg). Ruby-red, thin-skinned Guloysha pomegranates for fresh pressing, narsharab, and grill.",
            AvailableStockKg = 600.00m,
            MinOrderKg = 15.00m,
            // 0.90 AZN/kg ($0.53/kg) farm-gate price
            BasePricePerKg = 0.53m,
            Metadata = new ProductMetadata
            {
                Variety = "Guloysha Red",
                HarvestDate = today.AddDays(-1),
                Grade = "Grade A (350g+)",
                Packaging = "15 kg wooden crate",
                IsOrganic = true
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1541344999736-83eca272f6fc?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-2),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000020"),
                    ProductId = ProductGoychayPomegranateId,
                    MinQuantityKg = 50.00m,
                    // 0.78 AZN/kg
                    PricePerKg = 0.46m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000021"),
                    ProductId = ProductGoychayPomegranateId,
                    MinQuantityKg = 150.00m,
                    // 0.68 AZN/kg
                    PricePerKg = 0.40m
                }
            ]
        },
        new Product
        {
            Id = ProductBardaOnionId,
            FarmerId = farmerGubaId,
            Category = "Onions",
            Title = "Barda Golden & Red Cooking Onions",
            Description = "Field-gate sack price (0.24–0.32 AZN/kg paid to farmers by wholesale buyers). Sun-cured, firm cooking onions for stocks, kebabs, and grill.",
            AvailableStockKg = 1800.00m,
            MinOrderKg = 25.00m,
            // 0.32 AZN/kg ($0.19/kg) farm-gate price
            BasePricePerKg = 0.19m,
            Metadata = new ProductMetadata
            {
                Variety = "Barda Sweet Yellow",
                HarvestDate = today.AddDays(-4),
                Grade = "Grade A (50-70mm)",
                Packaging = "25 kg mesh sack",
                IsOrganic = false
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1508747703725-719777637510?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-5),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000022"),
                    ProductId = ProductBardaOnionId,
                    MinQuantityKg = 100.00m,
                    // 0.27 AZN/kg
                    PricePerKg = 0.16m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000023"),
                    ProductId = ProductBardaOnionId,
                    MinQuantityKg = 300.00m,
                    // 0.24 AZN/kg
                    PricePerKg = 0.14m
                }
            ]
        },
        new Product
        {
            Id = ProductZiraEggplantId,
            FarmerId = farmerZiraId,
            Category = "Tomatoes",
            Title = "Zira Glossy Purple Eggplants (Badımcan)",
            Description = "First-hand farm-gate price (0.39–0.54 AZN/kg). Seedless, thin-skinned purple eggplants ideal for mangal salad, dolma, and charcoal grilling.",
            AvailableStockKg = 410.00m,
            MinOrderKg = 15.00m,
            // 0.54 AZN/kg ($0.32/kg) farm-gate price
            BasePricePerKg = 0.32m,
            Metadata = new ProductMetadata
            {
                Variety = "Absheron Long",
                HarvestDate = today,
                Grade = "Grade A",
                Packaging = "15 kg carton crate",
                IsOrganic = true
            },
            ImageUrls =
            [
                "https://images.unsplash.com/photo-1615484477778-ca3b77940c25?auto=format&fit=crop&w=800&q=80"
            ],
            IsActive = true,
            UpdatedAt = now.AddHours(-2),
            PriceTiers =
            [
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000024"),
                    ProductId = ProductZiraEggplantId,
                    MinQuantityKg = 50.00m,
                    // 0.46 AZN/kg
                    PricePerKg = 0.27m
                },
                new PriceTier
                {
                    Id = Guid.Parse("21000000-0000-0000-0000-000000000025"),
                    ProductId = ProductZiraEggplantId,
                    MinQuantityKg = 120.00m,
                    // 0.39 AZN/kg
                    PricePerKg = 0.23m
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
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (seedOptions.Value.AutoMigrateOnStartup)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (!seedOptions.Value.Enabled)
        {
            logger.LogInformation("Database seeding is disabled via configuration (Seed:Enabled=false).");
            return;
        }

        await DatabaseSeeder.SeedAsync(
            db,
            bootstrapOptions.Value,
            seedOptions.Value,
            logger,
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
