using System.Data;
using BusTicket.Api.Data;
using BusTicket.Api.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BusTicket.Api.Services;

public sealed class DichVuThanhToan(KetNoiSql ketNoi, DichVuGiuGhe giuGhe,
    IWebHostEnvironment moiTruong, ILogger<DichVuThanhToan> nhatKy)
{
    public async Task<object> KhoiTao(long maDatVe, long maNguoiDung)
    {
        await using var sql = ketNoi.TaoKetNoi();
        await sql.OpenAsync();
        await using var giaoDich = (SqlTransaction)await sql.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var don = await sql.QuerySingleOrDefaultAsync<dynamic>("""
                SELECT BookingId,Status,ExpiresAtUtc,TotalVnd,HoldId FROM dbo.Bookings WITH (UPDLOCK,HOLDLOCK)
                WHERE BookingId=@maDatVe AND UserId=@maNguoiDung
                """, new { maDatVe, maNguoiDung }, giaoDich);
            if (don is null) throw new NgoaiLeNghiepVu("Không tìm thấy đơn đặt vé.", 404);
            if (don.Status != "PENDING" || (DateTime)don.ExpiresAtUtc <= DateTime.UtcNow)
                throw new NgoaiLeNghiepVu("Đơn không còn cho phép thanh toán.", 409);
            // Không tạo giao dịch mới cho đơn đã hết quyền giữ ghế.
            await giuGhe.KiemTra((Guid)don.HoldId, maNguoiDung);
            var daCo = await sql.QuerySingleOrDefaultAsync("""
                SELECT TOP (1) PaymentId AS MaThanhToan,MerchantRequestId AS MaYeuCau,
                    AmountVnd AS SoTien,Status AS TrangThai,Provider AS CongThanhToan
                FROM dbo.Payments WHERE BookingId=@maDatVe AND Status='PENDING' ORDER BY PaymentId DESC
                """, new { maDatVe }, giaoDich);
            if (daCo is not null)
            {
                await giaoDich.CommitAsync();
                return daCo;
            }
            var maYeuCau = "REQ-" + Guid.NewGuid().ToString("N");
            var soTien = (decimal)don.TotalVnd;
            var maThanhToan = await sql.ExecuteScalarAsync<long>("""
                INSERT dbo.Payments(BookingId,Provider,MerchantRequestId,AmountVnd,Status)
                OUTPUT INSERTED.PaymentId VALUES(@maDatVe,'SANDBOX',@maYeuCau,@soTien,'PENDING')
                """, new { maDatVe, maYeuCau, soTien }, giaoDich);
            await giaoDich.CommitAsync();
            return new { maThanhToan, maYeuCau, soTien, trangThai = "PENDING", congThanhToan = "SANDBOX" };
        }
        catch { await giaoDich.RollbackAsync(); throw; }
    }

    // Chỉ phục vụ đồ án tại localhost. TUYỆT ĐỐI không cung cấp xác nhận từ frontend trong production.
    public async Task<object> XacNhanSandbox(long maThanhToan, long maNguoiDung, bool laNhanVien)
    {
        if (!moiTruong.IsDevelopment())
            throw new NgoaiLeNghiepVu("Tính năng thanh toán giả lập chỉ cho phép ở môi trường Development.", 403);
        await using var sql = ketNoi.TaoKetNoi();
        var thanhToan = await sql.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT p.PaymentId,p.BookingId,p.Status AS PaymentStatus,p.ProviderTransactionId,
                   b.HoldId,b.UserId,b.Status AS BookingStatus,b.ExpiresAtUtc
            FROM dbo.Payments p JOIN dbo.Bookings b ON b.BookingId=p.BookingId
            WHERE p.PaymentId=@maThanhToan AND p.Provider='SANDBOX'
              AND (b.UserId=@maNguoiDung OR @laNhanVien=1)
            """, new { maThanhToan, maNguoiDung, laNhanVien });
        if (thanhToan is null) throw new NgoaiLeNghiepVu("Không tìm thấy giao dịch giả lập.", 404);
        if (thanhToan.BookingStatus == "PAID" && thanhToan.PaymentStatus == "SUCCEEDED")
            return new { maDatVe = (long)thanhToan.BookingId, trangThai = "PAID", ketQua = "ALREADY_CONFIRMED" };
        if (thanhToan.BookingStatus != "PENDING" || (DateTime)thanhToan.ExpiresAtUtc <= DateTime.UtcNow)
            throw new NgoaiLeNghiepVu("Đơn đã quá hạn hoặc đã được xử lý.", 409);
        var maDatVe = (long)thanhToan.BookingId;
        var maChuDon = (long)thanhToan.UserId;
        await giuGhe.KiemTra((Guid)thanhToan.HoldId, maChuDon);
        var maGiaoDich = "SANDBOX-" + maThanhToan.ToString();
        var ketQua = await sql.QuerySingleAsync("dbo.usp_ConfirmVerifiedPayment",
            new { BookingId = maDatVe, PaymentId = maThanhToan, ProviderTransactionId = maGiaoDich },
            commandType: CommandType.StoredProcedure);
        try { await giuGhe.BoGiuTheoMa((Guid)thanhToan.HoldId); }
        catch (StackExchange.Redis.RedisException loi) { nhatKy.LogWarning(loi, "Đã bán vé; Redis sẽ tự hết hạn key giữ ghế."); }
        return new { maDatVe, trangThai = "PAID", ketQua = (string)ketQua.Result,
            thongBao = "Đã phát hành vé, yêu cầu gửi email đã được ghi vào EmailOutbox." };
    }

    public async Task<IEnumerable<dynamic>> DanhSachGiaoDich(long maDatVe, long maNguoiDung, bool laNhanVien)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var tonTai = await sql.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.Bookings WHERE BookingId=@maDatVe AND (UserId=@maNguoiDung OR @laNhanVien=1)
            """, new { maDatVe, maNguoiDung, laNhanVien });
        if (tonTai == 0) throw new NgoaiLeNghiepVu("Không tìm thấy đơn.", 404);
        return await sql.QueryAsync("""
            SELECT PaymentId AS MaThanhToan,Provider AS CongThanhToan,
                MerchantRequestId AS MaYeuCau,AmountVnd AS SoTien,Status AS TrangThai,
                CreatedAtUtc AS NgayTaoUtc,PaidAtUtc AS DaThanhToanUtc
            FROM dbo.Payments WHERE BookingId=@maDatVe ORDER BY PaymentId DESC
            """, new { maDatVe });
    }
}
