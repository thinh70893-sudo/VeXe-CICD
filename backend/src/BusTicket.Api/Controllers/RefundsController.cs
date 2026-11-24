using System.Data;
using BusTicket.Api.Data;
using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/refunds"), Authorize]
public sealed class RefundsController(KetNoiSql ketNoi) : ControllerBase
{
    [NonAction]
    public async Task<IActionResult> Tao(long maDatVe, YeuCauHoanTienDto yeuCau)
    {
        if (string.IsNullOrWhiteSpace(yeuCau.LyDo)) return BadRequest("Hãy nhập lý do yêu cầu hoàn vé.");
        var maNguoiDung = User.LayMaNguoiDung();
        await using var sql = ketNoi.TaoKetNoi();
        await sql.OpenAsync();
        await using var gd = (SqlTransaction)await sql.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var don = await sql.QuerySingleOrDefaultAsync<dynamic>("""
                SELECT b.Status,b.TotalVnd,b.TripId,tr.DepartureAtUtc FROM dbo.Bookings b WITH (UPDLOCK,HOLDLOCK)
                JOIN dbo.Trips tr ON tr.TripId=b.TripId
                WHERE b.BookingId=@maDatVe AND b.UserId=@maNguoiDung
                """, new { maDatVe, maNguoiDung }, gd);
            if (don is null || don.Status != "PAID" || (DateTime)don.DepartureAtUtc <= DateTime.UtcNow)
                throw new NgoaiLeNghiepVu("Chỉ yêu cầu hoàn tiền cho vé đã thanh toán và chưa khởi hành.", 409);
            var daSoatVe = await sql.ExecuteScalarAsync<int>("""
                SELECT COUNT(*) FROM dbo.Tickets ti JOIN dbo.BookingItems bi ON bi.BookingItemId=ti.BookingItemId
                WHERE bi.BookingId=@maDatVe AND ti.Status='CHECKED_IN'
                """, new { maDatVe }, gd);
            if (daSoatVe > 0) throw new NgoaiLeNghiepVu("Vé đã soát không được hoàn.", 409);
            var daYeuCau = await sql.ExecuteScalarAsync<int>("""
                SELECT COUNT(*) FROM dbo.Refunds WHERE BookingId=@maDatVe AND Status IN ('REQUESTED','APPROVED','COMPLETED')
                """, new { maDatVe }, gd);
            if (daYeuCau > 0) throw new NgoaiLeNghiepVu("Đơn đã có yêu cầu hoàn tiền.", 409);
            var maThanhToan = await sql.ExecuteScalarAsync<long?>("""
                SELECT TOP (1) PaymentId FROM dbo.Payments WHERE BookingId=@maDatVe AND Status='SUCCEEDED'
                ORDER BY PaymentId DESC
                """, new { maDatVe }, gd);
            if (!maThanhToan.HasValue) throw new NgoaiLeNghiepVu("Chưa tìm thấy giao dịch thành công.", 409);
            var maHoan = "RF-" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
            var maHoanTien = await sql.ExecuteScalarAsync<long>("""
                INSERT dbo.Refunds(BookingId,PaymentId,RefundCode,AmountVnd,Reason)
                OUTPUT INSERTED.RefundId VALUES(@maDatVe,@maThanhToan,@maHoan,@soTien,@lyDo)
                """, new { maDatVe, maThanhToan, maHoan, soTien = (decimal)don.TotalVnd, lyDo = yeuCau.LyDo.Trim() }, gd);
            await gd.CommitAsync();
            return StatusCode(201, new { maHoanTien, maHoan, trangThai = "REQUESTED" });
        }
        catch { await gd.RollbackAsync(); throw; }
    }

    [HttpGet("me")]
    public async Task<IActionResult> CuaToi()
    {
        await using var sql = ketNoi.TaoKetNoi();
        return Ok(await sql.QueryAsync("""
            SELECT rf.RefundId AS MaHoanTien,rf.RefundCode AS MaHoan,rf.AmountVnd AS SoTien,
                rf.Reason AS LyDo,rf.Status AS TrangThai,rf.RequestedAtUtc AS NgayYeuCauUtc
            FROM dbo.Refunds rf JOIN dbo.Bookings b ON b.BookingId=rf.BookingId
            WHERE b.UserId=@maNguoiDung ORDER BY rf.RefundId DESC
            """, new { maNguoiDung = User.LayMaNguoiDung() }));
    }

    [HttpGet, Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> DanhSach()
    {
        await using var sql = ketNoi.TaoKetNoi();
        return Ok(await sql.QueryAsync("SELECT TOP (200) * FROM dbo.Refunds ORDER BY RefundId DESC"));
    }

    [NonAction]
    public async Task<IActionResult> Duyet(long maHoanTien, TrangThaiHoanTienDto yeuCau)
    {
        if (yeuCau.TrangThai is not ("APPROVED" or "REJECTED" or "COMPLETED"))
            return BadRequest("Chỉ APPROVED, REJECTED hoặc COMPLETED.");
        await using var sql = ketNoi.TaoKetNoi();
        await sql.OpenAsync();
        await using var gd = (SqlTransaction)await sql.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var hoan = await sql.QuerySingleOrDefaultAsync<dynamic>("""
                SELECT rf.BookingId,rf.PaymentId,rf.Status,b.TripId,b.ContactEmail,
                    b.Status AS BookingStatus,tr.DepartureAtUtc
                FROM dbo.Refunds rf WITH (UPDLOCK,HOLDLOCK)
                JOIN dbo.Bookings b ON b.BookingId=rf.BookingId
                JOIN dbo.Trips tr ON tr.TripId=b.TripId
                WHERE rf.RefundId=@maHoanTien
                """, new { maHoanTien }, gd);
            if (hoan is null) throw new NgoaiLeNghiepVu("Không tìm thấy yêu cầu hoàn.", 404);
            string trangThaiCu = hoan.Status;
            string trangThaiMoi = yeuCau.TrangThai;
            if ((trangThaiMoi == "APPROVED" || trangThaiMoi == "REJECTED") && trangThaiCu != "REQUESTED" ||
                trangThaiMoi == "COMPLETED" && trangThaiCu != "APPROVED")
                throw new NgoaiLeNghiepVu("Chuyển trạng thái hoàn tiền không hợp lệ.", 409);
            if (trangThaiMoi == "COMPLETED")
            {
                if (hoan.BookingStatus != "PAID" || (DateTime)hoan.DepartureAtUtc <= DateTime.UtcNow)
                    throw new NgoaiLeNghiepVu("Không thể trả ghế nếu chuyến đã khởi hành hoặc đơn không còn PAID.", 409);
                // Chỉ gọi COMPLETED SAU KHI nhân viên đã xử lý hoàn tiền ngoài hệ thống (sandbox/manual).
                await sql.ExecuteAsync("""
                    UPDATE dbo.Bookings SET Status='REFUNDED',CancelledAtUtc=SYSUTCDATETIME() WHERE BookingId=@maDatVe;
                    UPDATE dbo.Payments SET Status='REFUNDED' WHERE PaymentId=@maThanhToan;
                    UPDATE ti SET Status='CANCELLED' FROM dbo.Tickets ti
                       JOIN dbo.BookingItems bi ON bi.BookingItemId=ti.BookingItemId WHERE bi.BookingId=@maDatVe;
                    UPDATE ts SET Status='AVAILABLE',SoldAtUtc=NULL
                    FROM dbo.TripSeats ts JOIN dbo.BookingItems bi ON bi.TripId=ts.TripId AND bi.SeatId=ts.SeatId
                    WHERE bi.BookingId=@maDatVe AND ts.Status='SOLD';
                    INSERT dbo.EmailOutbox(BookingId,RecipientEmail,EventType,DeduplicationKey)
                    VALUES(@maDatVe,@email,'REFUND_CONFIRMED',CONCAT('REFUND:',@maDatVe));
                    """, new { maDatVe = (long)hoan.BookingId, maThanhToan = (long)hoan.PaymentId,
                        email = (string)hoan.ContactEmail }, gd);
            }
            await sql.ExecuteAsync("""
                UPDATE dbo.Refunds SET Status=@trangThaiMoi,
                    CompletedAtUtc=CASE WHEN @trangThaiMoi='COMPLETED' THEN SYSUTCDATETIME() ELSE NULL END
                WHERE RefundId=@maHoanTien
                """, new { maHoanTien, trangThaiMoi }, gd);
            await gd.CommitAsync();
            return Ok(new { maHoanTien, trangThai = trangThaiMoi,
                ghiChu = trangThaiMoi == "COMPLETED" ? "Đánh dấu hoàn tiền thủ công. Đây không phải lệnh hoàn tiền qua cổng thật." : "Đã cập nhật." });
        }
        catch { await gd.RollbackAsync(); throw; }
    }
}

