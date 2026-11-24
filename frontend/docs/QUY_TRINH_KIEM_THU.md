# QUY TRÌNH TEST FRONTEND VỚI BACKEND

## 1. Kiểm tra hệ thống
- SQL Server: Database `BusTicketDB` đã được seed, xem các bảng `Trips`, `TripSeats`, `Users`.
- Redis: lệnh `PING` trả `PONG` trên `localhost:6379`.
- Mailpit: http://localhost:8025.
- API: http://localhost:5080/health → `trangThai: OK`.
- Frontend: http://localhost:5173.

## 2. Các case bắt buộc
| ID | Thao tác | Kết quả |
|---|---|---|
| FE01 | Xem trang chủ desktop 1440px, mobile 390px | Không tràn ngang, tìm chuyến dễ bấm |
| FE02 | Tra cứu chuyến theo ngày hợp lệ | Thấy danh sách từ SQL Server |
| FE03 | Khách chưa login bấm giữ ghế | Được chuyển tới login |
| FE04 | Khách A giữ ghế A01 | Mã hold + đếm ngược 5 phút |
| FE05 | Khách B đồng thời giữ cùng ghế | `409`, thấy ghế HELD khi tải lại |
| FE06 | Khách A nhập đúng hành khách và tạo booking | Backend trả `PENDING` |
| FE07 | Mã khuyến mãi không tồn tại | Backend báo lỗi; không tạo vé |
| FE08 | Thanh toán sandbox trong 5 phút | Booking `PAID`, tạo vé và EmailOutbox |
| FE09 | Xem chi tiết đơn và vé QR | QR hiển thị qua endpoint có JWT |
| FE10 | Tải QR / gửi lại email | Ảnh PNG tải được, tạo request outbox |
| FE11 | Mở Mailpit | Có email HTML và QR của hành khách |
| FE12 | STAFF check-in QR | Lần đầu thành công, lặp lại bị từ chối |
| FE13 | ADMIN CRUD bến, tuyến, xe, ghế | POST/PATCH/DELETE tương ứng API/SQL |
| FE14 | ADMIN tạo chuyến mới với xe đã đủ ghế | Sinh `TripSeats` và `TripStops` |
| FE15 | Email server tắt tạm thời | Đơn PAID không bị hủy; Outbox retry |
| FE16 | Browser không hỗ trợ BarcodeDetector | Vẫn nhập mã QR thủ công được |
| FE17 | Token JWT hết hạn | API trả 401 và người dùng cần login lại |

## 3. Tiêu chí responsive
- 320px: không có thành phần quan trọng bị mất/tràn; bảng admin được cuộn ngang.
- 390px: menu dưới cùng, card một cột, form không nhỏ hơn kích thước chạm hợp lý.
- 768px: form và sơ đồ ghế dễ dùng, không che nội dung.
- 1024px: nav và dashboard không đè nhau.
- 1440px: grid danh sách chuyến 2 cột, trang chi tiết vé và checkout 2 cột.

## 4. Giới hạn kiểm thử tự động
Các case trên là checklist để chạy trên Windows với service thật. Trong gói này mới chạy unit test có sẵn (`npm test`) và static syntax validation. Chưa có test Playwright E2E do môi trường phát triển tại đây chưa cài các phụ thuộc npm/runtime Backend.
