# Validated Requirements — MongoDB Patterns & Benchmarks

**Source**: `docs/requirements.md`  
**Validated**: 2026-04-21  
**Status**: VALIDATED — Ready for implementation planning

---

## 1. Requirement Decomposition

Requirements are grouped by domain: Environment/Infrastructure, Domain Model, Use Cases, Performance, User Experience, and Project Hygiene.

---

### REQ-ENV-001: Docker Compose One-Command Build & Run

**Description**: The entire application — MongoDB server, build, tests, and benchmark execution — must be launchable via a single `docker compose` command sequence.

**Acceptance Criteria**:
- [ ] A `docker-compose.yml` (or `compose.yaml`) exists at the repository root.
- [ ] `docker compose build` succeeds without manual pre-steps.
- [ ] `docker compose up` starts MongoDB, runs tests, and executes benchmarks.
- [ ] No host-side tooling beyond Docker Desktop is required.

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear.
- **Feasibility**: Standard docker-compose workflow; feasible.
- **Testability**: Verifiable by running `docker compose build && docker compose up` on a clean machine.

---

### REQ-ENV-002: README.md

**Description**: Maintain an up-to-date `README.md` at the repository root with build/run instructions, architecture overview, and configuration reference.

**Acceptance Criteria**:
- [ ] `README.md` exists at repo root.
- [ ] Contains quick-start instructions (docker compose commands).
- [ ] Documents configuration options (load size, concurrency, batch size, IOPS limit).
- [ ] Lists prerequisites (Docker Desktop).

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear.
- **Feasibility**: Trivial.
- **Testability**: Manual review; can lint for required headings.

---

### REQ-ENV-003: GitHub Copilot Workflow Initialization

**Description**: Provide a `.github/copilot-instructions.md` file giving Copilot context about document locations and project structure.

**Acceptance Criteria**:
- [ ] `.github/copilot-instructions.md` exists and is auto-loaded by Copilot.
- [ ] References docs, artifacts, and folder structure.

**Assessment**:
- **Completeness**: Complete — already validated and implemented in `artifacts/validated/validated-requirements-copilot-workflow.md`.
- **Clarity**: Clear.
- **Feasibility**: Already done.
- **Testability**: File existence check.

**Note**: This requirement is satisfied by the existing Copilot workflow setup. No further work needed.

---

### REQ-ENV-004: MongoDB Docker Container with Replica Set

**Description**: The docker-compose must provision a MongoDB server container with:
- Password: `N05@ssword`
- A non-default host port (avoid conflict with local MongoDB on 27017)
- Replica set enabled (required for change streams)

**Acceptance Criteria**:
- [ ] MongoDB container defined in docker-compose with a mapped port ≠ 27017 (e.g., 27018).
- [ ] MongoDB started with `--replSet` option (e.g., `rs0`).
- [ ] Replica set is initialized automatically (init script or entrypoint).
- [ ] Authentication enabled with root password `N05@ssword`.
- [ ] Change streams are functional against the replica set.

**Ambiguity Resolution**:
- "read-replica option" in the source text refers to the MongoDB replica set feature needed for change streams, not a read-replica scaling topology. Resolved as: single-node replica set for development.

**Assessment**:
- **Completeness**: Complete after ambiguity resolution.
- **Clarity**: Clear after resolution.
- **Feasibility**: Standard pattern — single-node replica set with `--replSet` and `rs.initiate()`.
- **Testability**: Run a change stream watch against the container and verify events are received.

---

### REQ-ENV-005: Configurable IOPS Simulation

**Description**: The application must simulate restricted disk IOPS, defaulting to 3000 IOPS, with the value being configurable.

**Acceptance Criteria**:
- [ ] IOPS limit is configurable (environment variable, config file, or CLI argument).
- [ ] Default value is 3000 IOPS.
- [ ] The throttling mechanism is documented in README.
- [ ] Benchmarks run under the configured IOPS constraint.

**Ambiguity Resolution**:
- The mechanism for simulating IOPS is not specified. Options considered:
  1. **Docker `blkio_config` device IOPS limits** (`device_read_iops`, `device_write_iops` in docker-compose) — OS/kernel-level, applies real disk pressure to the MongoDB container.
  2. **MongoDB `storageEngine` or `syncdelay` tuning** — not a true IOPS cap.
  3. **Application-level rate limiting** — throttle write operations; does NOT create real disk pressure so it defeats the purpose of benchmarking I/O-bound patterns.
