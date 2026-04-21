---
created: 2026-04-21 22:01
context: Full implementation of MongoDB outbox pattern benchmarking application from validated requirements (18 requirements across environment, domain, use cases, performance, and UX)
status: pending
---

# Implementation Plan: MongoDB Outbox Pattern Benchmarks

## Design Context

This plan implements the complete MongoDB Patterns & Benchmarks application — a .NET 10 C# console application that benchmarks two outbox pattern variants for DDD aggregates against MongoDB, with and without change stream monitoring, under configurable concurrency and load.

The project is greenfield. No source code, solution files, Dockerfiles, or docker-compose files exist yet. The repository currently contains only documentation (`docs/`), validated requirements (`artifacts/validated/`), planning context (`artifacts/context/`), and Copilot workflow configuration (`.github/`). REQ-ENV-003 (Copilot workflow initialization) is already satisfied.

The two use cases under benchmark are: (1) a two-phase commit outbox pattern using MongoDB multi-document transactions to save an `OrderAggregate` and its domain events in separate collections atomically, and (2) an embedded events outbox pattern where domain events are stored as an array within the Order document using optimistic concurrency via a version filter.

The architecture follows DDD with a clear separation: `Domain` project owns aggregates, events, and repository interfaces; `Infrastructure` project provides MongoDB implementations; `Benchmarks` console app hosts the benchmark runner; and `Tests` projects validate behaviour at unit and integration levels. Docker Compose orchestrates MongoDB (single-node replica set for change streams and transactions) and the benchmark application.

## Guiding Principles

- **DDD**: Aggregates own their invariants. Repository interfaces live in Domain.
  Infrastructure implements them. No domain entity may depend on infrastructure.
- **TDD**: Every behavioural change is covered by at least one test written before
  (or alongside) the implementation. Tests must pass before a task is marked done.
- **DRY**: Extract shared logic only when the same concept appears in three or more
  places. Prefer clarity over premature abstraction.
- **SOLID**: Single-responsibility classes; depend on interfaces not concretions;
  extension over modification.

## Platform Conventions

### Build and Test Commands

```bash
dotnet build
dotnet test
```

### Code Conventions

| Concern | Convention |
|---------|-----------|
| Indentation | 4 spaces |
| Encoding | UTF-8 |
| Line endings | CRLF |
| Nullable refs | Enabled |
| Target framework | net10.0 |
| Domain rules | No domain entity depends on infrastructure |
| Repository pattern | Interfaces in Domain, implementations in Infrastructure |

## Progress Tracker

| # | Task | Status | Commit Message |
|---|------|--------|----------------|
| 1 | Create .gitignore and scaffold .NET solution | `done` | `Scaffold .NET 10 solution with domain, infrastructure, benchmarks, and test projects` |
| 2 | Create Docker Compose with MongoDB replica set | `done` | `Add Docker Compose with single-node MongoDB replica set` |
| 3 | Implement connection settings configuration | `done` | `Add connection settings model with auto-creation of local config file` |
| 4 | Implement OrderAggregate with version tracking | `done` | `Add OrderAggregate domain model with version tracking` |
| 5 | Implement domain events with version correlation | `done` | `Add domain events with aggregate version correlation` |
| 6 | Implement Case 1 two-phase commit outbox pattern | `done` | `Add two-phase commit outbox pattern with MongoDB transactions` |
| 7 | Implement Case 2 embedded events outbox with optimistic concurrency | `done` | `Add embedded events outbox pattern with optimistic concurrency` |
| 8 | Implement change stream monitoring | `done` | `Add change stream monitoring with event counting` |
| 9 | Implement extensible benchmark harness with configurable load | `done` | `Add extensible benchmark harness with configurable load parameters` |
| 10 | Implement benchmark statistics display | `done` | `Add formatted benchmark statistics output` |
| 11 | Configure IOPS simulation via Docker blkio_config | `done` | `Add configurable IOPS simulation on MongoDB container` |
| 12 | Docker-integrated build, test, and benchmark pipeline | `pending` | `Add multi-stage Dockerfile for build, test, and benchmark execution` |
| 13 | Create README.md | `pending` | `Add README with quick-start, architecture, and configuration reference` |

