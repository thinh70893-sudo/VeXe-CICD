using BusTicket.Api.Data;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Services;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/admin"), Authorize(Roles = "ADMIN,STAFF")]
public sealed class AdminController(KetNoiSql ketNoi, DichVuDatVe datVe) : ControllerBase
{
    [HttpGet("bookings")]
    public async Task<IActionResult> DonDatVe() => Ok(await datVe.DanhSach(User.LayMaNguoiDung(), true));
    [HttpGet("revenue")]
    public async Task<IActionResult> DoanhThu([FromQuery] DateOnly? tuNgay, [FromQuery] DateOnly? denNgay)

{
    await using var sql = ketNoi.TaoKetNoi();

    return Ok(await sql.QueryAsync("""
            SELECT
                PaidDateUtc AS NgayUtc,
                PaidBookings AS SoDon,
                NetRevenueVnd AS DoanhThu
            FROM dbo.vwDailyRevenue
            WHERE
                (@tuNgay IS NULL OR PaidDateUtc >= @tuNgay)
                AND (@denNgay IS NULL OR PaidDateUtc <= @denNgay)
            ORDER BY PaidDateUtc DESC
            """, new
    {
        tuNgay = tuNgay?.ToDateTime(TimeOnly.MinValue),
        denNgay = denNgay?.ToDateTime(TimeOnly.MinValue)
    }));
}

    [HttpGet("email-logs")]
    public async Task<IActionResult> NhatKyEmail()
    {
        await using var sql = ketNoi.TaoKetNoi();
        return Ok(await sql.QueryAsync("""
            SELECT TOP (200) EmailOutboxId AS MaEmail,BookingId AS MaDatVe,
                RecipientEmail AS EmailNhan,EventType AS LoaiThongBao,
                Status AS TrangThai,AttemptCount AS SoLanGui,LastError AS LoiCuoi,
                CreatedAtUtc AS NgayTaoUtc,SentAtUtc AS NgayGuiUtc
            FROM dbo.EmailOutbox ORDER BY EmailOutboxId DESC
            """));
    }
    [HttpGet("email-attempts/{maEmail:long}")]
    public async Task<IActionResult> LanGuiEmail(long maEmail)
    {
        await using var sql = ketNoi.TaoKetNoi();
        return Ok(await sql.QueryAsync("SELECT * FROM dbo.EmailDeliveryAttempts WHERE EmailOutboxId=@maEmail ORDER BY AttemptNo DESC", new { maEmail }));
    }
    [HttpGet("users")]
    public async Task<IActionResult> NguoiDung()
    {
        await using var sql = ketNoi.TaoKetNoi();
        return Ok(await sql.QueryAsync("""
            SELECT TOP (200) u.UserId AS MaNguoiDung,u.FullName AS HoTen,u.Email,
                u.PhoneNumber AS SoDienThoai,u.IsActive AS DangHoatDong,r.RoleCode AS VaiTro
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId ORDER BY u.UserId DESC
            """));
    }
    [HttpPatch("users/{maNguoiDung:long}/active"), Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> KhoaNguoiDung(long maNguoiDung, [FromQuery] bool hoatDong)
    {
        if (maNguoiDung == User.LayMaNguoiDung()) throw new NgoaiLeNghiepVu("Không tự khóa tài khoản quản trị của mình.", 409);
        await using var sql = ketNoi.TaoKetNoi();
        var soDong = await sql.ExecuteAsync("UPDATE dbo.Users SET IsActive=@hoatDong,UpdatedAtUtc=SYSUTCDATETIME() WHERE UserId=@maNguoiDung", new { maNguoiDung, hoatDong });
        return soDong > 0 ? NoContent() : NotFound();
    }
    [HttpPatch("users/{maNguoiDung:long}/role"), Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DoiVaiTro(long maNguoiDung, [FromQuery] string vaiTro)
    {
        if (maNguoiDung == User.LayMaNguoiDung()) throw new NgoaiLeNghiepVu("Không tự đổi vai trò tài khoản của mình.", 409);
        if (vaiTro is not ("ADMIN" or "STAFF" or "CUSTOMER")) return BadRequest("Vai trò không hợp lệ.");
        await using var sql = ketNoi.TaoKetNoi();
        var soDong = await sql.ExecuteAsync("""
            UPDATE dbo.Users SET RoleId=(SELECT RoleId FROM dbo.Roles WHERE RoleCode=@vaiTro),
                UpdatedAtUtc=SYSUTCDATETIME() WHERE UserId=@maNguoiDung
            """, new { maNguoiDung, vaiTro });
        return soDong > 0 ? NoContent() : NotFound();
    }
    [HttpGet("audit-logs"), Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> NhatKyHeThong()
    {
        await using var sql = ketNoi.TaoKetNoi();
        return Ok(await sql.QueryAsync("SELECT TOP (200) * FROM dbo.AuditLogs ORDER BY AuditLogId DESC"));
    }
}
