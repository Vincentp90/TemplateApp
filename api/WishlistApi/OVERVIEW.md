# WishlistApi (.NET) Project Overview

## Technology Stack
- **Framework**: ASP.NET Core 10 (.NET 10)
- **Language**: C# (C# 14)
- **Database**: PostgreSQL with Entity Framework Core
- **ORM Convention**: Snake_case naming via `NpgsqlSnakeCaseNamingConvention`
- **Authentication**: JWT Bearer tokens (cookie-based via `auth_token`)
- **Real-time**: SignalR (`/auctionHub`)
- **API Docs**: Swagger/OpenAPI
- **Caching**: IMemoryCache (size-limited)
- **Testing**: xUnit (unit + integration tests)
- **Benchmarks**: BenchmarkDotNet

### Development guidelines
- When using a primary constructor, don't add private fields, just use the parameter directly
- This is .NET 10 project so use .NET 10 features if suitable
- Run unit tests with this command: `dotnet test api/WishlistApi/WishlistApi.sln 2>&1`
- Ignore the Benchmarks project

## Architecture (Clean Architecture / Layered)
```
WishlistApi.sln
├── Domain/                    # Core domain layer (no dependencies)
│   ├── Entities
│   │   ├── User.cs            # Aggregate root (User + UserDetails)
│   │   ├── WishlistItem.cs    # Wishlist items with stats calculation
│   │   ├── Auction.cs         # Auctions with bid logic
│   │   └── AppListing.cs      # Steam game listings
│   ├── ValueObjects
│   │   ├── FullName.cs
│   │   └── Address.cs
│   ├── Repositories/          # Repository interfaces (port)
│   │   ├── IAppListingRepository.cs
│   │   ├── IAuctionRepository.cs
│   │   ├── IUserRepository.cs
│   │   └── IWishlistItemRepository.cs
│   ├── Helpers
│   │   └── IUnitOfWork.cs
│   ├── Exceptions
│   │   ├── DomainException.cs
│   │   └── NotFoundException.cs
│   └── ISteamApiClient.cs     # External API contract
│
├── Application/               # Business logic / use cases
│   ├── Services
│   │   ├── AuthService.cs     # Login/Register with PBKDF2 password hashing
│   │   ├── WishlistService.cs # CRUD for wishlist items
│   │   ├── UserService.cs     # User management with IMemoryCache
│   │   ├── AppListingService.cs # Steam app listing sync + search
│   │   └── AuctionService.cs  # Auction lifecycle + bid simulation
│   ├── Commands               # Command objects (CQRS-like)
│   │   ├── AuthCommands.cs
│   │   ├── PlaceBidCommand.cs
│   │   ├── UserCommands.cs
│   │   └── WishlistItemCommands.cs
│   ├── Queries
│   │   ├── AuctionQueries.cs
│   │   └── UserQueries.cs
│   └── Contracts              # DTOs for cross-layer communication
│       ├── AuctionDto.cs
│       └── UserSummaryDto.cs
│
├── Infrastructure/            # Implementation details / persistence
│   ├── Persistence
│   │   ├── WishlistDbContext.cs           # EF Core DbContext + migrations
│   │   ├── Users/                         # User persistence
│   │   │   ├── User.cs                    # EF entity
│   │   │   ├── UserDetails.cs             # EF entity
│   │   │   └── UserRepository.cs
│   │   ├── Wishlist/                      # Wishlist persistence
│   │   │   ├── WishlistItem.cs
│   │   │   └── WishlistItemRepository.cs
│   │   ├── AppListings/                   # App listing persistence
│   │   │   ├── AppListing.cs
│   │   │   └── AppListingRepository.cs
│   │   └── Auctions/                      # Auction persistence
│   │       ├── Auction.cs
│   │       └── AuctionRepository.cs
│   ├── Migrations/                        # EF Core migrations
│   └── ExternalServices
│       └── SteamApiClient.cs              # Steam API client
│
├── WishlistApi/                           # API layer / web host
│   ├── Controllers/
│   │   ├── AuthController.cs              # POST register, login, logout, GET me
│   │   ├── WishlistController.cs          # CRUD wishlist items + stats
│   │   ├── UsersController.cs             # User profile + admin user management
│   │   ├── AuctionsController.cs          # Auctions + SignalR hub
│   │   └── AppListingsController.cs       # Search Steam games
│   ├── DTOs/
│   │   ├── AuthDTOs.cs
│   │   ├── UserDTOs.cs
│   │   └── WishlistDTOs.cs
│   ├── Helpers
│   │   ├── JwtTokenGenerator.cs
│   │   └── UserContext.cs                 # Extract user from JWT
│   ├── HostedServices
│   │   ├── AuctionBackgroundService.cs    # Periodic auction management
│   │   └── SteamUpdaterService.cs         # Periodic Steam data sync
│   └── Program.cs                         # App startup, DI, middleware
│
├── Tests/                                 # Unit + integration tests
│   ├── ApplicationTests/
│   │   ├── AppListingServiceTests.cs
│   │   ├── AuctionConcurrencyTests.cs
│   │   ├── AuctionTests.cs
│   │   ├── UserQueriesTests.cs
│   │   └── WishlistServiceTests.cs
│   ├── ControllerTests/
│   │   ├── UsersControllerUnitTests.cs
│   │   ├── UsersControllerIntegrationTests.cs
│   │   └── WishlistControllerTest.cs
│   ├── DataAccessTests/
│   │   └── AuctionRepoTest.cs
│   ├── IntegrationTests/
│   │   ├── ApiAuthorizedTests.cs
│   │   ├── ApiUnauthorizedTests.cs
│   │   └── DatabaseTest.cs
│   └── Helpers/
│       ├── ApiFactory.cs
│       ├── LiveDbFactAttribute.cs
│       └── UserControllerFixture.cs
│
├── Benchmarks/                            # BenchmarkDotNet benchmarks
│   └── UserContextBenchmarks.cs
│
├── Dockerfile / Dockerfile.dev
└── agentic.md
```

