# Task 5 of 13 — Implement Domain Events with Version Correlation

**Plan**: `2026-04-21-22-01-mongodb-outbox-benchmarks`
**Completed**: 2026-04-21

---

## What Was Implemented

- `src/MongoDbPatterns.Domain/Events/DomainEvent.cs` — abstract base class with `EventId`, `AggregateId`, `AggregateVersion`, `OccurredAt`.
- `src/MongoDbPatterns.Domain/Events/OrderCreated.cs` — sealed event, version 0.
- `src/MongoDbPatterns.Domain/Events/OrderReadyForFulfilment.cs` — sealed event, version 1.
- Modified `OrderAggregate` to maintain `List<DomainEvent>` with `IReadOnlyList<DomainEvent>` public accessor, `ClearEvents()` method.
- `tests/MongoDbPatterns.Domain.Tests/Events/DomainEventTests.cs` — 2 tests for version correlation.
- `tests/MongoDbPatterns.Domain.Tests/Aggregates/OrderAggregateEventTests.cs` — 3 tests for event production and clearing.

## Key Decisions

1. **Abstract base class (not interface)**: `DomainEvent` carries common state (`EventId`, `AggregateId`, etc.) — a base class avoids boilerplate in each event type.
2. **Sealed concrete events**: `OrderCreated` and `OrderReadyForFulfilment` are sealed — no need for further inheritance.
3. **Events added inline in aggregate methods**: `Create()` adds `OrderCreated`, `MarkReadyForFulfilment()` adds `OrderReadyForFulfilment` — events are produced at the point of state change.

## Issues Encountered

None.

## State for Next Task

- `OrderAggregate.DomainEvents` is a `IReadOnlyList<DomainEvent>` — repositories (Tasks 6, 7) should read events from here after Create/Update and call `ClearEvents()` after persisting.
- `DomainEvent` properties (`EventId`, `AggregateId`, `AggregateVersion`, `OccurredAt`) need to be mapped to BSON documents in Task 6.
