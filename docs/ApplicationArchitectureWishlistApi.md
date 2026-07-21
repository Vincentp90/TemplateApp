# WishlistApi Application Architecture

## Overview

WishlistApi is a **layered (Clean Architecture)** ASP.NET Core 10 service that serves as the primary REST API for the wishlist application. It handles user authentication, wishlist CRUD operations, auction management, and acts as a proxy for SteamTracker alert operations.

### Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | ASP.NET Core 10 (.NET 10) |
| Language | C# 14 |
| Database | PostgreSQL + EF Core |
| Authentication | JWT cookie-based |
| Real-time | SignalR (`/auctionHub`) |
| Messaging | RabbitMQ (fanout exchange for domain events) |
| Caching | IMemoryCache |
| API Docs | Swagger/OpenAPI |

---

## Architecture: Layered (Clean Architecture)

```
┌─────────────────────────────────────────────────────────┐
│              WishlistApi (Host / Web)                   │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────────┐  │
│  │Controllers│  │ DTOs     │  │ HostedServices       │  │
│  │AuthController│  │AuthDTOs│  │AuctionBackgroundSvc │  │
│  │WishlistCtrl │  │UserDTOs│  │SteamUpdaterService  │  │
│  │AuctionsCtrl │  │WishlistDTOs│                     │  │
│  └─────┬──────┘  └──────────┘  └──────────────────────┘  │
│        │                                                  │
├────────┼──────────────────────────────────────────────────┤
│        ▼                                                  │
│  ┌──────────────────────────────────────────────────┐     │
│  │              Application Layer                   │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ UseCases/ (one class per action)       │    │     │
│  │  │   Auth/  RegisterUserUseCase            │    │     │
│  │  │   Wishlist/ AddWishlistItemUseCase      │    │     │
│  │  │   Auction/ PlaceBidUseCase              │    │     │
│  │  │   User/   GetUserProfileUseCase         │    │     │
│  │  │   AppListing/ SearchAppListingsUseCase  │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ Events/ (domain event records)          │    │     │
│  │  │   WishlistItemAdded                     │    │     │
│  │  │   WishlistItemRemoved                   │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ Contracts/ (shared DTOs)                │    │     │
│  │  │   AuctionDto, WishlistDtos              │    │     │

│  │  │   ISteamTrackerAlertProxy               │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ IEventPublisher (port interface)        │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  └──────────────────────────┬───────────────────────┘     │
│                             │ depends on                  │
├─────────────────────────────┼─────────────────────────────┤
│                             ▼                             │
│  ┌──────────────────────────────────────────────────┐     │
│  │              Domain Layer                        │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ Entities (aggregate roots)              │    │     │
│  │  │   User (with UserDetails)               │    │     │
│  │  │   WishlistItem                          │    │     │
│  │  │   Auction                               │    │     │
│  │  │   AppListing                            │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ ValueObjects                            │    │     │
│  │  │   FullName, Address                     │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ Repositories/ (port interfaces)         │    │     │
│  │  │   IUserRepository                       │    │     │
│  │  │   IWishlistItemRepository               │    │     │
│  │  │   IAuctionRepository                    │    │     │
│  │  │   IAppListingRepository                 │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ Helpers                                   │    │     │
│  │  │   IUnitOfWork                             │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ Exceptions                                │    │     │
│  │  │   DomainException, NotFoundException      │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ ISteamApiClient (external port)         │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  └──────────────────────────────────────────────────┘     │
│                                                            │
│  ┌──────────────────────────────────────────────────┐     │
│  │         Infrastructure Layer                     │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ Persistence (EF Core)                   │    │     │
│  │  │   WishlistDbContext                     │    │     │
│  │  │   UserRepository, WishlistItemRepo      │    │     │
│  │  │   AuctionRepository, AppListingRepo     │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ ExternalServices                        │    │     │
│  │  │   SteamApiClient                        │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  │  ┌──────────────────────────────────────────┐    │     │
│  │  │ Messaging                               │    │     │
│  │  │   RabbitMqEventPublisher (IEventPub impl)│    │     │
│  │  │   IRabbitMqConnectionFactory            │    │     │
│  │  └──────────────────────────────────────────┘    │     │
│  └──────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
```

### Dependency Flow

```
WishlistApi (host) ──► Application ──► Domain
WishlistApi (host) ──► Infrastructure ──► Domain
Domain has ZERO dependencies on Application or Infrastructure
```

---

## Layer Responsibilities

### Domain Layer (Pure, No Dependencies)

The domain layer contains the business logic and rules with **zero framework dependencies**.

