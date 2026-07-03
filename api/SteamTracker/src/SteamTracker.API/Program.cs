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

// Global exception handler
builder.Services.AddExceptionHandler<SteamTracker.API.ExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Ensure DB is up-to-date (migration applied at startup — use Migrate in prod)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SteamTrackerDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Global exception handler
app.UseExceptionHandler();

// Minimal API endpoints
var api = app.MapGroup("/api");

// GET /api/wishlist — returns wishlist with prices (internal caller, userId from header)
api.MapGet("/wishlist", async (IGetWishlistWithPricesQuery query, HttpContext context) =>
{
    var userId = context.Request.Headers["X-Internal-UserId"].ToString();
    var results = await query.ExecuteAsync(userId);
    return Results.Ok(results);
});

// POST /api/games/{appId}/alert — create alert rule (internal caller, userId from header)
api.MapPost("/games/{appId}/alert", async (
    ISetAlertRuleUseCase useCase,
    HttpContext context,
    int appId,
    [FromQuery] decimal thresholdAmount,
    [FromQuery] string currency = "EUR") =>
{
    var userId = context.Request.Headers["X-Internal-UserId"].ToString();
    await useCase.ExecuteAsync(userId, appId, thresholdAmount, currency);
    return Results.Created($"/api/games/{appId}/alert", null);
});

// DELETE /api/alert/{alertRuleId} — delete alert rule (internal caller, userId from header)
api.MapDelete("/alert/{alertRuleId}", async (
    IDeleteAlertRuleUseCase useCase,
    HttpContext context,
    Guid alertRuleId) =>
{
    var userId = context.Request.Headers["X-Internal-UserId"].ToString();
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
