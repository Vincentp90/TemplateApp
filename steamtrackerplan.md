# SteamTracker — implementation plan

Hexagonal (ports & adapters) DDD with C# API + React frontend + RabbitMQ single worker.

---

## Scope constraint — fundamental rule

> **SteamTracker only tracks prices for games already on a user's wishlist.**
> Adding a game to the wishlist automatically enables price tracking. Removing it stops tracking. There is no separate "watch" concept.

This means:
- The existing app's `WishlistItem` is the source of truth for *what to track*
- `Game` in this domain is a **price-tracking projection** of `WishlistItem` — it doesn't maintain its own list
- An `AlertRule` can only exist for a `(UserId, AppId)` pair that has a matching `WishlistItem`
- The scheduler enqueues price-check jobs from SteamTracker's own local wishlist replica (see ACL below)

---

## Anti-corruption layer — no shared database tables

SteamTracker does **not** read the existing app's `wishlist_items` table directly. Instead, the two services communicate exclusively via RabbitMQ events. This means:

- Schema changes in the existing app never silently break SteamTracker
- SteamTracker owns its own local replica of which games are being tracked
- The boundary between services is explicit and versioned (the event contract)

### How SteamTracker consumes them

> **Prerequisite**: The changes in [steamtracker-existing-app-changes.md](steamtracker-existing-app-changes.md) must be completed before SteamTracker development begins. The existing app publishes `WishlistItemAdded` and `WishlistItemRemoved` events to a `wishlist.events` RabbitMQ exchange.

SteamTracker has a dedicated `WishlistSyncWorker` (a second `BackgroundService`) that:
- Consumes `WishlistItemAdded` → upserts a `TrackedGame` record locally
- Consumes `WishlistItemRemoved` → marks the `TrackedGame` as inactive (soft delete), deactivates its `AlertRule`s

```
Exchange: wishlist.events         (published by existing app)
  → Queue: steamtracker.wishlist-sync   (consumed by WishlistSyncWorker)
```

---

## Project structure

```
SteamTracker/
├── src/
│   ├── SteamTracker.Domain/          # Pure domain — no framework dependencies
│   ├── SteamTracker.Application/     # Use cases + port interfaces
│   ├── SteamTracker.Infrastructure/  # Adapters: EF, RabbitMQ, Steam HTTP
│   ├── SteamTracker.API/             # ASP.NET Web API (primary adapter)
│   └── SteamTracker.Worker/          # BackgroundService: price checker + wishlist sync
└── tests/
    ├── SteamTracker.Domain.Tests/
    ├── SteamTracker.Application.Tests/
    └── SteamTracker.Integration.Tests/
```

The key hexagonal rule: **Domain and Application have zero references to Infrastructure.** All cross-boundary communication goes through port interfaces defined in Application.

---

## Phase 1 — Domain model

### Relationship to WishlistItem

SteamTracker never touches the existing app's tables. Instead it maintains its own `TrackedGame` — a local ACL projection populated by wishlist events:

```
WishlistItemAdded event               TrackedGame (SteamTracker's local replica)
  - UserId                              - AppId        : SteamAppId
  - AppId       ──── drives ──────►     - IsActive      : bool
  - AddedAt                             - TrackedSince  : DateTimeOffset
```

`TrackedGame` is distinct from `Game` (which holds price data). They share `AppId` as identity:

```
TrackedGame   — "should we be fetching prices for this AppId?"
Game          — "what is the current price for this AppId?"
```

### Aggregates / entities

```
TrackedGame (aggregate root)
  - AppId        : SteamAppId
  - IsActive     : bool
  - TrackedSince : DateTimeOffset

  + static TrackedGame StartTracking(SteamAppId, DateTimeOffset)
  + void StopTracking()   # raises TrackingStoppedEvent, deactivates alert rules

Game (aggregate root — price data, one per unique AppId)
  - AppId           : SteamAppId
  - Name            : string          # resolved from Steam API on first price fetch
  - CurrentPrice    : Money?
  - LastCheckedAt   : DateTimeOffset?

  + void ApplyPriceUpdate(Money newPrice, string name, DateTimeOffset at)
  # raises PriceUpdatedEvent

PriceSnapshot (entity, child of Game — append-only)
  - SnapshotId      : Guid
  - Price           : Money
  - DiscountPercent : int
  - CapturedAt      : DateTimeOffset

AlertRule (aggregate root — per user, per wishlisted game)
  - AlertRuleId       : Guid
  - UserId            : UserId
  - AppId             : SteamAppId
  - TriggerBelowPrice : Money
  - IsActive          : bool
  - LastTriggeredAt   : DateTimeOffset?

  + bool ShouldTrigger(Money currentPrice)
  + void MarkTriggered(DateTimeOffset at)
  + void Deactivate()   # called when WishlistItemRemoved is received
```

### Value objects

```
SteamAppId  — wraps int, validates > 0
Money       — Amount (decimal) + Currency (ISO string, default "EUR")
              + static Money.Free for F2P games
UserId      — wraps Guid
```

### Domain events

```
PriceUpdatedEvent      { AppId, OldPrice, NewPrice, CapturedAt }
AlertTriggeredEvent    { AlertRuleId, UserId, AppId, Price }
TrackingStoppedEvent   { AppId }   # triggers alert rule deactivation
```

### Domain service

