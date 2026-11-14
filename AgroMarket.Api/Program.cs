using AgroMarket.Api.Common;
using FluentValidation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddAgroMarketRateLimiting();
builder.Services.AddHealthChecks();

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
