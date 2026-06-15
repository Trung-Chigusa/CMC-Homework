# Bài 6 - Pagination & Filtering

```bash
curl "http://localhost:8080/assets?page=1&limit=2&type=domain"
```

```json
{"data":[{"id":"9f2ec19b-94ec-4767-9c20-7552b01bac0d","name":"single.com","type":"domain","status":"active","created_at":"2026-06-15T03:16:17.6891676+00:00"},{"id":"c88e13ea-05ed-481e-b347-295e77b8f2a5","name":"test1.com","type":"domain","status":"active","created_at":"2026-06-15T03:16:17.7010449+00:00"}],"pagination":{"page":1,"limit":2,"total":3,"total_pages":2}}
```