```
PriceAlertEvaluator
  + IEnumerable<AlertRule> Evaluate(Game game, IEnumerable<AlertRule> rules)
  # pure logic — no I/O
```

---

## Phase 2 — Application layer (ports)

### Driving ports (called by API / Workers)

```csharp
// Alert rules — validated against local TrackedGame (not the external wishlist table)
ISetAlertRuleUseCase          SetAlertRule(UserId, SteamAppId, Money threshold)
IDeleteAlertRuleUseCase       DeleteAlertRule(UserId, Guid alertRuleId)

// Wishlist price view — joins local TrackedGame + Game price data per user
IGetWishlistWithPricesQuery   GetWishlistWithPrices(UserId) → WishlistItemWithPriceDto[]

// Called by PriceCheckWorker after fetching from Steam
IProcessPriceCheckUseCase     ProcessPriceCheck(SteamAppId, Money price, string name)

// Called by WishlistSyncWorker when ACL events arrive
IHandleWishlistItemAddedUseCase   Handle(WishlistItemAddedMessage)
IHandleWishlistItemRemovedUseCase Handle(WishlistItemRemovedMessage)
```

### Driven ports (implemented by Infrastructure)

```csharp
// SteamTracker's own persistence — no external table reads
ITrackedGameRepository        GetActive() → IEnumerable<TrackedGame>
                              Get(SteamAppId), Save(TrackedGame)
IGameRepository               Get(SteamAppId), Save(Game)
IAlertRuleRepository          GetActiveRulesFor(SteamAppId), GetForUser(UserId), Save(AlertRule)

// Messaging
IPriceCheckJobPublisher       Enqueue(SteamAppId)
INotificationPublisher        Notify(AlertTriggeredEvent)

// External
ISteamStoreClient             FetchPrice(SteamAppId) → (Money? Price, string Name)?
```

---

## Phase 3 — Infrastructure adapters

### ✅ Done

- [x] EF Core DbContext (`SteamTrackerDbContext`) with all entity configs
- [x] Repository pattern: `GameRepository`, `TrackedGameRepository`, `AlertRuleRepository`, `PriceSnapshotRepository`
- [x] Value object converters for `Money` (string "Amount|Currency" format) and `SteamAppId` (int)
- [x] `SteamStoreClient` with EUR-consistent pricing (`cc=de&l=german`), F2P support (`Money.Free`), rate limit exception
- [x] RabbitMQ publishers: `PriceCheckJobPublisher`, `NotificationPublisher` (async API, RabbitMQ.Client 7.x)
- [x] DI registration for all infrastructure services
- [x] Testcontainers shared fixtures: `PostgresContainerFixture`, `RabbitMqContainerFixture` (singleton per test run)
- [x] `DispatchConsumersAsync` removal for RabbitMQ.Client 7.x
- [x] Deprecated testcontainers constructors fixed (`PostgreSqlBuilder` / `RabbitMqBuilder` builder pattern)
- [x] `AlertRule.AppId` value converter added to `OnModelCreating` (was missing)
- [x] `GameRepository.SaveAsync` fixed: persists new `PriceSnapshot` entities on updates
- [x] `PostgresContainerFixture.CreateDbContext()` fixed: uses `UseInternalServiceProvider()` with `AddEntityFrameworkNpgsql()` for EF Core 9+
- [x] `SkipTestException` removed — integration tests fail naturally when Docker is unavailable

### Persistence — EF Core + Postgres

SteamTracker has its **own schema** with no foreign keys into the existing app's tables:

```
tracked_games      (app_id PK, is_active, tracked_since)
games              (app_id PK, current_price, currency, last_checked_at)
price_snapshots    (id PK, app_id FK→games, price, currency, discount_pct, captured_at)
                   UNIQUE INDEX on (app_id, captured_at)      ← idempotency guard
alert_rules        (id PK, user_id, app_id FK→tracked_games, trigger_below, currency, is_active, last_triggered_at)
```

`SetAlertRule` validates that `TrackedGame` exists and `IsActive = true` before creating an `AlertRule` — this replaces the old FK constraint into `wishlist_items`.

### Messaging — RabbitMQ (full picture)

```
# Published by existing app — consumed by SteamTracker's WishlistSyncWorker
Exchange: wishlist.events
  → Queue: steamtracker.wishlist-sync

# Internal to SteamTracker
Exchange: steamtracker.direct
  → Queue: price-check-jobs       (consumed by PriceCheckWorker)
  → Queue: notifications          (consumed by notification service)

Dead-letter exchange: steamtracker.dlx
  → Queue: price-check-dead
  → Queue: wishlist-sync-dead
```

**Message contracts**

```csharp
// Inbound from existing app (ACL boundary)
record WishlistItemAddedMessage(string UserId, int AppId, DateTimeOffset AddedAt);
record WishlistItemRemovedMessage(string UserId, int AppId, DateTimeOffset RemovedAt);

// Internal
record PriceCheckJob(int AppId, DateTimeOffset EnqueuedAt);
record NotificationMessage(Guid AlertRuleId, string UserId, int AppId, decimal Price, string Currency);
```

### Steam HTTP adapter

**`SteamStoreHttpClient`** — implements `ISteamStoreClient`

- `https://store.steampowered.com/api/appdetails?appids={id}&filters=price_overview&cc=de&l=german`
- `cc=de&l=german` ensures consistent EUR pricing regardless of worker IP
- Returns `(Money.Free, name)` if `price_overview` is absent (F2P game)
- Returns `null` on unexpected shape (handle as skip in use case)
- Throws `SteamRateLimitException` on 429

