# API Backend Overview

This directory contains two .NET 10 backend services that together power the wishlist application.

## Services

### [WishlistApi](WishlistApi/OVERVIEW.md)
The primary REST API with JWT authentication, SignalR real-time updates, and a rich domain model (users, wishlists, auctions, Steam game listings). It serves the React frontend directly and proxies SteamTracker alert operations.

**Key characteristics**:
- User authentication (JWT cookie-based)
- Auction system with real-time SignalR updates
- Steam game listing search
- Separate Postgres database (different DB from SteamTracker)

### [SteamTracker](SteamTracker/OVERVIEW.md)
A hexagonal (ports & adapters) DDD service that tracks Steam game prices for wishlisted items. It operates as an independent service with its own Postgres schema and communicates with the existing app via RabbitMQ events.

**Key characteristics**:
- Hexagonal architecture (Domain → Application → Infrastructure)
- RabbitMQ event-driven wishlist sync (ACL boundary)
- Scheduled price fetching from Steam Store API
- Alert rules with notification publishing

## Data Flow

```
React Frontend  ──►  WishlistApi  ──►  PostgreSQL (`postgres` DB)
                           │
                           └──►  SteamTracker (proxy for alerts)
                                       │
                                       └──►  PostgreSQL (`steamtracker` DB)
```

## Running Tests

```bash
# WishlistApi
dotnet test api/WishlistApi/WishlistApi.sln

# SteamTracker
dotnet test api/SteamTracker/SteamTracker.slnx
```
