using System.Data;
using BusTicket.Api.Data;
using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BusTicket.Api.Services;

public sealed class DichVuDatVe(KetNoiSql ketNoi, DichVuGiuGhe giuGhe)
{
    public async Task<object> TaoDon(long maNguoiDung, DatVeDto yeuCau)
    {
        var duLieuGiu = await giuGhe.KiemTra(yeuCau.MaGiuGhe, maNguoiDung);
        var hanhKhach = yeuCau.HanhKhach ?? [];
        var maGheDaChon = hanhKhach.Select(x => x.MaGhe).ToArray();
        if (hanhKhach.Count == 0 || hanhKhach.Count != duLieuGiu.DanhSachMaGhe.Length ||
            maGheDaChon.Distinct().Count() != maGheDaChon.Length ||
            !maGheDaChon.Order().SequenceEqual(duLieuGiu.DanhSachMaGhe.Order()) ||
            hanhKhach.Any(x => string.IsNullOrWhiteSpace(x.HoTen)))
            throw new NgoaiLeNghiepVu("Thông tin hành khách và danh sách ghế không trùng với phiên giữ ghế.");
        if (string.IsNullOrWhiteSpace(yeuCau.TenLienHe) || string.IsNullOrWhiteSpace(yeuCau.SoDienThoaiLienHe))
            throw new NgoaiLeNghiepVu("Thiếu thông tin người liên hệ.");

        await using var sql = ketNoi.TaoKetNoi();
        await sql.OpenAsync();
        await using var giaoDich = (SqlTransaction)await sql.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Tránh gửi vé đến địa chỉ do người khác tự nhập mà chưa xác nhận là email tài khoản.
            var emailTaiKhoan = await sql.ExecuteScalarAsync<string?>(
                "SELECT Email FROM dbo.Users WITH (UPDLOCK) WHERE UserId=@maNguoiDung AND IsActive=1",
                new { maNguoiDung }, giaoDich);
            if (emailTaiKhoan is null || !emailTaiKhoan.Equals(yeuCau.EmailLienHe.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new NgoaiLeNghiepVu("Email nhận vé phải trùng với email tài khoản đăng nhập.", 403);

            var chuyen = await sql.QuerySingleOrDefaultAsync<dynamic>("""
                SELECT TripId,Status,DepartureAtUtc FROM dbo.Trips WITH (UPDLOCK,HOLDLOCK)
                WHERE TripId=@maChuyen
                """, new { maChuyen = duLieuGiu.MaChuyen }, giaoDich);
            if (chuyen is null || chuyen.Status != "SCHEDULED" || (DateTime)chuyen.DepartureAtUtc <= DateTime.UtcNow)
                throw new NgoaiLeNghiepVu("Chuyến xe không còn nhận đặt vé.", 409);
            if (duLieuGiu.HetHanUtc <= DateTime.UtcNow)
                throw new NgoaiLeNghiepVu("Giữ ghế hết hạn.", 410);

            var danhSachGia = (await sql.QueryAsync<dynamic>("""
                SELECT SeatId,FareVnd FROM dbo.TripSeats WITH (UPDLOCK,HOLDLOCK)
                WHERE TripId=@maChuyen AND SeatId IN @maGheDaChon AND Status='AVAILABLE'
                """, new { maChuyen = duLieuGiu.MaChuyen, maGheDaChon }, giaoDich)).ToList();
            if (danhSachGia.Count != maGheDaChon.Length)
                throw new NgoaiLeNghiepVu("Có ghế vừa bán/hết khả dụng.", 409);
            var giaTheoGhe = danhSachGia.ToDictionary(x => (int)x.SeatId, x => (decimal)x.FareVnd);
            decimal tamTinh = giaTheoGhe.Values.Sum();
            decimal giamGia = 0;
            int? maKhuyenMai = null;
            if (!string.IsNullOrWhiteSpace(yeuCau.MaKhuyenMai))
            {
                var ma = yeuCau.MaKhuyenMai.Trim().ToUpperInvariant();
                var uuDai = await sql.QuerySingleOrDefaultAsync<dynamic>("""
                    SELECT PromotionId,DiscountType,DiscountValue,MaxDiscountVnd
                    FROM dbo.Promotions WITH (UPDLOCK,HOLDLOCK)
                    WHERE PromoCode=@ma AND IsActive=1 AND SYSUTCDATETIME() BETWEEN StartsAtUtc AND EndsAtUtc
                      AND MinOrderVnd<=@tamTinh AND (MaxUses IS NULL OR UsedCount<MaxUses)
                    """, new { ma, tamTinh }, giaoDich);
                if (uuDai is null) throw new NgoaiLeNghiepVu("Mã khuyến mãi không hợp lệ hoặc đã hết lượt.", 409);
                maKhuyenMai = (int)uuDai.PromotionId;
                giamGia = uuDai.DiscountType == "PERCENT"
                    ? decimal.Truncate(tamTinh * (decimal)uuDai.DiscountValue / 100m)
                    : (decimal)uuDai.DiscountValue;
                if (uuDai.MaxDiscountVnd is not null) giamGia = Math.Min(giamGia, (decimal)uuDai.MaxDiscountVnd);
                giamGia = Math.Min(tamTinh, giamGia);
                await sql.ExecuteAsync("UPDATE dbo.Promotions SET UsedCount=UsedCount+1 WHERE PromotionId=@maKhuyenMai",
                    new { maKhuyenMai }, giaoDich);
            }
            var maDon = "BK-" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
            var maDatVe = await sql.ExecuteScalarAsync<long>("""
                INSERT dbo.Bookings(BookingCode,UserId,TripId,PromotionId,ContactName,ContactEmail,
                    ContactPhone,Status,SubtotalVnd,DiscountVnd,HoldId,ExpiresAtUtc)
                OUTPUT INSERTED.BookingId
                VALUES(@maDon,@maNguoiDung,@maChuyen,@maKhuyenMai,@tenLienHe,@emailLienHe,
                    @soDienThoai,'PENDING',@tamTinh,@giamGia,@maGiuGhe,@hetHanUtc)
                """, new { maDon, maNguoiDung, maChuyen = duLieuGiu.MaChuyen, maKhuyenMai,
                    tenLienHe = yeuCau.TenLienHe.Trim(), emailLienHe = emailTaiKhoan,
                    soDienThoai = yeuCau.SoDienThoaiLienHe.Trim(), tamTinh, giamGia,
                    maGiuGhe = duLieuGiu.MaGiuGhe, hetHanUtc = duLieuGiu.HetHanUtc }, giaoDich);
            foreach (var khach in hanhKhach)
            {
                var maHanhKhach = await sql.ExecuteScalarAsync<long>("""
                    INSERT dbo.BookingPassengers(BookingId,FullName,PhoneNumber)
                    OUTPUT INSERTED.PassengerId VALUES(@maDatVe,@hoTen,@soDienThoai)
                    """, new { maDatVe, hoTen = khach.HoTen.Trim(), soDienThoai = khach.SoDienThoai }, giaoDich);
                await sql.ExecuteAsync("""
                    INSERT dbo.BookingItems(BookingId,TripId,PassengerId,SeatId,PriceVnd)
                    VALUES(@maDatVe,@maChuyen,@maHanhKhach,@maGhe,@gia)
                    """, new { maDatVe, maChuyen = duLieuGiu.MaChuyen, maHanhKhach,
                        maGhe = khach.MaGhe, gia = giaTheoGhe[khach.MaGhe] }, giaoDich);
            }
            await giaoDich.CommitAsync();
            return new { maDatVe, maDon, trangThai = "PENDING", tamTinh, giamGia,
                tongTien = tamTinh - giamGia, hetHanUtc = duLieuGiu.HetHanUtc };
        }
        catch { await giaoDich.RollbackAsync(); throw; }
    }

    public async Task<IEnumerable<dynamic>> DanhSach(long maNguoiDung, bool xemTatCa = false)
    {
        await using var sql = ketNoi.TaoKetNoi();
        return await sql.QueryAsync("""
            SELECT TOP (100) BookingId AS MaDatVe,BookingCode AS MaDon,TripCode AS MaChuyen,
                ContactEmail AS EmailLienHe, Status AS TrangThai,TotalVnd AS TongTien,
                CreatedAtUtc AS NgayTaoUtc,ExpiresAtUtc AS HetHanUtc,PaidAtUtc AS DaThanhToanUtc,
                TicketCount AS SoVe
            FROM dbo.vwBookingSummary
            WHERE (@xemTatCa=1 OR UserId=@maNguoiDung)
            ORDER BY CreatedAtUtc DESC
            """, new { xemTatCa, maNguoiDung });
    }

    public async Task<object> ChiTiet(long maDatVe, long maNguoiDung, bool choPhepNhanVien = false)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var don = await sql.QuerySingleOrDefaultAsync("""
            SELECT BookingId AS MaDatVe,BookingCode AS MaDon,UserId AS MaNguoiDung,
                TripId AS MaChuyen,ContactName AS TenLienHe,ContactEmail AS EmailLienHe,
                ContactPhone AS SoDienThoai,Status AS TrangThai,SubtotalVnd AS TamTinh,
                DiscountVnd AS GiamGia,TotalVnd AS TongTien,ExpiresAtUtc AS HetHanUtc,
                CreatedAtUtc AS NgayTaoUtc,PaidAtUtc AS DaThanhToanUtc
            FROM dbo.Bookings WHERE BookingId=@maDatVe AND (@choPhepNhanVien=1 OR UserId=@maNguoiDung)
            """, new { maDatVe, maNguoiDung, choPhepNhanVien });
        if (don is null) throw new NgoaiLeNghiepVu("Không tìm thấy đơn đặt vé.", 404);
        var chiTiet = await sql.QueryAsync("""
            SELECT bp.FullName AS HoTen,bp.PhoneNumber AS SoDienThoai,
                   s.SeatCode AS MaGhe,bi.PriceVnd AS GiaVe,
                   ti.TicketId AS MaVe,ti.TicketCode AS MaVeIn,ti.Status AS TrangThaiVe
            FROM dbo.BookingItems bi
            JOIN dbo.BookingPassengers bp ON bp.PassengerId=bi.PassengerId
            JOIN dbo.Seats s ON s.SeatId=bi.SeatId
            LEFT JOIN dbo.Tickets ti ON ti.BookingItemId=bi.BookingItemId
            WHERE bi.BookingId=@maDatVe
            ORDER BY s.SeatCode
            """, new { maDatVe });
        return new { don, hanhKhach = chiTiet };
    }

