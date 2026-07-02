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
- [x] `SteamStoreHttpClient` with EUR-consistent pricing (`cc=de&l=german`), F2P support (`Money.Free`), rate limit exception
- [x] RabbitMQ publishers: `PriceCheckJobPublisher`, `NotificationPublisher` (async API, RabbitMQ.Client 7.x)
- [x] DI registration for all infrastructure services
- [x] All 21 infrastructure tests pass (InMemory + real scenarios)
- [x] Fixed `SkipTestException` shared between test files
- [x] Fixed `DispatchConsumersAsync` removal for RabbitMQ.Client 7.x
- [x] Fixed deprecated testcontainers constructors

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

## Phase 4 — Workers (BackgroundServices)

### PriceCheckWorker

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

- `PriceCheckScheduler` — runs every 24h, enqueues jobs for active tracked games
- `PriceCheckWorker` — consumes price-check jobs, calls `ISteamStoreClient.FetchPriceAsync`, triggers use cases
- `WishlistSyncWorker` — consumes from `steamtracker.wishlist-sync` queue bound to `wishlist.events` fanout exchange, dispatches to `HandleWishlistItemAddedUseCase` / `HandleWishlistItemRemovedUseCase`

All three registered in `Program.cs` via `AddHostedService<T>()`.

### Scheduler

Every 24h, reads `ITrackedGameRepository.GetActive()` and enqueues **one job per unique AppId**:

```csharp
var activeAppIds = await _trackedGameRepo.GetActive();
foreach (var game in activeAppIds.DistinctBy(g => g.AppId))
    await _publisher.Enqueue(game.AppId);
```

---

## Phase 5 — API surface

```
# Alert rules (validated against local TrackedGame)
POST   /wishlist/{appId}/alert-rule       → SetAlertRuleUseCase  { triggerBelowPrice }
DELETE /wishlist/{appId}/alert-rule/{id}  → DeleteAlertRuleUseCase

# Enriched wishlist view (local TrackedGame + Game price data, filtered by UserId)
GET    /wishlist/prices                   → GetWishlistWithPricesQuery
```

Auth: JWT bearer — extract `UserId` from claims, pass into use cases.

---

## Phase 6 — React frontend

Changes are **additive to the existing wishlist UI**, not a separate page:

- Add a price badge to each existing `WishlistItem` card: green (on sale), gray (full price), amber (not yet fetched)
- Add a "Set alert" button per item → `AlertRuleModal` with price threshold input
- `useWishlistPrices()` hook — fetches `GET /wishlist/prices`, merges with existing wishlist state
- Optional: SignalR push for real-time price updates without polling

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
- **Test the worker's message handling** by calling `ProcessMessage` directly (extract the handler into a separate method or interface that the worker delegates to).

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
 11. ✅ Implement WishlistSyncWorker + rate limiter + scheduler
 12. ✅ Implement SteamStoreHttpClient (EUR pricing, F2P support, rate limit exception)
 13. ✅ Implement RabbitMQ publisher/consumer adapters + integration tests

Phase 4 — Wire it up
 14. ✅ Implement API controllers (POST /alert-rule, GET /wishlist/prices)
 15. ✅ Wire workers into DI, connect scheduler → publisher → worker → use case → notification
 16. ✅ React frontend additions to existing wishlist UI
 17. ⬜ End-to-end: wishlist add → event → TrackedGame → scheduler → price fetch → alert fires
```

---

## Completed

### Worker improvements

- `PriceCheckWorker` now properly calls `ISteamStoreClient.FetchPriceAsync` instead of using a stub
- `PriceCheckConsumer` handles `SteamRateLimitException` with requeue, null results with requeue
- `WishlistSyncWorker` now consumes from the correct `steamtracker.wishlist-sync` queue bound to `wishlist.events` fanout exchange
- `WishlistSyncConsumer` dispatches to the correct use case based on event type (`WishlistItemAdded` vs `WishlistItemRemoved`)
- Added ACL message contracts: `WishlistItemAddedMessage`, `WishlistItemRemovedMessage`
- `SteamStoreClient` uses `cc=de&l=german` for consistent EUR pricing, `Money.Free` for F2P games

### Test infrastructure fixes

- Fixed `SkipTestException` shared between `PostgresRepositoryIntegrationTests` and `RabbitMqIntegrationTests`
- Fixed `DispatchConsumersAsync` property removal for RabbitMQ.Client 7.x
- Fixed deprecated testcontainers constructors (`RabbitMqBuilder` / `PostgreSqlBuilder`)

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

All **94 tests pass**:
- 55 domain tests
- 18 application tests
- 21 infrastructure tests (InMemory + real scenarios)

### ✅ React frontend additions

- `useWishlistPrices()` hook — fetches `GET /api/wishlist?userId=...` from SteamTracker API
- `WishlistPriceBadge` — shows "Not fetched" (amber), "Price fetched" (green) badges
- `AlertRuleModal` — modal dialog with price threshold input, calls `POST /api/wishlist/{userId}/games/{appId}/alert`
- Enhanced `WLItemsList` — added price column, status badge, and "Set alert" / "Edit alert" buttons per item

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