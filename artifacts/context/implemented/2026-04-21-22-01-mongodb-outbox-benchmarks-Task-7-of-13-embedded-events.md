# Task 7 of 13 — Implement Case 2 Embedded Events Outbox with Optimistic Concurrency

**Plan**: `2026-04-21-22-01-mongodb-outbox-benchmarks`
**Completed**: 2026-04-21

---

## What Was Implemented

- `src/MongoDbPatterns.Domain/Exceptions/ConcurrencyException.cs` — thrown on version mismatch.
- `src/MongoDbPatterns.Infrastructure/Persistence/Documents/EmbeddedOrderDocument.cs` — BSON document with embedded `DomainEvents` array and `EmbeddedEventDocument` sub-document.
- `src/MongoDbPatterns.Infrastructure/Persistence/EmbeddedEventsOrderRepository.cs` — implements `IOrderRepository` using optimistic concurrency via version filter on `UpdateOneAsync` with `$push`.
- `tests/MongoDbPatterns.Infrastructure.Tests/Persistence/EmbeddedEventsOrderRepositoryTests.cs` — 3 integration tests including concurrency exception scenario.

## Key Decisions

1. **Collection name `OrdersEmbedded`**: Avoids collision with Case 1's `Orders` collection. Public constant for use by change stream watchers.
2. **`UpdateOneAsync` with version filter**: The filter `{ _id: id, Version: expectedVersion }` ensures atomic optimistic concurrency check. `PushEach` appends new events.
3. **Expected version calculation**: `order.Version - 1` is the version we expect in the database (the version before this update). The aggregate already incremented its version in `MarkReadyForFulfilment()`.

## Issues Encountered

None.

## State for Next Task

- Both `IOrderRepository` implementations are complete. Task 8 (change streams) and Task 9 (benchmark harness) can use them.
- Collection names: `Orders` + `OrderEvents` (Case 1), `OrdersEmbedded` (Case 2).
- `ConcurrencyException` is in `MongoDbPatterns.Domain.Exceptions`.
