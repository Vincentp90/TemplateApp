# Price Retrieval Flow

## Overview

When a user adds a game to their wishlist, the system triggers an async price-check pipeline that fetches the current Steam price, stores it, and evaluates alert rules. The flow spans two services — **WishlistApi** (existing app) and **SteamTracker** (price-tracking service) — connected via RabbitMQ.

---

## Step-by-Step Flow

### 1. Frontend → WishlistApi: User adds an item

The user clicks "Add to Wishlist" in the React frontend. The frontend calls `POST /wishlist/{appId}` on **WishlistApi**.

```
React Frontend  ──POST /wishlist/{appId}──►  WishlistApi
```

### 2. WishlistApi: Stores locally + publishes to RabbitMQ

`WishlistController.AddWishlistItemAsync()` calls `_wishlistService.AddToWishlistAsync()`, which:

- Persists the item to the **shared Postgres** (local wishlist table)
- Publishes a `WishlistItemAdded` event to the `wishlist.events` **fanout exchange** on RabbitMQ

```
WishlistApi  ──RabbitMQ (fanout)──►  wishlist.events  ──►  steamtracker.wishlist-sync
```

### 3. SteamTracker: WishlistSyncWorker picks up the event

The **`WishlistSyncWorker`** (a `BackgroundService`) subscribes to the `steamtracker.wishlist-sync` queue bound to the `wishlist.events` fanout exchange.

When a `WishlistItemAdded` message arrives, `WishlistSyncConsumer` deserializes it and calls:

```csharp
_addedUseCase.ExecuteAsync(userId, appId, addedAt);
```

### 4. HandleWishlistItemAddedUseCase: Upsert + enqueue price check

`HandleWishlistItemAddedUseCase.ExecuteAsync()`:

1. Checks `ITrackedGameRepository` — if the game is already being tracked (idempotent guard), returns early
2. Creates a `TrackedGame` entity via `TrackedGame.StartTracking(appId, addedAt)` and saves it
3. Publishes a **price-check job** to RabbitMQ:

```
HandleWishlistItemAddedUseCase  ──RabbitMQ (direct)──►  steamtracker.pricecheck  ──►  pricecheck.jobs
```

The message body is a `PriceCheckMessage { AppId, EnqueuedAt }`.

### 5. SteamTracker: PriceCheckWorker fetches the price

The **`PriceCheckWorker`** (another `BackgroundService`) consumes from the `pricecheck.jobs` queue.

When a job arrives, `PriceCheckConsumer`:

1. Deserializes the `PriceCheckMessage`
2. Calls `ISteamStoreClient.FetchPriceAsync(appId)` → hits `https://store.steampowered.com/api/appdetails?appids={appId}&cc=de&l=german`
3. Parses the JSON response to extract `(Money price, string name)`
4. On success: calls `IProcessPriceCheckUseCase.ExecuteAsync(appId, price, name)` and **acks** the message
5. On failure (e.g., Steam rate limit 429, HTTP error): **nacks and requeues** for retry

### 6. ProcessPriceCheckUseCase: Save price + evaluate alerts

`ProcessPriceCheckUseCase.ExecuteAsync()`:

1. Loads (or creates) the `Game` aggregate for the `appId`
2. Calls `game.ApplyPriceUpdate(price, name, now)` — this updates `CurrentPrice`, `Name`, `LastCheckedAt`, and appends a `PriceSnapshot`
3. Saves the `Game` (with its new `PriceSnapshot`) to the DB
4. Fetches all active `AlertRule`s for that game
5. Runs `PriceAlertEvaluator.Evaluate(game, rules)` — pure domain logic: `currentPrice <= triggerBelowPrice`
6. For each triggered rule:
   - Marks it as triggered (`LastTriggeredAt`)
   - Saves the rule
   - Publishes a notification via `INotificationPublisher`

### 7. NotificationPublisher: Alert notification

The `NotificationPublisher` publishes an alert message to the `steamtracker.notifications` **topic exchange** with routing key `alert.triggered`:

```json
{
  "AlertRuleId": "...",
  "UserId": "...",
  "AppId": 12345,
  "Price": 19.99,
  "Currency": "EUR",
  "TriggeredAt": "2026-07-05T..."
}
```

### 8. Frontend: Price appears in the wishlist

