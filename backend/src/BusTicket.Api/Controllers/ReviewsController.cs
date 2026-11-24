using BusTicket.Api.Data;
using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/reviews")]
public sealed class ReviewsController(KetNoiSql ketNoi) : ControllerBase
{
    [HttpGet("by-trip/{maChuyen:long}")]
    public async Task<IActionResult> TheoChuyen(long maChuyen)
    {
        await using var sql = ketNoi.TaoKetNoi();
        return Ok(await sql.QueryAsync("""
            SELECT r.ReviewId AS MaDanhGia,u.FullName AS HoTen,r.Rating AS SoSao,
                r.CommentText AS BinhLuan,r.CreatedAtUtc AS NgayTaoUtc
            FROM dbo.Reviews r JOIN dbo.Users u ON u.UserId=r.UserId
            WHERE r.TripId=@maChuyen ORDER BY r.CreatedAtUtc DESC
            """, new { maChuyen }));
    }
    [HttpPost, Authorize]
    public async Task<IActionResult> Tao(TaoDanhGiaDto yeuCau)
    {
        if (yeuCau.SoSao is < 1 or > 5) return BadRequest("Số sao phải từ 1 đến 5.");
        var maNguoiDung = User.LayMaNguoiDung();
        await using var sql = ketNoi.TaoKetNoi();
        var coVe = await sql.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.Bookings b JOIN dbo.Trips t ON t.TripId=b.TripId
            WHERE b.UserId=@maNguoiDung AND b.TripId=@maChuyen AND b.Status='PAID'
              AND t.ArrivalAtUtc<=SYSUTCDATETIME()
            """, new { maNguoiDung, maChuyen = yeuCau.MaChuyen });
        if (coVe == 0) throw new NgoaiLeNghiepVu("Chỉ đánh giá sau khi đã đi chuyến xe.", 403);
        var maDanhGia = await sql.ExecuteScalarAsync<long>("""
            INSERT dbo.Reviews(UserId,TripId,Rating,CommentText)
            OUTPUT INSERTED.ReviewId VALUES(@maNguoiDung,@maChuyen,@soSao,@binhLuan)
            """, new { maNguoiDung, maChuyen = yeuCau.MaChuyen,
                soSao = yeuCau.SoSao, binhLuan = yeuCau.BinhLuan });
        return StatusCode(201, new { maDanhGia });
    }
}
