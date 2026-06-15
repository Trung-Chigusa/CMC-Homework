# CMC Homework - Asset API bằng C#

Project này dùng ASP.NET Core Web API và lưu dữ liệu bằng in-memory storage, không dùng database.

## Yêu cầu môi trường

- .NET SDK 10.0 trở lên
- Port mặc định: `http://localhost:8080`

## Cách chạy

```bash
dotnet restore
dotnet run --project src/CmcHomework.Api/CmcHomework.Api.csproj --urls http://localhost:8080
```

Sau khi chạy, kiểm tra nhanh:

```bash
curl http://localhost:8080/health
```

Nếu chạy trong Windows PowerShell và gặp lỗi quote JSON với `curl.exe`, có thể dùng:

```powershell
Invoke-RestMethod -Method Post `
  -Uri "http://localhost:8080/assets" `
  -ContentType "application/json" `
  -Body '{"name":"example.com","type":"domain"}'
```

## Cấu trúc code

- `Models`: chứa entity `Asset`, request/response DTO và rule validate type/status.
- `Handlers`: nhận HTTP request, gọi service và trả response đúng status code.
- `Services`: xử lý nghiệp vụ như validate input, batch all-or-nothing, filter, search, pagination.
- `Storage`: in-memory storage dùng `ReaderWriterLockSlim` để an toàn khi nhiều request chạy đồng thời.

Luồng chính:

```text
HTTP request -> Handler -> Service -> Storage -> Model/Response
```

## Endpoint đã làm

- `POST /assets`
- `GET /assets/{id}`
- `GET /assets/stats`
- `GET /assets/count?type=&status=`
- `POST /assets/batch`
- `DELETE /assets/batch?ids=id1,id2`
- `GET /health`
- `GET /assets?page=&limit=&type=&status=`
- `GET /assets/search?q=`

## Ghi chú triển khai

- Batch create validate toàn bộ danh sách trước khi insert, nên nếu 1 item sai thì không tạo item nào.
- Storage khóa ghi khi create/delete và khóa đọc khi get/list/count để tránh race condition.
- `Guid.NewGuid()` được dùng để tạo ID nên không bị trùng ID trong các request đồng thời.
- JSON response dùng `snake_case` để khớp format đề bài như `by_type`, `asset_count`, `total_pages`.

Xem thêm phần giải thích chi tiết tại [docs/GIAI_THICH_CHI_TIET.md](docs/GIAI_THICH_CHI_TIET.md).
