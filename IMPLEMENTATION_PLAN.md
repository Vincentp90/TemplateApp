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
2. `GET /api/prices?appIds=1,2,3` → price data (passthrough to SteamTracker, no auth needed)

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

- [ ] Add endpoint in `SteamTracker.API.Program.cs`:
  ```csharp
  // GET /api/games/prices?appIds=1&appIds=2
  api.MapGet("/games/prices", async (
      IGameRepository gameRepo,
      [FromQuery] IEnumerable<int> appIds) =>
  {
      var results = new List<GamePriceDto>();
      foreach (var appId in appIds)
      {
          var game = await gameRepo.GetAsync(new SteamAppId(appId));
          if (game != null)
          {
              results.Add(new GamePriceDto(
                  AppId: game.AppId.Value,
                  Amount: game.CurrentPrice?.Amount,
                  Currency: game.CurrentPrice?.Currency.ToString() ?? "EUR",
                  LastCheckedAt: game.LastCheckedAt,
                  IsUnavailable: game.IsUnavailable
              ));
          }
      }
      return results;
  });
  ```

- [ ] Run `dotnet test api/SteamTracker/SteamTracker.slnx 2>&1 | tail -n 50` — all pass

> **Status**: Red phase files created. Tests will fail with 404 (endpoint doesn't exist yet). Green phase = implement the endpoint and verify tests pass.

---

## Phase 2: WishlistApi — Add passthrough prices endpoint (no auth)

### 2.1 Red: Write failing integration test for the passthrough

- [ ] **Create test** in `WishlistApi/Tests/IntegrationTests/PricesPassthroughTests.cs`:
  - [ ] Uses `WebApplicationFactory` with mocked SteamTracker HTTP client
  - [ ] Test: `GetPrices_forwardsToSteamTracker_and_returns_response` — no auth required
  - [ ] Test: `GetPrices_forwards_appIds_query_params_correctly`
  - [ ] Test: `GetPrices_returns_empty_when_steamtracker_returns_empty`

### 2.2 Green: Implement the passthrough endpoint

- [ ] **Create new controller** `api/WishlistApi/WishlistApi/Controllers/PricesController.cs`:
  ```csharp
  [ApiController]
  [Route("api/[controller]")]
  [AllowAnonymous]  // No auth needed — frontend calls this directly
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

- [ ] **Add DTO** to `WishlistApi/Application/Contracts/WishlistDtos.cs`:
  ```csharp
  public record GamePriceDto(int AppId, decimal? Amount, string Currency, DateTimeOffset? LastCheckedAt, bool IsUnavailable);
  ```

- [ ] **Register the HttpClient** in `Program.cs` (similar to existing `AddHttpClient<ISteamTrackerAlertProxy>`, but for `PricesController`):
  ```csharp
  // Replace the existing SteamTrackerUri config usage — centralize it
  services.AddHttpClient("SteamTracker", client =>
  {
      client.BaseAddress = new Uri(configuration.GetValue<string>("SteamTrackerUri")!);
  });
  ```

- [ ] Run `dotnet test api/WishlistApi/WishlistApi.sln 2>&1 | tail -n 50` — all pass

---

## Phase 3: WishlistApi — Simplify GetWishlistAsync (remove price enrichment)

### 3.1 Red: Write failing unit test for the new simplified response

- [ ] **Update** `WishlistApi/Tests/ControllerTests/WishlistControllerTest.cs`:
  - [ ] Test: `GetWishlistAsync_returnsItemsWithoutPriceData` — verify `Price` and `LastCheckedAt` are null
  - [ ] Test: `GetWishlistAsync_returnsItemsWithoutAlertData` — verify `AlertRuleId` and `AlertThreshold` are null
  - [ ] The test should NOT mock `ISharedDbPriceReader` anymore

### 3.2 Green: Refactor `GetWishlistAsync` to remove price/alert enrichment

- [ ] **Modify** `WishlistController.cs`:
  - [ ] Remove `ISharedDbPriceReader` from constructor parameters
  - [ ] Simplify `GetWishlistAsync` to only call `_getWishlistUseCase` and return items
  - [ ] The returned `WishlistItemDto` should only contain: `AppId`, `DateAdded`, `Name`
  - [ ] Remove the `fields` filtering logic for price/alert fields (or keep it for the 3 core fields)

- [ ] **Update DTO** `WishlistItemDto` in `WishlistDtos.cs`:
  - [ ] Remove `Price`, `PriceCurrency`, `LastCheckedAt`, `IsUnavailable`, `AlertRuleId`, `AlertThreshold`, `AlertCurrency`
  - [ ] Keep only: `AppId`, `DateAdded`, `Name`

- [ ] **Remove** `ISharedDbPriceReader` dependency from `Program.cs`:
  - [ ] Remove `builder.Services.AddScoped<ISharedDbPriceReader, SharedDbPriceReader>();`
  - [ ] Remove the `SharedDb` namespace references if unused

- [ ] **Delete** `Infrastructure/SharedDb/SharedDbPriceReader.cs` and `Application/Contracts/ISharedDbPriceReader.cs` (if no other consumers)

- [ ] Run `dotnet test api/WishlistApi/WishlistApi.sln 2>&1 | tail -n 50` — all pass

---

## Phase 4: Frontend — Split the overview query into two calls

### 4.1 Red: Write failing component test (or verify build fails)

- [ ] The current `WLItemsList` component calls `/wishlist?fields=appid,name,dateadded,price,...` and expects price/alert data
- [ ] After the backend changes, this call will return null for price fields → component should show "—" or "N/A" for prices
- [ ] Verify the build fails or the component renders with null prices

### 4.2 Green: Update frontend to make two queries

- [ ] **Modify** `frontend/src/components/wlItemsList.tsx`:
  - [ ] Query 1: `GET /wishlist?fields=appid,name,dateadded` → `WishlistItem[]` (from WishlistApi)
  - [ ] Query 2: `GET /api/prices?appIds=1&appIds=2&...` → `GamePriceDto[]` (from WishlistApi passthrough)
  - [ ] Merge the two results client-side: match by `appId`
  - [ ] Update `MergedWishlistItem` interface to still include price fields (filled from query 2)
  - [ ] Handle the case where an app has no price data (show "—")

- [ ] **Update** `frontend/src/routes/app/overview.tsx` if needed (no changes expected — it just wraps `WLItemsList`)

---

## Phase 5: Cleanup & Verification

- [ ] Remove `ISharedDbPriceReader` interface and `SharedDbPriceReader` class entirely from WishlistApi
- [ ] Verify `SteamTrackerAlertProxy` still works (it's used for alert set/delete — unaffected)
- [ ] Run full test suites:
  - [ ] `dotnet test api/WishlistApi/WishlistApi.sln 2>&1 | tail -n 50`
  - [ ] `dotnet test api/SteamTracker/SteamTracker.slnx 2>&1 | tail -n 50`
- [ ] Run frontend tests: `cd frontend && npm run test 2>&1 | tail -n 50`
- [ ] Update API documentation / Swagger (if auto-generated, it should pick up the new endpoints)

---

## Risk / Notes

1. **`SteamTrackerAlertProxy`** — still needed for `POST /wishlist/{appId}/alert` and `DELETE /wishlist/{alertRuleId}/alert`. These proxy to SteamTracker's alert endpoints and are unaffected by this change.

2. **`SharedDbFixture.cs`** — the test fixture seeds `games` and `alert_rules` tables. After Phase 3, `SharedDbPriceReader` is removed, so the `games` table seeding can be removed from the fixture (or kept if SteamTracker tests still need it).

3. **`WishlistControllerTest.cs`** — currently mocks `ISharedDbPriceReader`. After Phase 3, this mock is no longer needed. The test should be simplified.

4. **No auth on `/api/prices`** — this is intentional per the BFF pattern. The frontend is the only consumer, and it already has a session via cookies. The passthrough endpoint doesn't expose sensitive data (only public game prices).

5. **Performance** — the frontend will now make 2 API calls instead of 1. For large wishlists, this is acceptable since both calls are fast (one is a simple EF query, the other is an HTTP passthrough).
