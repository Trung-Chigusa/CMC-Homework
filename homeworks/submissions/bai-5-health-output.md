# Bài 5 - In-memory Health Check

## Khi chưa có dữ liệu

```bash
curl http://localhost:8080/health
```

```json
{"status":"ok","storage":{"type":"in-memory","asset_count":0},"uptime_seconds":0,"timestamp":"2026-06-15T03:16:17.6022016+00:00"}
```

## Sau khi tạo dữ liệu

```bash
curl http://localhost:8080/health
```

```json
{"status":"ok","storage":{"type":"in-memory","asset_count":23},"uptime_seconds":2,"timestamp":"2026-06-15T03:16:20.4716587+00:00"}
```
