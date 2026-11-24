using System.Net;
using System.Text;
using BusTicket.Api.Data;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Options;
using Dapper;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;

namespace BusTicket.Api.Services;

public sealed class DichVuEmail(KetNoiSql ketNoi, IOptions<CauHinhSmtp> caiDatEmail)
{
    public async Task<object> YeuCauGuiLai(long maVe, long maNguoiDung, bool laNhanVien)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var don = await sql.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT b.BookingId,b.ContactEmail FROM dbo.Tickets ti
            JOIN dbo.BookingItems bi ON bi.BookingItemId=ti.BookingItemId
            JOIN dbo.Bookings b ON b.BookingId=bi.BookingId
            WHERE ti.TicketId=@maVe AND ti.Status='ISSUED' AND b.Status='PAID'
              AND (b.UserId=@maNguoiDung OR @laNhanVien=1)
            """, new { maVe, maNguoiDung, laNhanVien });
        if (don is null) throw new NgoaiLeNghiepVu("Vé không hợp lệ hoặc không có quyền gửi lại.", 404);
        long maDatVe = don.BookingId;
        var soLan = await sql.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.EmailOutbox
            WHERE BookingId=@maDatVe AND EventType='BOOKING_CONFIRMED'
              AND CreatedAtUtc>=DATEADD(DAY,-1,SYSUTCDATETIME())
            """, new { maDatVe });
        if (soLan >= 4) throw new NgoaiLeNghiepVu("Đã vượt giới hạn gửi email trong 24 giờ.", 429);
        var vuaGui = await sql.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.EmailOutbox
            WHERE BookingId=@maDatVe AND CreatedAtUtc>=DATEADD(SECOND,-60,SYSUTCDATETIME())
            """, new { maDatVe });
        if (vuaGui > 0) throw new NgoaiLeNghiepVu("Vui lòng đợi 60 giây trước khi gửi lại.", 429);
        var khoa = "RESEND:" + maDatVe + ":" + Guid.NewGuid().ToString("N");
        await sql.ExecuteAsync("""
            INSERT dbo.EmailOutbox(BookingId,RecipientEmail,EventType,DeduplicationKey)
            VALUES(@maDatVe,@email,'BOOKING_CONFIRMED',@khoa)
            """, new { maDatVe, email = (string)don.ContactEmail, khoa });
        return new { thongBao = "Đã đưa yêu cầu gửi lại email vào hàng đợi.", maDatVe };
    }

    public async Task<string> GuiEmail(long maEmail)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var thongBao = await sql.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT EmailOutboxId,RecipientEmail,EventType,BookingId,DeduplicationKey
            FROM dbo.EmailOutbox WHERE EmailOutboxId=@maEmail
            """, new { maEmail });
        if (thongBao is null) throw new NgoaiLeNghiepVu("Không tìm thấy yêu cầu gửi email.", 404);
        var dsVe = (await sql.QueryAsync<dynamic>("""
            SELECT * FROM dbo.vwTicketEmailData WHERE EmailOutboxId=@maEmail ORDER BY TicketId
            """, new { maEmail })).ToList();
        if (dsVe.Count == 0) throw new NgoaiLeNghiepVu("Không tìm thấy dữ liệu để gửi email.");
        var don = dsVe[0];
        var laXacNhan = (string)thongBao.EventType == "BOOKING_CONFIRMED";
        if (laXacNhan && (string)don.BookingStatus != "PAID")
            throw new NgoaiLeNghiepVu("Không gửi mã QR cho booking chưa thanh toán.");
        var caiDat = caiDatEmail.Value;
        var diaChiEmail = (string)thongBao.RecipientEmail;
        var thu = new MimeMessage();
        thu.From.Add(new MailboxAddress(caiDat.TenHienThi, caiDat.EmailGui));
        thu.To.Add(MailboxAddress.Parse(diaChiEmail));
        thu.Subject = laXacNhan ? $"Vé xe điện tử - {don.BookingCode}" :
            (string)thongBao.EventType == "REFUND_CONFIRMED" ? $"Xác nhận hoàn tiền - {don.BookingCode}" : $"Thông báo hủy vé - {don.BookingCode}";
        var noiDung = new StringBuilder();
        var anToan = (string? giaTri) => WebUtility.HtmlEncode(giaTri ?? "");
        noiDung.Append("<html><body style='font-family:Arial,sans-serif;max-width:680px;margin:auto'>");
        noiDung.Append($"<h2>BusTicket - {anToan(thu.Subject)}</h2><p>Mã đặt vé: <b>{anToan((string)don.BookingCode)}</b></p>");
        noiDung.Append($"<p>Tuyến: <b>{anToan((string)don.OriginStation)} → {anToan((string)don.DestinationStation)}</b></p>");
        noiDung.Append($"<p>Chuyến: {anToan((string)don.TripCode)} | Giờ đi (Việt Nam): {((DateTime)don.DepartureAtUtc).AddHours(7):dd/MM/yyyy HH:mm}</p>");
        noiDung.Append($"<p>Tổng tiền: {((decimal)don.BookingTotalVnd):N0} VNĐ</p>");
        var noiDungEmail = new BodyBuilder();
        if (laXacNhan)
        {
            noiDung.Append("<p>Vui lòng xuất trình mã QR tương ứng với từng hành khách khi lên xe.</p>");
            foreach (var ve in dsVe)
            {
                if (ve.QrPublicId is null || (string)ve.TicketStatus != "ISSUED") continue;
                var anhQr = DichVuVe.TaoAnhQr((Guid)ve.QrPublicId);
                var tepAnh = noiDungEmail.LinkedResources.Add($"Ve-{ve.TicketCode}.png", anhQr);
                tepAnh.ContentId = MimeUtils.GenerateMessageId();
                noiDung.Append($"<div style='padding:12px;border:1px solid #ddd;margin:12px 0'>");
                noiDung.Append($"<p>Hành khách: {anToan((string)ve.PassengerName)} | Ghế: <b>{anToan((string)ve.SeatCode)}</b> | Vé: {anToan((string)ve.TicketCode)}</p>");
                noiDung.Append($"<img alt='Ma QR ve' width='180' height='180' src='cid:{tepAnh.ContentId}' />");
                noiDung.Append("</div>");
            }
        }
        else if ((string)thongBao.EventType == "REFUND_CONFIRMED" &&
         ((string)thongBao.DeduplicationKey).StartsWith("HOANVE:"))
{
    var chuoiMaDot = ((string)thongBao.DeduplicationKey)[7..];
    if (!long.TryParse(chuoiMaDot, out long maDotHoan))
        throw new NgoaiLeNghiepVu("Mã đợt hoàn vé không hợp lệ.");

    var cacVe = (await sql.QueryAsync<dynamic>("""
        SELECT ve.TicketCode, kh.FullName, ghe.SeatCode, ct.SoTienHoan
        FROM dbo.ChiTietHoanVe ct
        JOIN dbo.Tickets ve ON ve.TicketId=ct.MaVe
        JOIN dbo.BookingItems item ON item.BookingItemId=ve.BookingItemId
        JOIN dbo.BookingPassengers kh ON kh.PassengerId=item.PassengerId
        JOIN dbo.Seats ghe ON ghe.SeatId=item.SeatId
        WHERE ct.MaDotHoan=@maDotHoan
        ORDER BY ghe.SeatCode
        """, new { maDotHoan })).ToList();

    if (cacVe.Count == 0) throw new NgoaiLeNghiepVu("Không tìm thấy chi tiết vé đã hoàn.");
    noiDung.Append("<h3>Hoàn vé vào ví BusGo thành công</h3>");
    noiDung.Append("<p>Chỉ những vé dưới đây đã bị hủy. Các vé khác trong đơn vẫn còn hiệu lực.</p>");
    foreach (var veDaHoan in cacVe)
    {
        noiDung.Append($"<p>Vé {anToan((string)veDaHoan.TicketCode)} - " +
            $"Khách {anToan((string)veDaHoan.FullName)} - " +
            $"Ghế {anToan((string)veDaHoan.SeatCode)}: " +
            $"<b>{((decimal)veDaHoan.SoTienHoan):N0} VNĐ</b></p>");
    }
    noiDung.Append("<p>Số tiền hoàn đã được cộng vào ví BusGo. " +
                   "Mã QR của các vé vừa hoàn không còn sử dụng được.</p>");
}
else noiDung.Append("<p>Đây là thông báo trạng thái đơn hàng. Vé QR cũ không còn sử dụng được.</p>");
        noiDung.Append("<p>Đây là email tự động, vui lòng không trả lời.</p></body></html>");
        noiDungEmail.HtmlBody = noiDung.ToString();
        noiDungEmail.TextBody = $"BusTicket | Mã đơn {don.BookingCode} | {don.OriginStation} - {don.DestinationStation}";
        thu.Body = noiDungEmail.ToMessageBody();
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(caiDat.Host, caiDat.Port,
            caiDat.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
        if (!string.IsNullOrWhiteSpace(caiDat.Username))
            await smtp.AuthenticateAsync(caiDat.Username, caiDat.Password);
        var maThu = await smtp.SendAsync(thu);
        await smtp.DisconnectAsync(true);
        return maThu;
    }
}

