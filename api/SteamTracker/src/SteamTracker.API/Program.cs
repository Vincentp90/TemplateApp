using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamTracker.Application.Ports;
using SteamTracker.Application.UseCases;
using SteamTracker.Domain.Services;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure;
using SteamTracker.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Application — use cases
builder.Services.AddScoped<ISetAlertRuleUseCase, SetAlertRuleUseCase>();
builder.Services.AddScoped<IDeleteAlertRuleUseCase, DeleteAlertRuleUseCase>();
builder.Services.AddScoped<IGetWishlistWithPricesQuery, GetWishlistWithPricesQuery>();
builder.Services.AddScoped<IProcessPriceCheckUseCase, ProcessPriceCheckUseCase>();
builder.Services.AddScoped<IHandleWishlistItemAddedUseCase, HandleWishlistItemAddedUseCase>();
builder.Services.AddScoped<IHandleWishlistItemRemovedUseCase, HandleWishlistItemRemovedUseCase>();
builder.Services.AddSingleton<PriceAlertEvaluator>();

var app = builder.Build();

// Ensure DB is up-to-date (migration applied at startup — use Migrate in prod)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SteamTrackerDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Minimal API endpoints
var api = app.MapGroup("/api");

// GET /api/wishlist — returns wishlist with prices
api.MapGet("/wishlist", async (IGetWishlistWithPricesQuery query, string userId) =>
{
    var results = await query.ExecuteAsync(userId);
    return Results.Ok(results);
});

// POST /api/wishlist/{userId}/games/{appId}/alert — create alert rule
api.MapPost("/wishlist/{userId}/games/{appId}/alert", async (
    ISetAlertRuleUseCase useCase,
    string userId,
    int appId,
    decimal thresholdAmount,
    string currency = "EUR") =>
{
    await useCase.ExecuteAsync(userId, appId, thresholdAmount, currency);
    return Results.Created($"/api/wishlist/{userId}/games/{appId}/alert", null);
});

// DELETE /api/wishlist/{userId}/alert/{alertRuleId}
api.MapDelete("/wishlist/{userId}/alert/{alertRuleId}", async (
    IDeleteAlertRuleUseCase useCase,
    string userId,
    Guid alertRuleId) =>
{
    await useCase.ExecuteAsync(userId, alertRuleId);
    return Results.NoContent();
});

// POST /api/internal/price-check — called by PriceCheckWorker
api.MapPost("/internal/price-check", async (
    IProcessPriceCheckUseCase useCase,
    [FromBody] PriceCheckRequest request) =>
{
    await useCase.ExecuteAsync(request.AppId, new Money(request.Price, "EUR"), request.Name);
    return Results.Ok();
});

// POST /api/internal/wishlist-item-added — called by WishlistSyncWorker
api.MapPost("/internal/wishlist-item-added", async (
    IHandleWishlistItemAddedUseCase useCase,
    [FromBody] WishlistItemEvent request) =>
{
    await useCase.ExecuteAsync(request.UserId, request.AppId, request.AddedAt);
    return Results.Ok();
});

// POST /api/internal/wishlist-item-removed — called by WishlistSyncWorker
api.MapPost("/internal/wishlist-item-removed", async (
    IHandleWishlistItemRemovedUseCase useCase,
    [FromBody] WishlistItemEvent request) =>
{
    await useCase.ExecuteAsync(request.UserId, request.AppId);
    return Results.Ok();
});

app.Run();

// Request DTOs
public record PriceCheckRequest(int AppId, decimal Price, string Name);
public record WishlistItemEvent(string UserId, int AppId, DateTimeOffset AddedAt);
