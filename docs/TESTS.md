# Test Overview

## Frontend

| Technology | Location | Run Command |
|------------|----------|-------------|
| **Vitest** + Testing Library | `frontend/src/components/*.test.tsx` | `cd frontend && npm test` |
| **Playwright** | `frontend/src/tests/playwright/` | `cd frontend && npx playwright test` |

## Backend (.NET 10)

### WishlistApi

| Technology | Location | Run Command |
|------------|----------|-------------|
| **xUnit** + Moq + FluentAssertions | `api/WishlistApi/Tests/` | `cd api/WishlistApi && dotnet test` |

All WishlistApi tests are in a single project (`Tests.csproj`) organized into folders:

| Folder | Type |
|--------|------|
| `ApplicationTests/` | Service logic (mocked repos, in-memory DB, testcontainers) |
| `ControllerTests/` | Controller layer (unit + integration with in-memory DB) |
| `DataAccessTests/` | Repository layer (in-memory EF Core) |
| `IntegrationTests/` | Full HTTP tests via `WebApplicationFactory` + PostgreSQL testcontainers |

The load/perf tests (`Benchmarks/`) are skipped by default — run with:
```bash
dotnet run --project api/WishlistApi/Benchmarks -c Release --filter "*"
```

### SteamTracker

SteamTracker has **6 separate test projects**, each targeting a specific layer:

| Project | Folder | Type |
|---------|--------|------|
| `SteamTracker.Domain.Tests` | `tests/SteamTracker.Domain.Tests/` | Domain entities, value objects, domain rules (pure unit tests) |
| `SteamTracker.Application.Tests` | `tests/SteamTracker.Application.Tests/` | Application use cases / handlers (mocked infrastructure) |
| `SteamTracker.API.Tests` | `tests/SteamTracker.API.Tests/` | API endpoint tests (in-memory server) |
| `SteamTracker.Infrastructure.Tests` | `tests/SteamTracker.Infrastructure.Tests/` | Repositories, external clients, messaging (PostgreSQL testcontainers) |
| `SteamTracker.Integration.Tests` | `tests/SteamTracker.Integration.Tests/` | E2E cross-layer flows (testcontainers) |
| `SteamTracker.Worker.Tests` | `tests/SteamTracker.Worker.Tests/` | Background worker / message processing logic |

Run all SteamTracker tests:
```bash
cd api/SteamTracker && dotnet test
```

Run a specific test project:
```bash
dotnet test api/SteamTracker/tests/SteamTracker.Domain.Tests/SteamTracker.Domain.Tests.csproj
```

### Cross-Service Tests

| Project | Location | Type |
|---------|----------|------|
| `CrossService.Tests` | `api/CrossService.Tests/` | Full integration across WishlistApi + SteamTracker (testcontainers, HTTP clients) |

Run cross-service tests:
```bash
cd api && dotnet test CrossService.Tests/CrossService.Tests.csproj
```

These tests cover end-to-end flows such as:
- Register a user → add a wishlist item → retrieve wishlist with price data from SteamTracker