## Task Details

### Task 1 — Create .gitignore and Scaffold .NET Solution

**Scope:** Repository root: `.gitignore`, `MongoDbPatterns.sln`. Projects: `src/MongoDbPatterns.Domain/`, `src/MongoDbPatterns.Infrastructure/`, `src/MongoDbPatterns.Benchmarks/`, `tests/MongoDbPatterns.Domain.Tests/`, `tests/MongoDbPatterns.Infrastructure.Tests/`.

**Spec:**
- Create `.gitignore` with standard .NET entries (`bin/`, `obj/`, `*.user`, `.vs/`) plus `*.local` pattern for connection settings (REQ-GIT-001, REQ-ENV-006).
- Create `MongoDbPatterns.sln` solution file at repository root.
- Create `src/MongoDbPatterns.Domain/MongoDbPatterns.Domain.csproj` — class library, `net10.0`, nullable enabled, no dependencies.
- Create `src/MongoDbPatterns.Infrastructure/MongoDbPatterns.Infrastructure.csproj` — class library, `net10.0`, nullable enabled, references `MongoDbPatterns.Domain`, NuGet: `MongoDB.Driver`.
- Create `src/MongoDbPatterns.Benchmarks/MongoDbPatterns.Benchmarks.csproj` — console app, `net10.0`, nullable enabled, references `MongoDbPatterns.Domain` and `MongoDbPatterns.Infrastructure`, NuGet: `BenchmarkDotNet`.
- Create `tests/MongoDbPatterns.Domain.Tests/MongoDbPatterns.Domain.Tests.csproj` — xUnit test project, references `MongoDbPatterns.Domain`.
- Create `tests/MongoDbPatterns.Infrastructure.Tests/MongoDbPatterns.Infrastructure.Tests.csproj` — xUnit test project, references `MongoDbPatterns.Infrastructure` and `MongoDbPatterns.Domain`.
- Add a placeholder test in `MongoDbPatterns.Domain.Tests` that passes (satisfies REQ-TEST-001 initially).
- Add a minimal `Program.cs` in `MongoDbPatterns.Benchmarks` (e.g., `Console.WriteLine("MongoDbPatterns Benchmarks");`).
- `dotnet build` and `dotnet test` both succeed.

**Tests to write:**
- `tests/MongoDbPatterns.Domain.Tests/SolutionStructureTests.cs`: `SolutionBuilds_Successfully` — a trivial passing test asserting `true` (placeholder until domain model exists).

**Definition of done:**
- [x] `.gitignore` created with .NET + `*.local` entries.
- [x] Solution and all 5 projects created with correct references.
- [x] `dotnet build` succeeds with no errors.
- [x] `dotnet test` passes with at least one test.
- [x] All listed tests written and passing.
- [x] Build succeeds with no warnings on new code.
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 2 — Create Docker Compose with MongoDB Replica Set

**Scope:** Repository root: `docker-compose.yml`, `scripts/mongo-init-replica.sh`.

**Spec:**
- Create `docker-compose.yml` at repository root (REQ-ENV-001, REQ-ENV-004).
- Define `mongodb` service using `mongo:7` image.
- Map container port 27017 to host port 27018 (avoids conflict with local MongoDB).
- Enable authentication with root username `admin` and password `N05@ssword` via `MONGO_INITDB_ROOT_USERNAME` and `MONGO_INITDB_ROOT_PASSWORD` environment variables.
- Start MongoDB with `--replSet rs0` command option.
- Create `scripts/mongo-init-replica.sh` that runs `rs.initiate()` against the local MongoDB instance to initialize the single-node replica set.
- Add a healthcheck to the `mongodb` service that verifies replica set status.
- Define a named volume for MongoDB data persistence.

