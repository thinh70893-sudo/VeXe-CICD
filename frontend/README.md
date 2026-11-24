# BUSGO – FRONTEND WEBSITE BÁN VÉ XE KHÁCH

**Đồ án môn Lập trình Web | React + Vite + REST API ASP.NET Core | Responsive desktop/tablet/mobile**

Frontend này được viết để **kết nối trực tiếp với Backend BusTicket (đã tạo trong cuộc trò chuyện trước)**, không phải một trang HTML tĩnh. Giao diện tối giản với màu cam và xanh navy, tối ưu thao tác đặt vé trên mọi loại màn hình.

## 1. Những gì đã có

- Trang chủ tìm chuyến theo điểm đi, điểm đến, ngày khởi hành; đổi chiều tìm kiếm.
- Trang danh sách chuyến lọc tuyến/ngày, sắp xếp thời gian/giá.
- Sơ đồ ghế với trạng thái `AVAILABLE`, `HELD`, `SOLD`, `BLOCKED`; giữ ghế 300 giây qua Redis; đếm ngược và đổi ghế.
- Đăng ký, đăng nhập JWT và cập nhật hồ sơ/đổi mật khẩu.
- Đơn đặt vé có nhiều hành khách, nhập khuyến mãi, dùng email tài khoản để nhận vé.
- Thanh toán giả lập SANDBOX (chỉ ở Development), không thu tiền thật.
- Danh sách và chi tiết đơn, hủy đơn đang chờ, xem QR của từng vé, tải mã QR, in vé, yêu cầu gửi lại email.
- Gửi yêu cầu hoàn tiền; quản trị xem đơn, danh sách người dùng, email outbox, hoàn tiền.
- CRUD danh mục cho Admin theo whitelist Backend; tạo/hủy chuyến xe.
- STAFF/ADMIN nhập hoặc quét QR bằng camera (nếu browser hỗ trợ BarcodeDetector) để xác minh và soát vé.
- Tất cả giá/ghế/trạng thái được lấy từ API; frontend không tự xác nhận vé đã bán.

## 2. Chuẩn bị

Cài đặt trên Windows:
1. Visual Studio 2026 (hoặc VS Code) với ASP.NET/Web workload nếu cần chạy Backend.
2. Node.js **>= 22.12** (ưu tiên Node 22 LTS hoặc phiên bản được Vite hiện hành hỗ trợ).
3. SQL Server và Docker Desktop theo README Backend.
4. Bộ Database `BusTicketDB_Full.sql` và backend `BusTicketBackend.sln` **đã tạo trước**.

> Vite chạy bằng Node.js, **không phải C# project**. Trong Visual Studio mở thư mục frontend bằng **File > Open > Folder**, dùng **View > Terminal** để chạy `npm` hoặc chạy frontend bằng VS Code. Backend vẫn chạy trong Visual Studio bằng file `.sln`.

## 3. Khởi chạy đúng thứ tự

### Bước A – Database, Redis và Mailpit

Giải nén package Backend `BusTicket_Backend_Full_SourceCode.zip`. Trong thư mục Backend, mở PowerShell:

```powershell
docker compose up -d
```

Sau đó mở SSMS hoặc Azure Data Studio/SQL Server tool và thực thi file `database/BusTicketDB_Full.sql` **một lần trên database sạch** để tạo `BusTicketDB` + seed. File có lệnh `GO` nên hãy chạy bằng công cụ hỗ trợ SQL Server batch.

### Bước B – Backend ASP.NET Core

Tại thư mục `BusTicket_Backend_Full`:

```powershell
cd src/BusTicket.Api
dotnet restore
dotnet run --launch-profile http
```

Mở `http://localhost:5080/health` để kiểm tra. Backend phải cấu hình Development, SQL Server, Redis, SMTP theo README Backend.

### Bước C – Frontend React

Giải nén file frontend này. Trong PowerShell (thư mục có `package.json`):

```powershell
npm install
npm run dev
```

