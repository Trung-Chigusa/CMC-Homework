# Giải thích chi tiết project Day 3

## 1. Kiến trúc tổng quan

Project đi theo luồng:

```text
HTTP request -> Handler -> Service -> Storage/Scanner -> SQLite/Response
```

- Handler phụ trách HTTP: đọc route/query/body, gọi service, trả status code.
- Service phụ trách nghiệp vụ: validate dữ liệu, tạo ID, filter/search/pagination, start scan job.
- Storage phụ trách SQLite: tạo bảng, lưu asset, lưu scan job và scan result.
- Scanner phụ trách từng loại scan: DNS, WHOIS, subdomain, CT, ASN, IP, port, SSL, tech.
- Model chứa entity, request DTO, response DTO và rule chung.

## 2. Database

Project dùng SQLite để dữ liệu không mất khi restart server.

Khi app khởi động, `DatabaseInitializer` tự tạo các bảng:

- `assets`: lưu asset.
- `scan_jobs`: lưu trạng thái job scan.
- `scan_results`: lưu result JSON của từng scan.

Mặc định database nằm trong thư mục `data`. Có thể đổi bằng cấu hình:

```json
{
  "Database": {
    "Path": "data/cmc-homework.db"
  }
}
```

## 3. Asset API

Asset gồm:

- `id`: GUID.
- `name`: tên domain/IP/service.
- `type`: `domain`, `ip`, `service`.
- `status`: `active`, `inactive`.
- `created_at`: thời điểm tạo theo UTC.

Các endpoint asset:

- `POST /assets`
- `GET /assets`
- `GET /assets/{id}`
- `POST /assets/batch`
- `DELETE /assets/batch`
- `GET /assets/stats`
- `GET /assets/count`
- `GET /assets/search`

## 4. Scan API

Scan workflow:

```text
POST /assets/{id}/scan          -> tạo scan job
GET /scan-jobs/{job_id}         -> xem status
GET /scan-jobs/{job_id}/results -> lấy kết quả
```

Status có thể là:

- `pending`
- `running`
- `completed`
- `failed`
- `partial`

Khi start scan, API tạo job `pending`, trả `202 Accepted`, sau đó chạy scanner ở background task. Kết quả được lưu vào SQLite.

## 5. Scanner

Các scan type đã hỗ trợ:

- `dns`: lookup A/AAAA records.
- `whois`: trả response ổn định dạng placeholder an toàn cho môi trường local.
- `subdomain`: kiểm tra một số prefix phổ biến bằng DNS.
- `cert_trans`: trả metadata sẵn sàng tích hợp CT API.
- `asn`: trả ASN shape cho IP.
- `all`: chạy nhóm passive scan cho domain.
- `ip`: geolocation/ASN/reverse DNS shape cho IP.
- `port`: TCP port scan giới hạn các port phổ biến.
- `ssl`: đọc TLS certificate ở port 443.
- `tech`: đọc HTTP headers/meta tags để detect technology.

Port scan là active scan nên có safety check. API chỉ cho scan:

- `127.0.0.1`
- `10.x.x.x`
- `172.16.x.x` đến `172.31.x.x`
- `192.168.x.x`

Nếu truyền public IP như `8.8.8.8`, service trả lỗi validation.

## 6. Frontend

Frontend nằm trong thư mục `frontend`, dùng Vite.

Các chức năng:

- Hiển thị dashboard stats.
- Hiển thị danh sách assets.
- Tạo asset mới.
- Xóa asset.
- Khởi tạo scan.
- Poll scan job.
- Xem scan results.

Backend đã bật CORS cho:

- `http://localhost:5173`
- `http://localhost:3000`

## 7. Tests

Test project nằm ở:

```text
tests/CmcHomework.Api.Tests
```

Chạy:

```bash
dotnet test CMC-Homework.sln
```

Test hiện cover:

- Asset validation.
- Batch create all-or-nothing.
- SQLite persistence.
- Scan service workflow.
- DNS/IP scanner output shape.
- Port scan public IP rejection.

## 8. CI/CD và Docker

CI workflow:

```text
.github/workflows/ci.yml
```

Docker files:

- `src/CmcHomework.Api/Dockerfile`
- `frontend/Dockerfile`
- `docker-compose.yml`

Chạy Docker Compose:

```bash
docker compose up -d --build
```
