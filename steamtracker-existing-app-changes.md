# Existing App — Required Changes for SteamTracker

This plan covers the minimal changes needed in the **existing WishlistApi** to support the SteamTracker service. It must be completed before SteamTracker development begins.

---

## Scope

The existing app must publish domain events to RabbitMQ whenever a user adds or removes a wishlist item. These events are consumed by SteamTracker's ACL layer. That is the **only** change required to the existing app.

---

## 1. Add RabbitMQ infrastructure

The existing app currently has no messaging infrastructure. Add the following:

### 1.1 Dependencies

Add `RabbitMQ.Client` (or a higher-level abstraction like MassTransit) to `Infrastructure.csproj`.

### 1.2 Connection configuration

```json
"RabbitMq": {
  "Host": "rabbitmq",       // Docker hostname or localhost
  "Port": 5672,
  "VirtualHost": "/wishlist",
  "Username": "wishlist",
  "Password": "..."
}
```

### 1.3 Exchange type — **fanout**

Use a **fanout** exchange. Every message published to it is delivered to every bound queue, regardless of routing key. This is the right choice because:

- SteamTracker is the only subscriber today
- Future subscribers (analytics, notifications, etc.) can be added without any publisher-side changes — just bind a new queue
- If selective routing is ever needed, add a second exchange (see Key decisions below)

### 1.4 RabbitMQ topology

```
Exchange: wishlist.events   (fanout)
  → Queue: steamtracker.wishlist-sync   (SteamTracker's ACL consumer)

# Reserved for future subscribers
  → Queue: <future-consumer>.wishlist-sync
```

### 1.5 Connection factory registration

In `Program.cs`, register `IRabbitMqConnectionFactory` (or `IConnection`) as a singleton.

---

## 2. Publish domain events on wishlist mutations

### 2.1 Event contracts

These records live in the **existing app's** infrastructure layer (they are the wire format):

```csharp
// Published when a user adds a game to their wishlist
record WishlistItemAdded(string UserId, int AppId, DateTimeOffset AddedAt);

// Published when a user removes a game from their wishlist
record WishlistItemRemoved(string UserId, int AppId, DateTimeOffset RemovedAt);
```

**Only `UserId` and `AppId` are needed.** `AppName` is not included — SteamTracker resolves the game name from the Steam API during the first price fetch.

### 2.2 Where to publish

There are two clean approaches. Pick one:

**Option A — Domain events in the aggregate (preferred for DDD purity)**

Add an event list to the `WishlistItem` domain entity:

```csharp
public class WishlistItem
{
    // ... existing properties ...
    private readonly List<object> _domainEvents = new();

    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private void Raise<T>(T event) where T : class => _domainEvents.Add(event);

    public static WishlistItem CreateNew(int userId, int appId)
    {
        var item = new WishlistItem(appId, DateTimeOffset.UtcNow, userId);
        item.Raise(new WishlistItemAdded(userId, appId, item.DateAdded));
        return item;
    }
}
```

In the `WishlistItemRepository.AddWishlistItemAsync`, after `SaveChangesAsync`, publish any pending domain events.

For removal, add a `Delete` method on the domain entity that raises `WishlistItemRemoved`, or publish the event directly from the repository since deletion doesn't need a domain method.

**Option B — Application-layer event publisher (simpler, less DDD-pure)**

Add an `IEventPublisher` interface to `Domain` (or `Application`) and a `RabbitMqEventPublisher` in `Infrastructure`.

After each `AddToWishlistAsync` and `DeleteWishlistItemAsync` in `WishlistService`, publish the corresponding event. This is simpler but couples the service layer to event publishing.

---

## 3. Event replay / backfill endpoint

### 3.1 Why it's needed

If SteamTracker is deployed after users already have items in their wishlist, those items will never generate `WishlistItemAdded` events. SteamTracker's ACL will be out of sync. A one-time backfill endpoint solves this.

### 3.2 Endpoint

```
POST /wishlist/_backfill?appId={optional}
```

- **Auth**: Admin or the requesting user (if `appId` is specified)
- **Behavior**:
  - If `appId` is **not** specified: iterates over the user's entire wishlist and publishes a `WishlistItemAdded` event for each item (deduplicated by `(UserId, AppId)`).
  - If `appId` **is** specified: publishes `WishlistItemAdded` for just that item.
  - Events are published in batches (e.g., 50 at a time) to avoid blocking the request.
  - Returns `202 Accepted` with a `BackfillId` that can be polled or used for logging.

### 3.3 Deduplication