- **Resolution**: Use Docker `blkio_config` device-level IOPS limits on the MongoDB container. The docker-compose file will accept the IOPS value and device path as configurable parameters (environment variables with sensible defaults). On Docker Desktop (Windows WSL2 / macOS), the virtual block device path will be auto-detected or documented. This ensures MongoDB actually experiences disk I/O throttling.

**Assessment**:
- **Completeness**: Complete — mechanism decided.
- **Clarity**: Clear.
- **Feasibility**: Feasible; Docker `blkio_config` is supported in compose v3+ and works under WSL2. Device path may need documentation per host OS.
- **Testability**: Run benchmark with different IOPS settings and verify MongoDB write latency / throughput changes accordingly.

---

### REQ-ENV-006: Connection String Security

**Description**: The MongoDB connection string must never be committed to the repository. A `connection-setting.local` file (git-ignored) holds the connection configuration.

**Acceptance Criteria**:
- [ ] `.gitignore` contains an entry for `connection-setting.local` (or `*.local` pattern).
- [ ] No connection string appears in any tracked file.
- [ ] Application reads connection details from `connection-setting.local`.

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear.
- **Feasibility**: Standard .gitignore + file-based config pattern.
- **Testability**: `git grep` for connection strings; verify `.gitignore` entry.

---

### REQ-ENV-007: Auto-Create Connection Settings File

**Description**: On startup, if `connection-setting.local` does not exist, the application must create it with default values pointing to the docker-compose MongoDB container.

**Acceptance Criteria**:
- [ ] Application checks for `connection-setting.local` at startup.
- [ ] If missing, creates the file with a default connection string matching the docker-compose MongoDB container (host, port, credentials, replica set name).
- [ ] If the file already exists, it is not overwritten.
- [ ] The auto-generated file is immediately usable without edits.

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear.
- **Feasibility**: Simple file I/O at startup.
- **Testability**: Delete the file, run the app, verify the file is created with correct defaults.

---

### REQ-DOM-001: OrderAggregate with Version Tracking

**Description**: Implement an `OrderAggregate` DDD aggregate root that maintains an integer `Version` property on its MongoDB document. The version increments with each state change.

**Acceptance Criteria**:
- [ ] `OrderAggregate` class exists following DDD aggregate root pattern.
- [ ] Document contains a `Version` integer field, starting at 0.
- [ ] Version increments on each mutation (create → 0, first update → 1, etc.).
- [ ] Version is persisted in MongoDB.

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear.
- **Feasibility**: Standard DDD pattern with MongoDB.
- **Testability**: Unit test version increment logic; integration test persistence.

---

### REQ-DOM-002: Domain Events with Version Correlation

**Description**: Domain events (`OrderCreated`, `OrderReadyForFulfilment`) must record the aggregate version at which they were produced.

**Acceptance Criteria**:
- [ ] Base domain event type includes an `AggregateVersion` property.
- [ ] `OrderCreated` event is produced at version 0.
- [ ] `OrderReadyForFulfilment` event is produced at version 1.
- [ ] Events are correlated to the exact aggregate version that generated them.

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear.
- **Feasibility**: Straightforward DDD event pattern.
- **Testability**: Unit test event creation; assert version correlation.

---

### REQ-UC-001: Case 1 — Two-Phase Commit Outbox Pattern

**Description**: Implement an outbox pattern using two-phase commits: the `Order` document is saved to an `Orders` collection and the domain event is saved to a separate `OrderEvents` collection, both within a MongoDB multi-document transaction.

**Acceptance Criteria**:
- [ ] `Orders` collection stores Order documents.
- [ ] `OrderEvents` collection stores domain event documents.
- [ ] Insert/update of Order and insert of event happen within a single MongoDB transaction.
- [ ] Transaction uses MongoDB multi-document ACID transactions (requires replica set).
- [ ] On `OrderCreated`: Order inserted + `OrderCreated` event inserted in one transaction.
- [ ] On `OrderReadyForFulfilment`: Order updated + `OrderReadyForFulfilment` event inserted in one transaction.
- [ ] Change stream on `Orders` and/or `OrderEvents` captures the mutations.

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear — "two phase commits" maps to MongoDB multi-document transactions.
- **Feasibility**: MongoDB supports multi-document transactions on replica sets (REQ-ENV-004 prerequisite).
- **Testability**: Integration test: run the flow, assert both collections contain expected documents within a transaction.

