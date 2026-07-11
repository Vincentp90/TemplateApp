# Plan: Replace Services with Use Cases in WishlistApi Application Layer

## Current State

The application layer currently uses **5 service classes** that directly orchestrate domain logic, repository calls, and external interactions:

| Service | Responsibility | Controller Dependency |
|---------|---------------|----------------------|
| `AuthService` | Login / Register | `AuthController` |
| `WishlistService` | CRUD wishlist items + stats + events | `WishlistController` |
| `UserService` | User queries + profile updates | `UsersController` |
| `AppListingService` | Steam game search + seed data | `AppListingsController` |
| `AuctionService` | Auction lifecycle + bids + simulation | `AuctionsController` |

**Problems with the service approach:**
- Each service is a god object — one class handles all operations for a bounded context
- No explicit request/response contracts per operation (commands are plain records, not use case inputs)
- Controllers must know about multiple services and wire them together (e.g., `WishlistController` depends on 4 services)
- Hard to unit-test individual operations in isolation
- No standard pattern for validation, preconditions, or postconditions per use case

## Target Architecture

Replace the 5 services with **explicit use case classes**, each representing a single controller action's business logic.

```
Application/
├── UseCases/
│   ├── Auth/
│   │   ├── RegisterUserUseCase.cs
│   │   └── LoginUserUseCase.cs
│   ├── Wishlist/
│   │   ├── GetWishlistUseCase.cs
│   │   ├── AddWishlistItemUseCase.cs
│   │   ├── DeleteWishlistItemUseCase.cs
│   │   ├── GetWishlistStatsUseCase.cs
│   │   ├── PublishBackfillEventUseCase.cs
│   │   └── SetAlertRuleUseCase.cs
│   ├── User/
│   │   ├── GetUserProfileUseCase.cs
│   │   ├── UpdateUserProfileUseCase.cs
│   │   ├── GetPaginatedUsersUseCase.cs
│   │   └── GetInternalUserIdUseCase.cs
│   ├── AppListing/
│   │   ├── SearchAppListingsUseCase.cs
│   │   ├── GetRandomAppListingUseCase.cs
│   │   └── EnsureAppListingsPopulatedUseCase.cs
│   └── Auction/
│       ├── PlaceBidUseCase.cs
│       ├── StartNextAuctionUseCase.cs
│       └── SimulateBidUseCase.cs
├── UseCases/
│   ├── Requests/              # Request DTOs (replaces Commands folder)
│   │   ├── Auth/
│   │   │   ├── RegisterUserRequest.cs
│   │   │   └── LoginUserRequest.cs
│   │   ├── Wishlist/
│   │   │   ├── AddWishlistItemRequest.cs
│   │   │   └── DeleteWishlistItemRequest.cs
│   │   ├── User/
│   │   │   ├── GetUserProfileRequest.cs
│   │   │   └── UpdateUserProfileRequest.cs
│   │   ├── Auction/
│   │   │   ├── PlaceBidRequest.cs
│   │   │   └── StartNextAuctionRequest.cs
│   │   └── AppListing/
│   │       └── SearchAppListingsRequest.cs
│   ├── Responses/             # Response DTOs (replaces Contracts folder)
│   │   ├── Auth/
│   │   │   └── LoginUserResponse.cs
│   │   ├── Wishlist/
│   │   │   └── GetWishlistResponse.cs
│   │   ├── User/
│   │   │   └── GetUserProfileResponse.cs
│   │   └── Auction/
│   │       └── PlaceBidResponse.cs
│   ├── IUseCase.cs            # Common interface (optional, for DI)
│   └── Exceptions/
│       ├── UseCaseException.cs
│       └── ConflictException.cs
├── Contracts/                  # Shared DTOs (kept — AuctionDto, WishlistDtos, etc.)
├── Queries/                    # Read model interfaces (kept — IAuctionReadModel, IUserReadModel)
├── Events/                     # Domain event records (kept — WishlistItemAdded, etc.)
├── IEventPublisher.cs         # Port interface (kept)
└── Commands/                   # DEPRECATED — migrate to UseCases/Requests/
```

