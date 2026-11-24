using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Data;
using BusTicket.Api.Data;
using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/webhooks")]
public sealed class WebhooksController(KetNoiSql ketNoi, DichVuGiuGhe giuGhe,
    IConfiguration cauHinh, IWebHostEnvironment moiTruong, ILogger<WebhooksController> nhatKy) : ControllerBase
{
    [HttpPost("sandbox")]
    public async Task<IActionResult> NhanWebhookSandbox()
    {
        if (!moiTruong.IsDevelopment()) return NotFound();
        var khoa = cauHinh["SandboxWebhook:Secret"] ?? "";
        if (Encoding.UTF8.GetByteCount(khoa) < 32)
            throw new NgoaiLeNghiepVu("Chưa có khóa ký webhook sandbox 32 byte.", 503);
        using var boDoc = new StreamReader(Request.Body, Encoding.UTF8);
        var noiDungGoc = await boDoc.ReadToEndAsync();
        var chuKy = Request.Headers["X-BusTicket-Signature"].ToString().Trim();
        var chuKyDung = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(khoa), Encoding.UTF8.GetBytes(noiDungGoc)));
        byte[] chuKyNhan;
        try { chuKyNhan = Convert.FromHexString(chuKy); }
        catch (FormatException) { return Unauthorized(new { loi = "Chữ ký không hợp lệ." }); }
        if (!CryptographicOperations.FixedTimeEquals(chuKyNhan, Convert.FromHexString(chuKyDung)))
            return Unauthorized(new { loi = "Chữ ký không chính xác." });
        WebhookSandboxDto suKien;
        try { suKien = JsonSerializer.Deserialize<WebhookSandboxDto>(noiDungGoc,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!; }
        catch (JsonException) { return BadRequest(new { loi = "Nội dung JSON không hợp lệ." }); }
        if (suKien is null || string.IsNullOrWhiteSpace(suKien.MaSuKien) ||
            string.IsNullOrWhiteSpace(suKien.MaGiaoDich) || string.IsNullOrWhiteSpace(suKien.MaYeuCau) ||
            suKien.TrangThai != "SUCCEEDED") return BadRequest(new { loi = "Webhook sandbox không đúng định dạng." });
        await using var sql = ketNoi.TaoKetNoi();
        var thanhToan = await sql.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT p.PaymentId,p.BookingId,p.Status AS TrangThaiThanhToan,p.AmountVnd,
                b.UserId,b.HoldId,b.Status AS TrangThaiDon
            FROM dbo.Payments p JOIN dbo.Bookings b ON b.BookingId=p.BookingId
            WHERE p.MerchantRequestId=@maYeuCau AND p.Provider='SANDBOX'
            """, new { maYeuCau = suKien.MaYeuCau });
        if (thanhToan is null || (decimal)thanhToan.AmountVnd != suKien.SoTien)
            return BadRequest(new { loi = "Không tìm thấy giao dịch hoặc số tiền không khớp." });
        var maThanhToan = (long)thanhToan.PaymentId;
        var maDatVe = (long)thanhToan.BookingId;
        var soSuKien = await sql.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.PaymentWebhooks WHERE Provider='SANDBOX' AND ProviderEventId=@maSuKien
            """, new { maSuKien = suKien.MaSuKien });
        if (soSuKien > 0 && thanhToan.TrangThaiDon == "PAID" && thanhToan.TrangThaiThanhToan == "SUCCEEDED")
            return Ok(new { ketQua = "DA_XU_LY" });
        if (thanhToan.TrangThaiDon != "PAID")
            await giuGhe.KiemTra((Guid)thanhToan.HoldId, (long)thanhToan.UserId);
        // Stored procedure bảo vệ idempotency và tính nguyên tử của vé đã bán.
        var ketQua = await sql.QuerySingleAsync("dbo.usp_ConfirmVerifiedPayment",
            new { BookingId = maDatVe, PaymentId = maThanhToan, ProviderTransactionId = suKien.MaGiaoDich },
            commandType: CommandType.StoredProcedure);
        try
        {
            await sql.ExecuteAsync("""
                IF NOT EXISTS (SELECT 1 FROM dbo.PaymentWebhooks WHERE Provider='SANDBOX' AND ProviderEventId=@maSuKien)
                INSERT dbo.PaymentWebhooks(Provider,ProviderEventId,PaymentId,PayloadSha256,ProcessingStatus,ProcessedAtUtc)
                VALUES('SANDBOX',@maSuKien,@maThanhToan,@hash,'PROCESSED',SYSUTCDATETIME())
                """, new { maSuKien = suKien.MaSuKien, maThanhToan,
                    hash = SHA256.HashData(Encoding.UTF8.GetBytes(noiDungGoc)) });
        }
        catch (Exception loi) { nhatKy.LogWarning(loi, "Vé đã xác nhận nhưng ghi log webhook thất bại; có thể gửi lại."); }
        try { await giuGhe.BoGiuTheoMa((Guid)thanhToan.HoldId); }
        catch (StackExchange.Redis.RedisException loi) { nhatKy.LogWarning(loi, "Redis sẽ tự hết hạn sau khi đã bán vé."); }
        return Ok(new { maDatVe, ketQua = (string)ketQua.Result });
    }
}
