# Planning Context — MongoDB Outbox Benchmarks

**Plan**: `artifacts/pending/2026-04-21-22-01-mongodb-outbox-benchmarks.md`
**Created**: 2026-04-21 22:01
**Source**: `artifacts/validated/validated-requirements-requirements.md` (18 validated requirements)

---

## 1. Codebase Observations

### Current State
- **Greenfield project**: No source code, solution files, Dockerfiles, or docker-compose files exist.
- **Repository contains only**: documentation (`docs/`), validated requirements (`artifacts/validated/`), planning context (`artifacts/context/`), and Copilot workflow configuration (`.github/`).
- **REQ-ENV-003 (Copilot workflow)** is already implemented — `.github/copilot-instructions.md` and `.github/prompts/` are in place. Excluded from the plan.

### Technology Decisions (from validated requirements)
- **.NET 10** target framework (`net10.0`) — latest SDK/runtime images.
- **MongoDB 7.x** Docker image — supports replica sets, multi-document transactions, change streams.
- **MongoDB.Driver** NuGet package — official C# driver.
- **BenchmarkDotNet** — referenced in requirements but the end-to-end scenario timing with concurrent workers, change streams, and summary statistics is better served by a custom harness. BenchmarkDotNet is included as a dependency for potential micro-benchmark additions but the primary runner is custom.
- **xUnit** — standard .NET test framework; consistent with typical .NET 10 projects.

### Patterns Identified
- **DDD Aggregate Root** — `OrderAggregate` owns invariants, produces domain events, maintains version.
- **Repository Pattern** — Interface in Domain (`IOrderRepository`), two implementations in Infrastructure (one per use case).
- **Outbox Pattern (variant 1)** — Multi-document transaction across `Orders` + `OrderEvents` collections.
- **Outbox Pattern (variant 2)** — Embedded events array with optimistic concurrency via version-filtered `FindOneAndUpdate`.
- **Change Streams** — MongoDB oplog-backed real-time notification; requires replica set.

---

## 2. Plan Rationale

### Task Ordering
Tasks follow a strict dependency chain:

1. **Scaffold first** (Task 1) — everything depends on a buildable solution.
2. **Docker Compose** (Task 2) — MongoDB infrastructure needed for integration tests in later tasks.
3. **Connection settings** (Task 3) — required before any code can talk to MongoDB.
4. **Domain model** (Tasks 4–5) — aggregates and events are prerequisites for repository implementations.
5. **Use cases** (Tasks 6–7) — implement the two outbox patterns against the domain model.
6. **Change streams** (Task 8) — cross-cutting concern used by both scenarios; depends on MongoDB infrastructure and collections from Tasks 6–7.
7. **Benchmark harness** (Task 9) — orchestrates use cases with configurable load; depends on both repository implementations and change stream watcher.
8. **Statistics display** (Task 10) — formatting layer on top of benchmark results.
9. **IOPS simulation** (Task 11) — Docker Compose configuration change; independent of .NET code but logically placed after the harness is functional.
10. **Docker pipeline** (Task 12) — integrates everything into the build→test→benchmark flow.
11. **README** (Task 13) — final task because it documents the complete system.

### Scoping Decisions
- **Separate collection names per use case**: Case 1 uses `Orders` + `OrderEvents`; Case 2 uses `OrdersEmbedded`. This avoids data collision and allows independent benchmarking.
- **Custom benchmark runner over BenchmarkDotNet for E2E**: BenchmarkDotNet is excellent for micro-benchmarks but the requirements call for end-to-end scenario timing with concurrent workers, change stream monitoring, and summary statistics. A custom `BenchmarkRunner` + `IBenchmarkScenario` interface provides the needed flexibility while keeping BenchmarkDotNet available for future micro-benchmarks (extensibility).
- **Integration tests require MongoDB**: Tests in `MongoDbPatterns.Infrastructure.Tests` are integration tests that need a running MongoDB instance. During local development, developers run MongoDB via `docker compose up mongodb`. During Docker build (Task 12), integration tests will be skipped or run against a build-stage MongoDB sidecar — this trade-off is addressed in Task 12.
- **Single `IOrderRepository` interface** shared by both use cases: This enables the benchmark harness to treat both use cases uniformly, satisfying OCP (REQ-EXT-001).

### Trade-offs Considered
| Decision | Alternative | Reason |
|----------|------------|--------|
| Custom benchmark runner | BenchmarkDotNet only | BenchmarkDotNet doesn't natively support concurrent workers + change stream monitoring in a single benchmark run. Custom harness provides richer control. |
| xUnit for tests | NUnit, MSTest | xUnit is the most common choice in modern .NET; no strong reason to deviate. |
| Separate collections per use case | Same collection with different databases | Separate collections in one database is simpler for change stream watching and avoids cross-database transaction complexity. |
| `blkio_config` for IOPS | App-level rate limiting | `blkio_config` applies real disk pressure to MongoDB; app-level throttling doesn't simulate actual IOPS constraints (per validated requirements). |
| JSON for `connection-setting.local` | appsettings.json / env vars only | Requirements explicitly call for a `connection-setting.local` file; JSON is idiomatic .NET config format. |

---

## 3. Risks and Open Questions

| # | Risk / Question | Mitigation |
|---|----------------|------------|
| R1 | **Integration tests in Docker build**: Running integration tests that need MongoDB during `docker compose build` requires either a multi-stage build with a MongoDB sidecar or splitting unit/integration test runs. | Task 12 will address this — likely by running only unit tests in the Docker build stage and integration tests as a separate `docker compose` service or test target. |
| R2 | **`blkio_config` on Docker Desktop (macOS/Windows)**: `blkio_config` device-level IOPS limits may not be enforced on Docker Desktop (which uses a Linux VM). The setting is silently ignored on some platforms. | Documented as best-effort in Task 11. On Linux hosts it will work natively. README will note platform limitations. |
| R3 | **.NET 10 SDK availability**: .NET 10 is in preview as of April 2026. The `mcr.microsoft.com/dotnet/sdk:10.0` image must exist. | If the image is not yet available, fall back to the latest preview tag (e.g., `10.0-preview`). |
| R4 | **MongoDB replica set initialization timing**: The replica set init script must wait for MongoDB to be ready before calling `rs.initiate()`. | Task 2 includes a healthcheck and the init script will retry until mongosh connects successfully. |
| R5 | **Change stream event count accuracy**: Under high concurrency, the change stream watcher may miss events if it starts after the first writes or stops before all events are delivered. | The benchmark harness will start the watcher before any writes and add a brief drain period after writes complete before stopping the watcher. |
| R6 | **Connection string in docker-compose.yml**: The benchmarks service needs a connection string pointing to the MongoDB container. If this is hardcoded in `docker-compose.yml`, it technically appears in a tracked file. | The connection string in `docker-compose.yml` uses the internal Docker network hostname (not a production secret). The `connection-setting.local` file is for local/external development. This aligns with the requirement's intent (no production credentials in repo). |
