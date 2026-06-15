# Bài 2 - Batch Create Assets

## Success case

```bash
curl -X POST http://localhost:8080/assets/batch \
  -H "Content-Type: application/json" \
  -d '{"assets":[{"name":"test1.com","type":"domain"},{"name":"test2.com","type":"domain"},{"name":"192.168.1.1","type":"ip"}]}'
```

```json
{"created":3,"ids":["c88e13ea-05ed-481e-b347-295e77b8f2a5","86ed4f38-731c-4bd0-93a4-4308e5641004","fb818a60-6bc6-4a3a-98c2-7b0c57337bd7"]}
```

## Invalid type, all-or-nothing

```bash
curl -X POST http://localhost:8080/assets/batch \
  -H "Content-Type: application/json" \
  -d '{"assets":[{"name":"will-not-save.com","type":"domain"},{"name":"bad.com","type":"invalid_type"}]}'
```

```text
HTTP_STATUS:400
{"message":"type chỉ nhận các giá trị: domain, ip, service."}
COUNT_BEFORE:23
COUNT_AFTER:23
```
