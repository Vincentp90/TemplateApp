# Test Overview

## Frontend

| Technology | Location | Run Command |
|------------|----------|-------------|
| **Vitest** + Testing Library | `frontend/src/components/search.test.tsx` | `cd frontend && npm test` |
| **Playwright** | `frontend/src/tests/playwright/` | `cd frontend && npx playwright test` |

## Backend (.NET)

| Technology | Location | Run Command |
|------------|----------|-------------|
| **xUnit** + Moq + FluentAssertions | `api/WishlistApi/Tests/` | `cd api/WishlistApi/Tests && dotnet test` |

All backend tests are in a single project (`Tests.csproj`) organized into folders:

| Folder | Type |
|--------|------|
| `ApplicationTests/` | Service logic (mocked repos, in-memory DB, testcontainers) |
| `ControllerTests/` | Controller layer (unit + integration with in-memory DB) |
| `DataAccessTests/` | Repository layer (in-memory EF Core) |
| `IntegrationTests/` | Full HTTP tests via `WebApplicationFactory` + PostgreSQL testcontainers |

The load/perf test (`DatabaseTest.cs`) is skipped by default — run with `$env:DOTNET_ENVIRONMENT="Test"; dotnet run --project api/WishlistApi/Benchmarks -c Release --filter "*"`.