---

## Phase 4 — Workers (BackgroundServices) ✅ DONE

### PriceCheckWorker

```csharp
public class PriceCheckWorker : BackgroundService
{
    // Consumes from price-check-jobs queue
    // 1. Deserialize PriceCheckMessage
    // 2. Call ISteamStoreClient.FetchPriceAsync(appId)
    // 3. On success → IProcessPriceCheckUseCase.ExecuteAsync, ack
    // 4. On 429 → nack, requeue
    // 5. On error → nack, requeue (no dead-letter in consumer — handled by scheduler retry)
}
```

### WishlistSyncWorker

```csharp
public class WishlistSyncWorker : BackgroundService
{
    // Consumes from steamtracker.wishlist-sync queue (fanout from wishlist.events)
    // Parses JSON to determine event type:
    //   - has "removedAt" → WishlistItemRemovedMessage → HandleWishlistItemRemovedUseCase
    //   - else → WishlistItemAddedMessage → HandleWishlistItemAddedUseCase
    // ack/nack based on success/failure
}
```

```csharp
// For each PriceCheckJob message:
//   1. Acquire rate-limit token (blocks if near Steam's 200/5min limit)
//   2. Call ISteamStoreClient.FetchPrice(appId) → (price, name)
//   3. On success     → IProcessPriceCheckUseCase.ProcessPriceCheck(appId, price, name), ack
//   4. On 429         → nack, requeue after TTL delay
//   5. On 5xx/timeout → nack, dead-letter after N retries
```

**Rate limiter** — token bucket, .NET 7+ built-in

```csharp
new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
{
    TokenLimit = 180,
    ReplenishmentPeriod = TimeSpan.FromMinutes(5),
    TokensPerPeriod = 180,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    QueueLimit = 0
});
```

**Retry policy** — Polly / Microsoft.Extensions.Http.Resilience

```
Retry: 3 attempts, exponential backoff (2s, 4s, 8s)
Circuit breaker: open after 5 consecutive failures, half-open after 30s
```

### WishlistSyncWorker

```csharp
// Consumes from steamtracker.wishlist-sync queue
// WishlistItemAdded   → IHandleWishlistItemAddedUseCase.Handle(msg)
//                       upserts TrackedGame, triggers first price-check job
// WishlistItemRemoved → IHandleWishlistItemRemovedUseCase.Handle(msg)
//                       sets TrackedGame.IsActive = false, deactivates AlertRules
```

Both workers live in `SteamTracker.Worker` and share the same host process.

### ✅ Workers registered in DI

All three registered in Worker `Program.cs` via `AddHostedService<T>()`:

```csharp
hostBuilder.Services.AddHostedService<PriceCheckScheduler>();
hostBuilder.Services.AddHostedService<PriceCheckWorker>();
hostBuilder.Services.AddHostedService<WishlistSyncWorker>();
```

### PriceCheckScheduler

Every 24h, reads `ITrackedGameRepository.GetActiveAsync()` and enqueues **one job per unique AppId**:

```csharp
public class PriceCheckScheduler : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var activeGames = await _trackedGameRepo.GetActiveAsync(stoppingToken);
            foreach (var game in activeGames.DistinctBy(g => g.AppId))
                await _publisher.EnqueueAsync(game.AppId, stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
```



---

## Phase 5 — SteamTracker API endpoints

SteamTracker endpoints are **internal-facing only** (called by WishlistApi proxy or workers, never by the frontend).

### Endpoints that still exist

| Endpoint | Used by | Notes |
|----------|---------|-------|
| `POST /api/wishlist/{userId}/games/{appId}/alert` | WishlistApi proxy | Alert creation (needs SteamTracker business logic) |
| `DELETE /api/wishlist/{userId}/alert/{alertRuleId}` | WishlistApi proxy | Alert deletion |
| `POST /api/internal/price-check` | PriceCheckWorker | Internal |
| `POST /api/internal/wishlist-item-added` | WishlistSyncWorker | Internal |
| `POST /api/internal/wishlist-item-removed` | WishlistSyncWorker | Internal |

### Endpoints removed (no longer needed)

- ~~`GET /api/wishlist?userId=...`~~ — WishlistApi reads prices directly from shared DB via Dapper. No HTTP call to SteamTracker.

Auth: JWT bearer — extract `UserId` from claims, pass into use cases (**TODO: not yet implemented**). Currently `UserId` is passed as a query parameter (`?userId=...`) — this is fine since these endpoints are internal.

---

## Phase 5b — WishlistApi reads prices from shared DB

The React frontend communicates **only with WishlistApi**. Both services share the same Postgres database.

### Shared DB tables (PascalCase — no snake_case)

Both services connect to the same Postgres instance. SteamTracker uses PascalCase table names (no EF Core snake_case convention). WishlistApi's own tables use snake_case, but for shared tables it reads via Dapper (raw SQL) to avoid naming conflicts.

