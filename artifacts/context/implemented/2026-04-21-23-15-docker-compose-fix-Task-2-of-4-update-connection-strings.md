# Task 2 of 4 — Update Connection Strings Across Codebase

## What was implemented
- **`ConnectionSettingsProvider.cs`**: Updated `DefaultSettings.ConnectionString` from `mongodb://admin:N05%40ssword@localhost:27018/?replicaSet=rs0&authSource=admin` to `mongodb://localhost:27018/?replicaSet=rs0`.
- **`MongoDbIntegrationTestBase.cs`**: Updated `TestConnectionString` to `mongodb://localhost:27018/?replicaSet=rs0`.
- **`docker-compose.yml`**: Updated benchmarks `CONNECTION_STRING` to `mongodb://mongodb:27017/?replicaSet=rs0`.
- **`ConnectionSettingsProviderTests.cs`**: Updated the `CreatesDefaultFile_WhenFileDoesNotExist` assertion to match the new default connection string.
- **`README.md`**: No changes needed — no explicit credential references found.

## Key decisions
- Updated the test assertion in `ConnectionSettingsProviderTests` since it validated the exact default connection string. This is part of the scope since connection strings are being updated.

## Issues encountered
- `ConnectionSettingsProviderTests.CreatesDefaultFile_WhenFileDoesNotExist` failed because it asserted the old connection string. Fixed by updating the expected value.

## State for next task
- All connection strings are now auth-free. Docker Compose is ready for E2E validation (Task 3).
