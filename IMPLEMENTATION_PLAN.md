# Implementation Plan: SteamTracker Price API + Wishlist BFF Pattern

## Architecture Goal

```
BEFORE:
  Frontend ──► WishlistApi ──► Shared Postgres (Dapper reads SteamTracker tables)
                         │
                         └──► SteamTracker (proxy for alerts only)

AFTER:
  Frontend ──► WishlistApi ──► WishlistApi own DB (wishlist items only)
         │                         │
         │                         └──► SteamTracker API (prices passthrough)
         │
         └──► SteamTracker API (prices passthrough)  [via WishlistApi]
```

WishlistApi becomes a **BFF (Backend for Frontend)** — the frontend makes two queries:
1. `GET /wishlist` → wishlist items (no price, no alert data)
2. `GET /api/prices?appIds=1,2,3` → price data (passthrough to SteamTracker, auth required)

---

## Phase 0: Pre-flight — understand current tests

- [x] Run existing WishlistApi tests: `dotnet test api/WishlistApi/WishlistApi.sln 2>&1 | tail -n 50`
  - Result: **101 passed, 1 skipped**
- [x] Run existing SteamTracker tests: `dotnet test api/SteamTracker/SteamTracker.slnx 2>&1 | tail -n 50`
  - Result: **176 passed** across all test projects
- [x] Note: `WishlistControllerTest.cs` mocks `ISharedDbPriceReader` — this test will need updating
- [x] Note: `AlertProxyEndpointTests.cs` tests proxy endpoints — unaffected by price changes

---

## Phase 1: SteamTracker — Add GET /api/games/prices endpoint

### 1.1 Red: Define the DTO and write a failing integration test

- [x] **Create DTO** in `SteamTracker.API`:
  ```csharp
  // ✅ Done: api/SteamTracker/src/SteamTracker.API/Models/GamePriceDto.cs
  namespace SteamTracker.API.Models;
  public record GamePriceDto(int AppId, decimal? Amount, string Currency, DateTimeOffset? LastCheckedAt, bool IsUnavailable);
  ```

- [x] **Write failing integration test** in `SteamTracker.API.Tests`:
  - [x] File: `api/SteamTracker/tests/SteamTracker.API.Tests/PriceEndpointTests.cs`
  - [x] Test: `GetPricesAsync_returnsPricesForGivenAppIds` — uses `TestApiFactory` + real Postgres (testcontainers)
  - [x] Assert: `GET /api/games/prices?appIds=42&appIds=100` returns `[{appId:42, amount:19.99, currency:"EUR", ...}, ...]`
  - [x] Test: `GetPricesAsync_returnsEmptyArray_whenNoAppIdsProvided`
  - [x] Test: `GetPricesAsync_returnsOnlyMatchingAppIds`
  - [x] Test: `GetPricesAsync_returnsUnavailableFlag` — verifies `IsUnavailable` flag

### 1.2 Green: Implement the endpoint

- [x] Add endpoint in `SteamTracker.API.Program.cs`:
  ```csharp
  // GET /api/games/prices?appIds=1&appIds=2
  api.MapGet("/games/prices", async (
      IGameRepository gameRepo,
      [FromQuery] int[] appIds) =>
  {
      if (appIds.Length == 0)
          return Results.Ok(Array.Empty<GamePriceDto>());

      var results = new List<GamePriceDto>();
      foreach (var appId in appIds)
      {
          var game = await gameRepo.GetAsync(new SteamAppId(appId));
          if (game != null)
          {
              results.Add(new GamePriceDto(
                  AppId: game.AppId.Value,
                  Amount: game.CurrentPrice?.Amount,
                  Currency: game.CurrentPrice?.Currency ?? "EUR",
                  LastCheckedAt: game.LastCheckedAt,
                  IsUnavailable: game.IsUnavailable
              ));
          }
      }
      return Results.Ok(results);
  });
  ```

- [x] Run `dotnet test api/SteamTracker/SteamTracker.slnx 2>&1 | tail -n 50` — all pass (176+ across all test projects, including 7 PriceEndpointTests)

---

## Phase 2: WishlistApi — Add passthrough prices endpoint (auth required)

### 2.1 Red: Write failing integration test for the passthrough

- [x] **Create test** in `WishlistApi/Tests/IntegrationTests/PricesPassthroughTests.cs`:
  - [x] Uses `WebApplicationFactory` with mocked SteamTracker HTTP client
  - [x] Test: `GetPrices_forwardsToSteamTracker_and_returns_response` — authenticated request
  - [x] Test: `GetPrices_forwards_appIds_query_params_correctly`
  - [x] Test: `GetPrices_returns_empty_when_steamtracker_returns_empty`
  - [x] Test: `GetPrices_returns_empty_when_no_appIds_provided`