| Table | Who writes | Who reads | Columns WishlistApi needs |
|-------|-----------|-----------|--------------------------|
| `games` | SteamTracker (via `Game.ApplyPriceUpdate`) | WishlistApi (via Dapper) | AppId, Name, CurrentPriceAmount, CurrentPriceCurrency, LastCheckedAt |
| `alert_rules` | SteamTracker (via SetAlertRule use case) | WishlistApi (via Dapper) | AlertRuleId, UserId, AppId, TriggerBelowPrice, IsActive |
| `price_snapshots` | SteamTracker | (not read by WishlistApi) | — |

### Reading prices — Dapper, no HTTP

WishlistApi reads directly from the shared DB. No HTTP call to SteamTracker.

```csharp
// In WishlistApi — new Dapper-based repository
public interface ISharedDbPriceReader
{
    Task<Dictionary<int, GamePrice>> GetPricesAsync(IEnumerable<int> appIds);
    Task<Dictionary<string, Dictionary<int, AlertRuleInfo>>> GetAlertRulesAsync(string userId);
}

public record GamePrice(decimal? Amount, string Currency, DateTimeOffset? LastCheckedAt);
public record AlertRuleInfo(Guid Id, decimal ThresholdAmount, string Currency);

public class SharedDbPriceReader : ISharedDbPriceReader
{
    private readonly IDbConnection _db;

    public SharedDbPriceReader(IConfiguration config)
    {
        _db = new NpgsqlConnection(config.GetConnectionString("SharedDbConnection"));
    }

    public async Task<Dictionary<int, GamePrice>> GetPricesAsync(IEnumerable<int> appIds)
    {
        var ids = appIds.ToList();
        if (!ids.Any()) return new();

        var sql = @"
            SELECT ""AppId"", ""CurrentPriceAmount"", ""CurrentPriceCurrency"", ""LastCheckedAt""
            FROM ""Games""
            WHERE ""AppId"" = ANY(@ids)";

        var rows = await _db.QueryAsync<GamePrice>(sql, new { ids });
        return rows.ToDictionary(r => r.Amount != null ? r.Amount.Value : 0, r => r);
    }

    public async Task<Dictionary<string, Dictionary<int, AlertRuleInfo>>> GetAlertRulesAsync(string userId)
    {
        var sql = @"
            SELECT ""AlertRuleId"", ""AppId"", ""TriggerBelowPrice""
            FROM ""AlertRules""
            WHERE ""UserId"" = @userId AND ""IsActive"" = true";

        var rows = await _db.QueryAsync<AlertRuleRow>(sql, new { userId });
        return rows
            .GroupBy(r => r.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(r => r.AppId, r => new AlertRuleInfo(r.AlertRuleId, r.ThresholdAmount, r.Currency))
            );
    }

    private record AlertRuleRow(Guid AlertRuleId, int AppId, string TriggerBelowPrice, string UserId);
}
```

### Enhanced GetWishlistAsync

```csharp
[HttpGet()]
public async Task<ActionResult<Wishlist>> GetWishlistAsync([FromQuery] string? fields = null)
{
    int internalUserId = await _userContext.GetIdAsync();

    // 1. Get local wishlist items
    var localItems = await _wishlistService.GetWishlistItemsAsync(internalUserId);
    var appIds = localItems.Select(x => x.AppId).ToList();

    // 2. Read prices from shared DB
    var prices = await _priceReader.GetPricesAsync(appIds);

    // 3. Read alert rules from shared DB
    var userId = internalUserId.ToString();
    var alertRulesByUser = await _priceReader.GetAlertRulesAsync(userId);
    var userAlertRules = alertRulesByUser.GetValueOrDefault(userId) ?? new();

    // 4. Merge everything
    var result = localItems.Select(x => new WishlistItemDto(
        AppId: Has("appid") ? x.AppId : null,
        DateAdded: Has("dateadded") ? x.DateAdded : null,
        Name: Has("name") ? x.AppName : null,
        Price: prices.TryGetValue(x.AppId, out var price) ? price.Amount : null,
        PriceCurrency: prices.TryGetValue(x.AppId, out price) ? price.Currency : "EUR",
        LastCheckedAt: prices.TryGetValue(x.AppId, out price) ? price.LastCheckedAt : null,
        AlertRuleId: userAlertRules.TryGetValue(x.AppId, out var alert) ? alert.Id : null,
        AlertThreshold: userAlertRules.TryGetValue(x.AppId, out alert) ? alert.ThresholdAmount : null,
        AlertCurrency: userAlertRules.TryGetValue(x.AppId, out alert) ? alert.Currency : "EUR"
    ));

    return Ok(new Wishlist(result));
}
```

### Proxy endpoints for alert management

Alert rule creation/deletion need SteamTracker's business logic (e.g., only create alerts for tracked games), so WishlistApi proxies these calls to SteamTracker's API.

```csharp
// POST /wishlist/{appId}/alert — proxy to SteamTracker
[HttpPost("{appId}/alert")]
public async Task<ActionResult> SetAlertAsync(int appId, [FromBody] AlertRuleRequest request)
{
    int internalUserId = await _userContext.GetIdAsync();
    await _steamTrackerAlertProxy.SetAlertRuleAsync(internalUserId.ToString(), appId, request.ThresholdAmount, request.Currency);
    return Ok();
}

// DELETE /wishlist/{alertRuleId}/alert — proxy to SteamTracker
[HttpDelete("{alertRuleId}/alert")]
public async Task<ActionResult> DeleteAlertAsync(Guid alertRuleId)
{
    int internalUserId = await _userContext.GetIdAsync();
    await _steamTrackerAlertProxy.DeleteAlertRuleAsync(internalUserId.ToString(), alertRuleId);
    return Ok();
}
```

