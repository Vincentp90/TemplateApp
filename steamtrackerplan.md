# SteamTracker — implementation plan

Hexagonal (ports & adapters) DDD with C# API + React frontend + RabbitMQ single worker.

---

## Project structure

```
SteamTracker/
├── src/
│   ├── SteamTracker.Domain/          # Pure domain — no framework dependencies
│   ├── SteamTracker.Application/     # Use cases + port interfaces
│   ├── SteamTracker.Infrastructure/  # Adapters: EF, RabbitMQ, Steam HTTP
│   ├── SteamTracker.API/             # ASP.NET Web API (primary adapter)
│   └── SteamTracker.Worker/          # BackgroundService (primary adapter)
└── tests/
    ├── SteamTracker.Domain.Tests/
    ├── SteamTracker.Application.Tests/
    └── SteamTracker.Integration.Tests/
```

The key hexagonal rule: **Domain and Application have zero references to Infrastructure.** All cross-boundary communication goes through port interfaces defined in Application.

---

## Phase 1 — Domain model

**Aggregates / entities**

```
Game (aggregate root)
  - AppId           : SteamAppId (value object)
  - Name            : string
  - CurrentPrice    : Money? (value object — nullable until first fetch)
  - LastCheckedAt   : DateTimeOffset?

PriceSnapshot (entity, child of Game)
  - SnapshotId      : Guid
  - Price           : Money
  - DiscountPercent : int
  - CapturedAt      : DateTimeOffset

AlertRule (aggregate root)
  - AlertRuleId     : Guid
  - UserId          : UserId (value object)
  - AppId           : SteamAppId
  - TriggerBelowPrice : Money          # alert fires when price <= this
  - IsActive        : bool
  - LastTriggeredAt : DateTimeOffset?

  + bool ShouldTrigger(Money currentPrice)
  + void MarkTriggered(DateTimeOffset at)
```

**Value objects**

```
SteamAppId  — wraps int, validates > 0
Money       — Amount (decimal) + Currency (ISO string, default "EUR")
UserId      — wraps Guid
```

**Domain events** (raised by aggregates, published by infrastructure)

```
PriceUpdatedEvent    { AppId, OldPrice, NewPrice, CapturedAt }
AlertTriggeredEvent  { AlertRuleId, UserId, AppId, Price }
```

**Domain service**

```
PriceAlertEvaluator
  + IEnumerable<AlertRule> Evaluate(Game game, IEnumerable<AlertRule> rules)
  # pure logic — no I/O
```

---

## Phase 2 — Application layer (ports)

### Driving ports (called by API / Worker)

```csharp
// Use cases the API exposes
IWatchGameUseCase           WatchGame(UserId, SteamAppId)
IUnwatchGameUseCase         UnwatchGame(UserId, SteamAppId)
ISetAlertRuleUseCase        SetAlertRule(UserId, SteamAppId, Money threshold)
IGetWatchlistQueryHandler   GetWatchlist(UserId) → WatchlistDto[]

// Use case the Worker calls after consuming a job
IProcessPriceCheckUseCase   ProcessPriceCheck(SteamAppId, Money fetchedPrice)
```

### Driven ports (implemented by Infrastructure)

```csharp
// Persistence
IGameRepository             Get(SteamAppId), Save(Game)
IAlertRuleRepository        GetActiveRulesFor(SteamAppId), Save(AlertRule)

// Messaging
IPriceCheckJobPublisher     Enqueue(SteamAppId)       // used by scheduler
INotificationPublisher      Notify(AlertTriggeredEvent)

// External
ISteamStoreClient           FetchPrice(SteamAppId) → Money
```

---

## Phase 3 — Infrastructure adapters

### Persistence — EF Core + Postgres

- One `DbContext` in Infrastructure, maps aggregates to tables
- `GameRepository` and `AlertRuleRepository` implement their ports
- Migrations: separate project or EF migrations inside Infrastructure
- Price snapshots stored as append-only table (no updates, only inserts)

**Schema (simplified)**

```
games              (app_id PK, name, current_price, currency, last_checked_at)
price_snapshots    (id PK, app_id FK, price, currency, discount_pct, captured_at)
alert_rules        (id PK, user_id, app_id, trigger_below, currency, is_active, last_triggered_at)
```

### Messaging — RabbitMQ

**Exchange / queue layout**

```
Exchange: steamtracker.direct
  → Queue: price-check-jobs     (worker consumes)
  → Queue: notifications        (notification service consumes)

Dead-letter exchange: steamtracker.dlx
  → Queue: price-check-dead     (failed jobs land here for inspection)
```

**Message contracts** (record types in Application, serialized as JSON)

