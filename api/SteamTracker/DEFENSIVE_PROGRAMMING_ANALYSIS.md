# Defensive Programming Analysis — SteamTracker

**Date:** 2026-07-05  
**Scope:** All source files across Domain, Application, Infrastructure, API, and Worker layers.

---

## Executive Summary

The codebase follows a clean architecture with good use of value objects (`SteamAppId`, `UserId`, `Money`) to enforce invariants at construction time. However, several defensive checks are **unnecessary** (caller contract violations) and some **missing validations** could be enforced with strict types instead of runtime null checks.

### Quick Reference

| Category | Verdict |
|---|---|
| Value object guards (`SteamAppId`, `UserId`, `Money`) | ✅ Good — but `Money` needs more validation |
| Null checks on repository return values | ⚠️ Mostly fine (nullable = expected absence) |
| Null checks on method parameters | ❌ Unnecessary — should be strict types / caller contract |
| `InvalidOperationException` for invariant violations | ✅ Correct |
| Global `ExceptionHandler` swallowing everything | ⚠️ Too broad — should be selective |
| Exception propagation to workers | ⚠️ Silently swallowed in some places |

---

## 1. UNNECESSARY DEFENSIVE CHECKS

These checks guard against caller contract violations. They should be removed and replaced with strict typing or let exceptions propagate.

### 1.1 `PriceAlertEvaluator.Evaluate` — `rules is null` check

**File:** `SteamTracker.Domain/Services/PriceAlertEvaluator.cs` (line ~13)

```csharp
public IEnumerable<AlertRule> Evaluate(Game game, IEnumerable<AlertRule> rules)
{
    if (game.CurrentPrice is null || rules is null)  // ← rules is null check
        return [];
    ...
}
```

**Verdict:** ❌ Remove `rules is null` check. The caller (`ProcessPriceCheckUseCase`) always passes a non-null list from the repository. `game.CurrentPrice is null` is a valid business guard — keep it.

**Fix:**
```csharp
public IEnumerable<AlertRule> Evaluate(Game game, IEnumerable<AlertRule> rules)
{
    if (game.CurrentPrice is null)
        return [];
    return rules
        .Where(r => r.IsActive)
        .Where(r => r.ShouldTrigger(game.CurrentPrice!.Value))
        .ToList();
}
```

---

### 1.2 `SetAlertRuleUseCase` — `InvalidOperationException` instead of domain exception

**File:** `SteamTracker.Application/UseCases/SetAlertRuleUseCase.cs` (line ~35)

```csharp
if (trackedGame is null || !trackedGame.IsActive)
    throw new InvalidOperationException($"No active tracking for AppId {appId}.");
```

**Verdict:** ⚠️ `InvalidOperationException` is too generic. Use a domain-specific exception like `TrackingNotFoundException` or `GameNotActiveException` so callers can distinguish this from a programming error.

---

### 1.3 `DeleteAlertRuleUseCase` — `InvalidOperationException` instead of domain exception

**File:** `SteamTracker.Application/UseCases/DeleteAlertRuleUseCase.cs` (line ~22)

```csharp
if (rule is null || rule.UserId != userId)
    throw new InvalidOperationException($"Alert rule {alertRuleId} not found for user {userId}.");
```

**Verdict:** ⚠️ Same as above. Use `AlertRuleNotFoundException` or `UnauthorizedAccessException`.

---

## 2. MISSING VALIDATIONS (should be in value objects, not defensive checks)

These are gaps where the value object should enforce invariants at construction time, preventing invalid states entirely.

### 2.1 `Money` — No negative amount validation

**File:** `SteamTracker.Domain/ValueObjects/Money.cs`

```csharp
public Money(decimal amount, string currency = "EUR")
{
    Amount = amount;  // ← No validation!
    Currency = currency ?? throw new ArgumentNullException(nameof(currency));
}
```

**Verdict:** ❌ `Amount` can be negative. Prices should never be negative.

**Fix:**
```csharp
public Money(decimal amount, string currency = "EUR")
{
    if (amount < 0m)
        throw new ArgumentException("Money amount cannot be negative.", nameof(amount));
    Amount = amount;
    Currency = currency ?? throw new ArgumentNullException(nameof(currency));
}
```

---

