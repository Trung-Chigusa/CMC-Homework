# Bài 4 - Concurrent-safe Create

Test tạo 20 request song song.

```text
CONCURRENT_CREATED_RESPONSES:20
```

Kiểm tra tổng số asset sau khi chạy concurrent test:

```bash
curl http://localhost:8080/assets/count
```

```json
{"count":23,"filters":{"type":null,"status":null}}
```

Kết luận: server không crash, tạo đủ 20 response và count tăng đúng.
