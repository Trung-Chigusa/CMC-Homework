# Homework Submission

**Họ tên:** [Điền họ tên của bạn]

## Các bài đã hoàn thành

- [x] Bài 1: Statistics APIs
- [x] Bài 2: Batch Create
- [x] Bài 3: Batch Delete
- [x] Bài 4: Concurrent-safe Create
- [x] Bài 5: In-memory Health Check
- [x] Bài 6: Pagination & Filtering (Bonus)
- [x] Bài 7: Search by Name (Bonus)

## Thông tin project

- Ngôn ngữ: C#
- Framework: ASP.NET Core Web API
- Target framework: .NET 10 (`net10.0`)
- Storage: in-memory, thread-safe bằng `ReaderWriterLockSlim`
- Cấu trúc: handler -> service -> storage -> model
- Giải thích chi tiết: [docs/GIAI_THICH_CHI_TIET.md](../../docs/GIAI_THICH_CHI_TIET.md)

## Cách chạy

```bash
dotnet restore
dotnet run --project src/CmcHomework.Api/CmcHomework.Api.csproj --urls http://localhost:8080
```

## File chứng minh test

- [Bài 1 - Statistics APIs](bai-1-statistics-output.md)
- [Bài 2 - Batch Create](bai-2-batch-create-output.md)
- [Bài 3 - Batch Delete](bai-3-batch-delete-output.md)
- [Bài 4 - Concurrent-safe Create](bai-4-concurrent-output.md)
- [Bài 5 - Health Check](bai-5-health-output.md)
- [Bài 6 - Pagination & Filtering](bai-6-pagination-output.md)
- [Bài 7 - Search by Name](bai-7-search-output.md)

## Build result

```text
dotnet build CMC-Homework.sln --no-restore

Build succeeded.
    0 Warning(s)
    0 Error(s)
```