### 2.2 Green: Implement the passthrough endpoint

- [x] **Create new controller** `api/WishlistApi/WishlistApi/Controllers/PricesController.cs`
  - Uses `IHttpClientFactory` with named "SteamTracker" client
  - `[Authorize]` — requires authentication
  - Case-insensitive JSON deserialization
  ```csharp
  [ApiController]
  [Route("api/[controller]")]
  [Authorize]  // Auth required — frontend calls this via the BFF pattern
  public class PricesController : ControllerBase
  {
      private readonly HttpClient _httpClient;
      private readonly string? _steamTrackerUri;

      public PricesController(HttpClient httpClient, IConfiguration configuration)
      {
          _httpClient = httpClient;
          _steamTrackerUri = configuration.GetValue<string>("SteamTrackerUri");
      }

      [HttpGet]
      public async Task<ActionResult<IEnumerable<GamePriceDto>>> GetPrices([FromQuery] IEnumerable<int> appIds)
      {
          if (string.IsNullOrEmpty(_steamTrackerUri) || !appIds.Any())
              return Ok(Array.Empty<GamePriceDto>());

          var query = string.Join("&", appIds.Select(a => $"appIds={a}"));
          var uri = $"{_steamTrackerUri}/api/games/prices?{query}";

          var response = await _httpClient.GetAsync(uri);
          var body = await response.Content.ReadAsStringAsync();

          if (!response.IsSuccessStatusCode)
              return StatusCode((int)response.StatusCode);

          return Ok(System.Text.Json.JsonSerializer.Deserialize<IEnumerable<GamePriceDto>>(body));
      }
  }
  ```

- [x] **Add DTO** to `WishlistApi/Application/Contracts/WishlistDtos.cs`
  - `GamePriceDto(int AppId, decimal? Amount, string Currency, DateTimeOffset? LastCheckedAt, bool IsUnavailable)`
  ```csharp
  public record GamePriceDto(int AppId, decimal? Amount, string Currency, DateTimeOffset? LastCheckedAt, bool IsUnavailable);
  ```

- [x] **Register the HttpClient** in `Program.cs`
  - Named client "SteamTracker" with base address from config
  ```csharp
  // Replace the existing SteamTrackerUri config usage — centralize it
  services.AddHttpClient("SteamTracker", client =>
  {
      client.BaseAddress = new Uri(configuration.GetValue<string>("SteamTrackerUri")!);
  });
  ```

- [x] Run `dotnet test api/WishlistApi/WishlistApi.sln 2>&1 | tail -n 50` — all pass (105 passed, 1 skipped)

---

## Phase 3: WishlistApi — Simplify GetWishlistAsync (remove price enrichment)

### 3.1 Red: Write failing unit test for the new simplified response

- [x] **Update** `WishlistApi/Tests/ControllerTests/WishlistControllerTest.cs`:
  - [x] Test: `GetWishlistAsync_returnsItemsWithoutPriceData` — verify `Price` and `LastCheckedAt` are null
  - [x] Test: `GetWishlistAsync_returnsItemsWithoutAlertData` — verify `AlertRuleId` and `AlertThreshold` are null
  - [x] The test should NOT mock `ISharedDbPriceReader` anymore

### 3.2 Green: Refactor `GetWishlistAsync` to remove price/alert enrichment

- [x] **Modify** `WishlistController.cs`:
  - [x] Remove `ISharedDbPriceReader` from constructor parameters
  - [x] Simplify `GetWishlistAsync` to only call `_getWishlistUseCase` and return items
  - [x] The returned `WishlistItemDto` now only contains: `AppId`, `DateAdded`, `Name`
  - [x] Removed the `fields` filtering logic — controller always returns all 3 core fields

- [x] **Update DTO** `WishlistItemDto` in `WishlistDtos.cs`:
  - [x] Remove `Price`, `PriceCurrency`, `LastCheckedAt`, `IsUnavailable`, `AlertRuleId`, `AlertThreshold`, `AlertCurrency`
  - [x] Keep only: `AppId`, `DateAdded`, `Name`

- [x] **Remove** `ISharedDbPriceReader` dependency from `Program.cs`:
  - [x] Removed `builder.Services.AddScoped<ISharedDbPriceReader, SharedDbPriceReader>();`
  - [x] Kept `Infrastructure.SharedDb` using for `SteamTrackerAlertProxy`