| Component | Purpose |
|-----------|---------|
| **Entities** | Aggregate roots: `User`, `WishlistItem`, `Auction`, `AppListing` |
| **ValueObjects** | `FullName`, `Address` — immutable, value-based equality |
| **Repositories/** | Port interfaces (`IUserRepository`, `IWishlistItemRepository`, etc.) — defines WHAT, not HOW |
| **ISteamApiClient** | Port for external Steam API dependency |
| **IUnitOfWork** | Transaction boundary abstraction |
| **Exceptions** | `DomainException`, `NotFoundException` |

### Application Layer (Use Cases + Ports)

Contains business logic orchestration. **Does not depend on Infrastructure** — it defines port interfaces that Infrastructure implements.

| Component | Purpose |
|-----------|---------|
| **UseCases/** | One class per controller action (e.g., `AddWishlistItemUseCase`, `PlaceBidUseCase`). Each is a focused, single-responsibility class. |
| **Events/** | Domain event records (`WishlistItemAdded`, `WishlistItemRemoved`) — lightweight, no behavior |
| **Contracts/** | Shared DTOs and cross-service interfaces (`ISteamTrackerAlertProxy`) |
| **IEventPublisher** | Port interface for publishing domain events (implemented by `RabbitMqEventPublisher` in Infrastructure) |

### Infrastructure Layer (Adapters)

Contains framework-specific implementations of port interfaces.

| Component | Purpose |
|-----------|---------|
| **Persistence/** | EF Core `WishlistDbContext`, entity configurations, repository implementations |
| **ExternalServices/** | `SteamApiClient` — HTTP adapter for the Steam API |
| **Messaging/** | `RabbitMqEventPublisher` — publishes events to the `wishlist.events` fanout exchange |
| **Migrations/** | EF Core migration history |

### WishlistApi (Host / Web)

The application host that wires everything together.

| Component | Purpose |
|-----------|---------|
| **Controllers/** | HTTP endpoints — thin, no business logic. They call use cases and return results. |
| **DTOs/** | HTTP response/request models (distinct from domain entities) |
| **HostedServices/** | Background services: `AuctionBackgroundService` (lifecycle management), `SteamUpdaterService` (periodic Steam data sync) |
| **Helpers/** | `JwtTokenGenerator`, `UserContext` (JWT extraction) |
| **Program.cs** | DI registration, middleware pipeline, startup |

---

## Key Design Patterns

### 1. Use Case per Action

Each controller action maps to exactly one use case class. This ensures:
- Single Responsibility Principle
- Easy to test (one class = one test file)
- Clear input/output boundaries

Example: `POST /wishlist/{appId}` → `AddWishlistItemUseCase`

### 2. Port & Adapter (Ports in Domain/Application, Adapters in Infrastructure)

```
Application defines:  IEventPublisher  ──►  Infrastructure implements: RabbitMqEventPublisher
Application defines:  IWishlistItemRepository  ──►  Infrastructure implements: WishlistItemRepository
Domain defines:       ISteamApiClient  ──►  Infrastructure implements: SteamApiClient
```

### 3. Anti-Corruption Layer (ACL) — Separate Databases

WishlistApi and SteamTracker each have their own PostgreSQL database on the same server, communicating through well-defined boundaries:

| Aspect | WishlistApi | SteamTracker |
|--------|-------------|--------------|
| **Database** | `postgres` (EF Core, PascalCase → snake_case) | `steamtracker` (EF Core, snake_case) |
| **Writes** | EF Core | EF Core |
| **Cross-service Read** | N/A — WishlistApi proxies alert operations via SteamTracker REST API | N/A (WishlistApi proxy calls SteamTracker REST API for alerts) |
| **Events** | Publishes `WishlistItemAdded`/`Removed` to RabbitMQ | Consumes via `steamtracker.wishlist-sync` queue |

### 4. Event-Driven Communication

When a user adds an item to their wishlist:

```
WishlistController.AddWishlistItemAsync
  └─► AddWishlistItemUseCase.ExecuteAsync
       ├─► IWishlistItemRepository.AddWishlistItemAsync  (EF Core write)
       ├─► IUnitOfWork.SaveChangesAsync                  (commit)
       └─► IEventPublisher.PublishAsync                  (RabbitMQ publish)
            └─► RabbitMqEventPublisher.PublishAsync
                 └─► wishlist.events (fanout) exchange
                      └─► steamtracker.wishlist-sync queue
                           └─► WishlistSyncConsumer (SteamTracker Worker)
```

### 5. Wishlist Enrichment

The `GET /wishlist` endpoint enriches local wishlist items with price data and alert rules from SteamTracker:

```
GET /wishlist
  └─► GetWishlistUseCase.ExecuteAsync
       └─► IWishlistItemRepository.GetAllAsync  (local wishlist)
  └─► ISteamTrackerAlertProxy.GetPricesAsync       (REST → SteamTracker)
  └─► ISteamTrackerAlertProxy.GetAlertRulesAsync   (REST → SteamTracker)
  └─► Merge local items + prices + alert rules → WishlistDto
```

---

## Authentication & Authorization

| Mechanism | Details |
|-----------|---------|
| **JWT** | Cookie-based (`auth_token`), `HttpOnly`, `Secure`, `SameSite=Strict` |
| **Expiry** | 2 hours |
| **Password Hashing** | PBKDF2 with SHA256, 100,000 iterations, 16-byte salt |
| **Role-Based** | Admin/User roles |

---

## Background Services

| Service | Description |
|---------|-------------|
| `AuctionBackgroundService` | Manages auction lifecycle — starts next auction, closes expired ones |
| `SteamUpdaterService` | Periodic Steam game listing synchronization |

---

## Optimistic Concurrency

`Auction` and `UserDetails` use row-version columns for optimistic concurrency control. Concurrent modifications that detect stale data will fail with a concurrency exception.

---

## Testing Strategy

| Test Type | Framework | Coverage |
|-----------|-----------|----------|
| **Unit** | xUnit + Moq + FluentAssertions | Use cases, domain logic |
| **Integration** | xUnit + WebApplicationFactory | Controller endpoints, DB interactions |
| **Data Access** | xUnit | Repository operations |
