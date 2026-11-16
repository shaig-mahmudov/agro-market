using System.Net;
using System.Net.Http.Json;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Features.Admin;
using AgroMarket.Api.Features.Auth;
using AgroMarket.Api.Features.Logistics;
using AgroMarket.Api.Features.Orders;
using AgroMarket.Api.Features.Products;

namespace AgroMarket.Tests;

public sealed class MarketplaceE2ETests : IClassFixture<PostgresWebAppFactory>
{
    private readonly PostgresWebAppFactory _factory;

    public MarketplaceE2ETests(PostgresWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullMilkRunLifecycle_CheckoutSplit_TelegramAccept_ReconcileVariance_AndSettle()
    {
        using var anonClient = _factory.CreateClient();

        // 1. Authenticate & Verify Admin, Farmer1, Farmer2, Restaurant, Driver
        var adminAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994500000000", "Platform Admin");
        using var adminClient = PostgresWebAppFactory.CreateAuthedClient(_factory, adminAuth.AccessToken);

        var f1Seed = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994507000001", "Farmer One", UserRole.Farmer);
        var f2Seed = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994507000002", "Farmer Two", UserRole.Farmer);
        var restSeed = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994507000003", "Caspian Bistro", UserRole.Restaurant);
        var driverSeed = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994507000004", "Elchin Driver", UserRole.Restaurant);

        await PostgresWebAppFactory.VerifyUserAsync(adminClient, f1Seed.User.Id);
        await PostgresWebAppFactory.VerifyUserAsync(adminClient, f2Seed.User.Id);
        await PostgresWebAppFactory.VerifyUserAsync(adminClient, restSeed.User.Id);
        await PostgresWebAppFactory.VerifyUserAsync(adminClient, driverSeed.User.Id, UserRole.Driver);

        var f1Auth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994507000001");
        var f2Auth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994507000002");
        var restAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994507000003");
        var driverAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994507000004");

        using var f1Client = PostgresWebAppFactory.CreateAuthedClient(_factory, f1Auth.AccessToken);
        using var f2Client = PostgresWebAppFactory.CreateAuthedClient(_factory, f2Auth.AccessToken);
        using var restClient = PostgresWebAppFactory.CreateAuthedClient(_factory, restAuth.AccessToken);
        using var driverClient = PostgresWebAppFactory.CreateAuthedClient(_factory, driverAuth.AccessToken);

        // 2. Farmers list tiered products
        var p1Resp = await f1Client.PostAsJsonAsync("/api/products", new CreateProduct.Request(
            Category: "Tomatoes",
            Title: "Guba Tomato",
            Description: null,
            AvailableStockKg: 100m,
            MinOrderKg: 5m,
            BasePricePerKg: 2.00m,
            PriceTiers: [new PriceTierInput(50m, 1.50m)]), PostgresWebAppFactory.JsonOptions);
        p1Resp.EnsureSuccessStatusCode();
        var p1 = (await p1Resp.Content.ReadFromJsonAsync<ProductDto>(PostgresWebAppFactory.JsonOptions))!;

        var p2Resp = await f2Client.PostAsJsonAsync("/api/products", new CreateProduct.Request(
            Category: "Greens",
            Title: "Dill Bundle",
            Description: null,
            AvailableStockKg: 40m,
            MinOrderKg: 2m,
            BasePricePerKg: 4.00m), PostgresWebAppFactory.JsonOptions);
        p2Resp.EnsureSuccessStatusCode();
        var p2 = (await p2Resp.Content.ReadFromJsonAsync<ProductDto>(PostgresWebAppFactory.JsonOptions))!;

        // 3. Link Farmer 1 Telegram account via deep link webhook
        var dlResp = await f1Client.GetFromJsonAsync<LinkTelegram.DeepLinkResponse>(
            "/api/auth/telegram-deep-link",
            PostgresWebAppFactory.JsonOptions);
        Assert.NotNull(dlResp);

        var startWebhook = await anonClient.PostAsJsonAsync("/api/webhooks/telegram", new
        {
            update_id = 5001,
            message = new
            {
                message_id = 10,
                chat = new { id = 777000111L },
                text = $"/start {dlResp.Token}"
            }
        });
        startWebhook.EnsureSuccessStatusCode();

        // 4. Multi-farmer Checkout splits into 2 orders, locks $1.50/kg tier for 50 kg tomato order
        var checkoutResp = await restClient.PostAsJsonAsync("/api/orders/checkout", new Checkout.Request(
            Items:
            [
                new Checkout.CartLineInput(p1.Id, 50m),
                new Checkout.CartLineInput(p2.Id, 10m)
            ],
            DeliveryAddress: "Fountain Sq 3, Baku",
            PaymentMethod: PaymentMethod.CashOnDelivery,
            FulfillmentType: FulfillmentType.PlatformCourier), PostgresWebAppFactory.JsonOptions);
        Assert.Equal(HttpStatusCode.Created, checkoutResp.StatusCode);
        var cg = (await checkoutResp.Content.ReadFromJsonAsync<CheckoutGroupDto>(PostgresWebAppFactory.JsonOptions))!;
        Assert.Equal(2, cg.Orders.Count);

        var o1 = cg.Orders.Single(o => o.FarmerId == f1Auth.User.Id);
        var o2 = cg.Orders.Single(o => o.FarmerId == f2Auth.User.Id);
        Assert.Equal(1.50m, o1.Items[0].UnitPriceApplied);

        // 5. Farmer 1 accepts via Telegram inline callback; Farmer 2 rejects (restores 10 kg stock)
        var cbResp = await anonClient.PostAsJsonAsync("/api/webhooks/telegram", new
        {
            update_id = 5002,
            callback_query = new
            {
                id = "cb_accept_1",
                from = new { id = 777000111L },
                data = $"order:accept:{o1.Id:N}"
            }
        });
        cbResp.EnsureSuccessStatusCode();

        var o2RejectResp = await f2Client.PatchAsJsonAsync(
            $"/api/orders/{o2.Id}/respond",
            new FarmerRespond.Request(false),
            PostgresWebAppFactory.JsonOptions);
        o2RejectResp.EnsureSuccessStatusCode();
        var o2Rejected = (await o2RejectResp.Content.ReadFromJsonAsync<OrderDto>(PostgresWebAppFactory.JsonOptions))!;
        Assert.Equal(OrderStatus.Cancelled, o2Rejected.Status);

        var p2AfterReject = (await anonClient.GetFromJsonAsync<ProductDto>($"/api/products/{p2.Id}", PostgresWebAppFactory.JsonOptions))!;
        Assert.Equal(40.00m, p2AfterReject.AvailableStockKg);

        // 6. Admin creates morning Milk-Run DeliveryRun; Driver transitions PickingUp -> InTransit
        var runResp = await adminClient.PostAsJsonAsync("/api/logistics/runs", new CreateDeliveryRun.Request(
            DriverName: "Elchin Driver",
            DriverPhone: "+994507000004",
            VehicleNote: "Sprinter #1",
            RunDate: cg.ScheduledDeliveryDate,
            OrderIds: [o1.Id]), PostgresWebAppFactory.JsonOptions);
        Assert.Equal(HttpStatusCode.Created, runResp.StatusCode);
        var run = (await runResp.Content.ReadFromJsonAsync<DeliveryRunDto>(PostgresWebAppFactory.JsonOptions))!;

        Assert.Equal(HttpStatusCode.OK, (await driverClient.PatchAsJsonAsync($"/api/logistics/runs/{run.Id}/status", new UpdateRunStatus.Request(DeliveryRunStatus.PickingUp), PostgresWebAppFactory.JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await driverClient.PatchAsJsonAsync($"/api/logistics/runs/{run.Id}/status", new UpdateRunStatus.Request(DeliveryRunStatus.InTransit), PostgresWebAppFactory.JsonOptions)).StatusCode);

        // 7. Reconcile within ±10% (48 kg vs 50 kg -> -4%, 0 rejected) -> Auto Delivered + locked $1.50/kg
        var recResp = await driverClient.PatchAsJsonAsync($"/api/orders/{o1.Id}/reconcile", new ReconcileDelivery.Request(
            Items: [new ReconcileDelivery.ItemReconciliationInput(o1.Items[0].Id, 48.00m, 0m)]), PostgresWebAppFactory.JsonOptions);
        recResp.EnsureSuccessStatusCode();
        var reconciled = (await recResp.Content.ReadFromJsonAsync<OrderDto>(PostgresWebAppFactory.JsonOptions))!;
        Assert.Equal(OrderStatus.Delivered, reconciled.Status);
        // Split delivery fee = $5.00 + 5% fee on $72.00 ($3.60) + $72.00 produce = $80.60
        Assert.Equal(80.60m, reconciled.FinalTotalAmount);

        var p1AfterRec = (await anonClient.GetFromJsonAsync<ProductDto>($"/api/products/{p1.Id}", PostgresWebAppFactory.JsonOptions))!;
        Assert.Equal(52.00m, p1AfterRec.AvailableStockKg);

        // 8. Admin completes run & marks COD order Paid
        Assert.Equal(HttpStatusCode.OK, (await driverClient.PatchAsJsonAsync($"/api/logistics/runs/{run.Id}/status", new UpdateRunStatus.Request(DeliveryRunStatus.Completed), PostgresWebAppFactory.JsonOptions)).StatusCode);
        var payResp = await adminClient.PatchAsJsonAsync($"/api/admin/orders/{o1.Id}/payment", new MarkOrderPaid.Request(PaymentStatus.Paid, PaymentMethod.CashOnDelivery), PostgresWebAppFactory.JsonOptions);
        payResp.EnsureSuccessStatusCode();
        var paidOrder = (await payResp.Content.ReadFromJsonAsync<OrderDto>(PostgresWebAppFactory.JsonOptions))!;
        Assert.Equal(PaymentStatus.Paid, paidOrder.PaymentStatus);

        // 9. RepeatOrder creates a fresh CheckoutGroup even with 1-click repeat
        var repeatResp = await restClient.PostAsJsonAsync($"/api/orders/{cg.Id}/repeat", new RepeatOrder.Request(), PostgresWebAppFactory.JsonOptions);
        Assert.Equal(HttpStatusCode.Created, repeatResp.StatusCode);
    }

    [Fact]
    public async Task LargeVarianceOrRejection_RequiresBuyerApproval_SupportsDisputeAndResolution()
    {
        using var anonClient = _factory.CreateClient();

        var adminAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994500000000", "Platform Admin");
        using var adminClient = PostgresWebAppFactory.CreateAuthedClient(_factory, adminAuth.AccessToken);

        var farmerSeed = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994508000001", "Lankaran Citrus", UserRole.Farmer);
        var restSeed = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994508000002", "Old City Kitchen", UserRole.Restaurant);
        await PostgresWebAppFactory.VerifyUserAsync(adminClient, farmerSeed.User.Id);
        await PostgresWebAppFactory.VerifyUserAsync(adminClient, restSeed.User.Id);

        var farmerAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994508000001");
        var restAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994508000002");
        using var farmerClient = PostgresWebAppFactory.CreateAuthedClient(_factory, farmerAuth.AccessToken);
        using var restClient = PostgresWebAppFactory.CreateAuthedClient(_factory, restAuth.AccessToken);

        var prodResp = await fProductAsync(farmerClient);
        var prod = prodResp;

        var checkoutResp = await restClient.PostAsJsonAsync("/api/orders/checkout", new Checkout.Request(
            Items: [new Checkout.CartLineInput(prod.Id, 20m)],
            DeliveryAddress: "Icherisheher 8, Baku",
            PaymentMethod: PaymentMethod.WeeklyInvoice), PostgresWebAppFactory.JsonOptions);
        checkoutResp.EnsureSuccessStatusCode();
        var cg = (await checkoutResp.Content.ReadFromJsonAsync<CheckoutGroupDto>(PostgresWebAppFactory.JsonOptions))!;
        var order = cg.Orders[0];

        (await farmerClient.PatchAsJsonAsync($"/api/orders/{order.Id}/respond", new FarmerRespond.Request(true), PostgresWebAppFactory.JsonOptions)).EnsureSuccessStatusCode();

        // Admin reconciles +25% variance (25 kg delivered) and 3 kg rejected => DeliveredPendingApproval
        var recResp = await adminClient.PatchAsJsonAsync($"/api/orders/{order.Id}/reconcile", new ReconcileDelivery.Request(
            Items: [new ReconcileDelivery.ItemReconciliationInput(order.Items[0].Id, 25m, 3m, "https://cdn.example.com/proof.webp")]), PostgresWebAppFactory.JsonOptions);
        recResp.EnsureSuccessStatusCode();
        var reconciled = (await recResp.Content.ReadFromJsonAsync<OrderDto>(PostgresWebAppFactory.JsonOptions))!;
        Assert.Equal(OrderStatus.DeliveredPendingApproval, reconciled.Status);
        // 22 chargeable kg * $3.00 = $66.00 + 5% ($3.30) + $10.00 delivery = $79.30
        Assert.Equal(79.30m, reconciled.FinalTotalAmount);

        // Restaurant disputes first, then confirms once resolved
        var dispResp = await restClient.PatchAsJsonAsync($"/api/orders/{order.Id}/confirm", new ConfirmReceipt.Request(false, "Damaged crate"), PostgresWebAppFactory.JsonOptions);
        dispResp.EnsureSuccessStatusCode();
        var disputed = (await dispResp.Content.ReadFromJsonAsync<OrderDto>(PostgresWebAppFactory.JsonOptions))!;
        Assert.Equal(OrderStatus.Disputed, disputed.Status);
        Assert.Equal(PaymentStatus.Disputed, disputed.PaymentStatus);
        Assert.Equal("Damaged crate", disputed.DisputeNote);

        var confResp = await restClient.PatchAsJsonAsync($"/api/orders/{order.Id}/confirm", new ConfirmReceipt.Request(true), PostgresWebAppFactory.JsonOptions);
        confResp.EnsureSuccessStatusCode();
        var confirmed = (await confResp.Content.ReadFromJsonAsync<OrderDto>(PostgresWebAppFactory.JsonOptions))!;
        Assert.Equal(OrderStatus.Delivered, confirmed.Status);
        Assert.Equal(PaymentStatus.Unpaid, confirmed.PaymentStatus);
    }

    [Fact]
    public async Task BootstrapAccounts_AndSampleData_AreSeededAndAccessibleByAllRoles()
    {
        using var anonClient = _factory.CreateClient();

        // 1. Bootstrap accounts endpoint lists all 9 pre-seeded accounts and demo OTP code
        var bootstrapResp = await anonClient.GetFromJsonAsync<AuthEndpoints.BootstrapAccountsResponse>(
            "/api/auth/bootstrap-accounts",
            PostgresWebAppFactory.JsonOptions);
        Assert.NotNull(bootstrapResp);
        Assert.True(bootstrapResp.DemoOtpEnabled);
        Assert.Equal("123456", bootstrapResp.DemoOtpCode);
        Assert.Equal(9, bootstrapResp.Accounts.Count);

        // 2. Public catalog includes the 9 seeded farmer products
        var catalog = await anonClient.GetFromJsonAsync<AgroMarket.Api.Common.PagedResult<ProductDto>>(
            "/api/products?pageSize=50",
            PostgresWebAppFactory.JsonOptions);
        Assert.NotNull(catalog);
        Assert.True(catalog.TotalCount >= 9);
        Assert.Contains(catalog.Items, p => p.Title == "Zira Pink Beefsteak Tomatoes" && p.PriceTiers.Count == 3);
        Assert.Contains(catalog.Items, p => p.Title == "Lankaran Satsuma Mandarins");

        // 3. Authenticate with seeded Farmer, Restaurant, and Driver bootstrap accounts using demo OTP 123456
        var farmerAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994501111111");
        Assert.Equal(UserRole.Farmer, farmerAuth.User.Role);
        Assert.True(farmerAuth.User.IsVerified);

        var restAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994502222221");
        Assert.Equal(UserRole.Restaurant, restAuth.User.Role);
        Assert.True(restAuth.User.IsVerified);

        var driverAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994503333331");
        Assert.Equal(UserRole.Driver, driverAuth.User.Role);
        Assert.True(driverAuth.User.IsVerified);

        using var restClient = PostgresWebAppFactory.CreateAuthedClient(_factory, restAuth.AccessToken);
        using var driverClient = PostgresWebAppFactory.CreateAuthedClient(_factory, driverAuth.AccessToken);

        // 4. Restaurant sees its seeded checkout groups; Driver sees seeded morning Milk-Run delivery runs
        var groups = await restClient.GetFromJsonAsync<AgroMarket.Api.Common.PagedResult<CheckoutGroupDto>>(
            "/api/orders/checkout-groups",
            PostgresWebAppFactory.JsonOptions);
        Assert.NotNull(groups);
        Assert.True(groups.TotalCount >= 2);

        var runs = await driverClient.GetFromJsonAsync<AgroMarket.Api.Common.PagedResult<DeliveryRunDto>>(
            "/api/logistics/runs",
            PostgresWebAppFactory.JsonOptions);
        Assert.NotNull(runs);
        Assert.True(runs.TotalCount >= 2);
    }

    private static async Task<ProductDto> fProductAsync(HttpClient farmerClient)
    {
        var resp = await farmerClient.PostAsJsonAsync("/api/products", new CreateProduct.Request(
            Category: "Citrus",
            Title: "Lankaran Mandarin",
            Description: null,
            AvailableStockKg: 60m,
            MinOrderKg: 5m,
            BasePricePerKg: 3.00m), PostgresWebAppFactory.JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ProductDto>(PostgresWebAppFactory.JsonOptions))!;
    }
}