- [x] **Delete** `Infrastructure/SharedDb/SharedDbPriceReader.cs` and `Application/Contracts/ISharedDbPriceReader.cs`

- [x] **Additional cleanup**:
  - [x] Deleted `Tests/IntegrationTests/SharedDbPriceReaderIntegrationTests.cs` (tested the removed reader)
  - [x] Deleted `Tests/IntegrationTests/SharedDbApiFactory.cs` (only used by deleted tests)
  - [x] Updated `WishlistControllerBackfillTests.cs` — removed priceReader mock from constructor
  - [x] Updated `Tests/Helpers/ApiFactory.cs` — removed priceReader mock
  - [x] Updated `Tests/IntegrationTests/AlertProxyEndpointTests.cs` — removed SharedDbPriceReader setup

- [x] Run `dotnet test api/WishlistApi/WishlistApi.sln 2>&1 | tail -n 50` — **92 passed, 1 skipped**

---

## Phase 4: Frontend — Split the overview query into two calls

### 4.1 Red: Write failing component test (or verify build fails)

- [x] Created `frontend/src/components/wlItemsList.test.tsx` with 7 tests
- [x] Tests verify the component makes two separate API calls: `/wishlist` and `/api/prices`
- [x] Tests cover: normal prices, null prices ("—"), unavailable ("N/A"), free (0), empty wishlist, missing price data

### 4.2 Green: Update frontend to make two queries

- [x] **Modified** `frontend/src/components/wlItemsList.tsx`:
  - [x] Query 1: `GET /wishlist` → `WishlistItemResponse[]` (from WishlistApi, returns appId, name, dateAdded)
  - [x] Query 2: `GET /api/prices?appIds=1&appIds=2&...` → `GamePriceResponse[]` (from WishlistApi passthrough)
  - [x] Merge the two results client-side: build a `Map<number, GamePriceResponse>` and match by `appId`
  - [x] Updated `MergedWishlistItem` interface to include price fields (filled from query 2)
  - [x] Handle the case where an app has no price data (show "—")
  - [x] Changed prices query from `useSuspenseQuery` to `useQuery` (since it depends on the first query's data)
  - [x] Simplified alert button text to "Set alert" (alert data no longer returned by wishlist endpoint)

- [x] **No changes needed** to `frontend/src/routes/app/overview.tsx` — it just wraps `WLItemsList`

- [x] Build passes: `npm run build` ✓
- [x] All tests pass: 10 tests (7 new + 3 existing) ✓

---

## Phase 5: Cleanup & Verification

- [x] Remove `ISharedDbPriceReader` interface and `SharedDbPriceReader` class entirely from WishlistApi
- [x] Verify `SteamTrackerAlertProxy` still works (it's used for alert set/delete — unaffected)
- [x] Run full test suites:
  - [x] `dotnet test api/WishlistApi/WishlistApi.sln 2>&1 | tail -n 50` — **92 passed, 1 skipped**
  - [x] `dotnet test api/SteamTracker/SteamTracker.slnx 2>&1 | tail -n 50` — **180 passed** (fixed pre-existing package downgrade)
- [x] Run frontend tests: `cd frontend && npm run test 2>&1 | tail -n 50` — **10 passed**
- [x] Update API documentation — OVERVIEW.md updated with `/api/prices` endpoint and alert endpoints
- [x] Fixed pre-existing dependency issue: bumped `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging.Abstractions` to 10.0.10 in `SteamTracker.Integration.Tests.csproj`

---

## Risk / Notes

1. **`SteamTrackerAlertProxy`** — still needed for `POST /wishlist/{appId}/alert` and `DELETE /wishlist/{alertRuleId}/alert`. These proxy to SteamTracker's alert endpoints and are unaffected by this change.

2. **`SharedDbFixture.cs`** — the test fixture seeds `games` and `alert_rules` tables. After Phase 3, `SharedDbPriceReader` is removed, so the `games` table seeding can be removed from the fixture (or kept if SteamTracker tests still need it).

3. **`WishlistControllerTest.cs`** — currently mocks `ISharedDbPriceReader`. After Phase 3, this mock is no longer needed. The test should be simplified.

4. **No auth on `/api/prices`** — this is intentional per the BFF pattern. The frontend is the only consumer, and it already has a session via cookies. The passthrough endpoint doesn't expose sensitive data (only public game prices).

5. **Performance** — the frontend will now make 2 API calls instead of 1. For large wishlists, this is acceptable since both calls are fast (one is a simple EF query, the other is an HTTP passthrough).