SteamTracker's `WishlistSyncWorker` should be **idempotent**: receiving a `WishlistItemAdded` for a `TrackedGame` that already exists (same `AppId`, `IsActive = true`) is a no-op. This is enforced by the upsert logic in `IHandleWishlistItemAddedUseCase`.

### 3.4 Implementation sketch

```csharp
[HttpPost("_backfill")]
public async Task<ActionResult> BackfillAsync(
    [FromQuery] int? appId = null,
    CancellationToken ct = default)
{
    int userId = await _userContext.GetIdAsync();

    var items = appId.HasValue
        ? new[] { (await _wishlistRepo.GetWishlistItemsAsync(userId))
            .FirstOrDefault(wi => wi.AppId == appId.Value) }
        : await _wishlistRepo.GetWishlistItemsAsync(userId);

    var validItems = items.Where(wi => wi != null).ToList();

    foreach (var item in validItems)
    {
        _eventPublisher.Publish(new WishlistItemAdded(
            userId.ToString(),
            item.AppId,
            item.DateAdded));
    }

    return Accepted(new { Count = validItems.Count });
}
```

### 3.5 Frontend trigger

Add a "Backfill price tracking" button to the existing wishlist UI (visible to the logged-in user). This is a manual action the user takes once after SteamTracker is deployed.

---

## 4. Summary of files to create/modify

### New files

| File | Project | Purpose |
|------|---------|---------|
| `Infrastructure/Messaging/RabbitMqConnectionFactory.cs` | Infrastructure | RabbitMQ connection singleton |
| `Infrastructure/Messaging/RabbitMqEventPublisher.cs` | Infrastructure | Publishes domain events to `wishlist.events` exchange |
| `Application/Events/WishlistItemAdded.cs` | Application (or Domain) | Event record type |
| `Application/Events/WishlistItemRemoved.cs` | Application (or Domain) | Event record type |
| `Application/IEventPublisher.cs` | Application | Port interface (optional, if using Option A for domain events) |

### Modified files

| File | Change |
|------|--------|
| `WishlistApi/Program.cs` | Register RabbitMQ connection, event publisher |
| `Domain/WishlistItem.cs` | Add domain event list + raise methods (Option A) |
| `Application/WishlistService.cs` | Publish events after add/delete (Option B) |
| `Infrastructure/Persistence/Wishlist/WishlistItemRepository.cs` | Publish pending domain events after save (Option A) |
| `WishlistApi/Controllers/WishlistController.cs` | Add `POST /_backfill` endpoint |

---

## Test strategy

### Framework
xUnit + FluentAssertions + **Moq** — matching the existing app's test stack exactly. All tests go in the existing `Tests/` project under a new namespace.

### TDD — tests first
**Every piece of logic starts with a failing test.** This is not optional.
- Event publishing: write failing tests → implement the publisher → make them pass.
- Backfill endpoint: write failing controller tests → implement the endpoint → make them pass.
- Integration: write failing integration tests → implement the wiring → make them pass.

Never write implementation code without a failing test first. If you can't write a failing test, the code doesn't need to exist.

### Pyramid

**Application.Tests — event publishing** (mocked RabbitMQ channel, no real network)

| What | How |
|------|-----|
| `WishlistItemAdded` record serialization | Serialize → deserialize → verify all fields round-trip |
| `RabbitMqEventPublisher.Publish<T>` | Mock `IChannel` — verify `BasicPublish` called with correct exchange, routing key, body |
| `WishlistItemRepository` with domain events (Option A) | Mock `DbContext` — verify `SaveChangesAsync` triggers event publishing |
| `WishlistService` with event publisher (Option B) | Mock `IEventPublisher` — verify `Publish` called after `AddToWishlistAsync` and `DeleteWishlistItemAsync` |
| Domain events not published on failed save | Mock `SaveChangesAsync` throws — verify no publish call |

**Controller.Tests — backfill endpoint**

| What | How |
|------|-----|
| `POST /_backfill` without `appId` | Mock `IWishlistItemRepository` → verify event published for each item |
| `POST /_backfill` with `appId` | Mock repo → verify only that specific item is published |
| `POST /_backfill` empty wishlist | Verify returns `202` with `Count = 0` |
| `POST /_backfill` unauthorized | Verify returns `401` (handled by `[Authorize]`) |
| `POST /_backfill` deduplication | Same `appId` added twice → only one event published |

**Integration.Tests — end-to-end event flow**

| What | How |
|------|-----|
| Add wishlist item → event published | Real `WishlistDbContext` + mocked RabbitMQ channel → verify event in channel calls |
| Delete wishlist item → event published | Same — verify `WishlistItemRemoved` published |
| Backfill → all events published | Add N items → call backfill → verify N events published |

### Testing the event publishing mechanism

