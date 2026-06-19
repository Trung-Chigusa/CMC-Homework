# Bài 1 - Database Persistence Output

## Implemented

- Added SQLite storage layer.
- Added automatic migration at application startup.
- Assets persist in `assets` table.
- Scan jobs persist in `scan_jobs` table.
- Scan results persist in `scan_results` table.

## Verification

```text
dotnet test CMC-Homework.sln

Passed! - Failed: 0, Passed: 7, Skipped: 0, Total: 7
```

Runtime health check:

```json
{
  "health_status": "ok",
  "storage_type": "sqlite",
  "total_assets": 2
}
```
