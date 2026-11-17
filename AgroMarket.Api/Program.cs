using System.Text;
using System.Text.Json.Serialization;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Features.Admin;
using AgroMarket.Api.Features.Auth;
using AgroMarket.Api.Features.Logistics;
using AgroMarket.Api.Features.Media;
using AgroMarket.Api.Features.Orders;
using AgroMarket.Api.Features.Products;
using AgroMarket.Api.Features.Profiles;
using AgroMarket.Api.Infrastructure.Auth;
using AgroMarket.Api.Infrastructure.Notifications;
using AgroMarket.Api.Infrastructure.Storage;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddOpenApi();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddAgroMarketRateLimiting();

var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var flatOrigins = builder.Configuration.GetValue<string>("Cors:AllowedOrigins");
var allowedOrigins = configuredOrigins
    .Concat(
        string.IsNullOrWhiteSpace(flatOrigins)
            ? []
            : flatOrigins.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .Select(origin =>
    {
        var trimmed = origin.Trim().TrimEnd('/');
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            ? $"{uri.Scheme}://{uri.Authority}"
            : trimmed;
    })
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"]
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' or 'DATABASE_URL' was not configured.");

var connectionString = NormalizePostgresConnectionString(rawConnectionString);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AuthBootstrapOptions>(builder.Configuration.GetSection(AuthBootstrapOptions.SectionName));
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.SectionName));
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));
builder.Services.Configure<MarketplaceOptions>(builder.Configuration.GetSection(MarketplaceOptions.SectionName));
builder.Services.AddSingleton<IImageStorageService, DatabaseImageStorageService>();
builder.Services.AddHttpClient<ITelegramNotifier, TelegramBotService>();
builder.Services.AddHostedService<DatabaseSeedHostedService>();
builder.Services.AddHostedService<OrderExpirationWorker>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be configured and at least 32 characters long.");
}

builder.Services.AddSingleton<JwtTokenService>();

var otpProvider = builder.Configuration.GetValue<string>("Otp:Provider");
if (string.Equals(otpProvider, "Telegram", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IOtpSender, TelegramGatewayOtpSender>();
}
else
{
    builder.Services.AddScoped<IOtpSender, ConsoleOtpSender>();
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.Admin, policy =>
        policy.RequireRole(nameof(UserRole.Admin)))
    .AddPolicy(AuthorizationPolicies.AdminOrDriver, policy =>
        policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Driver)))
    .AddPolicy(AuthorizationPolicies.VerifiedUser, policy =>
        policy.RequireClaim("is_verified", "true"))
    .AddPolicy(AuthorizationPolicies.VerifiedFarmer, policy =>
        policy.RequireRole(nameof(UserRole.Farmer))
              .RequireClaim("is_verified", "true"))
    .AddPolicy(AuthorizationPolicies.VerifiedRestaurant, policy =>
        policy.RequireRole(nameof(UserRole.Restaurant))
              .RequireClaim("is_verified", "true"));

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("AgroMarket B2B API")
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.MapHealthChecks("/health");

var api = app.MapGroup("/api")
    .RequireRateLimiting(RateLimitPolicies.GeneralApi);

api.MapGet("/ping", () => TypedResults.Ok(new
{
    service = "AgroMarket.Api",
    status = "healthy",
    utcNow = DateTimeOffset.UtcNow
}))
.WithName("Ping")
.WithTags("System");

api.MapAuthEndpoints();
api.MapAdminEndpoints();
api.MapProfileEndpoints();
api.MapMediaEndpoints();
api.MapProductEndpoints();
api.MapOrderEndpoints();
api.MapLogisticsEndpoints();

app.Run();

static string NormalizePostgresConnectionString(string raw)
{
    var trimmed = raw.Trim();
    if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return trimmed;
    }

    var uri = new Uri(trimmed);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var database = uri.AbsolutePath.Trim('/');

    var csb = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = database,
        Username = username,
        Password = password
    };

    if (!uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
        !uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
        !uri.Host.Equals("postgres", StringComparison.OrdinalIgnoreCase))
    {
        csb.SslMode = SslMode.Prefer;
    }

    return csb.ConnectionString;
}

public partial class Program;
