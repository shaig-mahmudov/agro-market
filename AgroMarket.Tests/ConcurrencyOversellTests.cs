using System.Net;
using System.Net.Http.Json;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Features.Orders;
using AgroMarket.Api.Features.Products;

namespace AgroMarket.Tests;

public sealed class ConcurrencyOversellTests : IClassFixture<PostgresWebAppFactory>
{
    private readonly PostgresWebAppFactory _factory;

    public ConcurrencyOversellTests(PostgresWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConcurrentCheckouts_CannotOversellAvailableStock()
    {
        using var anonClient = _factory.CreateClient();

        // Authenticate Admin, Farmer, and two competing Restaurants
        var adminAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994500000000", "Admin");
        using var adminClient = PostgresWebAppFactory.CreateAuthedClient(_factory, adminAuth.AccessToken);

        var farmerSeed = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994506000001", "Zira Farm", UserRole.Farmer);
        var rest1Seed = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994506000002", "Bistro One", UserRole.Restaurant);
        var rest2Seed = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994506000003", "Bistro Two", UserRole.Restaurant);

        await PostgresWebAppFactory.VerifyUserAsync(adminClient, farmerSeed.User.Id);
        await PostgresWebAppFactory.VerifyUserAsync(adminClient, rest1Seed.User.Id);
        await PostgresWebAppFactory.VerifyUserAsync(adminClient, rest2Seed.User.Id);

        var farmerAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994506000001");
        var rest1Auth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994506000002");
        var rest2Auth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994506000003");

        using var farmerClient = PostgresWebAppFactory.CreateAuthedClient(_factory, farmerAuth.AccessToken);
        using var rest1Client = PostgresWebAppFactory.CreateAuthedClient(_factory, rest1Auth.AccessToken);
        using var rest2Client = PostgresWebAppFactory.CreateAuthedClient(_factory, rest2Auth.AccessToken);

        // Farmer publishes a product with 100 kg stock
        var createResp = await farmerClient.PostAsJsonAsync("/api/products", new CreateProduct.Request(
            Category: "Tomatoes",
            Title: "Concurrent Pink Tomato",
            Description: "Greenhouse crop",
            AvailableStockKg: 100m,
            MinOrderKg: 10m,
            BasePricePerKg: 2.50m), PostgresWebAppFactory.JsonOptions);
        createResp.EnsureSuccessStatusCode();
        var product = await createResp.Content.ReadFromJsonAsync<ProductDto>(PostgresWebAppFactory.JsonOptions);
        Assert.NotNull(product);

        // Fire two concurrent checkouts requesting 70 kg each (140 kg total > 100 kg)
        var reqBody = new Checkout.Request(
            Items: [new Checkout.CartLineInput(product.Id, 70m)],
            DeliveryAddress: "Nizami St 12, Baku");

        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> SubmitCheckoutAsync(HttpClient client)
        {
            await barrier.Task;
            return await client.PostAsJsonAsync("/api/orders/checkout", reqBody, PostgresWebAppFactory.JsonOptions);
        }

        var t1 = SubmitCheckoutAsync(rest1Client);
        var t2 = SubmitCheckoutAsync(rest2Client);
        barrier.SetResult();

        var responses = await Task.WhenAll(t1, t2);
        var createdCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, createdCount);
        Assert.Equal(1, conflictCount);

        var refreshedProduct = await anonClient.GetFromJsonAsync<ProductDto>(
            $"/api/products/{product.Id}",
            PostgresWebAppFactory.JsonOptions);
        Assert.NotNull(refreshedProduct);
        Assert.Equal(30.00m, refreshedProduct.AvailableStockKg);
    }

    [Fact]
    public async Task ConcurrentProductUpdates_EnforceXminOptimisticConcurrency()
    {
        using var anonClient = _factory.CreateClient();

        var adminAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994500000000", "Admin");
        using var adminClient = PostgresWebAppFactory.CreateAuthedClient(_factory, adminAuth.AccessToken);

        var farmerSeed = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994506000011", "Guba Farm", UserRole.Farmer);
        await PostgresWebAppFactory.VerifyUserAsync(adminClient, farmerSeed.User.Id);

        var farmerAuth = await PostgresWebAppFactory.AuthenticateUserAsync(anonClient, "+994506000011");
        using var farmerClient = PostgresWebAppFactory.CreateAuthedClient(_factory, farmerAuth.AccessToken);

        var createResp = await farmerClient.PostAsJsonAsync("/api/products", new CreateProduct.Request(
            Category: "Apples",
            Title: "Golden Apple",
            Description: null,
            AvailableStockKg: 200m,
            MinOrderKg: 5m,
            BasePricePerKg: 1.80m), PostgresWebAppFactory.JsonOptions);
        createResp.EnsureSuccessStatusCode();
        var product = await createResp.Content.ReadFromJsonAsync<ProductDto>(PostgresWebAppFactory.JsonOptions);
        Assert.NotNull(product);

        // First update succeeds and advances PostgreSQL xmin
        var patch1 = await farmerClient.PatchAsJsonAsync(
            $"/api/products/{product.Id}",
            new UpdateProduct.Request(AvailableStockKg: 250m, RowVersion: product.RowVersion),
            PostgresWebAppFactory.JsonOptions);
        Assert.Equal(HttpStatusCode.OK, patch1.StatusCode);

        // Stale RowVersion is rejected with 409 Conflict
        var patch2 = await farmerClient.PatchAsJsonAsync(
            $"/api/products/{product.Id}",
            new UpdateProduct.Request(AvailableStockKg: 300m, RowVersion: product.RowVersion),
            PostgresWebAppFactory.JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, patch2.StatusCode);
    }
}
