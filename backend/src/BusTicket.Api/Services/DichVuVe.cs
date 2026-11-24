using System.Data;
using BusTicket.Api.Data;
using BusTicket.Api.Infrastructure;
using Dapper;
using QRCoder;

namespace BusTicket.Api.Services;

public sealed class DichVuVe(KetNoiSql ketNoi)
{
    public static byte[] TaoAnhQr(Guid maQr)
    {
        using var taoMa = new QRCodeGenerator();
        using var duLieuQr = taoMa.CreateQrCode($"BTICKET:{maQr:D}", QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(duLieuQr).GetGraphic(12);
    }

    public async Task<object> ChiTiet(long maVe, long maNguoiDung, bool laNhanVien)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var ve = await sql.QuerySingleOrDefaultAsync("""
            SELECT ti.TicketId AS MaVe,ti.TicketCode AS MaVeIn,ti.Status AS TrangThai,
                ti.IssuedAtUtc AS PhatHanhUtc,ti.CheckedInAtUtc AS SoatVeUtc,
                b.BookingCode AS MaDon,b.ContactEmail AS EmailLienHe,
                bp.FullName AS HoTen,s.SeatCode AS MaGhe,tr.TripCode AS MaChuyen,
                tr.DepartureAtUtc AS GioKhoiHanhUtc,tr.ArrivalAtUtc AS GioDenUtc,
                st1.StationName AS DiemDi,st2.StationName AS DiemDen
            FROM dbo.Tickets ti
            JOIN dbo.BookingItems bi ON bi.BookingItemId=ti.BookingItemId
            JOIN dbo.Bookings b ON b.BookingId=bi.BookingId
            JOIN dbo.BookingPassengers bp ON bp.PassengerId=bi.PassengerId
            JOIN dbo.Seats s ON s.SeatId=bi.SeatId
            JOIN dbo.Trips tr ON tr.TripId=b.TripId
            JOIN dbo.Routes r ON r.RouteId=tr.RouteId
            JOIN dbo.Stations st1 ON st1.StationId=r.OriginStationId
            JOIN dbo.Stations st2 ON st2.StationId=r.DestinationStationId
            WHERE ti.TicketId=@maVe AND (b.UserId=@maNguoiDung OR @laNhanVien=1)
            """, new { maVe, maNguoiDung, laNhanVien });
        return ve ?? throw new NgoaiLeNghiepVu("Không tìm thấy vé.", 404);
    }

    public async Task<byte[]> AnhQr(long maVe, long maNguoiDung, bool laNhanVien)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var maQr = await sql.ExecuteScalarAsync<Guid?>("""
            SELECT ti.QrPublicId FROM dbo.Tickets ti
            JOIN dbo.BookingItems bi ON bi.BookingItemId=ti.BookingItemId
            JOIN dbo.Bookings b ON b.BookingId=bi.BookingId
            WHERE ti.TicketId=@maVe AND b.Status='PAID' AND ti.Status IN ('ISSUED','CHECKED_IN')
              AND (b.UserId=@maNguoiDung OR @laNhanVien=1)
            """, new { maVe, maNguoiDung, laNhanVien });
        if (!maQr.HasValue) throw new NgoaiLeNghiepVu("Vé không khả dụng hoặc bạn không được phép xem.", 404);
        return TaoAnhQr(maQr.Value);
    }

    public async Task<object> KiemTra(Guid maQr)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var trangThai = await sql.QuerySingleOrDefaultAsync("""
            SELECT ti.TicketCode AS MaVe,ti.Status AS TrangThaiVe,b.Status AS TrangThaiDon,
                bp.FullName AS HoTen,s.SeatCode AS MaGhe,tr.TripCode AS MaChuyen,
                tr.DepartureAtUtc AS GioKhoiHanhUtc
            FROM dbo.Tickets ti JOIN dbo.BookingItems bi ON bi.BookingItemId=ti.BookingItemId
            JOIN dbo.BookingPassengers bp ON bp.PassengerId=bi.PassengerId
            JOIN dbo.Seats s ON s.SeatId=bi.SeatId
            JOIN dbo.Bookings b ON b.BookingId=bi.BookingId
            JOIN dbo.Trips tr ON tr.TripId=b.TripId
            WHERE ti.QrPublicId=@maQr
            """, new { maQr });
        return trangThai ?? throw new NgoaiLeNghiepVu("Mã QR không có trong hệ thống.", 404);
    }

    public async Task<object> SoatVe(Guid maQr, long maNhanVien)
    {
        await using var sql = ketNoi.TaoKetNoi();
        return await sql.QuerySingleAsync("dbo.usp_CheckInTicket",
            new { QrPublicId = maQr, StaffUserId = maNhanVien }, commandType: CommandType.StoredProcedure);
    }
}