## Use Case Design

Each use case follows a consistent pattern:

```csharp
// Example: AddWishlistItemUseCase.cs
namespace Application.UseCases.Wishlist;

public class AddWishlistItemUseCase(
    IWishlistItemRepository wishlistItemRepository,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher)
{
    public async Task ExecuteAsync(AddWishlistItemRequest request)
    {
        // 1. Validation / preconditions
        if (await wishlistItemRepository.AppIsOnWishlistAsync(request.UserId, request.AppId))
            throw new ConflictException("Item already on wishlist");

        // 2. Domain logic
        var wishlistItem = Domain.WishlistItem.CreateNew(request.UserId, request.AppId);
        await wishlistItemRepository.AddWishlistItemAsync(wishlistItem);
        await unitOfWork.SaveChangesAsync();

        // 3. Postconditions / side effects
        await eventPublisher.PublishAsync(new Events.WishlistItemAdded(
            request.UserId.ToString(),
            request.AppId,
            wishlistItem.DateAdded));
    }
}

public record AddWishlistItemRequest(int UserId, int AppId);
```

### Use Case → Controller Action Mapping

| Controller Action | Use Case | Request | Response |
|-------------------|----------|---------|----------|
| `POST /auth/register` | `RegisterUserUseCase` | `RegisterUserRequest` (from DTO) | `Ok()` |
| `POST /auth/login` | `LoginUserUseCase` | `LoginUserRequest` (from DTO) | `LoginUserResponse` |
| `GET /wishlist` | `GetWishlistUseCase` | `GetWishlistRequest` (query params) | `Wishlist` (from Contracts) |
| `GET /wishlist/stats` | `GetWishlistStatsUseCase` | `GetWishlistStatsRequest` | `Stats` (from Contracts) |
| `POST /wishlist/{appId}` | `AddWishlistItemUseCase` | `AddWishlistItemRequest` | `Ok()` |
| `DELETE /wishlist/{appId}` | `DeleteWishlistItemUseCase` | `DeleteWishlistItemRequest` | `Ok()` |
| `POST /wishlist/_backfill` | `PublishBackfillEventUseCase` | `BackfillRequest` | `{ Count }` |
| `POST /wishlist/{appId}/alert` | `SetAlertRuleUseCase` | `SetAlertRuleRequest` | `Ok()` |
| `DELETE /wishlist/{alertId}/alert` | `DeleteAlertRuleUseCase` | `DeleteAlertRuleRequest` | `Ok()` |
| `GET /users/me` | `GetUserProfileUseCase` | `GetUserProfileRequest` | `UserDetailsDto` |
| `PATCH /users/me` | `UpdateUserProfileUseCase` | `UpdateUserProfileRequest` | `Ok()` |
| `GET /users/{id}` | `GetUserProfileUseCase` | `GetUserProfileRequest` | `UserDetailsDto` |
| `PATCH /users/{id}` | `UpdateUserProfileUseCase` | `UpdateUserProfileRequest` | `Ok()` |
| `GET /users` | `GetPaginatedUsersUseCase` | `GetPaginatedUsersRequest` | `{ items, hasNextPage }` |
| `GET /auctions/current` | `GetCurrentAuctionUseCase` | `GetCurrentAuctionRequest` | `AuctionDto` |
| `POST /auctions/current` | `PlaceBidUseCase` | `PlaceBidRequest` (from DTO) | `Ok()` |
| `GET /auctions/current/SimulateBid` | `SimulateBidUseCase` | — | `Ok()` |
| `GET /applistings/search/{term}` | `SearchAppListingsUseCase` | `SearchAppListingsRequest` | `List<AppListingDto>` |

### Background / Infrastructure Operations

These are called from hosted services, not controllers, but still benefit from use case extraction:

| Caller | Use Case |
|--------|----------|
| `AuctionBackgroundService` | `StartNextAuctionUseCase` |
| `SteamUpdaterService` | `EnsureAppListingsPopulatedUseCase` |
| `AuctionService.SimulateBid()` | `SimulateBidUseCase` |