**Tests to write:**
- No automated tests in this task (Docker Compose is validated by `docker compose build` and manual `docker compose up` in Task 12).

**Definition of done:**
- [x] `docker-compose.yml` exists with `mongodb` service on port 27018.
- [x] MongoDB starts with `--replSet rs0` and authentication enabled.
- [x] Replica set initialization script exists at `scripts/mongo-init-replica.sh`.
- [x] `dotnet build && dotnet test` still passes (no .NET changes).
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 3 — Implement Connection Settings Configuration

**Scope:** `src/MongoDbPatterns.Infrastructure/Configuration/ConnectionSettings.cs`, `src/MongoDbPatterns.Infrastructure/Configuration/ConnectionSettingsProvider.cs`, `tests/MongoDbPatterns.Infrastructure.Tests/Configuration/ConnectionSettingsProviderTests.cs`.

**Spec:**
- Create `ConnectionSettings` record/class in `MongoDbPatterns.Infrastructure.Configuration` with properties: `ConnectionString`, `DatabaseName` (REQ-ENV-006).
- Create `ConnectionSettingsProvider` class that:
  - Reads `connection-setting.local` (JSON format) from the application's working directory.
  - If the file does not exist, creates it with defaults matching the docker-compose MongoDB container: `mongodb://admin:N05%40ssword@localhost:27018/?replicaSet=rs0&authSource=admin`, database name `MongoDbPatterns` (REQ-ENV-007).
  - If the file exists, reads and deserializes it without overwriting.
- JSON schema: `{ "ConnectionString": "...", "DatabaseName": "..." }`.

**Tests to write:**
- `ConnectionSettingsProviderTests.CreatesDefaultFile_WhenFileDoesNotExist` — verify file creation with expected content.
- `ConnectionSettingsProviderTests.ReadsExistingFile_WhenFileExists` — pre-create file with custom values, verify they are read correctly.
- `ConnectionSettingsProviderTests.DoesNotOverwriteExistingFile` — pre-create file, call provider, verify file unchanged.

**Definition of done:**
- [x] `ConnectionSettings` model created.
- [x] `ConnectionSettingsProvider` creates default file when missing and reads existing files.
- [x] All listed tests written and passing.
- [x] `dotnet build && dotnet test` passes.
- [x] Build succeeds with no warnings on new code.
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 4 — Implement OrderAggregate with Version Tracking

**Scope:** `src/MongoDbPatterns.Domain/Aggregates/OrderAggregate.cs`, `src/MongoDbPatterns.Domain/Aggregates/OrderStatus.cs`, `tests/MongoDbPatterns.Domain.Tests/Aggregates/OrderAggregateTests.cs`.

**Spec:**
- Create `OrderStatus` enum: `Created`, `ReadyForFulfilment` (REQ-DOM-001).
- Create `OrderAggregate` class in `MongoDbPatterns.Domain.Aggregates` (REQ-DOM-001):
  - Properties: `Id` (Guid), `Version` (int, starts at 0), `Status` (OrderStatus).
  - Factory method `Create()` — returns a new `OrderAggregate` with `Version = 0`, `Status = Created`.
  - Method `MarkReadyForFulfilment()` — sets `Status = ReadyForFulfilment`, increments `Version` to 1.
  - Version is read-only externally; mutated only by aggregate methods.
- The aggregate does NOT yet produce domain events (Task 5 adds that).
- Remove the placeholder test from Task 1 (replaced by real domain tests).

**Tests to write:**
- `OrderAggregateTests.Create_ReturnsNewOrder_WithVersionZero` — assert `Version == 0`, `Status == Created`.
- `OrderAggregateTests.Create_AssignsUniqueId` — assert `Id != Guid.Empty`.
- `OrderAggregateTests.MarkReadyForFulfilment_IncrementsVersion` — assert `Version == 1` after call.
- `OrderAggregateTests.MarkReadyForFulfilment_SetsStatus` — assert `Status == ReadyForFulfilment`.