```csharp
record PriceCheckJob(int AppId, DateTimeOffset EnqueuedAt);
record NotificationMessage(Guid AlertRuleId, string UserId, int AppId, decimal Price, string Currency);
```

**`RabbitMqPriceCheckJobPublisher`** — implements `IPriceCheckJobPublisher`

**`RabbitMqNotificationPublisher`** — implements `INotificationPublisher`

### Steam HTTP adapter

**`SteamStoreHttpClient`** — implements `ISteamStoreClient`

- Calls `https://store.steampowered.com/api/appdetails?appids={id}&filters=price_overview`
- Maps `price_overview.final` (in cents) to `Money`
- Returns `null` if game is free or not yet released (handle in use case)
- Throws `SteamRateLimitException` on 429 (caught by Worker, message nacked with delay)

---

## Phase 4 — Worker (BackgroundService)

```csharp
public class PriceCheckWorker : BackgroundService
{
    // Consumes from RabbitMQ
    // For each message:
    //   1. Call ISteamStoreClient.FetchPrice(appId)
    //   2. On success  → IProcessPriceCheckUseCase.ProcessPriceCheck(appId, price), ack message
    //   3. On 429      → nack with requeue after delay (use RabbitMQ TTL dead-letter trick)
    //   4. On 5xx/timeout → nack, let dead-letter queue catch it after N retries
}
```

**Rate limiting strategy** — token bucket inside the worker

```csharp
// SemaphoreSlim or System.Threading.RateLimiting (built-in .NET 7+)
using var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
{
    TokenLimit = 180,           // stay below 200 hard limit
    ReplenishmentPeriod = TimeSpan.FromMinutes(5),
    TokensPerPeriod = 180,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    QueueLimit = 0
});
```

**Retry policy** — Polly (or built-in resilience via Microsoft.Extensions.Http.Resilience)

```
Retry: 3 attempts, exponential backoff (2s, 4s, 8s) for transient HTTP errors
Circuit breaker: open after 5 consecutive failures, half-open after 30s
```

### Scheduler

Use `PeriodicTimer` (simple) or Quartz.NET (if you need per-game intervals):

```csharp
// Simple version: every 24h, enqueue all watched games
// Advanced version: Quartz.NET job reads watched game list and enqueues in batches
```

---

## Phase 5 — API surface

```
POST   /watchlist/{appId}          → WatchGameUseCase
DELETE /watchlist/{appId}          → UnwatchGameUseCase
GET    /watchlist                   → GetWatchlistQueryHandler
POST   /alert-rules                 → SetAlertRuleUseCase  { appId, triggerBelowPrice }
DELETE /alert-rules/{id}            → DeleteAlertRuleUseCase
```

Auth: JWT bearer (existing setup in your app — just add `[Authorize]` + extract `UserId` from claims).

---

## Phase 6 — React frontend

Minimal additions to existing app:

- `WatchlistPage` — lists watched games with current price + last checked timestamp
- `AlertRuleModal` — set a price threshold per game
- `useWatchlist()` hook — fetches `/watchlist`, polls every 60s or uses SignalR for push
- Price displayed as badge: green (on sale), gray (full price), amber (price unknown)

Optional: SignalR hub in the API that pushes `PriceUpdatedEvent` to connected clients so the UI updates without polling.

---

## Implementation order

```
1. Domain model + domain tests (no infra, pure C#)
2. Port interfaces in Application
3. Use case implementations + unit tests (mock ports)
4. EF Core + Postgres adapter + migration
5. SteamStoreHttpClient adapter + integration test (real HTTP, sandboxed)
6. RabbitMQ adapters (publisher + consumer)
7. Worker BackgroundService with rate limiter
8. API controllers wired to use cases
9. React frontend additions
10. End-to-end test: enqueue → worker fetches → alert fires → notification sent
```

---

## Key decisions to revisit later

- **Multiple workers**: When you scale out, move rate limiting from the worker process into a shared token bucket (Redis + Lua script or a dedicated rate-limit microservice). Each worker instance then holds no local state.
- **Idempotency**: `ProcessPriceCheck` should be idempotent — if the same job is delivered twice (RabbitMQ at-least-once), inserting a duplicate `PriceSnapshot` with the same `(app_id, captured_at)` should be a no-op. Add a unique index on `(app_id, captured_at)` and catch `UniqueConstraintException` in the repo.
- **Currency**: Steam returns prices in the store's local currency based on IP. Consider fixing the worker's outbound IP or passing `cc=de&l=german` query params to get EUR consistently.
- **Free-to-play games**: `price_overview` is absent in the API response. Domain should model `Money.Free` explicitly rather than nullable.