# Task 4 of 13 — Implement OrderAggregate with Version Tracking

**Plan**: `2026-04-21-22-01-mongodb-outbox-benchmarks`
**Completed**: 2026-04-21

---

## What Was Implemented

- `src/MongoDbPatterns.Domain/Aggregates/OrderStatus.cs` — enum with `Created` and `ReadyForFulfilment` values.
- `src/MongoDbPatterns.Domain/Aggregates/OrderAggregate.cs` — DDD aggregate root with `Id`, `Version`, `Status`. Factory `Create()` method and `MarkReadyForFulfilment()` transition.
- `tests/MongoDbPatterns.Domain.Tests/Aggregates/OrderAggregateTests.cs` — 4 tests covering version, status, and id assignment.
- Updated `SolutionStructureTests` to use actual domain type instead of trivial `Assert.True(true)`.

## Key Decisions

1. **Private constructor + static factory**: Enforces creation only through `Create()`, ensuring version starts at 0 and status starts at `Created`.
2. **Private setters**: Version and Status are only modifiable by aggregate methods, protecting invariants.
3. **No domain events yet**: Per the plan, events are added in Task 5.

## Issues Encountered

None.

## State for Next Task

- `OrderAggregate` is ready for Task 5 to add domain event production (`List<DomainEvent>`, `ClearEvents()`).
- Properties use private setters — Task 5 should add event list as a private field with public read-only accessor.
