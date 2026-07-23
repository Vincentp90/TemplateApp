# SteamTracker (.NET) Project Overview

## Technology Stack
- **Framework**: ASP.NET Core 10 (.NET 10)
- **Language**: C# (C# 14)
- **Database**: PostgreSQL with Entity Framework Core
- **ORM Convention**: snake_case naming (via `NpgsqlSnakeCaseNamingConvention`)
- **Messaging**: RabbitMQ (RabbitMQ.Client 7.x)
- **HTTP Client**: `HttpClient` (Steam Store API)
- **Testing**: xUnit + FluentAssertions + Moq + testcontainers
- **Architecture**: Hexagonal (Ports & Adapters) + DDD

### Development guidelines
- When using a primary constructor, don't add private fields, just use the parameter directly
- This is .NET 10 project so use .NET 10 features if suitable
- Run tests with this command: `dotnet test api/SteamTracker/SteamTracker.slnx 2>&1`

## Architecture (Hexagonal / Ports & Adapters)
```
SteamTracker.slnx
├── SteamTracker.Domain/               # Pure domain — zero framework dependencies
│   ├── Entities
│   │   ├── TrackedGame.cs             # Aggregate root — "should we track this AppId?"
│   │   ├── Game.cs                    # Aggregate root — "what is the current price?"
│   │   ├── PriceSnapshot.cs           # Child entity of Game (append-only)
│   │   └── AlertRule.cs              # Aggregate root — per user, per game
│   ├── ValueObjects
│   │   ├── SteamAppId.cs              # wraps int, validates > 0
│   │   ├── Money.cs                   # Amount (decimal) + Currency (ISO string)
│   │   └── CurrencyCode.cs            # ISO 4217 currency code validation
│   ├── Exceptions
│   │   ├── AlertRuleNotFoundException.cs
│   │   └── TrackingNotFoundException.cs
│   └── Services
│       └── PriceAlertEvaluator.cs     # Pure alert trigger logic
│
├── SteamTracker.Application/          # Use cases + port interfaces
│   ├── Ports                          # Driven & driving interfaces
│   │   ├── ITrackedGameRepository.cs
│   │   ├── IGameRepository.cs
│   │   ├── IAlertRuleRepository.cs
│   │   ├── ISteamStoreClient.cs       # External (driven)
│   │   ├── IPriceCheckJobPublisher.cs # External (driven)
│   │   ├── INotificationPublisher.cs  # External (driven)
│   │   ├── ISetAlertRuleUseCase.cs    # Driving
│   │   ├── IDeleteAlertRuleUseCase.cs # Driving
│   │   ├── IProcessPriceCheckUseCase.cs    # Driving
│   │   ├── IHandleWishlistItemAddedUseCase.cs
│   │   ├── IHandleWishlistItemRemovedUseCase.cs
│   │   └── SteamPriceResult.cs        # Steam API response model
│   └── UseCases/                      # Implementation of driving ports
│       ├── SetAlertRuleUseCase.cs
│       ├── DeleteAlertRuleUseCase.cs
│       ├── ProcessPriceCheckUseCase.cs
│       ├── HandleWishlistItemAddedUseCase.cs
│       └── HandleWishlistItemRemovedUseCase.cs
│
├── SteamTracker.Infrastructure/       # Adapters: EF, RabbitMQ, Steam HTTP
│   ├── Data/
│   │   ├── SteamTrackerDbContext.cs   # EF Core DbContext
│   │   └── Config/                    # Entity configurations
│   │       ├── TrackedGameConfig.cs
│   │       ├── GameConfig.cs
│   │       ├── PriceSnapshotConfig.cs
│   │       └── AlertRuleConfig.cs
│   ├── Repositories/                  # Port implementations (driven)
│   │   ├── TrackedGameRepository.cs
│   │   ├── GameRepository.cs
│   │   └── AlertRuleRepository.cs
│   ├── External/
│   │   ├── SteamStoreClient.cs        # HTTP adapter (EUR pricing, F2P support)
│   │   └── SteamRateLimitException.cs
│   ├── Messaging/
│   │   ├── PriceCheckJobPublisher.cs  # RabbitMQ publisher
│   │   └── NotificationPublisher.cs   # RabbitMQ publisher
│   ├── Migrations/
│   │   └── 20260711114733_InitialCreate.cs
│   └── DependencyInjection.cs         # DI registration
│
├── SteamTracker.API/                  # Minimal API (internal-facing)
│   ├── Program.cs                     # Startup, DI, minimal API endpoints
│   └── ExceptionHandler.cs
│
├── SteamTracker.Worker/               # BackgroundServices
│   ├── Program.cs                     # Host builder, DI, migration
│   ├── PriceCheckScheduler.cs         # 24h scheduler
│   ├── PriceCheckConsumer.cs          # RabbitMQ consumer for price-check jobs
│   ├── PriceCheckWorker.cs            # Fetches price from Steam API
│   ├── WishlistSyncConsumer.cs        # RabbitMQ consumer for wishlist events
│   ├── WishlistSyncWorker.cs          # Handles wishlist add/remove events
│   ├── MessageContracts.cs            # Message DTOs (PriceCheckMessage, etc.)
│   └── WorkerHelpers.cs               # Transient error detection
│
├── Tests/
│   ├── SteamTracker.Domain.Tests/     # 63 tests — pure C#, no mocks
│   │   ├── GameTests.cs
│   │   ├── TrackedGameTests.cs
│   │   ├── AlertRuleTests.cs
│   │   ├── PriceSnapshotTests.cs
│   │   ├── PriceAlertEvaluatorTests.cs
│   │   ├── MoneyTests.cs
│   │   ├── SteamAppIdTests.cs
│   │   └── CurrencyCodeTests.cs
│   ├── SteamTracker.Application.Tests/ # 21 tests — Moq mocks
│   │   ├── SetAlertRuleUseCaseTests.cs
│   │   ├── DeleteAlertRuleUseCaseTests.cs
│   │   ├── ProcessPriceCheckUseCaseTests.cs
│   │   ├── HandleWishlistItemAddedUseCaseTests.cs
│   │   └── HandleWishlistItemRemovedUseCaseTests.cs
│   ├── SteamTracker.Infrastructure.Tests/ # 38 tests — testcontainers
│   │   ├── Repositories/
│   │   │   ├── AlertRuleRepositoryTests.cs
│   │   │   ├── GameRepositoryTests.cs
│   │   │   └── TrackedGameRepositoryTests.cs
│   │   │   └── PostgresRepositoryIntegrationTests.cs
│   │   ├── Messaging/
│   │   │   └── RabbitMqIntegrationTests.cs
│   │   ├── External/
│   │   │   └── SteamStoreClientTests.cs
│   │   ├── TestContainers/
│   │   │   ├── PostgresContainerFixture.cs
│   │   │   └── RabbitMqContainerFixture.cs
│   │   ├── TestDbContextFactory.cs
│   │   └── UseCasesIntegrationTests.cs
│   ├── SteamTracker.API.Tests/        # 3 tests — WebApplicationFactory
│   │   ├── WishlistApiIntegrationTests.cs
│   │   └── Helpers/TestApiFactory.cs
│   ├── SteamTracker.Integration.Tests/ # 3 tests — E2E (testcontainers)
│   │   ├── E2E/
│   │   │   └── WishlistToAlertEndToEndTests.cs
│   │   └── TestContainers/
│   │       ├── PostgresContainerFixture.cs
│   │       └── RabbitMqContainerFixture.cs
│   └── SteamTracker.Worker.Tests/     # 48 tests — unit
│       ├── PriceCheckConsumerTests.cs
│       ├── PriceCheckSchedulerTests.cs
│       ├── PriceCheckWorkerTests.cs
│       ├── WishlistSyncConsumerTests.cs
│       ├── WishlistSyncWorkerTests.cs
│       └── MessageContractTests.cs
│
└── docker-compose.yml (dev container shared)
```