**Definition of done:**
- [x] `OrderAggregate` and `OrderStatus` created in Domain project.
- [x] All listed tests written and passing.
- [x] `dotnet build && dotnet test` passes.
- [x] Build succeeds with no warnings on new code.
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 5 — Implement Domain Events with Version Correlation

**Scope:** `src/MongoDbPatterns.Domain/Events/DomainEvent.cs`, `src/MongoDbPatterns.Domain/Events/OrderCreated.cs`, `src/MongoDbPatterns.Domain/Events/OrderReadyForFulfilment.cs`, `src/MongoDbPatterns.Domain/Aggregates/OrderAggregate.cs` (modified), `tests/MongoDbPatterns.Domain.Tests/Events/DomainEventTests.cs`, `tests/MongoDbPatterns.Domain.Tests/Aggregates/OrderAggregateEventTests.cs`.

**Spec:**
- Create abstract `DomainEvent` base class in `MongoDbPatterns.Domain.Events` with properties: `EventId` (Guid), `AggregateId` (Guid), `AggregateVersion` (int), `OccurredAt` (DateTime UTC) (REQ-DOM-002).
- Create `OrderCreated : DomainEvent` — produced at version 0.
- Create `OrderReadyForFulfilment : DomainEvent` — produced at version 1.
- Modify `OrderAggregate` to maintain a `List<DomainEvent>` (internal, read-only externally as `IReadOnlyList<DomainEvent>`).
  - `Create()` adds an `OrderCreated` event.
  - `MarkReadyForFulfilment()` adds an `OrderReadyForFulfilment` event.
- Add a `ClearEvents()` method on the aggregate (used by repositories after persisting events).

**Tests to write:**
- `DomainEventTests.OrderCreated_HasCorrectAggregateVersion` — assert `AggregateVersion == 0`.
- `DomainEventTests.OrderReadyForFulfilment_HasCorrectAggregateVersion` — assert `AggregateVersion == 1`.
- `OrderAggregateEventTests.Create_ProducesOrderCreatedEvent` — assert single event of type `OrderCreated` with version 0.
- `OrderAggregateEventTests.MarkReadyForFulfilment_ProducesOrderReadyForFulfilmentEvent` — assert event list contains `OrderReadyForFulfilment` with version 1.
- `OrderAggregateEventTests.ClearEvents_EmptiesEventList` — create, clear, assert empty.

**Definition of done:**
- [x] `DomainEvent`, `OrderCreated`, `OrderReadyForFulfilment` created.
- [x] `OrderAggregate` produces events on state transitions.
- [x] All listed tests written and passing.
- [x] `dotnet build && dotnet test` passes.
- [x] Build succeeds with no warnings on new code.
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 6 — Implement Case 1 Two-Phase Commit Outbox Pattern

**Scope:** `src/MongoDbPatterns.Domain/Repositories/IOrderRepository.cs`, `src/MongoDbPatterns.Infrastructure/Persistence/MongoDbContext.cs`, `src/MongoDbPatterns.Infrastructure/Persistence/Documents/OrderDocument.cs`, `src/MongoDbPatterns.Infrastructure/Persistence/Documents/OrderEventDocument.cs`, `src/MongoDbPatterns.Infrastructure/Persistence/TwoPhaseCommitOrderRepository.cs`, `tests/MongoDbPatterns.Domain.Tests/Repositories/OrderRepositoryContractTests.cs`, `tests/MongoDbPatterns.Infrastructure.Tests/Persistence/TwoPhaseCommitOrderRepositoryTests.cs`.