The event publishing is the critical bridge between the existing app and SteamTracker. It must be tested thoroughly.

**Option A (domain events in aggregate)** — test the aggregate's event collection:
```csharp
// Failing test first
[Fact]
public void CreateNew_raises_WishlistItemAdded()
{
    var item = WishlistItem.CreateNew(userId: 1, appId: 123);
    item.DomainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<WishlistItemAdded>();
}
```

Then test that the repository publishes them:
```csharp
[Fact]
public async void AddWishlistItemAsync_publishes_pending_events()
{
    // Arrange: mock IEventPublisher
    var repo = new WishlistItemRepository(context, eventPublisher);
    
    // Act: add item (which raises an event)
    await repo.AddWishlistItemAsync(item);
    await context.SaveChangesAsync();
    
    // Assert: event was published
    eventPublisher.Received(1).Publish(Arg.Is<WishlistItemAdded>(...));
}
```

**Option B (application-layer publisher)** — test the service directly:
```csharp
[Fact]
public async void AddToWishlistAsync_publishes_WishlistItemAdded()
{
    // Arrange: mock IEventPublisher
    var service = new WishlistService(repo, unitOfWork, eventPublisher);
    
    // Act
    await service.AddToWishlistAsync(new AddToWishlistCommand(userId: 1, appId: 123));
    
    // Assert
    eventPublisher.Received(1).Publish(Arg.Is<WishlistItemAdded>(e =>
        e.UserId == "1" && e.AppId == 123));
}
```

Pick **one option** and test accordingly. Option A is more DDD-pure but requires the repository to coordinate event publishing. Option B is simpler but couples the service to event publishing.

### Testing the backfill endpoint

The backfill is a safety net — it must be tested to ensure it doesn't lose data or publish duplicates.

```csharp
[Fact]
public async Task BackfillAsync_publishes_one_event_per_wishlist_item()
{
    // Arrange: 3 items in wishlist
    var repo = Mock.Of<IWishlistItemRepository>(r =>
        r.GetWishlistItemsAsync(1) == Task.FromResult(new[]
        {
            new WishlistItem(1, 100, "Game A", DateTimeOffset.UtcNow, 1),
            new WishlistItem(2, 200, "Game B", DateTimeOffset.UtcNow, 1),
            new WishlistItem(3, 300, "Game C", DateTimeOffset.UtcNow, 1)
        }));
    
    var publisher = new Mock<IEventPublisher>();
    var controller = new WishlistController(context, service, publisher.Object);
    
    // Act
    var result = await controller.BackfillAsync(appId: null);
    
    // Assert
    result.Should().BeOfType<AcceptedObjectResult>();
    publisher.Verify(p => p.Publish(Arg.Any<WishlistItemAdded>()), Times.Exactly(3));
}
```

### Testing RabbitMQ infrastructure

The RabbitMQ connection and exchange setup are infrastructure concerns. Test them as integration tests with testcontainers (real RabbitMQ).

| What | How |
|------|-----|
| Connection factory creates a live connection | Spin up RabbitMQ via testcontainers → connect → verify connection is open |
| Exchange `wishlist.events` exists | Publish a test message → verify queue binds and receives it |
| Fanout delivery | Bind two queues → publish one message → both queues receive it |

These are slow but catch real infrastructure bugs. Run them in CI.

---

## Implementation order

Each step starts with **failing tests**, then implementation, then the tests pass before moving on.

```
Phase 1 — Infrastructure (TDD)
  1. Write failing tests for RabbitMqEventPublisher (mocked IChannel) → implement publisher
  2. Write failing tests for RabbitMqConnectionFactory (testcontainers: real RabbitMQ) → implement connection
  3. Register in Program.cs

Phase 2 — Domain events (TDD)
  4. Pick Option A (aggregate events) or Option B (service-level publisher)
  5. Write failing tests for event publishing on add/delete → implement
  6. Wire into WishlistService or WishlistItemRepository

Phase 3 — Backfill endpoint (TDD)
  7. Write failing controller tests for POST /_backfill → implement endpoint
  8. Add "Backfill" button to frontend wishlist UI

Phase 4 — Verify
  9. Integration test: add item → check RabbitMQ queue → confirm event is published
 10. Integration test: backfill → check all events published
```

---

## Key decisions

- **Exchange type**: `wishlist.events` is a **fanout** exchange. Every event is delivered to every bound queue. To add a selective subscriber later, publish to a second direct exchange (e.g. `wishlist.events.added` with binding key `wishlist.added`) — no migration needed.
- **Event versioning**: Start with v1 implicitly. If the contract changes, add a `Version` property to the record and handle both versions in SteamTracker.
