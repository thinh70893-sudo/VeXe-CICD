# ĐỐI CHIẾU TRƯỜNG FRONTEND VỚI BACKEND

Dưới đây là các payload trọng tâm đã được đối chiếu với `src/BusTicket.Api/DTOs/YeuCau.cs` trong Backend.

## Đăng nhập
POST `/api/auth/login`
```json
{"email":"khach1@example.test","matKhau":"Demo@123456"}
```

## Giữ ghế Redis
POST `/api/seat-holds` (Bearer JWT)
```json
{"maChuyen":15,"danhSachMaGhe":[101,102]}
```

## Đặt vé
POST `/api/bookings` (Bearer JWT)
```json
{
  "maGiuGhe":"00000000-0000-0000-0000-000000000000",
  "tenLienHe":"Nguyễn Văn A",
  "emailLienHe":"khach1@example.test",
  "soDienThoaiLienHe":"0900000000",
  "hanhKhach":[
    {"hoTen":"Nguyễn Văn A","soDienThoai":"0900000000","maGhe":101},
    {"hoTen":"Trần Thị B","soDienThoai":"0900000001","maGhe":102}
  ],
  "maKhuyenMai":null
}
```
`maGiuGhe` phải lấy từ API giữ ghế thực tế, ví dụ trên là placeholder.

## Thanh toán thử nghiệm
POST `/api/payments` (Bearer JWT)
```json
{"maDatVe":123}
```
Sau đó POST `/api/payments/{maThanhToan}/sandbox-confirm`, **chỉ có ở Development**.

## Xác minh QR
POST `/api/tickets/verify` (STAFF/ADMIN JWT)
```json
{"maQr":"BTICKET:00000000-0000-0000-0000-000000000000"}
```

## Admin CRUD
POST `/api/admin/crud/routes` (ADMIN JWT)
```json
{"maTuyen":"SG-DL-NEW","maBenDi":1,"maBenDen":4,"soKm":315,"soPhutDuKien":440,"hoatDong":true}
```

Giá trị mã ID minh họa phải thay bằng bản ghi thật trong Database.