Mở **http://localhost:5173/**. Vite tự proxy `/api` sang `http://localhost:5080`; không cần thiết lập CORS cho trường hợp mặc định. **Không** nhập `http://localhost:5080` vào các link frontend.

Muốn đổi địa chỉ Backend trong dự án frontend, có thể chỉnh `vite.config.js` phần `server.proxy`, hoặc tạo `.env.local` từ `.env.example` và đặt `VITE_API_URL`, rồi khởi động lại Vite.

### Bước D – Build frontend

```powershell
npm run build
npm run preview
```

> Nếu dùng BrowserRouter trên hosting static, cần cấu hình fallback mọi URL frontend không có file tương ứng về `index.html`.

## 4. Tài khoản demo

Sau khi seed database Backend:
- `khach1@example.test` / `Demo@123456`: khách hàng.
- `admin@busticket.test` / `Demo@123456`: admin.
- `staff@busticket.test` / `Demo@123456`: nhân viên.

Không dùng thông tin này trên môi trường production.

## 5. Cách demo đặt vé A–Z

1. Mở trang chủ, chọn thành phố, ngày có chuyến (đối chiếu kết quả `/api/trips`).
2. Chọn chuyến → chọn ghế trống → **Giữ ghế**.
3. Đăng nhập nếu chưa có JWT. Giữ ghế cần tài khoản đăng nhập.
4. Điền người liên hệ và hành khách, email nhận vé chỉ đọc và phải trùng email tài khoản.
5. Tạo booking, chuyển sang `/thanh-toan/:maDatVe`. Chọn **Tạo thanh toán Sandbox** → **Xác nhận thanh toán (Demo)** trước khi Redis TTL hết hạn 300 giây.
6. Xem chi tiết đơn (`PAID`) và mã QR tại `/ve/:maVe`; có thể tải ảnh QR hoặc yêu cầu gửi lại email.
7. Mở Mailpit: `http://localhost:8025` để xem email HTML với QR. EmailWorker có thể gửi chậm một chút do lịch xử lý.
8. Đăng nhập STAFF và mở `/soat-ve`; lấy nội dung `BTICKET:{guid}` bằng cách quét ảnh QR (hoặc nhập chuỗi), kiểm tra và check-in một lần.

**Lưu ý:** Database seed có các chuyến tương lai tính theo ngày sinh dữ liệu; nếu không tìm thấy chuyến ngày bạn chọn, dùng Admin tạo chuyến mới hoặc tìm không lọc ngày.

## 6. Chi tiết thư mục

```text
BusTicket_Frontend_Full/
├─ index.html
├─ package.json
├─ vite.config.js
├─ .env.example
├─ public/
├─ src/
│  ├─ main.jsx, App.jsx, styles.css
│  ├─ api/client.js             # wrapper fetch + đường dẫn API
│  ├─ context/                  # auth JWT, toast
│  ├─ components/               # header, footer, form tìm chuyến, trip card, UI chung
│  ├─ pages/                    # trang web khách hàng và quản trị
│  ├─ data/quanTri.js           # mapping tên trường CRUD với Backend
│  └─ utils/hienThi.js          # tiền VND, ngày giờ VN, status
├─ docs/KIEN_TRUC_FRONTEND.md
└─ README.md
```

## 7. API được sử dụng

- Public: `GET /api/catalog/cities`, `/api/trips`, `/api/trips/{id}/seats`, `/api/trips/{id}/stops`.
- Auth: `POST /api/auth/login`, `/register`, `GET /api/auth/me`, `PUT /api/auth/profile` và `/password`.
- Redis: `POST /api/seat-holds`, `GET/DELETE /api/seat-holds/{guid}`.
- Booking: `POST /api/bookings`, `GET /api/bookings/me`, `/api/bookings/{id}`, `POST /api/bookings/{id}/cancel`.
- Payment: `POST /api/payments`, `/api/payments/{id}/sandbox-confirm`.
- Vé: `GET /api/tickets/{id}`, `/qr`, `POST /resend-email`, `POST /api/tickets/verify`, `/check-in`.
- Admin: `GET/POST/PATCH/DELETE /api/admin/crud/{resource}`, `/api/admin/revenue`, `/users`, `/email-logs`, `/bookings`, `/api/refunds`.

