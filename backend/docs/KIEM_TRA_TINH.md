# Báo cáo kiểm tra tĩnh mã nguồn

- Đối chiếu thủ công các bảng/view/procedure trong SQL với các câu truy vấn sử dụng trong API.
- Kiểm tra tính hợp lệ JSON cấu hình, `bruno.json`, `csproj` dạng XML.
- Kiểm tra số lượng dấu ngoặc trong file C# (xử lý bỏ qua string/comments) và kiểm tra file không trống.
- Bản gốc Database đi kèm có script SQL Server: `BusTicketDB_Full.sql`, 24 bảng, 4 views và 3 stored procedures.
- **Chưa kiểm thử runtime:** container tạo mã nguồn không có .NET SDK, SQL Server hay Docker engine và không truy cập được NuGet. Vì vậy chưa thể khẳng định build/run thành công hoặc mọi luồng đã qua kiểm thử tích hợp. Hãy chạy `dotnet restore`, `dotnet build`, `scripts/demo_dat_ve.ps1` trên máy Windows theo README.
- **Thanh toán** là SANDBOX, không tích hợp tiền thật; **hoàn tiền** admin xác nhận thủ công; **email** tới Mailpit trên localhost.
