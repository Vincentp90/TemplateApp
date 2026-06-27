# Hexagonal Architecture Analysis — WishlistApi

## Executive Summary

The current codebase follows a **Clean Architecture / Layered** pattern but has several violations that prevent it from being a proper hexagonal (ports & adapters) architecture. The most critical issue is that the **Application layer depends on Infrastructure**, which directly contradicts the dependency rule of hexagonal architecture.

---

## Current Architecture

```
WishlistApi (host) ──→ Application ──→ Domain
                        ↑             ↓
                        └──── Infrastructure
```

### Current Dependencies

| Project | Dependencies |
|---------|-------------|
| **Domain** | None (pure C#, no NuGet packages) |
| **Infrastructure** | Domain |
| **Application** | Domain **+ Infrastructure** ❌ |
| **WishlistApi (host)** | Application + Infrastructure |
| **Tests** | Application + Infrastructure + WishlistApi |

---

## Issues & Required Changes

### 1. 🔴 CRITICAL: Application depends on Infrastructure

**Problem**: `Application.csproj` has a project reference to `Infrastructure.csproj`. This means business logic can directly consume EF Core types, DbContext, etc.

**Affected files**:
- `api/WishlistApi/Application/Application.csproj` — remove Infrastructure reference
- `api/WishlistApi/Application/Queries/AuctionQueries.cs` — depends on `WishlistDbContext`
- `api/WishlistApi/Application/Queries/UserQueries.cs` — depends on `WishlistDbContext`
- `api/WishlistApi/Application/UserService.cs` — imports `Infrastructure.Persistence.Users`

**Fix**: Move all infrastructure-dependent code out of Application. Queries should be pulled into Infrastructure as "read-side adapters."

---

### 2. 🔴 CRITICAL: Queries in Application directly use DbContext

**Problem**: `AuctionQueries` and `UserQueries` are in the Application layer but directly depend on `WishlistDbContext` from Infrastructure. This is the clearest violation of hexagonal architecture.

**Affected files**:
- `api/WishlistApi/Application/Queries/AuctionQueries.cs`
- `api/WishlistApi/Application/Queries/UserQueries.cs`

**Fix**: Move queries to Infrastructure as read-side adapters, or create **application query interfaces** in Application and implement them in Infrastructure.

---

### 3. 🟡 MEDIUM: UserService imports Infrastructure types

**Problem**: `UserService` has `using Infrastructure.Persistence.Users;` — it directly references EF entity types rather than domain types.

**Affected file**: `api/WishlistApi/Application/UserService.cs`

**Fix**: Replace with domain types only. The `FullName` and `Address` value objects are already in Domain — the import is unnecessary.

---

### 4. 🟡 MEDIUM: Domain entities depend on repositories (WishlistItem.AddAsync)

**Problem**: `WishlistItem.AddAsync()` takes an `IWishlistItemRepository` parameter. Domain entities should not know about repositories.

**Affected file**: `api/WishlistApi/Domain/WishlistItem.cs`

**Fix**: Move the "check if already on wishlist" logic to the Application layer (WishlistService). The domain entity should only handle its own invariants, not repository lookups.

---

### 5. 🟡 MEDIUM: Domain entities have data-mapping constructors

**Problem**: `Auction` has a constructor that takes individual EF-mapped fields. This is a data-mapping concern that belongs in Infrastructure, not Domain.

**Affected file**: `api/WishlistApi/Domain/Auction.cs`

**Fix**: Remove the data-mapping constructor. Let Infrastructure repositories do the mapping to domain objects, or use a mapper. The domain entity should only have constructors for creating valid domain objects.

---

### 6. 🟡 MEDIUM: DTOs split between Application and WishlistApi

**Problem**: DTOs are scattered:
- `Application.Contracts` → `AuctionDto`, `UserSummaryDto` (read-side DTOs)
- `WishlistApi.DTOs` → `WishlistItemDto`, `Stats`, `AuthDTOs`, `UserDTOs` (API response/request DTOs)

In hexagonal architecture, **all application-level DTOs should be in Application** (as ports), and the API layer should only map to/from them.

**Affected files**:
- `api/WishlistApi/Application/Contracts/AuctionDto.cs`
- `api/WishlistApi/Application/Contracts/UserSummaryDto.cs`
- `api/WishlistApi/WishlistApi/DTOs/WishlistDTOs.cs`
- `api/WishlistApi/WishlistApi/DTOs/AuthDTOs.cs`
- `api/WishlistApi/WishlistApi/DTOs/UserDTOs.cs`

**Fix**: Move all DTOs to Application. Create a mapping layer in WishlistApi.

---

### 7. 🟢 LOW: JwtTokenGenerator location

**Problem**: `JwtTokenGenerator` lives in WishlistApi (host), not Infrastructure. The code comment acknowledges this: "Would be more DDD like to put this in infra layer."

**Affected file**: `api/WishlistApi/WishlistApi/Helpers/JwtTokenGenerator.cs`

**Fix**: Move to Infrastructure. This is a minor pragmatic concern — keeping it in the host avoids adding JWT NuGet packages to Infrastructure, which is a valid tradeoff.

---

### 8. 🟢 LOW: AuctionBackgroundService directly calls Application services

**Problem**: The background service creates scopes and directly resolves `IAuctionService` and `IAppListingService`. This is technically acceptable but means background services are tightly coupled to the Application layer's service contracts.

**Affected file**: `api/WishlistApi/WishlistApi/HostedServices/AuctionBackgroundService.cs`

**Fix**: Consider wrapping this in an application use-case or application command that the background service invokes, decoupling the service from the specific service interfaces.

---

## Target Hexagonal Architecture

```
                    ┌─────────────────────────────────┐
                    │        WishlistApi (host)        │
                    │  ┌─────────┐  ┌──────────────┐  │
                    │  │Controllers│  │HostedServices│  │
                    │  │DTOs (map) │  │              │  │
                    │  └────┬─────┘  └──────┬───────┘  │
                    │       │                │          │
                    │       ▼                ▼          │
                    │  ┌──────────────────────────┐    │
                    │  │   Application Layer       │    │
                    │  │  (Ports + Use Cases)      │    │
                    │  │  ┌────────────────────┐   │    │
                    │  │  │ Services (use cases)│   │    │
                    │  │  │ Commands/Queries    │   │    │
                    │  │  │ Port interfaces     │   │    │
                    │  │  │ Application DTOs    │   │    │
                    │  │  └────────────────────┘   │    │
                    │  └──────────┬─────────────────┘    │
                    └─────────────┼──────────────────────┘
                                  │ depends on
                    ┌─────────────▼──────────────────────┐
                    │           Domain Layer              │
                    │  ┌──────────────────────────────┐   │
                    │  │ Entities, ValueObjects       │   │
                    │  │ Repository port interfaces   │   │
                    │  │ ISteamApiClient interface    │   │
                    │  │ IUnitOfWork                  │   │
                    │  │ Exceptions                   │   │
                    │  └──────────────────────────────┘   │
                    └─────────────────────────────────────┘
                                  ▲ depends on
                    ┌─────────────┴──────────────────────┐
                    │       Infrastructure Layer          │
                    │  ┌──────────────────────────────┐   │
                    │  │ Persistence (EF Core)        │   │
                    │  │ Repository implementations   │   │
                    │  │ DbContext                    │   │
                    │  │ Read adapters (Queries)      │   │
                    │  │ External services (Steam)    │   │
                    │  │ JWT Token Generator          │   │
                    │  └──────────────────────────────┘   │
                    └─────────────────────────────────────┘
```

### Dependency Flow (after changes)

```
WishlistApi (host) → Application (ports + use cases)
WishlistApi (host) → Domain (contracts)

Infrastructure → Application (implements ports)
Infrastructure → Domain (implements repository interfaces)

Application → Domain (only)
Domain → (nothing)
```

---

## Detailed Change Plan

### Phase 1: Fix Application layer dependencies

1. **Remove Infrastructure dependency from Application.csproj**
2. **Move queries out of Application**:
   - Option A: Create `IReadModelRepository` / `IAuctionReadModel` / `IUserReadModel` interfaces in Application, implement in Infrastructure
   - Option B: Move queries to Infrastructure as "read adapters" (simpler, less DDD-pure)
3. **Remove `Infrastructure.Persistence.Users` import from UserService.cs**
4. **Move DTOs from WishlistApi.DTOs to Application.Contracts**

### Phase 2: Clean up Domain layer

5. **Remove `IWishlistItemRepository` parameter from `WishlistItem.AddAsync()`**
   - Move the repository check into `WishlistService`
6. **Remove data-mapping constructor from `Auction`**
   - Move mapping logic to `AuctionRepository`

### Phase 3: Restructure Infrastructure

7. **Move `JwtTokenGenerator` from WishlistApi to Infrastructure**
8. **Move `AuctionQueries` and `UserQueries` to Infrastructure** (or implement port interfaces)

### Phase 4: Restructure WishlistApi (host)

9. **Create mapping layer** — WishlistApi DTOs should map to/from Application DTOs
10. **Update Program.cs** — dependency injection remains similar, but Infrastructure now implements Application's ports instead of being a direct dependency

---

## Files Affected by Each Change

| # | File | Change |
|---|------|--------|
| 1 | `Application/Application.csproj` | Remove Infrastructure project reference |
| 2 | `Application/Queries/AuctionQueries.cs` | Move to Infrastructure (or create port interface) |
| 3 | `Application/Queries/UserQueries.cs` | Move to Infrastructure (or create port interface) |
| 4 | `Application/UserService.cs` | Remove `Infrastructure.Persistence.Users` import |
| 5 | `Domain/WishlistItem.cs` | Remove `IWishlistItemRepository` parameter from `AddAsync()` |
| 6 | `Domain/Auction.cs` | Remove data-mapping constructor |
| 7 | `WishlistApi/DTOs/*.cs` | Move to `Application/Contracts/` |
| 8 | `WishlistApi/Helpers/JwtTokenGenerator.cs` | Move to Infrastructure |
| 9 | `WishlistApi/Program.cs` | Update DI registration (Infrastructure now implements Application ports) |
| 10 | `WishlistApi/Controllers/*.cs` | Update to use Application DTOs, add mapping |
| 11 | `Tests/*.cs` | Update project references and test setup |

---

## Impact on Testing

- **Unit tests** for Application services will need fewer mocks (no Infrastructure dependency)
- **Infrastructure tests** will cover repository implementations and query adapters
- **Integration tests** remain largely the same — they test the full stack
- The `Application.csproj` will no longer need EF Core packages, making it easier to mock

---

## Summary of Issues by Severity

| Severity | Count | Description |
|----------|-------|-------------|
| 🔴 Critical | 2 | Application depends on Infrastructure; Queries use DbContext directly |
| 🟡 Medium | 4 | UserService imports infra types; Domain entities depend on repos; Data-mapping constructors in Domain; DTOs scattered |
| 🟢 Low | 2 | JwtTokenGenerator location; Background services tightly coupled |

**Total changes needed: ~10 file-level changes across 4 projects**