    public async Task HuyDonCho(long maDatVe, long maNguoiDung)
    {
        Guid? maGiu = null;
        await using var sql = ketNoi.TaoKetNoi();
        await sql.OpenAsync();
        await using var giaoDich = (SqlTransaction)await sql.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var don = await sql.QuerySingleOrDefaultAsync<dynamic>("""
                SELECT Status,HoldId,PromotionId FROM dbo.Bookings WITH (UPDLOCK,HOLDLOCK)
                WHERE BookingId=@maDatVe AND UserId=@maNguoiDung
                """, new { maDatVe, maNguoiDung }, giaoDich);
            if (don is null) throw new NgoaiLeNghiepVu("Không tìm thấy đơn.", 404);
            if (don.Status != "PENDING") throw new NgoaiLeNghiepVu("Chỉ hủy trực tiếp được đơn đang chờ thanh toán; vé đã trả tiền phải yêu cầu hoàn.", 409);
            maGiu = (Guid?)don.HoldId;
            await sql.ExecuteAsync("UPDATE dbo.Bookings SET Status='CANCELLED',CancelledAtUtc=SYSUTCDATETIME() WHERE BookingId=@maDatVe",
                new { maDatVe }, giaoDich);
            await sql.ExecuteAsync("UPDATE dbo.Payments SET Status='FAILED',FailureReason=N'Khách hủy đơn' WHERE BookingId=@maDatVe AND Status='PENDING'",
                new { maDatVe }, giaoDich);
            if (don.PromotionId is not null)
                await sql.ExecuteAsync("UPDATE dbo.Promotions SET UsedCount=CASE WHEN UsedCount>0 THEN UsedCount-1 ELSE 0 END WHERE PromotionId=@ma",
                    new { ma = (int)don.PromotionId }, giaoDich);
            await giaoDich.CommitAsync();
        }
        catch { await giaoDich.RollbackAsync(); throw; }
        // Giao dịch SQL đã hoàn tất; không làm mất trạng thái đã hủy nếu Redis lỗi.
        try { await giuGhe.BoGiuTheoMa(maGiu); } catch (StackExchange.Redis.RedisException) { }
    }
}