### 2.2 `UserId` — No Guid.Empty check

**File:** `SteamTracker.Domain/ValueObjects/UserId.cs`

```csharp
public UserId(Guid value) => Value = value;
```

**Verdict:** ⚠️ A `Guid.Empty` should be rejected as invalid.

**Fix:**
```csharp
public UserId(Guid value)
{
    if (value == Guid.Empty)
        throw new ArgumentException("UserId cannot be empty.", nameof(value));
    Value = value;
}
```

---

### 2.3 `SteamStoreClient.FetchPriceAsync` — `GetString() ?? string.Empty`

**File:** `SteamTracker.Infrastructure/External/SteamStoreClient.cs` (line ~38)

```csharp
var name = data.GetProperty("name").GetString() ?? string.Empty;
```

**Verdict:** ⚠️ If Steam returns a game without a name, this silently uses `string.Empty`. The external API response should be treated as trusted but validated. Consider throwing if the name is missing, or using a `NonEmptyString` value object.

---

## 3. ACCEPTABLE DEFENSIVE CHECKS

These are legitimate business guards or external data handling.

### 3.1 Repository null returns — expected absence

All repository methods return `T?` where null means "not found". This is correct:

- `IGameRepository.GetAsync` → `Game?`
- `ITrackedGameRepository.GetAsync` → `TrackedGame?`
- `IAlertRuleRepository.GetAsync` → `AlertRule?`
- `ISteamStoreClient.FetchPriceAsync` → `(...)?`

The callers handle these correctly:
- **`SetAlertRuleUseCase`**: Throws on null (invariant violation) ✅
- **`DeleteAlertRuleUseCase`**: Throws on null (invariant violation) ✅
- **`HandleWishlistItemAddedUseCase`**: Creates new if null (idempotent) ✅
- **`HandleWishlistItemRemovedUseCase`**: Silent no-op if null/Inactive ✅
- **`GetWishlistWithPricesQuery`**: Skips null games with `continue` ✅
- **`ProcessPriceCheckUseCase`**: Creates new Game if null ✅
- **`SteamStoreClient`**: Returns null for "not found" ✅

---

### 3.2 `HandleWishlistItemRemovedUseCase` — silent no-op

```csharp
if (trackedGame is null || !trackedGame.IsActive)
    return;
```

**Verdict:** ✅ Correct. In an event-driven system, a "remove" event for a non-existent or already-removed game is expected (e.g., duplicate events, out-of-order delivery). Silent no-op is the right behavior.

---

### 3.3 `HandleWishlistItemAddedUseCase` — idempotent no-op

```csharp
if (existing is not null && existing.IsActive)
    return;
```

**Verdict:** ✅ Correct. Idempotency is essential for event-driven systems.

---

### 3.4 `PriceCheckConsumer` — null deserialization guard

```csharp
if (request is null)
{
    _logger.LogWarning("Received null PriceCheckMessage");
    await _channel.BasicNackAsync(..., requeue: false);
    return;
}
```

**Verdict:** ✅ Correct. External message broker data can be corrupted. Logging + dead-lettering (requeue: false) is the right approach.

---

### 3.5 `WishlistSyncConsumer` — null deserialization guard

```csharp
if (evt is not null)
    await _addedUseCase.ExecuteAsync(...);
```

**Verdict:** ✅ Correct. Same reasoning as above.

---

### 3.6 `PriceCheckScheduler` — per-job exception handling

```csharp
foreach (var appId in uniqueAppIds)
{
    try
    {
        await _publisher.EnqueueAsync(appId, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to enqueue price-check job for AppId {AppId}", appId);
    }
}
```

**Verdict:** ✅ Correct. A single job failure shouldn't stop the entire scheduler cycle.

---

## 4. EXCEPTION HANDLING — TOO BROAD

### 4.1 Global `ExceptionHandler` catches everything

**File:** `SteamTracker.API/ExceptionHandler.cs`

```csharp
public class ExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ...
    }
}
```

**Verdict:** ⚠️ This catches ALL exceptions, including:
- `ArgumentException` from `SteamAppId` constructor (programming error — should be 500, but logged better)
- `InvalidOperationException` from use cases (business error — should be 404 or 409, not 500)
- `SteamRateLimitException` (external error — should be 429)