## Implementation Status

### Phase 2: Use Case Implementation — ✅ COMPLETE

All use case classes have been implemented under `Application/UseCases/`:

| Bounded Context | Use Cases | Status |
|-----------------|-----------|--------|
| Auth (2) | `RegisterUserUseCase`, `LoginUserUseCase` | ✅ Done |
| Wishlist (6) | `GetWishlistUseCase`, `AddWishlistItemUseCase`, `DeleteWishlistItemUseCase`, `GetWishlistStatsUseCase`, `PublishBackfillEventUseCase`, `SetAlertRuleUseCase`, `DeleteAlertRuleUseCase` | ✅ Done |
| User (3) | `GetUserProfileUseCase`, `UpdateUserProfileUseCase`, `GetPaginatedUsersUseCase` | ✅ Done |
| AppListing (3) | `SearchAppListingsUseCase`, `GetRandomAppListingUseCase`, `EnsureAppListingsPopulatedUseCase` | ✅ Done |
| Auction (3) | `PlaceBidUseCase`, `StartNextAuctionUseCase`, `SimulateBidUseCase` | ✅ Done |

All request DTOs created under `UseCases/*/Requests/`.
All unit tests created and passing (117 passed, 1 skipped).

### Phase 3: Controller Refactoring — ✅ COMPLETE

All 5 controllers have been refactored to inject and call use case interfaces directly. Both hosted services (`AuctionBackgroundService`, `SteamUpdaterService`) also updated.

### Phase 4: DI Registration Update — ✅ COMPLETE

`Program.cs` now registers all 18 use case classes with their interfaces.

### Phase 5: Test Migration — ✅ COMPLETE

All unit tests have been created/updated to target the new use case classes.

### Phase 6: Cleanup — ⏳ TODO

Delete old service files (`AuthService.cs`, `WishlistService.cs`, etc.), `Commands/` folder, update `OVERVIEW.md`.

## Implementation Steps

### Phase 1: Foundation (no breaking changes yet)

1. **Create `UseCases/` folder structure** with subfolders per bounded context
2. **Create `IUseCase<TRequest, TResponse>` interface** (optional — can skip if not using MediatR):
   ```csharp
   public interface IUseCase<TRequest, TResponse>
   {
       Task<TResponse> ExecuteAsync(TRequest request);
   }
   ```
3. **Create exception classes** in `UseCases/Exceptions/`
4. **Migrate one simple use case** as a proof-of-concept (e.g., `GetWishlistStatsUseCase` — no side effects, easy to verify)

### Phase 2: Migrate Each Use Case (one per PR or batch)

For each service, convert to use cases in this order:

#### 2.1 `AuthService` → 2 use cases
- `RegisterUserUseCase` — takes `RegisterUserCommand` → returns `void`
- `LoginUserUseCase` — takes `LoginCommand` → returns `LoginResult`
- **No behavioral change** — copy logic verbatim from `AuthService`

