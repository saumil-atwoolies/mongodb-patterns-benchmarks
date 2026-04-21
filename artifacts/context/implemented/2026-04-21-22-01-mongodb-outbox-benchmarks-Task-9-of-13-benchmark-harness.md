# Task 9 of 13 — Implement Extensible Benchmark Harness with Configurable Load

**Plan**: `2026-04-21-22-01-mongodb-outbox-benchmarks`
**Completed**: 2026-04-21

---

## What Was Implemented

- `src/MongoDbPatterns.Benchmarks/Configuration/BenchmarkConfig.cs` — record with `LoadSize`, `Concurrency`, `BatchSize` defaults and `FromEnvironment()` parser.
- `src/MongoDbPatterns.Benchmarks/Scenarios/IBenchmarkScenario.cs` — interface with `Name` and `RunAsync`.
- `src/MongoDbPatterns.Benchmarks/Scenarios/TwoPhaseCommitScenario.cs` — Case 1 scenario using `TwoPhaseCommitOrderRepository` + change stream watchers on `Orders` and `OrderEvents`.
- `src/MongoDbPatterns.Benchmarks/Scenarios/EmbeddedEventsScenario.cs` — Case 2 scenario using `EmbeddedEventsOrderRepository` + change stream watcher on `OrdersEmbedded`.
- `src/MongoDbPatterns.Benchmarks/Results/ScenarioResult.cs` — record with timing, throughput, and change stream results.
- `src/MongoDbPatterns.Benchmarks/Runner/BenchmarkRunner.cs` — iterates scenarios sequentially, collects results.
- `src/MongoDbPatterns.Benchmarks/Program.cs` — wires config → settings → context → scenarios → runner.
- `tests/MongoDbPatterns.Domain.Tests/Benchmarks/BenchmarkConfigTests.cs` — 2 tests for defaults and env var parsing.

## Key Decisions

1. **`ScenarioResult` created early**: Plan placed it in Task 10 but `IBenchmarkScenario.RunAsync` returns it, so it was created here. Task 10 will add `StatisticsFormatter`.
2. **`Parallel.ForAsync`**: Uses .NET 8+ `Parallel.ForAsync` with `MaxDegreeOfParallelism` for concurrent workload execution.
3. **Change stream drain delay**: 2-second delay after workload completes before stopping watchers, to allow in-flight change events to propagate.
4. **Scenario list in Program.cs**: Scenarios are explicitly listed (not auto-discovered) — keeps it simple. Adding a new scenario is just one line in `Program.cs`.
5. **Domain.Tests references Benchmarks**: `BenchmarkConfigTests` is in Domain.Tests because the plan specified it there. Added project reference.

## Issues Encountered

None.

## State for Next Task

- `ScenarioResult` already exists in `src/MongoDbPatterns.Benchmarks/Results/` — Task 10 should add `StatisticsFormatter` alongside it and integrate into `BenchmarkRunner`.
- `BenchmarkRunner.RunAllAsync` returns `List<ScenarioResult>` — Task 10 will format and print these.
