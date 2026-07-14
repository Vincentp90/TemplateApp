using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamTracker.API.Models;
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

builder.Services.AddScoped<IProcessPriceCheckUseCase, ProcessPriceCheckUseCase>();
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

// GET /api/games/prices?appIds=1&appIds=2 — public price passthrough
api.MapGet("/games/prices", async (
    IGameRepository gameRepo,
    [FromQuery] int[] appIds) =>
{
    if (appIds.Length == 0)
        return Results.Ok(Array.Empty<GamePriceDto>());

    var results = new List<GamePriceDto>();
    foreach (var appId in appIds)
    {
        var game = await gameRepo.GetAsync(new SteamAppId(appId));
        if (game != null)
        {
            results.Add(new GamePriceDto(
                AppId: game.AppId.Value,
                Amount: game.CurrentPrice?.Amount,
                Currency: game.CurrentPrice?.Currency ?? "EUR",
                LastCheckedAt: game.LastCheckedAt,
                IsUnavailable: game.IsUnavailable
            ));
        }
    }
    return Results.Ok(results);
});

app.Run();


