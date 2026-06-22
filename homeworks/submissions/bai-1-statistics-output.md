# Bài 1 - Statistics APIs

## GET /assets/stats

```bash
curl http://localhost:8080/assets/stats
```

```json
{"total":0,"by_type":{"domain":0,"ip":0,"service":0},"by_status":{"active":0,"inactive":0}}
```

## GET /assets/count?type=domain&status=active

```bash
curl "http://localhost:8080/assets/count?type=domain&status=active"
```

```json
{"count":3,"filters":{"type":"domain","status":"active"}}
```