## API Endpoints

### Internal API (`/api`)
All endpoints are **internal-facing only** — called by WishlistApi proxy or workers, never by the frontend.

| Method | Path | Used by | Description |
|--------|------|---------|-------------|
| POST | `/api/games/{appId}/alert` | WishlistApi proxy | Create alert rule (threshold/currency from query params) |
| DELETE | `/api/alert/{alertRuleId}` | WishlistApi proxy | Delete alert rule |
| POST | `/api/internal/price-check` | PriceCheckWorker | Process a price check result |
| POST | `/api/internal/wishlist-item-added` | WishlistSyncWorker | Handle wishlist item added event |
| POST | `/api/internal/wishlist-item-removed` | WishlistSyncWorker | Handle wishlist item removed event |

### Background Workers (no HTTP endpoints)

| Worker | Queue Consumed | Description |
|--------|---------------|-------------|
| `PriceCheckScheduler` | — | Every 24h, enqueues one `PriceCheckJob` per active `TrackedGame` |
| `PriceCheckConsumer` + `PriceCheckWorker` | `price-check-jobs` | Fetches price from Steam API, processes via use case, ack/nack |
| `WishlistSyncConsumer` + `WishlistSyncWorker` | `steamtracker.wishlist-sync` | Consumes `WishlistItemAdded`/`Removed` events from existing app |