## API Endpoints

### Auth (`/auth`)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | `/auth/register` | Register new user | Public |
| POST | `/auth/login` | Login (sets `auth_token` cookie) | Public |
| POST | `/auth/logout` | Logout (clears cookie) | Public |
| GET | `/auth/me` | Get current user info | Required |

### Wishlist (`/wishlist`)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/wishlist` | Get wishlist items (field filtering) | Required |
| GET | `/wishlist/stats` | Get wishlist statistics | Required |
| POST | `/wishlist/{appId}` | Add item to wishlist | Required |
| DELETE | `/wishlist/{appId}` | Remove item from wishlist | Required |

### Users (`/users`)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/users/me` | Get current user details | Required |
| GET | `/users/{UserId}` | Get user by UUID | Admin |
| PATCH | `/users/me` | Update own profile | Required |
| PATCH | `/users/{UserId}` | Update user (admin) | Admin |
| GET | `/users?page=&limit=` | Paginated user list | Admin |

### Auctions (`/auctions`)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/auctions/current` | Get current auction | Required |
| POST | `/auctions/current` | Place bid | Required |
| GET | `/auctions/current/SimulateBid` | Simulate a bid (demo) | Required |

### App Listings (`/applistings`)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/applistings/search/{term}` | Search Steam games | Required |

### SignalR Hub
| Path | Description |
|------|-------------|
| `/auctionHub` | Real-time auction updates |

## Domain Model
- **User** (Aggregate Root): Contains `UserDetails` (name, address, row version for optimistic concurrency). Role-based access (Admin/User).
- **WishlistItem**: Tracks app ID, name, date added per user.
- **Auction**: Has starting price, current price, status (Open/Closed), row version for optimistic concurrency. 30-minute duration.
- **AppListing**: Steam game listings (fetched from Steam API).

## Key Technical Details
- **Password Hashing**: PBKDF2 with SHA256, 100,000 iterations, 16-byte salt
- **JWT**: Cookie-based (`HttpOnly`, `Secure`, `SameSite=Strict`, 2-hour expiry)
- **Optimistic Concurrency**: Row version on Auction and UserDetails
- **Background Services**: Auction lifecycle management + Steam data synchronization
- **Memory Cache**: User ID lookups cached by external GUID
- **EF Core Migrations**: 5 migrations tracked (Initial, AuctionAdded, AuctionAddedRV, AuctionRowVersion, UserDetails)

## Project References (Dependency Flow)
```
WishlistApi (host) → Application → Domain
WishlistApi (host) → Infrastructure → Domain
Tests → WishlistApi, Infrastructure
Benchmarks → (standalone)
```