---

### REQ-UC-002: Case 2 — Embedded Events Outbox with Optimistic Concurrency

**Description**: Implement an outbox pattern where domain events are embedded as an array within the Order document itself. Optimistic concurrency is enforced via the document version. New events are appended to the array on each mutation.

**Acceptance Criteria**:
- [ ] `Orders` collection stores Order documents with an embedded `DomainEvents` array.
- [ ] Each mutation appends a new event to the array (no separate collection).
- [ ] Optimistic concurrency is enforced: updates include a version filter (`Version == expected`); if the filter matches zero documents, a concurrency conflict is raised.
- [ ] `OrderCreated` event appended at version 0.
- [ ] `OrderReadyForFulfilment` event appended at version 1.
- [ ] Change stream on `Orders` captures the mutations.

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear.
- **Feasibility**: Standard MongoDB `FindOneAndUpdate` with version filter + `$push` to array.
- **Testability**: Integration test: run the flow, assert embedded events; provoke a concurrency conflict and assert it is detected.

---

### REQ-CS-001: Change Stream Monitoring

**Description**: For both use cases, a change stream must be opened on the relevant collection(s). Events received via the change stream must be counted and the count displayed in the benchmark results.

**Acceptance Criteria**:
- [ ] A change stream watcher is started before the benchmark workload begins.
- [ ] For Case 1: watches `Orders` and/or `OrderEvents` collections.
- [ ] For Case 2: watches `Orders` collection.
- [ ] Total change events received are counted per use case.
- [ ] Change stream event count is included in the output statistics.
- [ ] Change stream start/stop time is recorded and displayed.

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear.
- **Feasibility**: MongoDB driver supports change streams on replica sets.
- **Testability**: Integration test: run workload, assert change event count matches expected mutations.

---

### REQ-PERF-001: Configurable Performance Load Parameters

**Description**: Benchmark load parameters must be configurable with the following defaults:
- **Load size**: 1000 documents
- **Concurrency**: 5 parallel workers
- **Batch size**: 1 document per operation

**Acceptance Criteria**:
- [ ] Load size, concurrency, and batch size are configurable (CLI args, env vars, or config file).
- [ ] Default values: load size = 1000, concurrency = 5, batch size = 1.
- [ ] Changing parameters produces different benchmark results.
- [ ] Configuration values are displayed in benchmark output.

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear.
- **Feasibility**: Standard configuration pattern.
- **Testability**: Run with different values, verify output reflects configuration.

---

### REQ-UX-001: Single-Command Build, Test, and Benchmark Experience

**Description**: A user should be able to run a single docker-compose command sequence to build the application (with tests passing) and then execute benchmarks with statistics output.

**Acceptance Criteria**:
- [ ] `docker compose build` compiles the .NET application and runs all tests.
- [ ] Build fails if any test fails.
- [ ] `docker compose up` executes the benchmark suite.
- [ ] Results are printed to stdout (visible in `docker compose up` output).

**Ambiguity Resolution**:
- The requirement says "docker-compose build from scratch up command". This is interpreted as a two-step sequence: `docker compose build` (which runs `dotnet build && dotnet test` inside the Dockerfile) followed by `docker compose up` (which runs the benchmarks).

**Assessment**:
- **Completeness**: Complete after resolution.
- **Clarity**: Clear after resolution.
- **Feasibility**: Standard multi-stage Dockerfile pattern.
- **Testability**: Execute the full sequence on a clean environment.

---

### REQ-UX-002: Benchmark Statistics Display

**Description**: Benchmark output must display clear, separated statistics for each use case, including timing information.

**Acceptance Criteria**:
- [ ] Each use case (Case 1, Case 2) has its own clearly labelled statistics section.
- [ ] Statistics include: start time, end time, duration, throughput (ops/sec).
- [ ] Change stream statistics (start time, end time, events received) are displayed separately.
- [ ] Performance load configuration is displayed at the top of the output.
- [ ] Output is human-readable (formatted table or structured text).

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear.
- **Feasibility**: BenchmarkDotNet provides rich output; custom summary can supplement.
- **Testability**: Visual inspection; can assert output contains required sections.