**Spec:**
- Create `IOrderRepository` interface in `MongoDbPatterns.Domain.Repositories` with methods:
  - `Task CreateAsync(OrderAggregate order, CancellationToken ct)`
  - `Task<OrderAggregate> GetByIdAsync(Guid id, CancellationToken ct)`
  - `Task UpdateAsync(OrderAggregate order, CancellationToken ct)`
- Create `MongoDbContext` in Infrastructure that provides `IMongoDatabase` from `ConnectionSettings` (shared by both use cases).
- Create `OrderDocument` and `OrderEventDocument` BSON document models for the `Orders` and `OrderEvents` collections.
- Create `TwoPhaseCommitOrderRepository : IOrderRepository` that:
  - `CreateAsync`: starts a MongoDB session/transaction, inserts `OrderDocument` into `Orders` collection, inserts `OrderCreated` event document into `OrderEvents` collection, commits transaction (REQ-UC-001).
  - `UpdateAsync`: starts a MongoDB session/transaction, updates `OrderDocument` in `Orders`, inserts `OrderReadyForFulfilment` event into `OrderEvents`, commits transaction.
  - `GetByIdAsync`: reads from `Orders` collection by Id.
- Map between domain aggregates/events and BSON documents.

**Tests to write:**
- `TwoPhaseCommitOrderRepositoryTests.CreateAsync_InsertsOrderAndEvent_InTransaction` — requires running MongoDB; assert both collections contain expected documents.
- `TwoPhaseCommitOrderRepositoryTests.UpdateAsync_UpdatesOrderAndInsertsEvent_InTransaction` — create then update; assert Order version updated and new event exists.
- `TwoPhaseCommitOrderRepositoryTests.GetByIdAsync_ReturnsPersistedOrder` — create, retrieve, assert match.

**Definition of done:**
- [x] `IOrderRepository` interface created in Domain.
- [x] `MongoDbContext`, document models, and `TwoPhaseCommitOrderRepository` created in Infrastructure.
- [x] All listed tests written and passing (integration tests require MongoDB).
- [x] `dotnet build && dotnet test` passes.
- [x] Build succeeds with no warnings on new code.
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 7 — Implement Case 2 Embedded Events Outbox with Optimistic Concurrency

**Scope:** `src/MongoDbPatterns.Infrastructure/Persistence/Documents/EmbeddedOrderDocument.cs`, `src/MongoDbPatterns.Infrastructure/Persistence/EmbeddedEventsOrderRepository.cs`, `src/MongoDbPatterns.Domain/Exceptions/ConcurrencyException.cs`, `tests/MongoDbPatterns.Infrastructure.Tests/Persistence/EmbeddedEventsOrderRepositoryTests.cs`.

**Spec:**
- Create `ConcurrencyException` in `MongoDbPatterns.Domain.Exceptions` — thrown when optimistic concurrency check fails.
- Create `EmbeddedOrderDocument` BSON model with `Id`, `Status`, `Version`, and `DomainEvents` array (REQ-UC-002).
- Create `EmbeddedEventsOrderRepository : IOrderRepository` that:
  - `CreateAsync`: inserts `EmbeddedOrderDocument` with `Version = 0` and `DomainEvents` containing the `OrderCreated` event.
  - `UpdateAsync`: uses `FindOneAndUpdate` with filter `{ _id: id, Version: expectedVersion }` to atomically update status, increment version, and `$push` the new event to `DomainEvents`. If matched count is 0, throws `ConcurrencyException`.
  - `GetByIdAsync`: reads from `Orders` collection (different database or collection name to avoid Case 1 collision — use collection name convention, e.g., `OrdersEmbedded`).
- Optimistic concurrency enforced entirely by the version filter on update (REQ-UC-002).

**Tests to write:**
- `EmbeddedEventsOrderRepositoryTests.CreateAsync_InsertsOrderWithEmbeddedEvent` — assert document contains `OrderCreated` in events array.
- `EmbeddedEventsOrderRepositoryTests.UpdateAsync_AppendsEventAndIncrementsVersion` — create, update, assert `Version == 1` and two events in array.
- `EmbeddedEventsOrderRepositoryTests.UpdateAsync_ThrowsConcurrencyException_OnVersionMismatch` — create, tamper version, attempt update, assert `ConcurrencyException`.

