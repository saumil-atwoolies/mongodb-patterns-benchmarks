# Task 1 of 4 — Fix MongoDB Docker Configuration and Replica Set Initialization

## What was implemented
- **`docker-compose.yml`**: Removed `MONGO_INITDB_ROOT_USERNAME` and `MONGO_INITDB_ROOT_PASSWORD` environment variables from the `mongodb` service. Removed the `docker-entrypoint-initdb.d` volume mount. Removed auth flags (`--username`, `--password`, `--authenticationDatabase`) from the healthcheck. Added a new `mongodb-init` service that uses `mongo:7`, depends on `mongodb` (service_started), runs once (`restart: "no"`), and executes an idempotent `rs.initiate()` via `mongosh` entrypoint.
- **`scripts/mongo-init-replica.sh`**: Deleted via `git rm` — no longer needed since init is handled by the `mongodb-init` container.

## Key decisions
- Used `entrypoint` with inline `mongosh --eval` in the `mongodb-init` service rather than mounting a script file. This keeps the init logic self-contained in `docker-compose.yml` and avoids needing a separate script file.
- The init logic checks `rs.status()` before calling `rs.initiate()` to make it idempotent — safe to re-run on `docker compose up` if the replica set is already initialized.
- Used `mongodb:27017` (the Docker service hostname and internal port) as the replica set member host, not `localhost:27018`.

## Issues encountered
- None. Build and all 27 tests passed on first run.

## State for next task
- `docker-compose.yml` now has no auth on the MongoDB service. Task 2 must update all connection strings to remove credentials (`admin:N05%40ssword@` and `authSource=admin`).
- The `mongodb-init` service is added and will run `rs.initiate()` on startup.