#### 2.2 `WishlistService` → 5 use cases
- `GetWishlistUseCase` — takes `int userId` + field filters → returns `List<WishlistItem>`
- `AddWishlistItemUseCase` — takes `AddToWishlistCommand` → returns `void`
- `DeleteWishlistItemUseCase` — takes `int userId, int appId` → returns `void`
- `GetWishlistStatsUseCase` — takes `int userId` → returns `WishlistStats`
- `PublishBackfillEventUseCase` — takes `WishlistItem` → returns `void`
- **Note**: `GetWishlistUseCase` returns domain entities only — the controller still handles price/alert merging (that's controller responsibility, not a use case)

#### 2.3 `UserService` → 3 use cases
- `GetUserProfileUseCase` — takes `GetUserCommand` → returns `User`
- `UpdateUserProfileUseCase` — takes `UpdateUserDetailsCommand` → returns `void`
- `GetPaginatedUsersUseCase` — takes page/limit → returns `List<UserSummaryDto>`
- **Note**: `GetInternalUserIdAsync` with caching can either stay as a helper in the use cases, or become its own `GetInternalUserIdUseCase`

#### 2.4 `AppListingService` → 3 use cases
- `SearchAppListingsUseCase` — takes `string term` → returns `List<AppListingDto>`
- `GetRandomAppListingUseCase` — returns `AppListing`
- `EnsureAppListingsPopulatedUseCase` — takes `CancellationToken` → returns `void`

#### 2.5 `AuctionService` → 3 use cases
- `PlaceBidUseCase` — takes `PlaceBidCommand` → returns `void`
- `StartNextAuctionUseCase` — returns `void`
- `SimulateBidUseCase` — returns `void`
- **Note**: `SimulateBidUseCase` internally calls `LoginUserUseCase` + `PlaceBidUseCase` — this is **composition of use cases**, not services calling services

### Phase 3: Controller Refactoring

For each controller, replace service calls with use case calls:

```csharp
// Before (AuthController.cs)
public class AuthController(IAuthService authService, ...) {
    [HttpPost("register")]
    public async Task<ActionResult> RegisterAsync(RegisterRequest request) {
        await authService.AddUserAsync(new RegisterUserCommand(...));
        return Ok();
    }
}

// After
public class AuthController(IRegisterUserUseCase registerUserUseCase, ...) {
    [HttpPost("register")]
    public async Task<ActionResult> RegisterAsync(RegisterRequest request) {
        await registerUserUseCase.ExecuteAsync(request);
        return Ok();
    }
}
```

**Key controller changes:**
- Controllers become thin — 1 method → 1 use case call
- DTO-to-request mapping happens at the controller boundary
- Error handling (DomainException → HTTP status) stays in the controller
- SignalR broadcasts stay in the controller (not in the use case)

### Phase 4: DI Registration Update

Update `Program.cs` to register use cases instead of services:

```csharp
// Before
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();

// After
builder.Services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
builder.Services.AddScoped<ILoginUserUseCase, LoginUserUseCase>();
builder.Services.AddScoped<IGetWishlistUseCase, GetWishlistUseCase>();
// ... etc.
```

### Phase 5: Test Migration

For each migrated use case:
1. **Copy existing unit tests** from `ApplicationTests/` into new test methods targeting the use case class directly
2. **Rename test classes** to match: `WishlistServiceTests` → `GetWishlistUseCaseTests`, `AddWishlistItemUseCaseTests`, etc.
3. **Update mock setup** — same repositories, same uow, same event publisher — just different class under test
4. **Integration tests** in `ControllerTests/` and `IntegrationTests/` should continue to work — the HTTP surface hasn't changed

### Phase 6: Cleanup

1. **Delete service interfaces** (`IAuthService`, `IWishlistService`, etc.)
2. **Delete service implementations** (`AuthService.cs`, `WishlistService.cs`, etc.)
3. **Delete `Commands/` folder** — migrate all commands to `UseCases/Requests/`
4. **Update `OVERVIEW.md`** to reflect new architecture
5. **Run full test suite** to confirm nothing is broken

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Controllers become bloated with DTO-to-request mapping | Keep mapping minimal; if it's complex, add a mapper class |
| Use case composition (`SimulateBidUseCase` calls `LoginUserUseCase`) creates circular dependencies | Use case composition is fine — each use case depends on its own dependencies (repos, etc.), not on other use cases. If composition is needed, inject the other use case. |
| `GetWishlist` is complex (merges prices + alerts) | Keep price/alert merging in the controller; the use case only returns domain entities |
| Large diff in a single PR | Migrate one service → one PR at a time |
| Background services still reference old services | Update `AuctionBackgroundService` and `SteamUpdaterService` in Phase 3 |

## Files to Create (New)

```
Application/UseCases/Auth/RegisterUserUseCase.cs
Application/UseCases/Auth/LoginUserUseCase.cs
Application/UseCases/Auth/Requests/RegisterUserRequest.cs
Application/UseCases/Auth/Requests/LoginUserRequest.cs

Application/UseCases/Wishlist/GetWishlistUseCase.cs
Application/UseCases/Wishlist/AddWishlistItemUseCase.cs
Application/UseCases/Wishlist/DeleteWishlistItemUseCase.cs
Application/UseCases/Wishlist/GetWishlistStatsUseCase.cs
Application/UseCases/Wishlist/PublishBackfillEventUseCase.cs
Application/UseCases/Wishlist/SetAlertRuleUseCase.cs
Application/UseCases/Wishlist/DeleteAlertRuleUseCase.cs
Application/UseCases/Wishlist/Requests/AddWishlistItemRequest.cs
Application/UseCases/Wishlist/Requests/DeleteWishlistItemRequest.cs

Application/UseCases/User/GetUserProfileUseCase.cs
Application/UseCases/User/UpdateUserProfileUseCase.cs
Application/UseCases/User/GetPaginatedUsersUseCase.cs
Application/UseCases/User/Requests/GetUserProfileRequest.cs
Application/UseCases/User/Requests/UpdateUserProfileRequest.cs

Application/UseCases/AppListing/SearchAppListingsUseCase.cs
Application/UseCases/AppListing/GetRandomAppListingUseCase.cs
Application/UseCases/AppListing/EnsureAppListingsPopulatedUseCase.cs
Application/UseCases/AppListing/Requests/SearchAppListingsRequest.cs

Application/UseCases/Auction/PlaceBidUseCase.cs
Application/UseCases/Auction/StartNextAuctionUseCase.cs
Application/UseCases/Auction/SimulateBidUseCase.cs
Application/UseCases/Auction/Requests/PlaceBidRequest.cs
Application/UseCases/Auction/Requests/StartNextAuctionRequest.cs

Application/UseCases/IUseCase.cs (optional)
Application/UseCases/Exceptions/ConflictException.cs
Application/UseCases/Exceptions/UseCaseException.cs
```

## Files to Delete (After Migration)

```
Application/AuthService.cs
Application/WishlistService.cs
Application/UserService.cs
Application/AppListingService.cs
Application/AuctionService.cs
Application/Commands/AuthCommands.cs       → migrate contents to UseCases/Auth/Requests/
Application/Commands/PlaceBidCommand.cs     → migrate contents to UseCases/Auction/Requests/
Application/Commands/UserCommands.cs        → migrate contents to UseCases/User/Requests/
Application/Commands/WishlistItemCommands.cs → migrate contents to UseCases/Wishlist/Requests/
```

## Files to Modify

| File | Changes |
|------|---------|
| `WishlistApi/Program.cs` | Replace service registrations with use case registrations |
| `WishlistApi/Controllers/AuthController.cs` | Replace `IAuthService` with `IRegisterUserUseCase` / `ILoginUserUseCase` |
| `WishlistApi/Controllers/WishlistController.cs` | Replace `IWishlistService` with individual use case interfaces |
| `WishlistApi/Controllers/UsersController.cs` | Replace `IUserService` with individual use case interfaces |
| `WishlistApi/Controllers/AuctionsController.cs` | Replace `IAuctionService` with individual use case interfaces |
| `WishlistApi/Controllers/AppListingsController.cs` | Replace `IAppListingService` with individual use case interfaces |
| `WishlistApi/HostedServices/AuctionBackgroundService.cs` | Replace `IAuctionService` with `StartNextAuctionUseCase` |
| `WishlistApi/HostedServices/SteamUpdaterService.cs` | Replace `IAppListingService` with `EnsureAppListingsPopulatedUseCase` |
| `Tests/ApplicationTests/WishlistServiceTests.cs` | Split into `GetWishlistUseCaseTests.cs`, `AddWishlistItemUseCaseTests.cs`, etc. |
| `Tests/ApplicationTests/AuctionTests.cs` | Split into `PlaceBidUseCaseTests.cs`, `SimulateBidUseCaseTests.cs`, etc. |
| `Tests/ApplicationTests/AppListingServiceTests.cs` | Split into `SearchAppListingsUseCaseTests.cs`, `EnsureAppListingsPopulatedUseCaseTests.cs`, etc. |