**Definition of done:**
- [x] `ConcurrencyException` created in Domain.
- [x] `EmbeddedOrderDocument` and `EmbeddedEventsOrderRepository` created in Infrastructure.
- [x] All listed tests written and passing (integration tests require MongoDB).
- [x] `dotnet build && dotnet test` passes.
- [x] Build succeeds with no warnings on new code.
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 8 — Implement Change Stream Monitoring

**Scope:** `src/MongoDbPatterns.Infrastructure/ChangeStreams/ChangeStreamWatcher.cs`, `src/MongoDbPatterns.Infrastructure/ChangeStreams/ChangeStreamResult.cs`, `tests/MongoDbPatterns.Infrastructure.Tests/ChangeStreams/ChangeStreamWatcherTests.cs`.

**Spec:**
- Create `ChangeStreamResult` record with properties: `StartTime` (DateTime), `EndTime` (DateTime), `EventsReceived` (long) (REQ-CS-001).
- Create `ChangeStreamWatcher` class that:
  - Accepts an `IMongoCollection<BsonDocument>` (or collection name + `MongoDbContext`).
  - `StartAsync(CancellationToken ct)` — opens a change stream cursor on the collection and counts insert/update events on a background task.
  - `StopAsync()` — cancels the cursor, records end time, returns `ChangeStreamResult`.
  - Thread-safe event counter (use `Interlocked.Increment`).
- For Case 1: watch both `Orders` and `OrderEvents` collections (two watcher instances).
- For Case 2: watch `OrdersEmbedded` collection (one watcher instance).
- Change stream events are counted but event payloads are not processed further.

**Tests to write:**
- `ChangeStreamWatcherTests.CountsInsertEvents` — insert documents into a test collection, verify `EventsReceived` matches insert count.
- `ChangeStreamWatcherTests.RecordsStartAndEndTimes` — start watcher, stop after delay, assert `StartTime < EndTime`.
- `ChangeStreamWatcherTests.StopAsync_ReturnsAccumulatedCount` — insert N documents, stop, assert count >= N.

**Definition of done:**
- [x] `ChangeStreamWatcher` and `ChangeStreamResult` created.
- [x] All listed tests written and passing (integration tests require MongoDB with replica set).
- [x] `dotnet build && dotnet test` passes.
- [x] Build succeeds with no warnings on new code.
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 9 — Implement Extensible Benchmark Harness with Configurable Load

**Scope:** `src/MongoDbPatterns.Benchmarks/Configuration/BenchmarkConfig.cs`, `src/MongoDbPatterns.Benchmarks/Scenarios/IBenchmarkScenario.cs`, `src/MongoDbPatterns.Benchmarks/Scenarios/TwoPhaseCommitScenario.cs`, `src/MongoDbPatterns.Benchmarks/Scenarios/EmbeddedEventsScenario.cs`, `src/MongoDbPatterns.Benchmarks/Runner/BenchmarkRunner.cs`, `src/MongoDbPatterns.Benchmarks/Program.cs` (modified).

**Spec:**
- Create `BenchmarkConfig` record with `LoadSize` (int, default 1000), `Concurrency` (int, default 5), `BatchSize` (int, default 1) (REQ-PERF-001). Read from environment variables or CLI arguments.
- Create `IBenchmarkScenario` interface (REQ-EXT-001):
  - `string Name { get; }`
  - `Task<ScenarioResult> RunAsync(BenchmarkConfig config, CancellationToken ct)`
