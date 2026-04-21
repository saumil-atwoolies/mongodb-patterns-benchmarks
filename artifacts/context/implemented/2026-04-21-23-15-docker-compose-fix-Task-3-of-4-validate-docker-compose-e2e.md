# Task 3 of 4 — Validate Docker Compose End-to-End Benchmark Output

## What was implemented
Validation task that required multiple fixes to achieve a passing E2E run:

1. **`scripts/mongo-entrypoint.sh`**: Changed replica set member host from `localhost:27017` to `mongodb:27017`. The MongoDB driver discovers replica set members from `rs.status()` — using `localhost` caused the benchmarks container (a different container) to try connecting to itself.

2. **`ConnectionSettingsProvider.cs` + `MongoDbIntegrationTestBase.cs` + `ConnectionSettingsProviderTests.cs`**: Changed host-side connection strings from `replicaSet=rs0` to `directConnection=true`. Since the replica set member is `mongodb:27017` (Docker DNS), the driver's topology discovery from the host would try to connect to `mongodb:27017` which doesn't resolve. `directConnection=true` skips discovery and connects directly.

3. **`TwoPhaseCommitOrderRepository.cs`**: Added `IsInTransaction` guard before `AbortTransactionAsync` in catch blocks. When `CommitTransactionAsync` fails due to write conflicts, the transaction is auto-aborted by the server, making a subsequent `AbortTransactionAsync` throw.

4. **`TwoPhaseCommitScenario.cs` + `EmbeddedEventsScenario.cs`**: Added collection pre-creation before concurrent operations. Without this, multiple parallel transactions would try to implicitly create the same collection, causing `WriteConflict` errors. Added `using MongoDB.Driver` for `ToListAsync` extension method.

## Key decisions
- Used `directConnection=true` for host-side connections rather than trying to make `mongodb` hostname resolvable on the host (e.g., via /etc/hosts). This is simpler and doesn't require host configuration.
- Pre-created collections explicitly rather than catching/retrying write conflicts. This is the recommended MongoDB pattern for concurrent transaction workloads.
- Used `mongodb:27017` as the replica set member hostname (Docker service name). This works inside Docker networking and the benchmarks container resolves it via Docker DNS.

## Issues encountered
1. **First attempt**: `mongodb-init` service exited, triggering `--abort-on-container-exit` before benchmarks could run. Fixed by replacing the init service with a custom entrypoint script that starts `mongod` in-process.
2. **Second attempt**: Benchmarks container crashed with `TimeoutException` — driver couldn't connect to `localhost:27017` (the replica set member from `rs.initiate()`). Fixed by using `mongodb:27017` as the member host.
3. **Third attempt**: `MongoCommandException: WriteConflict` on concurrent `commitTransaction`. Two parallel transactions tried to auto-create the `Orders` collection simultaneously. Fixed with explicit collection pre-creation.
4. **AbortTransactionAsync crash**: When `CommitTransactionAsync` fails, calling `AbortTransactionAsync` on an already-aborted transaction threw. Fixed with `IsInTransaction` guard.

## State for next task
- Docker Compose E2E validated: both scenarios produce formatted benchmark output, exit code 0.
- All connection strings use `directConnection=true` for host-side, `replicaSet=rs0` for Docker-internal.
- Task 4 should validate `dotnet test` against live MongoDB with the `directConnection=true` connection string.