When the user reloads the wishlist page, the frontend calls `GET /wishlist`. The `WishlistController`:

1. Reads local wishlist items from the shared DB
2. Reads prices from SteamTracker's `Games` table via `ISharedDbPriceReader` (Dapper, raw SQL)
3. Reads alert rules from SteamTracker's `AlertRules` table
4. Merges everything into `WishlistItemDto` with price, currency, last checked timestamp, and alert info

---

## Visual Summary

```
┌──────────┐   POST /wishlist/{id}    ┌────────────┐   RabbitMQ      ┌──────────────────┐
│  React   │ ──────────────────────►  │ WishlistApi│ ──────────────► │ WishlistSyncWorker │
│ Frontend │                          │            │   fanout exchange│ (BackgroundService)│
└──────────┘                          └────────────┘                  └────────┬───────────┘
                                                                            │
                                                                            ▼
                                                                     HandleWishlistItemAdded
                                                                     (upsert TrackedGame)
                                                                            │
                                            RabbitMQ (direct)               ▼
                                            ◄───────────────────  IPriceCheckJobPublisher
                                                                     EnqueueAsync
                                                                            │
                                                                            ▼
                                                                   ┌──────────────────┐
                                                                   │ PriceCheckWorker │
                                                                   │ (BackgroundSvc)  │
                                                                   └────────┬─────────┘
                                                                            │
                                                                            ▼
                                                                   SteamStoreClient
                                                                   (HTTP → Steam API)
                                                                            │
                                                                            ▼
                                                              ProcessPriceCheckUseCase
                                                              (save Game + PriceSnapshot)
                                                                            │
                                              ┌─────────────────────────────┴──────────────────┐
                                              ▼                                            ▼
                                      INotificationPublisher                    ISharedDbPriceReader
                                      (RabbitMQ topic exchange)               (Dapper → shared DB)
                                              │                                            │
                                              ▼                                            ▼
                                    Alert notification                           Price appears
                                    (external system)                            in frontend UI
```

---

## Key Architecture Points

| Concept | Detail |
|---|---|
| **ACL boundary** | RabbitMQ separates WishlistApi (existing app) from SteamTracker (new bounded context) |
| **Idempotency** | `HandleWishlistItemAddedUseCase` checks `IsActive` — no-op if already tracking |
| **Retry** | Transient errors (rate limits, timeouts) cause nack+requeue via `WorkerHelpers.IsTransient()` |
| **Shared DB** | Both services read/write the same Postgres, but SteamTracker owns `Games`, `TrackedGames`, `AlertRules` tables |
| **Hexagonal** | SteamTracker uses ports & adapters — all external deps (RabbitMQ, Steam API, DB) are driven ports |

---

## Relevant Files

| Component | Path |
|---|---|
| WishlistController | `WishlistApi/WishlistApi/Controllers/WishlistController.cs` |
| WishlistItemAdded event | `WishlistApi/Application/Events/WishlistItemAdded.cs` |
| WishlistSyncWorker | `SteamTracker/src/SteamTracker.Worker/Worker.cs` |
| HandleWishlistItemAddedUseCase | `SteamTracker/src/SteamTracker.Application/UseCases/HandleWishlistItemAddedUseCase.cs` |
| PriceCheckJobPublisher | `SteamTracker/src/SteamTracker.Infrastructure/Messaging/PriceCheckJobPublisher.cs` |
| PriceCheckWorker | `SteamTracker/src/SteamTracker.Worker/Worker.cs` |
| SteamStoreClient | `SteamTracker/src/SteamTracker.Infrastructure/External/SteamStoreClient.cs` |
| ProcessPriceCheckUseCase | `SteamTracker/src/SteamTracker.Application/UseCases/ProcessPriceCheckUseCase.cs` |
| PriceAlertEvaluator | `SteamTracker/src/SteamTracker.Domain/Services/PriceAlertEvaluator.cs` |
| NotificationPublisher | `SteamTracker/src/SteamTracker.Infrastructure/Messaging/NotificationPublisher.cs` |
| SharedDbPriceReader | `WishlistApi/Infrastructure/SharedDb/SharedDbPriceReader.cs` |
| ISharedDbPriceReader | `WishlistApi/Application/Contracts/ISharedDbPriceReader.cs` |