- New scenarios can be added by implementing `IBenchmarkScenario` without modifying existing code (Open/Closed Principle).
- Create `TwoPhaseCommitScenario : IBenchmarkScenario` — creates `LoadSize` orders using `TwoPhaseCommitOrderRepository` with `Concurrency` parallel workers, then updates each to ready-for-fulfilment. Integrates `ChangeStreamWatcher` on `Orders` and `OrderEvents` collections.
- Create `EmbeddedEventsScenario : IBenchmarkScenario` — same flow using `EmbeddedEventsOrderRepository` and `ChangeStreamWatcher` on `OrdersEmbedded`.
- Create `BenchmarkRunner` that discovers all `IBenchmarkScenario` implementations, runs each sequentially, and collects results.
- Update `Program.cs` to parse configuration, initialize `MongoDbContext` via `ConnectionSettingsProvider`, and run `BenchmarkRunner`.

**Tests to write:**
- `tests/MongoDbPatterns.Domain.Tests/Benchmarks/BenchmarkConfigTests.cs`:
  - `BenchmarkConfigTests.DefaultValues_AreCorrect` — assert `LoadSize == 1000`, `Concurrency == 5`, `BatchSize == 1`.
  - `BenchmarkConfigTests.ParsesEnvironmentVariables` — set env vars, parse, assert overridden values.

**Definition of done:**
- [x] `IBenchmarkScenario` interface and both scenario implementations created.
- [x] `BenchmarkRunner` discovers and executes scenarios.
- [x] `BenchmarkConfig` parses configuration with correct defaults.
- [x] `Program.cs` wires everything together.
- [x] All listed tests written and passing.
- [x] `dotnet build && dotnet test` passes.
- [x] Build succeeds with no warnings on new code.
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 10 — Implement Benchmark Statistics Display

**Scope:** `src/MongoDbPatterns.Benchmarks/Results/ScenarioResult.cs`, `src/MongoDbPatterns.Benchmarks/Results/StatisticsFormatter.cs`, `tests/MongoDbPatterns.Domain.Tests/Benchmarks/StatisticsFormatterTests.cs`.

**Spec:**
- Create `ScenarioResult` record with: `ScenarioName`, `StartTime`, `EndTime`, `Duration`, `TotalOperations`, `ThroughputOpsPerSec`, `ChangeStreamResults` (list of `ChangeStreamResult` per watched collection) (REQ-UX-002).
- Create `StatisticsFormatter` class that:
  - Accepts a list of `ScenarioResult` and `BenchmarkConfig`.
  - Produces a formatted console output with:
    - Configuration header (load size, concurrency, batch size).
    - Per-scenario sections with: name, start/end time, duration, throughput.
    - Per-scenario change stream sub-section: collection name, start/end time, events received.
  - Output is human-readable formatted text (table or structured sections).
- Integrate `StatisticsFormatter` into `BenchmarkRunner` to print results after each scenario completes.

**Tests to write:**
- `StatisticsFormatterTests.FormatsConfigurationHeader` — assert output contains load size, concurrency, batch size values.
- `StatisticsFormatterTests.FormatsScenarioResult` — assert output contains scenario name, timing, throughput.
- `StatisticsFormatterTests.FormatsChangeStreamStats` — assert output contains change stream event counts.

**Definition of done:**
- [x] `ScenarioResult` and `StatisticsFormatter` created.
- [x] Formatter integrated into `BenchmarkRunner`.
- [x] All listed tests written and passing.
- [x] `dotnet build && dotnet test` passes.
- [x] Build succeeds with no warnings on new code.
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 11 — Configure IOPS Simulation via Docker blkio_config

**Scope:** `docker-compose.yml` (modified).

**Spec:**
- Add `blkio_config` section to the `mongodb` service in `docker-compose.yml` (REQ-ENV-005).
- Set `device_read_iops` and `device_write_iops` to a configurable value, defaulting to 3000.
- Use an environment variable `IOPS_LIMIT` (default `3000`) and device path variable `BLKIO_DEVICE` to make the configuration portable across host OSes.
- Document the device path discovery process for Linux, Windows (WSL2), and macOS in a comment within `docker-compose.yml`.
- If `blkio_config` is not supported on the host (e.g., Docker Desktop on macOS), the compose file should still start successfully (blkio is best-effort on non-Linux kernels).

