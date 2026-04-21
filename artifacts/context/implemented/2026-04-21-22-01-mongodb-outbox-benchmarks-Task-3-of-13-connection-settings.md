# Task 3 of 13 — Implement Connection Settings Configuration

**Plan**: `2026-04-21-22-01-mongodb-outbox-benchmarks`
**Completed**: 2026-04-21

---

## What Was Implemented

- `src/MongoDbPatterns.Infrastructure/Configuration/ConnectionSettings.cs` — sealed record with `ConnectionString` and `DatabaseName` properties.
- `src/MongoDbPatterns.Infrastructure/Configuration/ConnectionSettingsProvider.cs` — reads from `connection-setting.local` JSON file; auto-creates with defaults if missing.
- `tests/MongoDbPatterns.Infrastructure.Tests/Configuration/ConnectionSettingsProviderTests.cs` — 3 tests covering creation, reading, and non-overwrite.

## Key Decisions

1. **Constructor-injectable file path**: `ConnectionSettingsProvider` accepts an optional `filePath` parameter for testability. Default constructor uses current working directory.
2. **`System.Text.Json`**: Used for JSON serialization (built into .NET; no additional dependencies).
3. **Sealed record for `ConnectionSettings`**: Immutable value type appropriate for configuration.

## Issues Encountered

None.

## State for Next Task

- `ConnectionSettingsProvider` is ready for use by `MongoDbContext` (Task 6) and `Program.cs` (Task 9).
- Default connection string: `mongodb://admin:N05%40ssword@localhost:27018/?replicaSet=rs0&authSource=admin`.
- Default database name: `MongoDbPatterns`.