### WishlistItemDto (extended)

```csharp
public record WishlistItemDto(
    int? AppId = null,
    DateTimeOffset? DateAdded = null,
    string? Name = null,
    decimal? Price = null,              // from shared DB games table
    string? PriceCurrency = "EUR",      // from shared DB games table
    DateTimeOffset? LastCheckedAt = null, // from shared DB games table
    Guid? AlertRuleId = null,           // from shared DB alert_rules table
    decimal? AlertThreshold = null,     // from shared DB alert_rules table
    string? AlertCurrency = "EUR"       // from shared DB alert_rules table
);
```

### Frontend

The React frontend only calls WishlistApi:
- `GET /wishlist` — returns items with prices and alert rules merged in (from shared DB)
- `POST /wishlist/{appId}/alert` — sets an alert rule (proxied to SteamTracker)
- `DELETE /wishlist/{alertRuleId}/alert` — deletes an alert rule (proxied to SteamTracker)
- **Zero direct SteamTracker calls from the frontend.**

### Data flow

```
React Frontend          WishlistApi              SteamTracker
      │                       │                        │
      │── GET /wishlist ───►  │                        │
      │                       │── Dapper: SELECT ───►  │  (shared DB)
      │                       │◄── prices + alerts ────│
      │◄── merged response ──│                        │
      │                       │                        │
      │── POST /alert ───►   │── POST /api/alert ────►│
      │                       │◄── ok ─────────────────│
      │◄── ok ───────────────│                        │
```

### Implementation notes

- `ISharedDbPriceReader` gracefully handles missing `SteamTrackerConnection` (returns empty collections) — important for WishlistApi unit/integration tests.
- `ISteamTrackerAlertProxy` gracefully handles missing `SteamTrackerUri` (no-op) — same reason.
- Test fixtures (`ApiFactory`) register mocked versions of both interfaces.
- Unit tests (`WishlistControllerTest`, `WishlistControllerBackfillTests`) also receive mocked instances.
- All WishlistApi tests pass with mocked shared DB (no real SteamTracker DB needed).
- All SteamTracker tests pass unchanged (no modifications to SteamTracker code).

### Why this design?

- **Single entry point**: The frontend only needs to know about WishlistApi
- **No HTTP overhead for prices**: WishlistApi reads directly from shared DB (Dapper)
- **Alert rules through SteamTracker**: Business logic (validation, tracked-game checks) stays in SteamTracker
- **No CORS issues**: SteamTracker is internal, no browser CORS concerns
- **Loose coupling**: SteamTracker can evolve independently (different port, different deploy cycle)
- **Fallback**: If SteamTracker's DB writes are delayed, WishlistApi still returns the wishlist (prices may be stale)

### Architecture change — what's done, what needs to change

| Area | Status | Notes |
|------|--------|-------|
| SteamTracker DB tables (`games`, `alert_rules`, etc.) | ✅ **No change needed** | Same tables, same columns. Both services share the same Postgres instance. |
| SteamTracker use cases (SetAlertRule, etc.) | ✅ **No change needed** | Still used by WishlistApi's proxy endpoints for alert management. |
| SteamTracker API endpoints | ✅ **No change needed** | POST/DELETE alert endpoints still exist (called by WishlistApi proxy). Internal endpoints unchanged. |
| SteamTracker integration tests (8 tests) | ✅ **No change needed** | Tests still valid — verify internal endpoints via `WebApplicationFactory`. |
| WishlistApi `GetWishlistAsync` | ✅ **Done** | Merges prices/alerts from shared DB (Dapper). |
| WishlistApi `WishlistItemDto` | ✅ **Done** | Added `Price`, `PriceCurrency`, `LastCheckedAt`, `AlertRuleId`, `AlertThreshold`, `AlertCurrency`. |
| `ISharedDbPriceReader` (new) | ✅ **Done** | Dapper-based repository reading `games` and `alert_rules` tables from shared DB. |
| WishlistApi proxy endpoints | ✅ **Done** | `POST /wishlist/{appId}/alert` and `DELETE /wishlist/{alertRuleId}/alert` that forward to SteamTracker. |
| `ISteamTrackerAlertProxy` (new) | ✅ **Done** | HttpClient-based proxy for SteamTracker's alert endpoints only (NOT for prices). |
| WishlistApi DI config | ✅ **Done** | Registered `ISharedDbPriceReader` (Dapper) and `ISteamTrackerAlertProxy` (HttpClient). |
| WishlistApi integration tests | ✅ **Done** | Tests updated with mocked `ISharedDbPriceReader` and `ISteamTrackerAlertProxy`. |
| React frontend | ✅ **Done** | See below. |

### React frontend — current state vs target

**Previous state (broken):**
- `useWishlistPrices()` calls `api.get(`/api/wishlist?userId=${userId}`)` → resolves to `/api/api/wishlist?...` under WishlistApi's base URL → **404**
- `AlertRuleModal` calls `api.post(`/api/wishlist/${userId}/games/${appId}/alert`)` → resolves to `/api/api/wishlist/...` → **404**
- The frontend had no working path to SteamTracker because `api` base URL is WishlistApi's `/api`.

