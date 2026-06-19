# Homework Submission - Day 3

**Họ tên:** [Điền họ tên của bạn]

## Các bài đã hoàn thành

- [x] Bài 1: Migrate sang Database
- [x] Bài 2: Mở rộng Scan API
- [x] Bài 3: Viết Unit Tests
- [x] Bài 4: Tích hợp Frontend
- [x] Bài 5: CI/CD với GitHub Actions (Bonus - config)
- [x] Bài 6: Deploy với Docker Compose (Bonus - config)
- [ ] Bài 7: Tính năng EASM mới (Bonus)
- [ ] Bài 8: Deploy lên Cloud VM (Bonus - cần VM thật)
- [ ] Bài 9: Domain & TLS/HTTPS (Bonus - cần domain/VM thật)
- [ ] Bài 10: Auto Deploy on Merge (Bonus - cần server/secrets thật)

## Link Repository

[Điền link GitHub repository]

## Link Demo

Local:

- Backend: `http://localhost:8080`
- Frontend dev: `http://localhost:5173`
- Frontend Docker/preview: `http://localhost:3000`

## Ghi chú triển khai

- Backend dùng ASP.NET Core Web API (.NET 10).
- Database dùng SQLite, migration tự động khi app khởi động.
- Scan job chạy nền và lưu status/results vào SQLite.
- Port scan có safety check, từ chối public IP.
- Frontend dùng Vite, gọi backend qua `VITE_API_URL`.
- API documentation đã cập nhật tại `api.yml`.

## File chứng minh test

- [Bài 1 - Database persistence](bai-1-database-output.md)
- [Bài 2 - Scan API](bai-2-scan-api-output.md)
- [Bài 3 - Unit tests](bai-3-unit-tests-output.md)
- [Bài 4 - Frontend](bai-4-frontend-output.md)
- [Bonus - CI/CD và Docker Compose](bonus-ci-docker-output.md)
