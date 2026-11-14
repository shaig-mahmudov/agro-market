using AgroMarket.Api.Database;
using AgroMarket.Api.Features.Admin;
using AgroMarket.Api.Features.Auth;
using AgroMarket.Api.Features.Orders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgroMarket.Tests;

public sealed class PostgresWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _databaseName = $"agromarket_test_{Guid.NewGuid():N}";

    public string ConnectionString =>
        Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION") is { Length: > 0 } envConn
            ? envConn
            : $"Host=localhost;Port=5432;Database={_databaseName};Username=agromarket;Password=agromarket_dev_password";

    public async Task InitializeAsync()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

        await using var db = new AppDbContext(optionsBuilder.Options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

        await using var db = new AppDbContext(optionsBuilder.Options);
        await db.Database.EnsureDeletedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Marketplace:ExpirationSweepIntervalSeconds", "3600");
    }

    public static async Task<VerifyOtp.Response> AuthenticateUserAsync(
        HttpClient client,
        string phoneNumber,
        string? fullName = null,
        Api.Database.Entities.UserRole? role = null)
    {
        var sendResp = await client.PostAsJsonAsync(
            "/api/auth/send-otp",
            new SendOtp.Request(phoneNumber),
            JsonOptions);
        sendResp.EnsureSuccessStatusCode();

        var otp = await sendResp.Content.ReadFromJsonAsync<SendOtp.Response>(JsonOptions)
            ?? throw new InvalidOperationException("Missing SendOtp response.");

        var verifyResp = await client.PostAsJsonAsync(
            "/api/auth/verify-otp",
            new VerifyOtp.Request(phoneNumber, otp.DevOtpCode!, fullName, role),
            JsonOptions);
        verifyResp.EnsureSuccessStatusCode();

        return await verifyResp.Content.ReadFromJsonAsync<VerifyOtp.Response>(JsonOptions)
            ?? throw new InvalidOperationException("Missing VerifyOtp response.");
    }

    public static HttpClient CreateAuthedClient(WebApplicationFactory<Program> factory, string accessToken)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public static async Task VerifyUserAsync(
        HttpClient adminClient,
        Guid userId,
        Api.Database.Entities.UserRole? role = null)
    {
        var resp = await adminClient.PatchAsJsonAsync(
            $"/api/admin/users/{userId}/verify",
            new VerifyUser.Request(true, role),
            JsonOptions);
        resp.EnsureSuccessStatusCode();
    }
}
