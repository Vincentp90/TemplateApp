# SteamTracker — Defensive Programming Refactor Checklist

task to optimise defensive programming in this solution.
After each task done, run test and mark checkbox before continueing to the next task.

Expanded explanation: api/SteamTracker/DEFENSIVE_PROGRAMMING_ANALYSIS.md

## ❌ Remove (Unnecessary Defensive Checks)

- [ ] **1. `PriceAlertEvaluator.Evaluate` — Remove `rules is null` check**
  - File: `SteamTracker.Domain/Services/PriceAlertEvaluator.cs`
  - Remove `|| rules is null` from guard condition
  - Keep `game.CurrentPrice is null` — that's a valid business guard

---

## ❌ Add (Strict Type Validations)

- [ ] **2. `Money` — Reject negative amounts**
  - File: `SteamTracker.Domain/ValueObjects/Money.cs`
  - Add `if (amount < 0m) throw new ArgumentException(...)` in constructor

- [ ] **3. `UserId` — Reject `Guid.Empty`**
  - File: `SteamTracker.Domain/ValueObjects/UserId.cs`
  - Add `if (value == Guid.Empty) throw new ArgumentException(...)` in constructor

- [ ] **4. `Money.Currency` — Use `CurrencyCode` value object**
  - File: `SteamTracker.Domain/ValueObjects/CurrencyCode.cs` (new)
  - Closed set of valid codes: EUR, USD, GBP, RUB, BRL, etc.
  - Replace `string` parameter in `Money` constructor

- [ ] **5. `Game.Name` / `AlertRule.UserId` — Use `NonEmptyString` value object**
  - File: `SteamTracker.Domain/ValueObjects/NonEmptyString.cs` (new)
  - Reject `string.IsNullOrWhiteSpace` at construction
  - Replace `string` properties in `Game` and `AlertRule`

---

## ⚠️ Improve (Exception Handling)

- [ ] **6. `SetAlertRuleUseCase` — Use domain-specific exception**
  - File: `SteamTracker.Application/UseCases/SetAlertRuleUseCase.cs`
  - Replace `InvalidOperationException` with `TrackingNotFoundException` or `GameNotActiveException`

- [ ] **7. `DeleteAlertRuleUseCase` — Use domain-specific exception**
  - File: `SteamTracker.Application/UseCases/DeleteAlertRuleUseCase.cs`
  - Replace `InvalidOperationException` with `AlertRuleNotFoundException` or `UnauthorizedAccessException`

- [ ] **8. `ExceptionHandler` — Map exceptions to correct HTTP status codes**
  - File: `SteamTracker.API/ExceptionHandler.cs`
  - `InvalidOperationException` → 404
  - `ArgumentException` → 400
  - `SteamRateLimitException` → 429
  - All others → 500

---

## ℹ️ Also Consider (not in the 8 points, but worth noting)

- [ ] **Bonus: `PriceCheckConsumer` / `WishlistSyncConsumer` — Classify exceptions**
  - File: `SteamTracker.Worker/Worker.cs`
  - Transient errors → requeue
  - Programming errors → dead-letter (requeue: false)
  - Prevents infinite requeue loops

- [ ] **Bonus: `SteamStoreClient` — Validate missing game name**
  - File: `SteamTracker.Infrastructure/External/SteamStoreClient.cs`
  - Currently silently uses `string.Empty` for missing names
