using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddAgroMarketRateLimiting();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres");

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();

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

app.Run();

public partial class Program;
