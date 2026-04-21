---
created: 2026-04-21 23:15
context: Fix Docker Compose MongoDB configuration so that docker compose up produces end-to-end benchmark output and integration tests pass against live MongoDB
status: pending
---

# Implementation Plan: Docker Compose Fix and End-to-End Validation

## Design Context

The implementation plan `2026-04-21-22-01-mongodb-outbox-benchmarks` (13 tasks) is complete and committed. However, the `docker compose up` workflow has never been validated end-to-end. Two configuration defects prevent MongoDB from starting:

1. **Auth + replica set requires keyFile**: The `docker-compose.yml` uses `MONGO_INITDB_ROOT_USERNAME/PASSWORD` environment variables, which cause the official `mongo:7` Docker entrypoint to inject `--auth`. Combined with `--replSet rs0`, MongoDB 7 requires a `security.keyFile` for internal replica set member authentication — even for a single-node set. No keyFile is provided, so the real `mongod` process fails to start.

2. **Replica set init timing**: The `scripts/mongo-init-replica.sh` is mounted into `/docker-entrypoint-initdb.d/`, which runs against a temporary `mongod` instance started by the entrypoint (without `--replSet`). The `rs.initiate()` call fails with *"This node was not started with replication enabled"*.

The fix removes authentication entirely (acceptable for a local dev/benchmark tool) and replaces the init approach with a separate `mongodb-init` service that runs `rs.initiate()` against the real `mongod`. Connection strings in `ConnectionSettingsProvider`, `MongoDbIntegrationTestBase`, and `docker-compose.yml` are updated accordingly. After fixing, the full `docker compose up` pipeline is validated end-to-end.

Affected files from the prior plan:
- `docker-compose.yml` — mongodb service config, benchmarks service env
- `scripts/mongo-init-replica.sh` — currently broken init approach
- `src/MongoDbPatterns.Infrastructure/Configuration/ConnectionSettingsProvider.cs` — default connection string
- `tests/MongoDbPatterns.Infrastructure.Tests/MongoDbIntegrationTestBase.cs` — test connection string
- `README.md` — connection string references

## Guiding Principles

- **DDD**: No domain layer changes. All fixes are infrastructure/configuration.
- **TDD**: Integration tests must pass against live MongoDB after the fix.
- **DRY**: Single source of truth for connection strings where possible.
- **SOLID**: No unnecessary abstractions — direct configuration fixes.

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
| 1 | Fix MongoDB Docker configuration and replica set initialization | `done` | `Fix MongoDB replica set startup by removing auth and using init container` |
| 2 | Update connection strings across codebase | `done` | `Update connection strings to match auth-free MongoDB configuration` |
| 3 | Validate Docker Compose end-to-end benchmark output | `done` | `Validate docker compose up produces benchmark results` |
| 4 | Validate integration tests against live MongoDB | `done` | `Verify all integration tests pass against live MongoDB replica set` |

## Task Details

### Task 1 — Fix MongoDB Docker Configuration and Replica Set Initialization

**Scope:** `docker-compose.yml`, `scripts/mongo-init-replica.sh` (delete or repurpose).

**Spec:**
- Remove `MONGO_INITDB_ROOT_USERNAME` and `MONGO_INITDB_ROOT_PASSWORD` environment variables from the `mongodb` service. This eliminates the `--auth` flag injected by the Docker entrypoint, removing the keyFile requirement.
- Remove the `docker-entrypoint-initdb.d` volume mount for `mongo-init-replica.sh` (it cannot run `rs.initiate()` against the temporary entrypoint mongod).
- Add a new `mongodb-init` service to `docker-compose.yml`:
  - Uses `mongo:7` image.
  - `depends_on: mongodb: condition: service_started`.
  - Runs a shell command that waits for MongoDB to accept connections, then calls `rs.initiate()` idempotently (check `rs.status()` first).
  - `restart: "no"` — runs once and exits.
- Update the `mongodb` healthcheck: since there is no auth, remove `--username` and `--password` flags from the `mongosh` command.
- Update the `benchmarks` service `depends_on` to depend on `mongodb` with `condition: service_healthy` (unchanged) — the healthcheck will only pass after `mongodb-init` has run `rs.initiate()` and a primary is elected.
- Delete `scripts/mongo-init-replica.sh` (no longer needed) or keep as documentation reference.

**Tests to write:**
- No automated tests. Validated by `docker compose up` in Task 3.

