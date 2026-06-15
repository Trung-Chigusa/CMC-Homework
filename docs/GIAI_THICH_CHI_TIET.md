# Giải thích chi tiết project

## 1. Kiến trúc tổng quan

Project đi theo luồng:

```text
Handler -> Service -> Storage -> Model
```

- Handler chỉ phụ trách HTTP: đọc route/query/body, gọi service, trả status code.
- Service phụ trách nghiệp vụ: validate dữ liệu, tạo ID, filter, search, pagination, thống kê.
- Storage phụ trách lưu trữ in-memory và đảm bảo thread-safe.
- Model chứa entity `Asset`, request DTO, response DTO và rule chung.

## 2. Model Asset

`Asset` gồm:

- `Id`: chuỗi GUID, tạo bằng `Guid.NewGuid()`.
- `Name`: tên asset, ví dụ domain hoặc IP.
- `Type`: chỉ nhận `domain`, `ip`, `service`.
- `Status`: mặc định là `active`, có thể là `active` hoặc `inactive`.
- `CreatedAt`: thời điểm tạo theo UTC.

Các rule hợp lệ được gom trong `AssetRules` để service dùng lại, tránh viết trùng logic validate.

## 3. Handler

`AssetHandlers` map các endpoint:

- `POST /assets`: tạo 1 asset.
- `POST /assets/batch`: tạo nhiều asset.
- `DELETE /assets/batch`: xóa nhiều asset theo danh sách ID.
- `GET /assets`: list có pagination/filter.
- `GET /assets/stats`: thống kê theo type/status.
- `GET /assets/count`: đếm theo filter.
- `GET /assets/search`: tìm theo tên.
- `GET /assets/{id}`: lấy chi tiết asset.

Handler bắt `ValidationException` và trả `400 Bad Request` kèm message tiếng Việt. Nếu không tìm thấy asset thì trả `404 Not Found`.

## 4. Service

`AssetService` là nơi xử lý nghiệp vụ chính.

### Validate khi tạo asset

Service kiểm tra:

- `name` không được rỗng.
- `type` không được rỗng và phải thuộc `domain`, `ip`, `service`.
- `status` nếu truyền vào thì phải thuộc `active`, `inactive`.

Nếu không truyền `status`, service tự gán mặc định là `active`.

### Batch create all-or-nothing

Với `POST /assets/batch`, service làm 2 bước:

1. Validate toàn bộ danh sách trước.
2. Chỉ khi tất cả item hợp lệ mới build asset và gọi storage để insert.

Nhờ vậy nếu có 1 asset sai, request trả `400` và không asset nào được lưu.

### Pagination/filter/search

`GET /assets` hỗ trợ:

- `page`: mặc định 1.
- `limit`: mặc định 20, tối đa 100.
- `type`: optional.
- `status`: optional.

`GET /assets/search?q=` tìm theo `Name`, không phân biệt hoa thường, partial match và giới hạn tối đa 100 kết quả.

## 5. Storage thread-safe

`InMemoryAssetStorage` dùng:

```csharp
Dictionary<string, Asset>
ReaderWriterLockSlim
```

Ý nghĩa:

- Khi đọc dữ liệu, dùng read lock.
- Khi ghi dữ liệu, dùng write lock.
- Batch create/delete chạy trong cùng một write lock để không bị request khác chen vào giữa.

Khi list dữ liệu, storage trả về một bản snapshot bằng `ToList()`. Code bên ngoài không cầm trực tiếp `Dictionary`, nên tránh lỗi khi dictionary bị thay đổi đồng thời.

## 6. Health check

`GET /health` trả:

- `status`: trạng thái server.
- `storage.type`: loại storage đang dùng.
- `storage.asset_count`: số asset hiện có trong memory.
- `uptime_seconds`: thời gian app đã chạy.
- `timestamp`: thời điểm response.

`AppLifetime` lưu thời điểm app start để tính uptime.

## 7. JSON format

Trong `Program.cs`, project cấu hình:

```csharp
JsonNamingPolicy.SnakeCaseLower
```

Vì vậy C# property như `AssetCount`, `TotalPages`, `ByType` sẽ được trả về JSON dạng `asset_count`, `total_pages`, `by_type`, đúng format đề bài.

## 8. Error handling

Các lỗi validate trả về dạng:

```json
{"message":"type chỉ nhận các giá trị: domain, ip, service."}
```

Malformed JSON được cấu hình trả `400 Bad Request` thay vì bung stack trace trong Development.
