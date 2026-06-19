# Bài 3 - Unit Tests Output

## Command

```bash
dotnet test CMC-Homework.sln
```

## Result

```text
Passed! - Failed: 0, Passed: 7, Skipped: 0, Total: 7, Duration: 312 ms
```

## Coverage areas

- Asset validation rules.
- Asset service create and batch all-or-nothing behavior.
- SQLite persistence through storage layer.
- Scan service workflow.
- IP scanner result shape.
- DNS scanner graceful error behavior.
- Port scan public IP safety rejection.
