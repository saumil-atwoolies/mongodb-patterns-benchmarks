# Task 2 of 13 — Create Docker Compose with MongoDB Replica Set

**Plan**: `2026-04-21-22-01-mongodb-outbox-benchmarks`
**Completed**: 2026-04-21

---

## What Was Implemented

- `docker-compose.yml` — defines `mongodb` service using `mongo:7`, port 27018→27017, auth enabled, `--replSet rs0`, healthcheck verifying replica set status.
- `scripts/mongo-init-replica.sh` — initialization script mounted into `/docker-entrypoint-initdb.d/` that runs `rs.initiate()` on first startup and waits for primary election.
- Named volume `mongodb-data` for data persistence.

## Key Decisions

1. **Init script via `docker-entrypoint-initdb.d`**: MongoDB official image runs scripts in this directory on first init. The script waits for MongoDB readiness, then initiates the replica set.
2. **Healthcheck uses `rs.status().ok`**: Ensures the service is only marked healthy once the replica set is fully initialized, which is critical for dependent services (Task 12 `benchmarks` service).
3. **`start_period: 30s`**: Gives MongoDB enough time to start and complete replica set initialization before healthcheck failures count.

## Issues Encountered

None.

## State for Next Task

- Docker Compose is ready but not yet tested with `docker compose up` (validated in Task 12).
- Connection string for the MongoDB container: `mongodb://admin:N05%40ssword@localhost:27018/?replicaSet=rs0&authSource=admin`.
- Replica set name: `rs0`.
