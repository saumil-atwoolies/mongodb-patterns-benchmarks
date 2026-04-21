# MongoDB Patterns & Benchmarks

A .NET 10 console application that benchmarks MongoDB outbox pattern variants for DDD aggregates — with change stream monitoring, configurable concurrency and load, and optional IOPS throttling.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Quick Start

```bash
docker compose build && docker compose up
```

This builds the application (including running all tests), starts a MongoDB 7 replica set, and executes benchmarks. Results are printed to stdout.

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

On first run, a `connection-setting.local` file is created in the working directory with default connection parameters. Set the `CONNECTION_STRING` environment variable to override.

## Extending

To add a new benchmark scenario:

1. Create a class implementing `IBenchmarkScenario` in `src/MongoDbPatterns.Benchmarks/Scenarios/`.
2. Implement the `Name` property and `RunAsync` method.
3. Add an instance to the scenarios array in `Program.cs`.
