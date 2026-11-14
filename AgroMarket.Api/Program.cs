using System.Text;
using System.Text.Json.Serialization;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Features.Admin;
using AgroMarket.Api.Features.Auth;
using AgroMarket.Api.Features.Orders;
using AgroMarket.Api.Features.Products;
using AgroMarket.Api.Features.Profiles;
using AgroMarket.Api.Infrastructure.Auth;
using AgroMarket.Api.Infrastructure.Notifications;
using AgroMarket.Api.Infrastructure.Storage;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddAgroMarketRateLimiting();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AuthBootstrapOptions>(builder.Configuration.GetSection(AuthBootstrapOptions.SectionName));
builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.SectionName));
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));
builder.Services.Configure<R2Options>(builder.Configuration.GetSection(R2Options.SectionName));
builder.Services.Configure<MarketplaceOptions>(builder.Configuration.GetSection(MarketplaceOptions.SectionName));
builder.Services.AddSingleton<IObjectStorageService, R2ObjectStorageService>();
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
    .AddPolicy(AuthorizationPolicies.VerifiedUser, policy =>
        policy.RequireClaim("is_verified", "true"))
    .AddPolicy(AuthorizationPolicies.VerifiedFarmer, policy =>
        policy.RequireRole(nameof(UserRole.Farmer))
              .RequireClaim("is_verified", "true"))
    .AddPolicy(AuthorizationPolicies.VerifiedRestaurant, policy =>
        policy.RequireRole(nameof(UserRole.Restaurant))
              .RequireClaim("is_verified", "true"));

var app = builder.Build();

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
api.MapProductEndpoints();
api.MapOrderEndpoints();

app.Run();

public partial class Program;