**Tests to write:**
- No automated tests (Docker Compose configuration validated in Task 12 pipeline).

**Definition of done:**
- [x] `docker-compose.yml` updated with `blkio_config` on `mongodb` service.
- [x] IOPS limit defaults to 3000 and is configurable via environment variable.
- [x] `dotnet build && dotnet test` still passes (no .NET changes).
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 12 — Docker-Integrated Build, Test, and Benchmark Pipeline

**Scope:** `Dockerfile`, `docker-compose.yml` (modified).

**Spec:**
- Create a multi-stage `Dockerfile` at repository root (REQ-UX-001):
  - **Stage 1 (build)**: From `mcr.microsoft.com/dotnet/sdk:10.0`. Copy solution + projects. Run `dotnet restore`, `dotnet build`, `dotnet test`. Build fails if any test fails.
  - **Stage 2 (publish)**: Run `dotnet publish` for the `MongoDbPatterns.Benchmarks` project.
  - **Stage 3 (runtime)**: From `mcr.microsoft.com/dotnet/runtime:10.0`. Copy published output. Entry point runs the benchmark console app.
- Update `docker-compose.yml`:
  - Add `benchmarks` service that builds from the `Dockerfile`.
  - `benchmarks` depends on `mongodb` (with `condition: service_healthy`).
  - Pass environment variables for benchmark configuration (`LOAD_SIZE`, `CONCURRENCY`, `BATCH_SIZE`).
  - Pass `CONNECTION_STRING` or mount `connection-setting.local` pointing to the `mongodb` service's internal hostname.
- `docker compose build` compiles and tests the application.
- `docker compose up` starts MongoDB and runs benchmarks, printing results to stdout.

**Tests to write:**
- No new .NET tests. Validation: `docker compose build` succeeds and `docker compose up` produces benchmark output.

**Definition of done:**
- [ ] `Dockerfile` created with build, test, publish, and runtime stages.
- [ ] `docker-compose.yml` updated with `benchmarks` service.
- [ ] `docker compose build` succeeds (runs `dotnet test` as part of build).
- [ ] `dotnet build && dotnet test` still passes locally.
- [ ] Task row in Progress Tracker updated to `done`.
- [ ] Changes committed with the exact commit message from the tracker.

---

### Task 13 — Create README.md

**Scope:** Repository root: `README.md`.

**Spec:**
- Create `README.md` at repository root (REQ-ENV-002) containing:
  - **Project title and overview**: MongoDB Patterns & Benchmarks — what it does.
  - **Prerequisites**: Docker Desktop.
  - **Quick Start**: `docker compose build && docker compose up`.
  - **Architecture Overview**: Solution structure diagram (projects, responsibilities).
  - **Use Cases**: Brief description of Case 1 (two-phase commit outbox) and Case 2 (embedded events outbox).
  - **Configuration Reference**: Table of all configurable parameters (`LOAD_SIZE`, `CONCURRENCY`, `BATCH_SIZE`, `IOPS_LIMIT`, `BLKIO_DEVICE`), their defaults, and how to set them.
  - **IOPS Simulation**: How `blkio_config` works and device path discovery per OS.
  - **Development**: Local build/test instructions (`dotnet build && dotnet test`), connection settings, project structure.
  - **Extending**: How to add new benchmark scenarios (implement `IBenchmarkScenario`).

**Tests to write:**
- No automated tests. Manual review for completeness.

**Definition of done:**
- [ ] `README.md` exists at repository root with all specified sections.
- [ ] `dotnet build && dotnet test` still passes.
- [ ] Task row in Progress Tracker updated to `done`.
- [ ] Changes committed with the exact commit message from the tracker.
