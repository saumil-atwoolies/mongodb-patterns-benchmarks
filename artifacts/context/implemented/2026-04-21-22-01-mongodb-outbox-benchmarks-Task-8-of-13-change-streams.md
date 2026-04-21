# Task 8 of 13 — Implement Change Stream Monitoring

**Plan**: `2026-04-21-22-01-mongodb-outbox-benchmarks`
**Completed**: 2026-04-21

---

## What Was Implemented

- `src/MongoDbPatterns.Infrastructure/ChangeStreams/ChangeStreamResult.cs` — sealed record with `CollectionName`, `StartTime`, `EndTime`, `EventsReceived`.
- `src/MongoDbPatterns.Infrastructure/ChangeStreams/ChangeStreamWatcher.cs` — watches a MongoDB collection via change stream cursor on a background task. Thread-safe counter via `Interlocked.Increment`.
- `tests/MongoDbPatterns.Infrastructure.Tests/ChangeStreams/ChangeStreamWatcherTests.cs` — 3 integration tests.

## Key Decisions

1. **Background task pattern**: `StartAsync` launches a fire-and-forget `Task` that loops on `cursor.MoveNextAsync`. `StopAsync` cancels via `CancellationTokenSource` and awaits the task.
2. **Pipeline filter**: Only counts `Insert`, `Update`, and `Replace` operations — ignores deletes and other change types.
3. **`Interlocked.Increment`**: Thread-safe counter for events received, as the watch loop and stop may overlap.
4. **`BsonDocument` collection type**: The watcher accepts `IMongoCollection<BsonDocument>` to be agnostic of the typed document model.

## Issues Encountered

None.

## State for Next Task

- `ChangeStreamWatcher` is ready for integration into benchmark scenarios (Task 9).
- Constructor takes `IMongoCollection<BsonDocument>` and a collection name string.
- `StartAsync` / `StopAsync` lifecycle must be managed by the benchmark runner.