---

### REQ-EXT-001: Extensible Benchmark Architecture

**Description**: The application architecture must support adding new MongoDB usage patterns and benchmark scenarios without modifying existing code (Open/Closed Principle).

**Acceptance Criteria**:
- [ ] New use cases can be added by creating new classes implementing a common interface/base.
- [ ] No changes to existing benchmark runner or infrastructure code required to add a use case.
- [ ] Architecture follows DDD, SOLID, and DRY principles.

**Assessment**:
- **Completeness**: Implied by "easily extensible and benchmarkable" in the high-level requirement.
- **Clarity**: Clear intent.
- **Feasibility**: Standard strategy/plugin pattern.
- **Testability**: Add a mock use case and verify it integrates without changes to existing code.

---

### REQ-TEST-001: Minimum Test Coverage

**Description**: The project must include at least one passing test case.

**Acceptance Criteria**:
- [ ] At least one unit or integration test exists.
- [ ] `dotnet test` passes with zero failures.
- [ ] Tests run as part of the Docker build (build fails if tests fail).

**Assessment**:
- **Completeness**: Minimal but explicit ("definition of done").
- **Clarity**: Clear.
- **Feasibility**: Trivial.
- **Testability**: `dotnet test` exit code.

---

### REQ-GIT-001: .gitignore for .NET and Local Config

**Description**: The repository must include a comprehensive `.gitignore` for .NET development, plus entries for local configuration files.

**Acceptance Criteria**:
- [ ] `.gitignore` exists at repo root.
- [ ] Ignores standard .NET artifacts (`bin/`, `obj/`, `*.user`, etc.).
- [ ] Ignores `connection-setting.local` (or `*.local`).
- [ ] Ignores IDE-specific files (`.vs/`, `.vscode/` settings if applicable).

**Assessment**:
- **Completeness**: Complete.
- **Clarity**: Clear.
- **Feasibility**: Trivial.
- **Testability**: Verify ignored files are not tracked.

---

## 2. Assumptions

| # | Assumption | Rationale |
|---|-----------|-----------|
| A1 | "Read-replica option" means single-node MongoDB replica set for change stream support, not a multi-node read scaling topology. | Change streams require an oplog, which requires a replica set. A single-node RS is standard for development. |
| A2 | "Two phase commits" refers to MongoDB multi-document ACID transactions, not the legacy two-phase commit pattern from pre-4.0 MongoDB. | MongoDB 4.0+ supports multi-document transactions on replica sets; this is the modern equivalent. |
| A3 | The `.NET 10` target means the latest .NET 10 preview/RC available at time of implementation (April 2026). | .NET 10 is the stated stack. |
| A4 | `connection-setting.local` is a JSON file containing at minimum the MongoDB connection string. | Requirements say "connection-setting" — JSON is the idiomatic .NET config format. |
| A5 | BenchmarkDotNet is used for micro-benchmarks; a custom harness wraps the end-to-end scenario (create → update → change stream) to produce summary statistics. | BenchmarkDotNet excels at method-level benchmarks; end-to-end scenario timing needs a lightweight custom wrapper. |
| A6 | IOPS simulation will use Docker `blkio_config` device-level IOPS limits on the MongoDB container. Device path will be configurable and documented per host OS (Linux native, Windows WSL2, macOS). | Real disk pressure is required for meaningful I/O benchmarking; app-level throttling does not simulate actual IOPS constraints. |
| A7 | Batch size of 1 means each operation processes a single document (no bulk inserts). | "batchsize: 1 document" is explicit. |
| A8 | The Docker build includes `dotnet test` in the Dockerfile so that a failing test prevents image creation. | "docker-compose build from scratch" should validate tests. |

---

## 3. Dependencies

