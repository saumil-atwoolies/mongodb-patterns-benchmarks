# MongoDB Patterns & Benchmarks

A .NET 10 console application that benchmarks MongoDB outbox pattern variants for DDD aggregates — with change stream monitoring, configurable concurrency and load, and optional IOPS throttling.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Quick Start

### Option 1 — Run in background

```bash
docker compose build && docker compose up
```

This builds the application (including running all tests), starts a MongoDB 7 replica set, and executes benchmarks. Results are printed to stdout. MongoDB keeps running after the benchmarks finish.

### Option 2 — Run and stop on exit

```bash
docker compose up --build --abort-on-container-exit
```

Same as Option 1 but tears down all containers as soon as the benchmarks container exits. Useful for CI or one-shot runs. Reports are saved to `./results/`.

## Architecture Overview

```
src/
  MongoDbPatterns.Domain/          # DDD aggregates, domain events, repository interfaces
  MongoDbPatterns.Infrastructure/  # MongoDB persistence, change streams, configuration
  MongoDbPatterns.Benchmarks/      # Benchmark scenarios, runner, statistics output
tests/
  MongoDbPatterns.Domain.Tests/    # Unit tests for domain + benchmark config
  MongoDbPatterns.Infrastructure.Tests/  # Integration tests (requires MongoDB)
```

## Use Cases

### Case 1 — Two-Phase Commit Outbox

Stores aggregate state in an `Orders` collection and domain events in a separate `OrderEvents` collection within a single MongoDB transaction. Change streams watch both collections independently.

### Case 2 — Embedded Events Outbox

Stores aggregate state and domain events together in a single `OrdersEmbedded` document using an embedded array. Uses optimistic concurrency (version field) instead of transactions. One change stream watches the single collection.

## Configuration Reference

| Variable | Default | Description |
|----------|---------|-------------|
| `LOAD_SIZE` | `1000` | Number of aggregate operations per scenario |
| `CONCURRENCY` | `5` | Maximum degree of parallelism |
| `BATCH_SIZE` | `1` | Operations per batch |
| `IOPS_LIMIT` | `3000` | Read/write IOPS limit on MongoDB container |
| `BLKIO_DEVICE` | `/dev/sda` | Block device path for IOPS throttling |
| `CONNECTION_STRING` | *(from file)* | MongoDB connection string override |
| `DATABASE_NAME` | `MongoDbPatterns` | Database name override (with `CONNECTION_STRING`) |

Override via environment variables:

```bash
LOAD_SIZE=5000 CONCURRENCY=10 docker compose up
```

## IOPS Simulation

The `docker-compose.yml` uses `blkio_config` to throttle MongoDB container disk I/O via Linux cgroups. This is effective on Linux hosts with cgroups v1. On Docker Desktop (macOS/Windows WSL2), blkio limits are silently ignored.

**Device path discovery:**

| OS | Command | Typical path |
|----|---------|-------------|
| Linux | `lsblk` | `/dev/sda` |
| WSL2 | `lsblk` | `/dev/sda` (virtual) |
| macOS | N/A | Not supported |

## Development

### Local Build and Test

```bash
dotnet build && dotnet test
```

Integration tests require a running MongoDB replica set (port 27018). When MongoDB is unavailable, integration tests pass as no-ops.

### Start MongoDB Locally

```bash
docker compose up mongodb -d
```

### Connection Settings

There are two ways to use a custom MongoDB connection string, depending on how you run the benchmarks. Both use local files that are excluded from git.

#### Docker runs — `.env` file

Create a `.env` file in the repository root. Docker Compose reads it automatically:

```env
CONNECTION_STRING=mongodb+srv://user:pass@cluster.mongodb.net/?retryWrites=true
DATABASE_NAME=MyBenchmarks
```

If omitted, the default (`mongodb://mongodb:27017/?replicaSet=rs0`) targets the local Docker MongoDB container.

#### Local runs — `connection-setting.local` file

On first run, a `connection-setting.local` JSON file is created in the working directory:

```json
{
  "ConnectionString": "mongodb://localhost:27018/?directConnection=true",
  "DatabaseName": "MongoDbPatterns"
}
```

Edit this file to point at your own MongoDB server. Alternatively, set the `CONNECTION_STRING` environment variable to override both the file and the defaults.

## Extending

To add a new benchmark scenario:

1. Create a class implementing `IBenchmarkScenario` in `src/MongoDbPatterns.Benchmarks/Scenarios/`.
2. Implement the `Name` property and `RunAsync` method.
3. Add an instance to the scenarios array in `Program.cs`.
