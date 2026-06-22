# Bài 3 - Batch Delete Assets

```bash
curl -X DELETE "http://localhost:8080/assets/batch?ids=<real-id>,fake-uuid-123"
```

```json
{"deleted":1,"not_found":1}
```

Verify asset đã bị xóa:

```bash
curl http://localhost:8080/assets/<real-id>
```

```text
HTTP_STATUS:404
{"message":"Không tìm thấy asset."}
```