| Dependency | Required By | Notes |
|-----------|-------------|-------|
| Docker Desktop | REQ-ENV-001, REQ-ENV-004 | Must be installed on the host machine. |
| .NET 10 SDK | All REQ-* | Available as Docker base image (`mcr.microsoft.com/dotnet/sdk:10.0`). |
| MongoDB 7.x+ Docker image | REQ-ENV-004 | Official `mongo:7` image supports replica sets. |
| MongoDB.Driver NuGet package | REQ-UC-001, REQ-UC-002, REQ-CS-001 | Official MongoDB C# driver. |
| BenchmarkDotNet NuGet package | REQ-PERF-001, REQ-UX-002 | Performance benchmarking framework. |
| REQ-ENV-004 (Replica Set) | REQ-UC-001 (transactions), REQ-CS-001 (change streams) | Transactions and change streams require replica set. |
| REQ-DOM-001, REQ-DOM-002 | REQ-UC-001, REQ-UC-002 | Use cases depend on the domain model. |
| REQ-ENV-003 (Copilot workflow) | — | Already satisfied; no further work. |

---

## 4. Recommended Implementation Order

The following order respects dependencies and builds incrementally:

| Phase | Requirements | Rationale |
|-------|-------------|-----------|
| **1. Project Scaffolding** | REQ-GIT-001, REQ-ENV-002, REQ-ENV-003 | Foundation: gitignore, README, Copilot config. REQ-ENV-003 already done. |
| **2. Infrastructure** | REQ-ENV-004, REQ-ENV-001, REQ-ENV-006, REQ-ENV-007 | Docker compose with MongoDB replica set, connection settings security and auto-creation. |
| **3. Domain Model** | REQ-DOM-001, REQ-DOM-002 | OrderAggregate and domain events — prerequisite for use cases. |
| **4. Use Case 1** | REQ-UC-001 | Two-phase commit outbox pattern. |
| **5. Use Case 2** | REQ-UC-002 | Embedded events outbox pattern. |
| **6. Change Streams** | REQ-CS-001 | Change stream monitoring across both use cases. |
| **7. Benchmark Harness** | REQ-PERF-001, REQ-EXT-001, REQ-ENV-005 | Configurable load, extensible runner, IOPS simulation. |
| **8. User Experience** | REQ-UX-001, REQ-UX-002 | Docker-integrated build/test/run and statistics display. |
| **9. Tests** | REQ-TEST-001 | At least one test (likely written alongside phases 3–5 via TDD). |

---

## 5. Validation Summary

| Req ID | Title | Status | Notes |
|--------|-------|--------|-------|
| REQ-ENV-001 | Docker Compose One-Command Build & Run | **VALID** | Standard docker-compose pattern. |
| REQ-ENV-002 | README.md | **VALID** | Maintained throughout. |
| REQ-ENV-003 | Copilot Workflow Initialization | **VALID** | Already implemented. |
| REQ-ENV-004 | MongoDB Docker Container with Replica Set | **VALID** | Ambiguity resolved: single-node RS for change streams. |
| REQ-ENV-005 | Configurable IOPS Simulation | **VALID** | Mechanism resolved: Docker `blkio_config` device-level IOPS limits. |
| REQ-ENV-006 | Connection String Security | **VALID** | .gitignore + local file pattern. |
| REQ-ENV-007 | Auto-Create Connection Settings | **VALID** | File auto-generation on startup. |
| REQ-DOM-001 | OrderAggregate with Version Tracking | **VALID** | Standard DDD aggregate pattern. |
| REQ-DOM-002 | Domain Events with Version Correlation | **VALID** | Standard DDD event pattern. |
| REQ-UC-001 | Case 1 — Two-Phase Commit Outbox | **VALID** | Requires replica set (REQ-ENV-004). |
| REQ-UC-002 | Case 2 — Embedded Events Outbox | **VALID** | Optimistic concurrency via version filter. |
| REQ-CS-001 | Change Stream Monitoring | **VALID** | Requires replica set (REQ-ENV-004). |
| REQ-PERF-001 | Configurable Performance Load | **VALID** | Clear defaults provided. |
| REQ-UX-001 | Single-Command Build/Test/Benchmark | **VALID** | Multi-stage Dockerfile + compose. |
| REQ-UX-002 | Benchmark Statistics Display | **VALID** | BenchmarkDotNet + custom summary. |
| REQ-EXT-001 | Extensible Benchmark Architecture | **VALID** | Implied by "easily extensible"; SOLID/OCP. |
| REQ-TEST-001 | Minimum Test Coverage | **VALID** | At least one test; TDD recommended. |
| REQ-GIT-001 | .gitignore | **VALID** | Standard .NET + local config ignores. |

**Overall Status**: All 18 requirements are **VALID**. No items require clarification. Ready for implementation planning.