### RabbitMQ Topology
```
# Inbound from existing app (ACL boundary)
Exchange: wishlist.events (fanout)
  → Queue: steamtracker.wishlist-sync

# Internal to SteamTracker
Exchange: steamtracker.direct (direct)
  → Queue: price-check-jobs
  → Queue: notifications

Dead-letter exchange: steamtracker.dlx
  → Queue: price-check-dead
  → Queue: wishlist-sync-dead
```

## Domain Model
- **TrackedGame** (Aggregate Root): Local ACL projection of `WishlistItem` — determines *what* to track. Has `IsActive` flag.
- **Game** (Aggregate Root): Price data for a Steam AppId — one per unique game. Contains `CurrentPrice`, `LastCheckedAt`, and append-only `PriceSnapshot` children.
- **PriceSnapshot** (Child Entity): Append-only price history per game. Unique index on `(app_id, captured_at)` for idempotency.
- **AlertRule** (Aggregate Root): Per user, per game alert — triggers when `currentPrice < triggerBelowPrice`.

### Value Objects
- **SteamAppId** — wraps `int`, validates > 0
- **Money** — `Amount` (decimal) + `Currency` (ISO string, default "EUR") + `Money.Free` for F2P games
- **CurrencyCode** — ISO 4217 currency code validation

### Exceptions
- **AlertRuleNotFoundException** — thrown when an alert rule doesn't exist for the given user
- **TrackingNotFoundException** — thrown when a tracked game is not found for the given user

### Key Technical Details
- **Hexagonal Architecture**: Domain and Application have **zero references** to Infrastructure. All cross-boundary communication goes through port interfaces.
- **Anti-Corruption Layer**: SteamTracker never reads the existing app's `wishlist_items` table. It consumes `WishlistItemAdded`/`Removed` events via RabbitMQ.
- **Separate DB with WishlistApi**: Each service uses its own PostgreSQL database on the same server (`postgres` vs `steamtracker`). SteamTracker uses snake_case table/column names (via `NpgsqlSnakeCaseNamingConvention`). The services are fully decoupled via RabbitMQ events and REST — no cross-service DB queries.
- **Rate Limiting**: Token bucket (.NET 7+ `TokenBucketRateLimiter`) — 180 tokens per 5-minute replenishment period (near Steam's 200/5min limit).
- **Retry Policy**: 3 attempts, exponential backoff (2s, 4s, 8s). Circuit breaker: open after 5 consecutive failures, half-open after 30s.
- **EUR Pricing**: Steam API called with `cc=de&l=german` for consistent EUR pricing regardless of worker IP.
- **F2P Games**: `price_overview` absent → `Money.Free`. `AlertRule.ShouldTrigger(Money.Free)` always returns false.

## Project References (Dependency Flow)
```
SteamTracker.API (host) → Application → Domain
SteamTracker.API (host) → Infrastructure → Domain
SteamTracker.Worker (host) → Application → Domain
SteamTracker.Worker (host) → Infrastructure → Domain
Tests → respective source projects
```