Tham khảo `docs/API_ENDPOINTS.md` trong package Backend để biết trường request/response. Tên biến tiếng Việt không dấu được giữ nhất quán: `maChuyen`, `maDatVe`, `danhSachMaGhe`, `tenLienHe`, `hanhKhach`, `maQr`.

## 8. Xử lý lỗi thường gặp

| Lỗi | Cách sửa |
|---|---|
| `Không kết nối được Backend` | Mở `http://localhost:5080/health`, kiểm tra Vite proxy |
| `404 /api/catalog/cities` | Kiểm tra Backend, endpoint và seed database |
| `401 Unauthorized` | Đăng nhập lại, token có thể đã hết hạn |
| `403 khi thanh toán` | `sandbox-confirm` chỉ hoạt động trong môi trường Development |
| `409 Conflict` khi giữ ghế | Khách khác đang giữ hoặc SQL đã bán; chọn ghế khác |
| `410 Gone` | TTL giữ ghế hết, quay lại sơ đồ và chọn lại |
| Không có chuyến | Kiểm tra bộ lọc ngày, tuyến và dữ liệu `Trips` trong database |
| Chưa có email trong Mailpit | Kiểm tra WorkerEmail, EmailOutbox, SMTP localhost:1025 |
| Browser không quét QR | Dùng Chromium hỗ trợ `BarcodeDetector`, HTTPS/localhost, hoặc nhập mã thủ công |

## 9. Bảo mật quan trọng

- `sessionStorage` đang chứa JWT **chỉ phù hợp cho bài demo**; production nên sử dụng BFF/cookie HttpOnly + CSRF.
- Không xác nhận thanh toán chỉ bằng redirect Frontend; Backend SQL transaction mới là nguồn xác nhận.
- Không đưa `Jwt:Key`, SMTP password, thông tin SQL, Redis, khóa webhook lên React hoặc biến `VITE_*`.
- Email gửi thành công chỉ có nghĩa SMTP chấp nhận thư, không chắc đã được khách đọc.
- Sandbox chỉ demo, không thanh toán và hoàn tiền thật.

## 10. Tình trạng kiểm thử

- Đã đối chiếu danh sách endpoint và DTO trực tiếp với bộ Backend trong cuộc trò chuyện.
- Đã kiểm tra cú pháp JSX/JavaScript bằng TypeScript parser và phân tích CSS bằng PostCSS.
- **Chưa chạy `npm install` / `npm run build` hoặc kiểm thử thực tế với SQL/Redis/Mailpit**, do môi trường tạo file không truy cập được npm registry, không có dịch vụ backend đang chạy. Cần kiểm thử tích hợp trên máy của bạn trước khi nộp.

## 11. Kiểm thử tiện ích không cần Backend

Chạy các unit test định dạng tiền, ngày UTC và ánh xạ dữ liệu SQL:

```powershell
npm test
```

Có thể khởi chạy bằng `scripts/chay_frontend.ps1`. Trước khi chạy, dùng `scripts/kiem_tra_backend.ps1` để kiểm tra API; mẫu trang chủ tĩnh tham khảo trong `docs/preview.html` (file này chỉ để xem cách bố trí, không gọi API).

**Lưu ý cho Admin:** Khi tạo xe mới, phải tạo đủ ghế trong CRUD `seats` bằng đúng `capacity` trước khi tạo chuyến với xe đó. Chức năng sửa chuyến cần chọn lại tài xế do API chi tiết chuyến hiện không trả `DriverId`.

Tài liệu kiểm thử đầy đủ: `docs/QUY_TRINH_KIEM_THU.md` và payload API minh họa: `docs/API_MAPPING.md`.