**Fix:** Make it selective. Let specific exception types return appropriate HTTP status codes:

```csharp
public class ExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        // Map known exceptions to HTTP status codes
        var (statusCode, title) = exception switch
        {
            InvalidOperationException => (StatusCodes.Status404NotFound, "Not Found"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
            SteamRateLimitException => (StatusCodes.Status429TooManyRequests, "Rate Limited"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        httpContext.Response.StatusCode = statusCode;
        ...
    }
}
```

---

### 4.2 `Worker.cs` — `WishlistSyncConsumer` swallows all exceptions

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing wishlist sync message");
    await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken);
}
```

**Verdict:** ⚠️ Every exception causes a requeue, creating an infinite loop if the root cause is a programming error (e.g., `NullReferenceException`). Should distinguish between:
- **Transient errors** → requeue
- **Programming errors** → dead-letter (requeue: false)
- **Rate limits** → requeue with delay

---

### 4.3 `Worker.cs` — `PriceCheckConsumer` catches all exceptions

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing price check for delivery {DeliveryTag}", deliveryTag);
    await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken);
}
```

**Verdict:** ⚠️ Same infinite-loop risk as above. The `SteamRateLimitException` is handled separately (good), but other exceptions should be classified.

---

## 5. STRICT TYPING OPPORTUNITIES

### 5.1 `string Name` → `NonEmptyString` value object

**Affected:** `Game.Name`, `AlertRule.UserId`, `WishlistItemEvent.UserId`

All string fields that should never be empty could use a `NonEmptyString` value object:

```csharp
public readonly record struct NonEmptyString
{
    public string Value { get; }
    public NonEmptyString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("String cannot be empty.", nameof(value));
        Value = value;
    }
}
```

This eliminates the need for null/empty checks throughout the codebase.

---

### 5.2 `string Currency` → `CurrencyCode` value object

**Affected:** `Money.Currency`

Currently accepts any string. Should be a closed set:

```csharp
public readonly record struct CurrencyCode
{
    public string Value { get; }
    private static readonly HashSet<string> ValidCodes = new() { "EUR", "USD", "GBP", "RUB", "BRL", ... };
    
    public CurrencyCode(string code)
    {
        if (!ValidCodes.Contains(code))
            throw new ArgumentException($"Unsupported currency: {code}", nameof(code));
        Value = code.ToUpperInvariant();
    }
}
```

---

### 5.3 `string UserId` → `UserId` value object

**Affected:** `AlertRule.UserId`, `DeleteAlertRuleUseCase.ExecuteAsync`, etc.

Currently uses `string` for UserId. Should use the existing `UserId` value object (wraps `Guid`) for type safety.

---

## 6. SUMMARY OF RECOMMENDED CHANGES

### Remove (unnecessary defensive checks)
| Location | Change |
|---|---|
| `PriceAlertEvaluator.Evaluate` | Remove `rules is null` check |

### Add (strict type validations)
| Location | Change |
|---|---|
| `Money` constructor | Add `amount < 0` validation |
| `UserId` constructor | Add `Guid.Empty` validation |
| `Money.Currency` | Consider `CurrencyCode` value object |
| `Game.Name` / `AlertRule.UserId` | Consider `NonEmptyString` value object |

### Improve (exception handling)
| Location | Change |
|---|---|
| `SetAlertRuleUseCase` | Use domain-specific exception instead of `InvalidOperationException` |
| `DeleteAlertRuleUseCase` | Use domain-specific exception instead of `InvalidOperationException` |
| `ExceptionHandler` | Map exceptions to appropriate HTTP status codes |
| `PriceCheckConsumer` | Classify exceptions: requeue transient, dead-letter programming errors |
| `WishlistSyncConsumer` | Same as above |

### Keep (legitimate guards)
| Location | Reason |
|---|---|
| Repository null returns | Expected absence — callers handle correctly |
| `HandleWishlistItemRemovedUseCase` null check | Idempotent event handling |
| `HandleWishlistItemAddedUseCase` active check | Idempotent event handling |
| `PriceCheckConsumer` null deserialization | External data validation |
| `WishlistSyncConsumer` null deserialization | External data validation |
| `PriceCheckScheduler` per-job try/catch | Fault isolation |
