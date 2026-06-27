# Hexagonal Architecture Migration Plan — WishlistApi

## Goal

Transform WishlistApi from a **Clean Architecture / Layered** pattern into a proper **Hexagonal (Ports & Adapters)** architecture, where the inner layers (Domain, Application) have zero dependency on outer layers (Infrastructure, Host).

---

## Target Dependency Flow

```
WishlistApi (host) → Application (ports + use cases)
WishlistApi (host) → Domain (contracts)

Infrastructure → Application (implements ports)
Infrastructure → Domain (implements repository interfaces)

Application → Domain (only)
Domain → (nothing)
```

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
                    │  └──────────────────────────────┘   │
                    └─────────────────────────────────────┘
```

---

## Current State

```
WishlistApi (host) ──→ Application ──→ Domain
                        ↑             ↓
                        └──── Infrastructure    ❌ Application depends on Infrastructure
```

### Current Dependencies

| Project | Dependencies |
|---------|-------------|
| **Domain** | None (pure C#, no NuGet packages) ✅ |
| **Infrastructure** | Domain ✅ |
| **Application** | Domain **+ Infrastructure** ❌ |
| **WishlistApi (host)** | Application + Infrastructure ✅ |
| **Tests** | Application + Infrastructure + WishlistApi ✅ |

---

## Issues

### 🔴 Critical (2)

#### 1. Application depends on Infrastructure

**Problem**: `Application.csproj` has a project reference to `Infrastructure.csproj`. Business logic can directly consume EF Core types, `DbContext`, etc.

**Affected files**:
- `Application/Application.csproj`
- `Application/Queries/AuctionQueries.cs` — depends on `WishlistDbContext`
- `Application/Queries/UserQueries.cs` — depends on `WishlistDbContext`
- `Application/UserService.cs` — imports `Infrastructure.Persistence.Users`

**Fix**: Remove the Infrastructure reference. Move all infrastructure-dependent code out of Application.

---

#### 2. Queries in Application directly use DbContext

**Problem**: `AuctionQueries` and `UserQueries` live in the Application layer but directly depend on `WishlistDbContext` from Infrastructure. This is the clearest violation of hexagonal architecture.

**Affected files**:
- `Application/Queries/AuctionQueries.cs`
- `Application/Queries/UserQueries.cs`

**Fix**: Create port interfaces in Application, implement them in Infrastructure as read adapters.

---

### 🟡 Medium (4)

#### 3. UserService imports Infrastructure types

**Problem**: `UserService` has `using Infrastructure.Persistence.Users;` — it directly references EF entity types rather than domain types.

**Affected file**: `Application/UserService.cs`

**Fix**: Remove the import. `FullName` and `Address` value objects already live in Domain.

---

#### 4. Domain entity depends on a repository (`WishlistItem.AddAsync`)

**Problem**: `WishlistItem.AddAsync()` takes an `IWishlistItemRepository` parameter. Domain entities should not know about repositories.

**Affected file**: `Domain/WishlistItem.cs`

**Fix**: Move the "check if already on wishlist" logic to `WishlistService`. The domain entity should only handle its own invariants.

---

#### 5. Domain entity has a data-mapping constructor (`Auction`)

**Problem**: `Auction` has a constructor that takes individual EF-mapped fields. Data mapping belongs in Infrastructure, not Domain.

**Affected file**: `Domain/Auction.cs`

**Fix**: Remove the data-mapping constructor. Let Infrastructure repositories do the mapping to domain objects.

---

#### 6. DTOs scattered between layers

**Problem**: DTOs are split:
- `Application.Contracts` → `AuctionDto`, `UserSummaryDto` (read-side DTOs)
- `WishlistApi.DTOs` → `WishlistItemDto`, `Stats`, `AuthDTOs`, `UserDTOs` (API request/response DTOs)

In hexagonal architecture, **all application-level DTOs belong in Application** (as ports). The API layer should only map to/from them.

**Affected files**:
- `Application/Contracts/AuctionDto.cs`
- `Application/Contracts/UserSummaryDto.cs`
- `WishlistApi/DTOs/WishlistDTOs.cs`
- `WishlistApi/DTOs/AuthDTOs.cs`
- `WishlistApi/DTOs/UserDTOs.cs`

**Fix**: Move all DTOs to `Application.Contracts`. The host layer handles the mapping between application DTOs and wire-format DTOs.

---

## Execution Plan

### Phase 1 — Fix Application layer dependencies

> **Goal**: Application no longer depends on Infrastructure or EF Core types.

1. **Remove Infrastructure project reference from `Application.csproj`**
   - This will immediately break the query classes and `UserService` — fix them next.

2. **Create port interfaces for read models in Application**
   - `Application/Queries/IAuctionReadModel.cs` — interface with `GetCurrentAuctionAsync`
   - `Application/Queries/IUserReadModel.cs` — interface with `GetUsersAsync`
   - Move the DTO records (`AuctionDto`, `UserSummaryDto`) into `Application.Contracts/`

3. **Move query implementations to Infrastructure as read adapters**
   - `Infrastructure/ReadAdapters/AuctionReadAdapter.cs` — implements `IAuctionReadModel`
   - `Infrastructure/ReadAdapters/UserReadAdapter.cs` — implements `IUserReadModel`
   - Delete `Application/Queries/AuctionQueries.cs` and `Application/Queries/UserQueries.cs`

4. **Fix `UserService` — remove Infrastructure import**
   - Remove `using Infrastructure.Persistence.Users;`
   - The `FullName` and `Address` value objects are already in Domain, so no change needed beyond removing the import.

5. **Wire up new implementations in `Program.cs`**
   - Register `IAuctionReadModel → AuctionReadAdapter`
   - Register `IUserReadModel → UserReadAdapter`
   - Remove old registrations for `AuctionQueries` / `UserQueries`

---

### Phase 2 — Clean up Domain layer

> **Goal**: Domain entities are pure — no repository dependencies, no data-mapping constructors.

6. **Refactor `WishlistItem.AddAsync()`**
   - Remove the `IWishlistItemRepository` parameter
   - Move the `AppIsOnWishlistAsync` check from `WishlistService.AddToWishlistAsync()` into the service method
   - `WishlistItem` should only have a simple constructor and the `CalculateStats` static method

7. **Remove data-mapping constructor from `Auction`**
   - Remove the constructor that takes `(id, dateAdded, currentPrice, startingPrice, status, userId, appListingId, rowVersion)`
   - `AuctionRepository.GetLatestAuctionAsync()` should create a domain object through a separate mapping method or factory
   - The two domain constructors to keep:
     - `Auction()` — parameterless (for EF mapping, internal)
     - `Auction(dateAdded, startingPrice, appListingId)` — for creating new auctions

---

### Phase 3 — Consolidate DTOs

> **Goal**: All application-level DTOs live in `Application.Contracts`. The host layer only maps between wire formats and application DTOs.

8. **Move DTOs from `WishlistApi.DTOs` to `Application.Contracts`**
   - `WishlistItemDto` → `Application.Contracts/WishlistItemDto.cs`
   - `Wishlist` → `Application.Contracts/Wishlist.cs`
   - `Stats` → `Application.Contracts/Stats.cs`
   - `RegisterRequest`, `LoginRequest`, `AuthResponse` → `Application.Contracts/AuthDtos.cs`
   - `UserDetailsDTO` → `Application.Contracts/UserDetailsDto.cs`

9. **Add mapping in WishlistApi controllers**
   - Each controller maps wire-format DTOs to application DTOs before calling services
   - Each controller maps application results back to wire-format responses
   - Consider a shared `Mapping` class or extension methods in WishlistApi

---

### Phase 4 — Update controllers and DI

> **Goal**: Controllers depend only on Application ports, not on Infrastructure types.

10. **Update `AuctionsController`**
    - Replace `IAuctionQueries` with `IAuctionReadModel`
    - The controller maps the application `AuctionDto` to the wire format (if needed)

11. **Update `UsersController`**
    - Replace `IUserQueries` with `IUserReadModel`
    - Remove any direct EF Core exception handling (`DbUpdateConcurrencyException`) — these should be caught at the Infrastructure boundary or translated to domain exceptions

12. **Update `Program.cs` DI registrations**
    - Remove: `AddScoped<IAuctionQueries, AuctionQueries>()`
    - Remove: `AddScoped<IUserQueries, UserQueries>()`
    - Add: `AddScoped<IAuctionReadModel, AuctionReadAdapter>()`
    - Add: `AddScoped<IUserReadModel, UserReadAdapter>()`

---

### Phase 5 — Update tests

> **Goal**: Tests compile and pass with the new structure.

13. **Update `Tests.csproj`** — no project reference changes needed (Tests already reference all layers)

14. **Update `ApplicationTests/UserQueriesTests.cs`** — test the new `IUserReadModel` interface or move to Infrastructure tests

15. **Update `ApplicationTests/AuctionTests.cs` and `AuctionConcurrencyTests.cs`** — ensure they still work with the refactored `Auction` class (removed data-mapping constructor)

16. **Update `DataAccessTests/AuctionRepoTest.cs`** — may need adjustments for the new `Auction` constructor

---

## Files Affected

| # | File | Change |
|---|------|--------|
| 1 | `Application/Application.csproj` | Remove Infrastructure project reference |
| 2 | `Application/Queries/IAuctionReadModel.cs` | **New** — port interface |
| 3 | `Application/Queries/IUserReadModel.cs` | **New** — port interface |
| 4 | `Application/Contracts/AuctionDto.cs` | **Move** from `Application.Contracts/` (no change) |
| 5 | `Application/Contracts/UserSummaryDto.cs` | **Move** from `Application.Contracts/` (no change) |
| 6 | `Application/Contracts/WishlistItemDto.cs` | **Move** from `WishlistApi.DTOs/` |
| 7 | `Application/Contracts/Wishlist.cs` | **Move** from `WishlistApi.DTOs/` |
| 8 | `Application/Contracts/Stats.cs` | **Move** from `WishlistApi.DTOs/` |
| 9 | `Application/Contracts/AuthDtos.cs` | **Move** from `WishlistApi.DTOs/` (combine into one file) |
| 10 | `Application/Contracts/UserDetailsDto.cs` | **Move** from `WishlistApi.DTOs/` |
| 11 | `Application/Queries/AuctionQueries.cs` | **Delete** — replaced by read adapter |
| 12 | `Application/Queries/UserQueries.cs` | **Delete** — replaced by read adapter |
| 13 | `Application/UserService.cs` | Remove `using Infrastructure.Persistence.Users;` |
| 14 | `Domain/WishlistItem.cs` | Remove `IWishlistItemRepository` from `AddAsync()` |
| 15 | `Domain/Auction.cs` | Remove data-mapping constructor |
| 16 | `Infrastructure/ReadAdapters/AuctionReadAdapter.cs` | **New** — implements `IAuctionReadModel` |
| 17 | `Infrastructure/ReadAdapters/UserReadAdapter.cs` | **New** — implements `IUserReadModel` |
| 18 | `WishlistApi/DTOs/*.cs` | **Delete** — moved to Application |
| 19 | `WishlistApi/Controllers/AuctionsController.cs` | Use `IAuctionReadModel`, add mapping |
| 20 | `WishlistApi/Controllers/UsersController.cs` | Use `IUserReadModel`, remove EF exception handling |
| 21 | `WishlistApi/Controllers/WishlistController.cs` | Add mapping to/from application DTOs |
| 22 | `WishlistApi/Controllers/AuthController.cs` | Add mapping to/from application DTOs |
| 23 | `WishlistApi/Program.cs` | Update DI registrations |
| 24 | `Tests/ApplicationTests/AuctionTests.cs` | Adjust for refactored `Auction` |
| 25 | `Tests/ApplicationTests/AuctionConcurrencyTests.cs` | Adjust for refactored `Auction` |
| 26 | `Tests/ApplicationTests/UserQueriesTests.cs` | Move to Infrastructure or update for new read model |
| 27 | `Tests/DataAccessTests/AuctionRepoTest.cs` | Adjust for new `Auction` constructor |

---

## Verification Checklist

After completing all phases:

- [ ] `Application.csproj` has zero project references to Infrastructure
- [ ] `Application.csproj` has zero EF Core NuGet packages
- [ ] No file in `Application/` has a `using Infrastructure.` directive
- [ ] No file in `Domain/` has a `using` for anything outside Domain
- [ ] `Domain` has no dependencies (no NuGet packages, no project references)
- [ ] All DTOs live in `Application.Contracts/`
- [ ] Controllers only depend on Application interfaces, not Infrastructure types
- [ ] `dotnet test api/WishlistApi/WishlistApi.sln` passes

---

## Impact on Testing

| Test Type | Change |
|-----------|--------|
| **Application unit tests** | Fewer mocks needed — no Infrastructure dependency. Services are pure use cases now. |
| **Infrastructure tests** | New tests for read adapters (`AuctionReadAdapter`, `UserReadAdapter`). Repository tests remain. |
| **Integration tests** | Largely unchanged — they test the full stack through the HTTP boundary. |

---

## Summary

| Severity | Count | Description |
|----------|-------|-------------|
| 🔴 Critical | 2 | Application depends on Infrastructure; Queries use DbContext directly |
| 🟡 Medium | 4 | UserService imports infra types; Domain entities depend on repos; Data-mapping constructors in Domain; DTOs scattered |

**Total: ~27 file changes across 4 projects**
