# System Architecture

## Overview

This document provides a high-level overview of how the different components of the TemplateApp system interact. The system consists of four primary layers:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Frontend (React)                         │
│   TanStack Router · TanStack Query · Zustand · SignalR          │
│   http://localhost:5173 (dev) / :80 (prod, via nginx)           │
└──────────────────────────────────────┬──────────────────────────┘
                                       │ HTTP (Axios)
                                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                     WishlistApi (ASP.NET Core)                  │
│   REST API · JWT Auth · SignalR · Swagger                      │
│   http://localhost:5186 (dev)                                   │
│                                                                 │
│   ┌─────────────┐  ┌─────────────┐  ┌──────────────────────┐   │
│   │ EF Core     │  │ REST Client │  │ RabbitMQ Publisher  │   │
│   │ (writes)    │  │ (via proxy) │  │ (domain events)     │   │
│   │ local DB    │  └─────────────┘             │              │
│   └──────┬──────┘                             │              │
│          │                                    │               │
│          ▼                ▼                    ▼              ┌┴───────────────┐
└──────────┼────────────────┼────────────────────┼──────────────┤  RabbitMQ     │
           │                │                    │              │  4.2-mgmt     │
           ▼                ▼                    ▼              └──┬────────────┘
┌─────────────────┐ ┌─────────────────┐                     ┌────┴──────┐
│   PostgreSQL    │ │    PostgreSQL   │                     │wishlist. │
│   (`postgres`)  │ │   (`steamtracker│                     │events    │
│                 │ │    schema)      │                     │(fanout)  │
│   wishlistapi   │ │   steamtracker  │                     └────┬─────┘
│   tables        │ │   tables        │                          │
└─────────────────┘ └─────────────────┘                         │
                                                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SteamTracker (.NET Worker)                    │
│   Hexagonal Architecture · DDD · RabbitMQ Consumer              │
│                                                                 │
│   ┌──────────────────────┐  ┌──────────────────────────────┐   │
│   │ Domain Layer         │  │  Application Layer           │   │
│   │ • TrackedGame        │  │  • HandleWishlistItemAdded   │   │
│   │ • Game               │  │  • HandleWishlistItemRemoved │   │
│   │ • PriceSnapshot      │  │  • ProcessPriceCheck         │   │
│   │ • AlertRule          │  │  • Set/Delete AlertRule      │   │
│   └──────────┬───────────┘  └──────────┬───────────────────┘   │
│              │                         │                        │
│              ▼                         ▼                        │
│   ┌──────────────────────────────────────────────────────┐     │
│   │ Infrastructure Layer                                 │     │
│   │ • EF Core (SteamTrackerDbContext)                   │     │
│   │ • SteamStoreClient (HTTP adapter)                   │     │
│   │ • PriceCheckJobPublisher (RabbitMQ publisher)       │     │
│   │ • NotificationPublisher (RabbitMQ publisher)        │     │
│   └──────────────────────────────────────────────────────┘     │
│                                                                 │
│   Background Workers:                                           │
│   • PriceCheckScheduler  (24h cycle)                          │
│   • PriceCheckConsumer   (consumes price-check jobs)          │
│   • WishlistSyncConsumer (consumes wishlist events)           │
└─────────────────────────────────────────────────────────────────┘
```

---

## Component Inventory

| Component | Role | Port(s) |
|-----------|------|---------|
| **React Frontend** | User interface, state management, data fetching | 5173 (dev), 80 (prod) |
| **WishlistApi** | REST API, authentication, wishlist CRUD, auctions | 5186 |
| **SteamTracker.API** | Internal REST API (alert management) | — (internal only) |
| **SteamTracker.Worker** | Background processing (price fetching, event consumption) | — |
| **PostgreSQL** | Separate databases on the same server | 5432 |
| **RabbitMQ** | Message broker for event-driven communication | 5672 (AMQP), 15672 (management) |
| **Adminer** | Database administration UI | 8085 |
| **Nginx** | Reverse proxy (production) | 80 |

---

## Core Functional Flows

### Flow 1: Adding an Item to a Wishlist

This flow covers the end-to-end path from the user adding a game to their wishlist in the React frontend through to the event being published and consumed by SteamTracker.

```
React  ──►  WishlistApi  ──►  Domain (WishlistItem)  ──►  PostgreSQL
  │           │                     │                          │
  │ POST      │ AddWishlistItem     │ EF Core write            │
  │ /wishlist │ UseCase             │                          │
  │/{appId}   │                     │                          │
  │           │                     │                          │
  │           │                     │ PublishAsync             │
  │           │                     │ (WishlistItemAdded)      │
  │           │                     │                          │
  │           │                     └──────────────────────────┼──► RabbitMQ
  │           │                                                │  wishlist.events
  │           │                                                │  (fanout)
  │           │                                                │
  │ 200 OK    │                                                ▼
  │ ◄─────────│                                          steamtracker.wishlist-sync
  │           │                                                │
  │           │                                          WishlistSyncConsumer
  │           │                                                │
  │           │                                          HandleWishlistItemAdded
  │           │                                          UseCase
  │           │                                                │
  │           │                                        Upsert TrackedGame
  │           │                                        Enqueue PriceCheck
  │           │                                                │
  │           │                                                ▼
  │           │                                          price-check-jobs queue
```

**Step-by-step:**

1. **Frontend** sends `POST /wishlist/{appId}` with JWT cookie authentication
2. **WishlistApi** controller extracts user ID from JWT and calls `AddWishlistItemUseCase`
3. **Use Case** validates the item isn't already on the wishlist (via `IWishlistItemRepository.AppIsOnWishlistAsync`)
4. **Use Case** creates a new `WishlistItem` domain entity and persists it via `IUnitOfWork.SaveChangesAsync()`
5. **Infrastructure** writes the entity to PostgreSQL via EF Core
6. **Use Case** publishes a `WishlistItemAdded` domain event via `IEventPublisher`
7. **Infrastructure** (`RabbitMqEventPublisher`) publishes the event to the `wishlist.events` fanout exchange
8. **SteamTracker Worker** (`WishlistSyncConsumer`) receives the event from the `steamtracker.wishlist-sync` queue
9. **SteamTracker** use case upserts a `TrackedGame` record and enqueues an immediate price-check job

---

### Flow 2: SteamTracker Price Retrieval

This flow covers how SteamTracker fetches game prices from the Steam Store API. It can be triggered in two ways:

- **Immediate trigger**: When a new item is added to a user's wishlist (see Flow 1, step 9)
- **Scheduled trigger**: The `PriceCheckScheduler` runs every 24 hours and enqueues all games that are due (never checked or last check > 24h ago)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         SteamTracker                                      │
│                                                                         │
│  PriceCheckScheduler (every 24h)                                        │
│       │                                                                   │
│       │ 1. Get all active games due for check                             │
│       │ 2. Enqueue one job per game                                       │
│       ▼                                                                   │
│  price-check-jobs queue ────────────────────────────────────────────────┐
│                                                                         │
│  PriceCheckConsumer (RabbitMQ consumer)                                 │
│       │                                                                 │
│       │ 3. Consume PriceCheckMessage                                    │
│       ▼                                                                 │
│  PriceCheckWorker                                                       │
│       │                                                                 │
│       │ 4. Fetch price from Steam Store API                             │
│       │    (EUR pricing via cc=de&l=german)                             │
│       │    Rate limited: TokenBucketRateLimiter                         │
│       │    180 tokens / 5 min (near Steam's 200/5min limit)             │
│       │    Retry: 3 attempts, exponential backoff (2s, 4s, 8s)          │
│       │    Circuit breaker: open after 5 failures, half-open after 30s  │
│       ▼                                                                 │
│  ProcessPriceCheck UseCase                                              │
│       │                                                                 │
│       │ 5. Save price + create PriceSnapshot (append-only)              │
│       │ 6. Evaluate AlertRules (PriceAlertEvaluator)                    │
│       │    - Triggers when currentPrice <= triggerBelowPrice            │
│       │    - Free games never trigger                                   │
│       │ 7. If triggered: mark rule + publish notification               │
│       ▼                                                                 │
│  notification queue ──────────────────────────────────────────────────► │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

**Step-by-step:**

1. **PriceCheckScheduler** runs on a 24-hour interval
2. It queries `Game.LastCheckedAt` to find games due for checking (never checked or > 24h since last check)
3. It enqueues one `PriceCheckMessage` per due game into the `price-check-jobs` queue
4. **PriceCheckConsumer** picks up the message and delegates to **PriceCheckWorker**
5. **PriceCheckWorker** calls `ISteamStoreClient.FetchPriceAsync()` to get the current price from the Steam Store API
   - Uses EUR pricing (`cc=de&l=german`) for consistency regardless of worker IP
   - Rate limited via `TokenBucketRateLimiter` (180 tokens / 5 min)
   - Retry policy: 3 attempts with exponential backoff (2s, 4s, 8s)
   - Circuit breaker: opens after 5 consecutive failures, half-opens after 30s
   - Free-to-play games (no `price_overview`) are handled as `Money.Free`
6. **ProcessPriceCheck UseCase** processes the result:
   - Saves the price as a new `PriceSnapshot` (append-only, unique index on `(app_id, captured_at)` for idempotency)
   - Updates `Game.CurrentPrice` and `Game.LastCheckedAt`
   - Evaluates all active `AlertRule` entries for the game using `PriceAlertEvaluator`
   - If any rules trigger (`currentPrice <= triggerBelowPrice`), marks them as triggered and publishes a notification

---

## Data Flow Summary

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Data Flow Summary                             │
├──────────────────────────┬──────────────────────────────────────────┤
│ Component                │ Data Path                                │
├──────────────────────────┼──────────────────────────────────────────┤
│ Frontend → WishlistApi   │ HTTP REST (Axios, cookie auth)           │
│ WishlistApi → DB         │ EF Core (writes)                         │
│ WishlistApi → RabbitMQ   │ Fanout exchange → wishlist-sync queue    │
│ RabbitMQ → SteamTracker  │ wishlist-sync queue → WishlistSyncWorker │
│ SteamTracker → Steam API │ HTTP GET (Steam Store API, EUR pricing)  │
│ SteamTracker → DB        │ EF Core (snake_case tables/columns)      │
│ WishlistApi → SteamTracker│ REST calls (proxy for prices, alerts)  │
│                          │ for wishlist enrichment                  │
└──────────────────────────┴──────────────────────────────────────────┘
```

---

## Separate Database Design

WishlistApi and SteamTracker each have their own PostgreSQL database on the same server:

| Aspect | WishlistApi | SteamTracker |
|--------|-------------|--------------|
| **Database** | `postgres` | `steamtracker` |
| **ORM** | EF Core (PascalCase → snake_case via `NpgsqlSnakeCaseNamingConvention`) | EF Core (snake_case via `NpgsqlSnakeCaseNamingConvention`) |
| **Writes** | EF Core | EF Core |
| **Cross-service reads** | N/A — WishlistApi proxies alert operations via REST | N/A (WishlistApi proxies alert operations via REST) |

**Anti-Corruption Layer**: SteamTracker never reads the existing app's `wishlist_items` table. It consumes `WishlistItemAdded`/`Removed` events via RabbitMQ, maintaining its own projection in the `tracked_game` table.

---

## RabbitMQ Topology

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

---

## Key Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| **Separate PostgreSQL databases** | Simplifies deployment; each service owns its own schema |
| **REST proxy for cross-service reads** | Clean ACL boundary; no shared DB coupling |
| **RabbitMQ for wishlist sync** | Loose coupling between WishlistApi and SteamTracker; SteamTracker is an ACL consumer |
| **Hexagonal architecture in SteamTracker** | Domain purity — zero framework dependencies; easy to swap adapters |
| **Layered architecture in WishlistApi** | Familiar pattern for CRUD-heavy service; clear separation of concerns |
| **EUR pricing** | Consistent pricing regardless of worker IP; Steam API supports `cc=de&l=german` |
| **24h price check interval** | Balances freshness with Steam API rate limits |
| **Append-only PriceSnapshot** | Full price history preserved; unique index on `(app_id, captured_at)` for idempotency |
