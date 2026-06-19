# Homework Submission - Day 3

**Họ tên:** [Điền họ tên của bạn]

## Các bài đã hoàn thành

- [x] Bài 1: Mở rộng Scan API
- [x] Bài 2: Viết Unit Tests
- [x] Bài 3: Tích hợp Frontend
- [x] Bài 4: CI/CD với GitHub Actions
- [x] Bài 5: Deploy với Docker Compose
- [ ] Bài 6: Tính năng EASM mới (Bonus)
- [ ] Bài 7: Deploy lên Cloud VM (Bonus)
- [ ] Bài 8: Domain & TLS/HTTPS (Bonus)
- [ ] Bài 9: Auto Deploy on Merge (Bonus)

## Link Repository

https://github.com/Trung-Chigusa/CMC-Homework/tree/day3-complete

## Link Demo

Local:

- Backend: `http://localhost:8080`
- Frontend dev: `http://localhost:5173`
- Frontend Docker/preview: `http://localhost:3000`

## Ghi chú triển khai

- Backend dùng ASP.NET Core Web API (.NET 10). Đề cho phép dùng ngôn ngữ khác Go nếu mô tả cách cài đặt/chạy.
- Scan job chạy nền và lưu status/results vào SQLite.
- Đã implement scan types: `dns`, `whois`, `subdomain`, `cert_trans`, `asn`, `all`, `ip`, `port`, `ssl`, `tech`.
- Port scan có safety check, chỉ cho localhost/private IP và từ chối public IP.
- Frontend dùng Vite, gọi backend qua `VITE_API_URL`.
- API documentation đã cập nhật tại `api.yml`.

## File chứng minh test

- [Bài 1 - Scan API](bai-1-scan-api-output.md)
- [Bài 2 - Unit tests](bai-2-unit-tests-output.md)
- [Bài 3 - Frontend](bai-3-frontend-output.md)
- [Bài 4 - CI/CD](bai-4-cicd-output.md)
- [Bài 5 - Docker Compose](bai-5-docker-compose-output.md)