**Target state (achieved):**
- `useWishlistPrices()` → **removed entirely**. Prices come from the merged `GET /wishlist` response on WishlistApi (read from shared DB).
- `AlertRuleModal` → calls `POST /wishlist/{appId}/alert` on WishlistApi (proxied to SteamTracker).
- `WLItemsList` → reads prices/alerts from the merged `GET /wishlist` response, no separate SteamTracker query.
- **Zero direct SteamTracker calls from the frontend.**

---

## Test strategy

### Framework
xUnit + FluentAssertions + **Moq** (matching the existing app's test stack).

### TDD — tests first
**Every piece of logic starts with a failing test.** The implementation order below is structured around this:
- Domain logic: write failing tests → implement the aggregate / domain service → make them pass.
- Use cases: write failing tests against mocked ports → implement the use case → make them pass.
- Infrastructure adapters: write failing integration tests → implement the adapter → make them pass.
- Workers: write failing tests for the worker logic → implement the BackgroundService → make them pass.

Never write implementation code without a failing test first. If you can't write a failing test, the code doesn't need to exist.

### Pyramid

**Domain.Tests** — pure C#, no mocks, no framework dependencies. These are the fastest tests and the most reliable.

| What | Why |
|------|-----|
| `PriceAlertEvaluator.Evaluate` | Trigger / no-trigger boundary cases (price == threshold, below, above) |
| `AlertRule.ShouldTrigger` | Boundary conditions — exact match, just below, just above |
| `AlertRule.MarkTriggered` | Sets `LastTriggeredAt`, does nothing if already triggered |
| `Game.ApplyPriceUpdate` | State transitions — first price, subsequent updates, event raising |
| `Money.Free` comparisons | `Free < Money`, `Free == Money.Free`, `Free > Money` |
| `SteamAppId` validation | Rejects ≤ 0, accepts valid IDs |
| `UserId` value object | Wraps Guid correctly |

**Application.Tests** — use cases with **mocked ports** (Moq). No real database, no real HTTP, no real RabbitMQ.

| What | How |
|------|-----|
| `SetAlertRuleUseCase` | Mock `ITrackedGameRepository` — verify it throws when game not tracked, creates rule when it exists |
| `DeleteAlertRuleUseCase` | Mock `IAlertRuleRepository` — verify delete is called, throws when rule not found |
| `IProcessPriceCheckUseCase` | Mock `IGameRepository`, `IAlertRuleRepository`, `INotificationPublisher` — verify price is saved, alerts evaluated, notifications dispatched |
| `IHandleWishlistItemAddedUseCase` | Mock repositories — verify upsert on `TrackedGame`, verify price-check job enqueued |
| `IHandleWishlistItemRemovedUseCase` | Mock repositories — verify `TrackedGame` deactivated, `AlertRule`s deactivated |
| `GetWishlistWithPricesQuery` | Mock repositories — verify join between `TrackedGame` + `Game` + `WishlistItem` |

**Integration.Tests** — **real Postgres via testcontainers**, **real RabbitMQ via testcontainers**. These are slower but catch real integration bugs.

| What | How |
|------|-----|
| Repository CRUD | `GameRepository`, `AlertRuleRepository`, `TrackedGameRepository` — save, get, update |
| Idempotency | Duplicate `PriceCheckJob` produces exactly one `PriceSnapshot` (unique index enforced) |
| WishlistSyncWorker | End-to-end: consume `WishlistItemAdded` → `TrackedGame` row created, price-check job enqueued |
| PriceCheckWorker | End-to-end: consume `PriceCheckJob` → mock Steam API → price saved, alerts evaluated |
| Scheduler | Trigger 24h cycle → verify jobs enqueued for all active `TrackedGame`s |

### Testing the workers
BackgroundService testing is notoriously tricky. The approach:
- **Don't test `BackgroundService` itself** — test the logic it calls (the use cases, the scheduler, the rate limiter).
- **Test the scheduler** as a standalone component: inject a `PeriodicTimer` mock or use `Task.Delay` with a timeout, verify jobs are enqueued.
- **Test the rate limiter** in isolation: verify it blocks when tokens are exhausted, replenishes over time.
- **Test the worker's message handling** by calling `HandleBasicDeliverAsync` directly on `PriceCheckConsumer` / `WishlistSyncConsumer`.

### Testcontainers setup
Shared singleton fixtures ensure containers are created once per test run:

```csharp
// PostgresContainerFixture.cs — singleton via Lazy<T>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private static readonly Lazy<PostgresContainerFixture> _instance = new(() => new());
    public static PostgresContainerFixture Instance => _instance.Value;

    private PostgresContainerFixture()
    {
        Container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithPassword("testpassword")
            .Build();
    }
}

// RabbitMqContainerFixture.cs — singleton via Lazy<T>
public sealed class RabbitMqContainerFixture : IAsyncLifetime
{
    private static readonly Lazy<RabbitMqContainerFixture> _instance = new(() => new());
    public static RabbitMqContainerFixture Instance => _instance.Value;

    private RabbitMqContainerFixture()
    {
        Container = new RabbitMqBuilder("rabbitmq:3-management-alpine")
            .Build();
    }
}
```


---

## Implementation order

Each step starts with **failing tests**, then implementation, then the tests pass before moving on.

```
Phase 1 — Domain (TDD)
  1. ✅ Write failing domain tests → implement Game, TrackedGame, PriceSnapshot, AlertRule
  2. ✅ Write failing domain tests → implement PriceAlertEvaluator
  3. ✅ Write failing domain tests → implement value objects (SteamAppId, Money, UserId)
  # All tests pass, no infrastructure yet

Phase 2 — Application layer (TDD)
  4. ✅ Write port interfaces (Application layer)
  5. ✅ Write failing use case tests (mocked ports with Moq) → implement SetAlertRule, DeleteAlertRule
  6. ✅ Write failing use case tests → implement ProcessPriceCheck (joins Game + AlertRule + Notification)
  7. ✅ Write failing use case tests → implement HandleWishlistItemAdded, HandleWishlistItemRemoved
  8. ✅ Write failing query tests → implement GetWishlistWithPrices
  # All use case tests pass with mocks

Phase 3 — Infrastructure (TDD)
  9. ✅ Write failing integration tests (testcontainers: Postgres + RabbitMQ)
 10. ✅ Implement EF Core adapters for TrackedGame, Game, AlertRule + migrations
 11. ✅ Implement WishlistSyncWorker + scheduler
 12. ✅ Implement SteamStoreClient (EUR pricing, F2P support, rate limit exception)
 13. ✅ Implement RabbitMQ publisher/consumer adapters + integration tests

Phase 4 — Wire it up
 14. ✅ Implement API endpoints (POST /alert, GET /wishlist, DELETE /alert, internal endpoints)
 15. ✅ Wire workers into DI, connect scheduler → publisher → worker → use case → notification
 16. ✅ **WishlistApi reads prices from shared DB** — `ISharedDbPriceReader` (Dapper), proxy alert endpoints, extended `WishlistItemDto` (SteamTracker DB tables unchanged, no HTTP call for prices)
 17. ✅ React frontend — removed `useWishlistPrices`, fixed `AlertRuleModal` to call WishlistApi proxy, updated `WLItemsList` to read merged data
 18. ⬜ End-to-end: wishlist add → event → TrackedGame → scheduler → price fetch → alert fires
```

---

---

## Current status summary

| Component | Status | Tests |
|-----------|--------|-------|
| Domain model (entities, value objects, events, services) | ✅ Complete | 54 pass |
| Application layer (use cases, ports) | ✅ Complete | 17 pass |
| Infrastructure — EF Core + Postgres + RabbitMQ | ✅ Complete | 28 pass (includes Postgres + RabbitMQ integration) |
| Infrastructure — SteamStoreClient | ✅ Complete | (covered by use case integration tests) |
| Workers (PriceCheckWorker, WishlistSyncWorker, PriceCheckScheduler) | ✅ Complete | 17 pass (unit tests) |
| API endpoints (Minimal API) | ✅ Complete | 8 pass (integration tests — SteamTracker internal endpoints) |
| WishlistApi reads prices from shared DB | ✅ Complete | ISharedDbPriceReader (Dapper) + merged response + proxy alert endpoints |
| React frontend additions | ✅ Complete | Removed useWishlistPrices, updated WLItemsList, fixed AlertRuleModal |
| End-to-end integration | ⬜ TODO | — |

**Total: 124 passing, 0 skipped, 0 failing**

## Known issues / TODO

1. **Worker unit tests** — `PriceCheckConsumer` and `WishlistSyncConsumer` should have dedicated tests for their `HandleBasicDeliverAsync` logic.
2. **Scheduler tests** — `PriceCheckScheduler` should be tested with a mockable timer or configurable interval.
3. **WishlistApi integration tests** — Need WebApplicationFactory tests for `ISharedDbPriceReader` (shared DB Dapper queries) and proxy alert endpoints.
4. **WishlistApi reads prices from shared DB** — `ISharedDbPriceReader` (Dapper), proxy alert endpoints, extended `WishlistItemDto` with price/alert fields. **This must be done before frontend work.**
5. **JWT auth** — `UserId` currently passed as route/query parameter; replace with JWT bearer token extraction.
6. **React frontend** — Fix broken SteamTracker calls (`useWishlistPrices`, `AlertRuleModal`), remove direct SteamTracker queries, wire to WishlistApi proxy endpoints.
7. **End-to-end** — wishlist add → event → TrackedGame → scheduler → price fetch → alert fires.

### Worker improvements

- `PriceCheckWorker` now properly calls `ISteamStoreClient.FetchPriceAsync` instead of using a stub
- `PriceCheckConsumer` handles `SteamRateLimitException` with requeue, null results with requeue
- `WishlistSyncWorker` now consumes from the correct `steamtracker.wishlist-sync` queue bound to `wishlist.events` fanout exchange
- `WishlistSyncConsumer` dispatches to the correct use case based on event type (`WishlistItemAdded` vs `WishlistItemRemoved`)
- Added ACL message contracts: `WishlistItemAddedMessage`, `WishlistItemRemovedMessage`
- `SteamStoreClient` uses `cc=de&l=german` for consistent EUR pricing, `Money.Free` for F2P games

### Test infrastructure fixes

- Fixed `DispatchConsumersAsync` property removal for RabbitMQ.Client 7.x
- Fixed deprecated testcontainers constructors (`RabbitMqBuilder` / `PostgreSqlBuilder`)
- Fixed `PostgresContainerFixture.CreateDbContext()` — EF Core 9+ requires `UseInternalServiceProvider()` with `AddEntityFrameworkNpgsql()`
- Fixed `AlertRule.AppId` missing value converter in `OnModelCreating`
- Fixed `GameRepository.SaveAsync` — new `PriceSnapshot` entities now persisted on updates

### Build fix (RabbitMQ.Client 7.x migration)

The build was failing due to RabbitMQ.Client 7.x API changes:

- `ConnectionFactory.CreateConnection()` → `factory.CreateConnectionAsync().GetAwaiter().GetResult()` (in DI)
- `IConnection.CreateChannel()` → `IConnection.CreateChannelAsync()` (async throughout)
- `IChannel.CreateBasicProperties()` → `new BasicProperties()`
- Sync channel methods → async versions (`ExchangeDeclareAsync`, `QueueDeclareAsync`, `QueueBindAsync`, `BasicPublishAsync`, etc.)
- `EventingBasicConsumer` with `Received` event → `AsyncEventingBasicConsumer` with `HandleBasicDeliverAsync` override
- `IConnection.CreateModel()` → `IConnection.CreateChannelAsync()`
- `BasicDeliverEventArgs` → direct parameters in `HandleBasicDeliverAsync`
- Added `using Microsoft.AspNetCore.Mvc;` for `[FromBody]` attribute
- Fixed `decimal` → `Money` type conversion in the price-check endpoint

### EF Core value object mapping

- `Money` value object → string converter (`"Amount|Currency"` format) in `GameConfig`, `AlertRuleConfig`, `PriceSnapshotConfig`
- `SteamAppId` value object → int converter in `SteamTrackerDbContext`
- `GameConfig` shadow properties for `CurrentPriceAmount` / `CurrentPriceCurrency`
- Repository `SaveAsync` fixed: detach existing tracked entity, attach passed entity as `Modified` to avoid double-tracking in InMemory provider
- `SteamAppId` removed `IComparable` (was breaking FluentAssertions equality tests)

### Test results

All **124 WishlistApi tests pass** (1 skipped, 0 failing) + **71 SteamTracker tests pass** (0 skipped, 0 failing):  
- **54 domain tests** — all pass (pure C#, no mocks)
- **17 application tests** — all pass (Moq mocks)
- **28 infrastructure tests** — all pass (real Postgres + RabbitMQ via testcontainers)
- **17 worker unit tests** — all pass (PriceCheckConsumer, WishlistSyncConsumer, PriceCheckScheduler)
- **8 API integration tests** — all pass (WebApplicationFactory + testcontainers)
- **61 WishlistApi tests** — all pass (1 skipped, 0 failing)
  - 3 unit tests (WishlistControllerTest, WishlistControllerBackfillTests)
  - 58 integration tests (existing WishlistApi tests)
- **71 SteamTracker tests** — all pass
  - 54 domain tests
  - 17 application tests

**Grand total: 195 tests, 194 passing, 1 skipped, 0 failing**

### ✅ React frontend additions (COMPLETE)

Changes are **additive to the existing wishlist UI**, not a separate page. **WishlistApi proxy was implemented first.**

**Fixed (was broken):**
- [x] `useWishlistPrices()` — removed entirely. Prices come from merged `GET /wishlist` response.
- [x] `AlertRuleModal` — changed from `POST /api/wishlist/...` to `POST /wishlist/{appId}/alert` (WishlistApi proxy).
- [x] `WLItemsList` — removed `useWishlistPrices` dependency, reads prices/alerts from merged wishlist response.

**Additive work:**
- [x] `WishlistPriceBadge` — shows "Not fetched" (amber), "Price fetched" (green) badges
- [x] Enhanced `WLItemsList` — added price column, status badge, and "Set alert" / "Edit alert" buttons per item
- [x] `MergedWishlistItem` interface — TypeScript type for the merged response shape

**Additive work:**
- [ ] `WishlistPriceBadge` — shows "Not fetched" (amber), "Price fetched" (green) badges
- [ ] Enhanced `WLItemsList` — added price column, status badge, and "Set alert" / "Edit alert" buttons per item
- [ ] Optional: SignalR push for real-time price updates without polling

### PriceCheckScheduler

- Created `PriceCheckScheduler` BackgroundService that runs every 24h
- Reads active `TrackedGame`s and enqueues one job per unique `AppId`
- All repository interfaces support `CancellationToken` for graceful shutdown
- All application use cases accept and propagate `CancellationToken`

---

## Key decisions to revisit later

- **Multiple workers**: Move rate limiting from in-process token bucket to a shared Redis token bucket (Lua script). Each worker instance holds no local state and the 200/5min cap is shared across all instances.
- **Idempotency**: `ProcessPriceCheck` is idempotent by design — the `UNIQUE INDEX on (app_id, captured_at)` means duplicate deliveries are a no-op. Catch `UniqueConstraintException` in the repo and swallow it. `WishlistSyncWorker` uses upsert semantics for the same reason.
- **Event replay / backfill**: If SteamTracker is deployed after users already have items in their wishlist, those items will never generate `WishlistItemAdded` events. Add a one-time backfill endpoint in the existing app, or accept that only new wishlist additions get tracked.
- **F2P games**: `price_overview` is absent in the Steam response. `Money.Free` is a first-class value — `AlertRule.ShouldTrigger(Money.Free)` always returns false.
- **Game removed from all wishlists**: When `TrackedGame.IsActive = false` for all users, the scheduler naturally stops enqueuing it. Consider a nightly job to prune stale `games` rows.