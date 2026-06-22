# CMC Homework - Day 3 EASM API

Project dùng C#/.NET 10 để triển khai backend EASM API, SQLite persistence, scan jobs, unit tests, frontend dashboard, CI workflow và Docker Compose config.

## Yêu cầu môi trường

- .NET SDK 10.0 trở lên
- Node.js 22 trở lên
- Docker Desktop nếu muốn chạy Docker Compose
- Port mặc định backend: `http://localhost:8080`
- Port mặc định frontend: `http://localhost:5173` khi dev, `http://localhost:3000` khi Docker/preview

## Chạy backend

```bash
dotnet restore
dotnet run --project src/CmcHomework.Api/CmcHomework.Api.csproj --urls http://localhost:8080
```

Kiểm tra nhanh:

```bash
curl http://localhost:8080/health
```

Backend tự tạo SQLite database tại `src/CmcHomework.Api/data/cmc-homework-dev.db` trong môi trường Development hoặc theo cấu hình `Database:Path`.

## Chạy frontend

```bash
cd frontend
npm install
npm run dev
```

Nếu backend không chạy ở `8080`, đặt biến môi trường trước khi chạy frontend:

```bash
set VITE_API_URL=http://localhost:18080
npm run dev
```

## Chạy tests

```bash
dotnet test CMC-Homework.sln
```

## Chạy Docker Compose

```bash
docker compose up -d --build
docker compose ps
curl http://localhost:8080/health
```

Frontend chạy ở `http://localhost:3000`, backend chạy ở `http://localhost:8080`.

## Cấu trúc code

- `Models`: entity, request/response DTO, rule validate asset và scan.
- `Handlers`: map HTTP endpoints, trả status code đúng cho success/error.
- `Services`: xử lý nghiệp vụ asset, scan workflow, validation, pagination/search/stats.
- `Storage`: SQLite connection, migration tự động, asset storage, scan job/result storage.
- `Scanners`: các scanner DNS, WHOIS, subdomain, cert transparency, ASN, IP, port, SSL, tech detection.
- `frontend`: Vite dashboard dùng fetch gọi backend API.
- `tests`: xUnit tests cho model/service/storage/scanner.

Luồng chính:

```text
HTTP request -> Handler -> Service -> Storage/Scanner -> SQLite/Response
```

## Endpoint chính

- `POST /assets`
- `GET /assets/{id}`
- `GET /assets/stats`
- `GET /assets/count?type=&status=`
- `POST /assets/batch`
- `DELETE /assets/batch?ids=id1,id2`
- `GET /health`
- `GET /assets?page=&limit=&type=&status=`
- `GET /assets/search?q=`
- `POST /assets/{id}/scan`
- `GET /scan-jobs/{id}`
- `GET /scan-jobs/{id}/results`
- `GET /assets/{id}/scans`
- `GET /assets/{id}/results`
- `GET /assets/{id}/dns`
- `GET /assets/{id}/whois`
- `GET /assets/{id}/subdomains`

## Scan types

- Domain: `dns`, `whois`, `subdomain`, `cert_trans`, `ssl`, `tech`, `all`
- IP: `ip`, `asn`, `port`
- Service: `tech`

Port scan có safety check và chỉ cho phép localhost/private IP: `127.0.0.1`, `10.x.x.x`, `172.16-31.x.x`, `192.168.x.x`.

## Tài liệu và bài nộp

- API spec: `api.yml`
- Submission Day 3: `homeworks/submissions/day3/SUBMISSION.md`
- Evidence command outputs: `homeworks/submissions/day3/*.md`
