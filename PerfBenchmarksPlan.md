# Plan: Standalone BenchmarkDotNet Project for `UserContext.GetIdAsync()`

## Structure

```
api/WishlistApi/
  Benchmarks/
    Benchmarks.csproj
    UserContextBenchmarks.cs
    Program.cs
```

## `Benchmarks.csproj`

- Target: `net10.0`
- **References:**
  - `WishlistApi` (for `UserContext`, `Program`)
  - `Tests` (for `ApiFactory` — reuses the testcontainer setup)
- **Packages:**
  - `BenchmarkDotNet`
  - `Moq` (for mocking `IHttpContextAccessor`)

## `Program.cs`

```csharp
using BenchmarkDotNet.Running;
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```

## `UserContextBenchmarks.cs`

**Class:** `UserContextBenchmarks`
- Decorated with `[MemoryDiagnoser]`
- Uses `IClassFixture<ApiFactory>` — one shared Postgres testcontainer across all benchmarks
- `[GlobalSetup]`: authenticate via `ApiFactory`, extract auth cookie, configure `DefaultHttpContext` with the user's `ClaimsIdentity`

**Benchmarks:**

| Method | Description |
|--------|-------------|
| `GetIdAsync_CacheMiss` | First call on instance — `IMemoryCache` miss + DB query via `UserService` + `UserRepository` |
| `GetIdAsync_CacheHit` | Second call on same instance — `IMemoryCache` hit (no DB) |
| `GetIdAsync_LocalCache` | Third call on same instance — `_cachedId` field hit (fastest path) |
| `GetIdAsync_Concurrent` | 100 parallel calls, **each creates a new `IUserContext` from DI** — simulates scoped lifetime, exercises DI resolution + shared `IMemoryCache` under load |

**Key design:**
- Only `IHttpContextAccessor` is mocked (via `DefaultHttpContext` with forged claims) — everything else (`IMemoryCache`, `UserService`, `UserRepository`, Postgres) is real
- Each benchmark method gets a **fresh `IUserContext`** via DI — simulates real `AddScoped` resolution per request
- Concurrent benchmark creates a new `IUserContext` per parallel call, but `IMemoryCache` is shared (singleton), so concurrent calls hit the warm cache
- Measures the actual hot path as it runs in production

## Execution

```bash
# PowerShell:
$env:DOTNET_ENVIRONMENT=Test; dotnet run --project api/WishlistApi/Benchmarks -c Release

# cmd:
set DOTNET_ENVIRONMENT=Test && dotnet run --project api/WishlistApi/Benchmarks -c Release
```

BenchmarkDotNet handles warmup automatically (default 5 iterations), statistical analysis, and outputs markdown tables with mean/stddev/min/max.