**Definition of done:**
- [x] `MONGO_INITDB_ROOT_USERNAME/PASSWORD` removed from `mongodb` service.
- [x] `docker-entrypoint-initdb.d` volume mount removed.
- [x] `mongodb-init` service added with idempotent `rs.initiate()`.
- [x] Healthcheck updated (no auth flags).
- [x] `dotnet build && dotnet test` still passes (no .NET changes in this task).
- [x] Task row in Progress Tracker updated to `done`.
- [x] Changes committed with the exact commit message from the tracker.

---

### Task 2 — Update Connection Strings Across Codebase

**Scope:** `src/MongoDbPatterns.Infrastructure/Configuration/ConnectionSettingsProvider.cs`, `tests/MongoDbPatterns.Infrastructure.Tests/MongoDbIntegrationTestBase.cs`, `docker-compose.yml` (benchmarks service), `README.md`.

**Spec:**
- Update `ConnectionSettingsProvider.DefaultSettings.ConnectionString` from `mongodb://admin:N05%40ssword@localhost:27018/?replicaSet=rs0&authSource=admin` to `mongodb://localhost:27018/?replicaSet=rs0`.
- Update `MongoDbIntegrationTestBase.TestConnectionString` from `mongodb://admin:N05%40ssword@localhost:27018/?replicaSet=rs0&authSource=admin` to `mongodb://localhost:27018/?replicaSet=rs0`.
- Update `docker-compose.yml` benchmarks service `CONNECTION_STRING` from `mongodb://admin:N05%40ssword@mongodb:27017/?replicaSet=rs0&authSource=admin` to `mongodb://mongodb:27017/?replicaSet=rs0`.
- Update `README.md` if it references the old connection string or credentials.

**Tests to write:**
- No new tests. Existing `ConnectionSettingsProviderTests` validate default behaviour. Integration tests will validate connectivity in Task 4.

**Definition of done:**
- [ ] All connection strings updated to auth-free format.
- [ ] `dotnet build && dotnet test` passes.
- [ ] Task row in Progress Tracker updated to `done`.
- [ ] Changes committed with the exact commit message from the tracker.

---

### Task 3 — Validate Docker Compose End-to-End Benchmark Output

**Scope:** No file changes. Validation task.

**Spec:**
- Run `docker compose down -v` to ensure clean state.
- Run `docker compose build` — must succeed.
- Run `docker compose up --abort-on-container-exit` — must:
  - Start `mongodb` service.
  - Start `mongodb-init` service, which initializes the replica set and exits.
  - `mongodb` healthcheck passes (primary elected).
  - `benchmarks` service starts, runs both scenarios, and prints formatted statistics (the `StatisticsFormatter` output containing `╔`, `Configuration`, `ops/sec`, `Change Streams`).
  - `benchmarks` container exits with code 0.
- If any step fails, fix the root cause and re-validate before marking done.

**Tests to write:**
- No automated tests. Manual validation via `docker compose up` output.

**Definition of done:**
- [ ] `docker compose build` succeeds.
- [ ] `docker compose up --abort-on-container-exit` produces formatted benchmark output.
- [ ] `benchmarks` container exits with code 0.
- [ ] Task row in Progress Tracker updated to `done`.
- [ ] Changes committed with the exact commit message from the tracker.

---

### Task 4 — Validate Integration Tests Against Live MongoDB

**Scope:** No file changes expected. Validation task; fix any test failures if found.

**Spec:**
- Ensure `mongodb` container is running and healthy: `docker compose up mongodb -d` then `docker compose ps` shows `healthy`.
- Run `dotnet test` and verify all 27 tests pass (not as no-ops). Integration tests in `MongoDbPatterns.Infrastructure.Tests` should now exercise real MongoDB operations:
  - `TwoPhaseCommitOrderRepositoryTests` — create, get, update with transactions.
  - `EmbeddedEventsOrderRepositoryTests` — create, get, update with optimistic concurrency.
  - `ChangeStreamWatcherTests` — start/stop change stream, receive events.
  - `ConnectionSettingsProviderTests` — file-based settings.
- If any test fails due to code issues (not config), fix the code and include the fix in this commit.
- Confirm all tests pass with exit code 0.

**Tests to write:**
- No new tests. Existing 27 tests validated end-to-end.

**Definition of done:**
- [ ] MongoDB container running and healthy.
- [ ] `dotnet test` passes with 27 tests, 0 failures, 0 no-ops.
- [ ] Any code fixes included in this commit if needed.
- [ ] Task row in Progress Tracker updated to `done`.
- [ ] Changes committed with the exact commit message from the tracker.
