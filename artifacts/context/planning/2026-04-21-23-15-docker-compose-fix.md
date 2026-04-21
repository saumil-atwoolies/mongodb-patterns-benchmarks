# Planning Context: Docker Compose Fix and End-to-End Validation

## Codebase Observations

### Root Causes Identified

1. **Auth + replSet keyFile requirement**: `docker-compose.yml` sets `MONGO_INITDB_ROOT_USERNAME` and `MONGO_INITDB_ROOT_PASSWORD`. The official `mongo:7` entrypoint injects `--auth` when these are present. MongoDB 7 requires `security.keyFile` when both `--auth` and `--replSet` are active (even single-node). No keyFile is provided → container exits with code 1 after the entrypoint restart.

2. **docker-entrypoint-initdb.d timing**: The official mongo Docker image runs init scripts against a temporary `mongod` (no `--replSet`, no auth). `rs.initiate()` fails with "This node was not started with replication enabled". The real `mongod` then starts with `--replSet` but the replica set is never initialized.

### Verification Steps Taken

- `docker compose down -v` (clean volumes) → `docker compose up mongodb -d` → container exited with code 1.
- Logs confirmed: init script error "This node was not started with replication enabled", then container exit.
- With stale volume: "BadValue: security.keyFile is required when authorization is enabled with replica sets".

### Affected Files

| File | Issue |
|------|-------|
| `docker-compose.yml` | Auth env vars trigger keyFile requirement; init script mount broken; healthcheck uses auth flags |
| `scripts/mongo-init-replica.sh` | Cannot run rs.initiate() during entrypoint init phase |
| `ConnectionSettingsProvider.cs` | Default connection string has credentials |
| `MongoDbIntegrationTestBase.cs` | Test connection string has credentials |
| `docker-compose.yml` (benchmarks) | CONNECTION_STRING env var has credentials |
| `README.md` | May reference credentials |

### Docker Build Status

- `docker compose build benchmarks` succeeds (all layers cached). `.dockerignore` correctly excludes `bin/`, `obj/`, `.vs/`, `*.local`.
- The Dockerfile `dotnet test` step passes inside the container (integration tests no-op without MongoDB).

## Plan Rationale

### Task Ordering

1. **Task 1 first**: Fix the Docker infrastructure — this unblocks everything else.
2. **Task 2 second**: Update connection strings — depends on the auth decision from Task 1.
3. **Task 3 third**: End-to-end validation — depends on Tasks 1 + 2 being correct.
4. **Task 4 last**: Integration test validation — depends on MongoDB being healthy (Task 3 proves it).

### Why Remove Auth Instead of Adding KeyFile

- This is a **local dev/benchmark tool**, not a production system. Auth adds complexity (keyFile generation, mounting, permission management) with no security benefit for localhost benchmarking.
- Removing auth simplifies connection strings, eliminates the keyFile requirement, and makes the setup more portable.
- If auth is needed later, it can be re-added with a proper keyFile in a future plan.

### Why Separate Init Container Instead of docker-entrypoint-initdb.d

- The official mongo image runs init scripts against a temporary mongod without `--replSet`. This is by design (for creating users/databases). It cannot be used for replica set initialization.
- A separate `mongodb-init` service runs after the real mongod starts with `--replSet`, ensuring `rs.initiate()` succeeds.
- The init container is idempotent (checks `rs.status()` first) so `docker compose up` is safe to run repeatedly.

## Risks and Open Questions

1. **Healthcheck timing**: The healthcheck checks `rs.status().ok`, which requires the replica set to be initialized. The `mongodb-init` service must complete before the healthcheck deadline (10s interval × 10 retries = 100s + 30s start_period). Should be ample time.

2. **Integration test connection timeout**: If MongoDB takes >30s to become healthy, the first integration test may timeout on connect. The `_mongoAvailable` cache pattern handles this gracefully.

3. **IOPS blkio_config warnings**: Docker Desktop on Windows logs "Your kernel does not support IOPS Block read/write limit". This is expected and harmless — blkio is Linux cgroups v1 only.
