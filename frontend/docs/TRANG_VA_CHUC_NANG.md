# DANH SÁCH TRANG / TÍNH NĂNG FRONTEND

| Trang | API | Mục đích |
|---|---|---|
| Trang chủ | GET cities | Bộ tìm chuyến, giới thiệu hệ thống |
| Tìm chuyến | GET trips | Lọc tuyến và ngày, sắp xếp |
| Sơ đồ ghế | GET trip/seats, POST seat-holds | Redis giữ chỗ, chọn tối đa 5 ghế, đếm TTL |
| Checkout | POST bookings | Thông tin liên hệ, hành khách, khuyến mãi |
| Thanh toán | POST payments, POST sandbox-confirm | Mô phỏng thanh toán DEV |
| Đăng nhập/đăng ký | POST auth/login, auth/register | JWT, phân quyền |
| Hồ sơ | GET auth/me, PUT profile/password | Cập nhật tên, điện thoại, mật khẩu |
| Đơn của tôi | GET bookings/me, GET refunds/me | Lịch sử đơn, yêu cầu hoàn |
| Chi tiết đơn | GET booking, POST cancel/refund/review | Trạng thái, hành khách, đánh giá sau chuyến |
| Chi tiết vé | GET ticket, GET ticket/qr, POST resend-email | Hiển thị/tải/in QR, gửi lại email |
| Admin tổng quan | GET admin/revenue/bookings/users/email-logs | Tổng hợp thống kê cơ bản |
| Admin CRUD | CRUD admin/crud/{resource} | 9 tài nguyên được Backend whitelist |
| Admin chuyến xe | GET/POST/PUT/DELETE trips | Tạo, sửa, hủy chuyến; khóa/mở ghế |
| Admin người dùng | GET admin/users, PATCH active/role | Kích hoạt, vô hiệu, phân quyền |
| Admin hoàn tiền | GET/PATCH refunds | Duyệt và ghi nhận thủ công |
| Nhân viên soát vé | POST tickets/verify/check-in | Xác minh và check-in một lần |

Tất cả những trang trên là **mã nguồn giao diện kết nối API**, không tự lưu trữ dữ liệu như một Backend thứ hai.
