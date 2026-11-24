using System.Data;
using BusTicket.Api.Data;
using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Services;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/trips")]
public sealed class TripsController(KetNoiSql ketNoi, DichVuGiuGhe giuGhe) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> TimKiem([FromQuery] int? diemDiId, [FromQuery] int? diemDenId,
        [FromQuery] DateOnly? ngay, [FromQuery] int trang = 1, [FromQuery] int soDong = 30)
    {
        if (trang < 1 || soDong is < 1 or > 100) return BadRequest("Tham số phân trang không hợp lệ.");
        // Ngày tìm kiếm tính theo giờ Việt Nam (UTC+7).
        var tuNgayUtc = ngay?.ToDateTime(TimeOnly.MinValue).AddHours(-7);
        var denNgayUtc = tuNgayUtc?.AddDays(1);
        await using var sql = ketNoi.TaoKetNoi();
        var chuyenXe = await sql.QueryAsync("""
            SELECT TripId AS MaChuyen,TripCode AS MaChuyenIn,RouteCode AS MaTuyenIn,
                OriginCity AS ThanhPhoDi,DestinationCity AS ThanhPhoDen,
                OriginStation AS BenDi,DestinationStation AS BenDen,
                DepartureAtUtc AS KhoiHanhUtc,ArrivalAtUtc AS DenNoiUtc,
                PriceVnd AS GiaVe,BusType AS LoaiXe,TotalSeats AS TongGhe,
                DbAvailableSeats AS SoGheDatabaseTrong,SoldSeats AS SoGheDaBan,
                Status AS TrangThai
            FROM dbo.vwTripSearch
            WHERE (@diemDiId IS NULL OR OriginCityId=@diemDiId)
              AND (@diemDenId IS NULL OR DestinationCityId=@diemDenId)
              AND (@tuNgayUtc IS NULL OR DepartureAtUtc>=@tuNgayUtc)
              AND (@denNgayUtc IS NULL OR DepartureAtUtc<@denNgayUtc)
              AND Status='SCHEDULED' AND DepartureAtUtc>SYSUTCDATETIME()
            ORDER BY DepartureAtUtc
            OFFSET @boQua ROWS FETCH NEXT @soDong ROWS ONLY
            """, new { diemDiId, diemDenId, tuNgayUtc, denNgayUtc,
                boQua = (trang - 1) * soDong, soDong });
        return Ok(chuyenXe);
    }

    [HttpGet("{maChuyen:long}")]
    public async Task<IActionResult> ChiTiet(long maChuyen)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var chuyenXe = await sql.QuerySingleOrDefaultAsync("SELECT * FROM dbo.vwTripSearch WHERE TripId=@maChuyen", new { maChuyen });
        return chuyenXe is null ? NotFound() : Ok(chuyenXe);
    }

    [HttpGet("{maChuyen:long}/seats")]
    public async Task<IActionResult> DanhSachGhe(long maChuyen)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var dsGhe = await sql.QueryAsync<dynamic>("""
            SELECT ts.SeatId AS MaGhe,s.SeatCode AS KyHieuGhe,s.FloorNumber AS Tang,
                ts.FareVnd AS GiaVe,ts.Status AS TrangThaiSql
            FROM dbo.TripSeats ts JOIN dbo.Seats s ON s.SeatId=ts.SeatId
            WHERE ts.TripId=@maChuyen ORDER BY s.FloorNumber,s.SeatCode
            """, new { maChuyen });
        var ketQua = new List<object>();
        foreach (var ghe in dsGhe)
        {
            bool dangGiu = (string)ghe.TrangThaiSql == "AVAILABLE" &&
                await giuGhe.DangDuocGiu(maChuyen, (int)ghe.MaGhe);
            ketQua.Add(new { maGhe = (int)ghe.MaGhe, kyHieuGhe = (string)ghe.KyHieuGhe,
                tang = (byte)ghe.Tang, giaVe = (decimal)ghe.GiaVe,
                trangThai = dangGiu ? "HELD" : (string)ghe.TrangThaiSql });
        }
        return Ok(ketQua);
    }

    [HttpGet("{maChuyen:long}/stops")]
    public async Task<IActionResult> DiemDung(long maChuyen)
    {
        await using var sql = ketNoi.TaoKetNoi();
        return Ok(await sql.QueryAsync("""
            SELECT ts.TripStopId AS MaDiemDung,ts.StopOrder AS ThuTu,
                s.StationName AS BenXe,ts.PlannedArrivalAtUtc AS DuKienDenUtc,
                ts.PlannedDepartureAtUtc AS DuKienDiUtc,
                ts.IsPickup AS DuocDon,ts.IsDropoff AS DuocTra
            FROM dbo.TripStops ts JOIN dbo.Stations s ON s.StationId=ts.StationId
            WHERE ts.TripId=@maChuyen ORDER BY ts.StopOrder
            """, new { maChuyen }));
    }

    [HttpPost, Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Them(TaoChuyenDto yeuCau)
    {
        KiemTraThoiGian(yeuCau.GioKhoiHanhUtc, yeuCau.GioDenUtc, yeuCau.GiaVe);
        await using var sql = ketNoi.TaoKetNoi();
        await sql.OpenAsync();
        await using var gd = (SqlTransaction)await sql.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await KiemTraXeTaiXe(sql, gd, yeuCau.MaXe, yeuCau.MaTaiXe,
                yeuCau.GioKhoiHanhUtc, yeuCau.GioDenUtc);
            var soGhe = await sql.ExecuteScalarAsync<int>("""
                SELECT COUNT(*) FROM dbo.Seats WHERE BusId=@maXe AND IsActive=1
                """, new { maXe = yeuCau.MaXe }, gd);
            var sucChua = await sql.ExecuteScalarAsync<int?>("""
                SELECT Capacity FROM dbo.Buses WHERE BusId=@maXe AND Status='ACTIVE'
                """, new { maXe = yeuCau.MaXe }, gd);
            if (!sucChua.HasValue || soGhe != sucChua.Value)
                throw new NgoaiLeNghiepVu("Xe phải ACTIVE và có số ghế đúng sức chứa trước khi lập chuyến.", 409);
            var maChuyen = await sql.ExecuteScalarAsync<long>("""
                INSERT dbo.Trips(TripCode,RouteId,BusId,DriverId,DepartureAtUtc,ArrivalAtUtc,PriceVnd)
                OUTPUT INSERTED.TripId
                VALUES(@ma,@maTuyen,@maXe,@maTaiXe,@gioDi,@gioDen,@giaVe)
                """, new { ma = yeuCau.MaChuyen, maTuyen = yeuCau.MaTuyen,
                    maXe = yeuCau.MaXe, maTaiXe = yeuCau.MaTaiXe,
                    gioDi = yeuCau.GioKhoiHanhUtc, gioDen = yeuCau.GioDenUtc,
                    giaVe = yeuCau.GiaVe }, gd);
            await sql.ExecuteAsync("""
                INSERT dbo.TripSeats(TripId,BusId,SeatId,FareVnd)
                SELECT @maChuyen,BusId,SeatId,@giaVe FROM dbo.Seats WHERE BusId=@maXe AND IsActive=1
                """, new { maChuyen, maXe = yeuCau.MaXe, giaVe = yeuCau.GiaVe }, gd);
            await sql.ExecuteAsync("""
                INSERT dbo.TripStops(TripId,StationId,StopOrder,PlannedArrivalAtUtc,PlannedDepartureAtUtc,IsPickup,IsDropoff)
                SELECT @maChuyen,OriginStationId,1,NULL,@gioDi,1,0 FROM dbo.Routes WHERE RouteId=@maTuyen;
                INSERT dbo.TripStops(TripId,StationId,StopOrder,PlannedArrivalAtUtc,PlannedDepartureAtUtc,IsPickup,IsDropoff)
                SELECT @maChuyen,DestinationStationId,3,@gioDen,NULL,0,1 FROM dbo.Routes WHERE RouteId=@maTuyen;
                """, new { maChuyen, maTuyen = yeuCau.MaTuyen,
                    gioDi = yeuCau.GioKhoiHanhUtc, gioDen = yeuCau.GioDenUtc }, gd);
            await gd.CommitAsync();
            return StatusCode(201, new { maChuyen, maChuyenIn = yeuCau.MaChuyen, soGhe });
        }
        catch { await gd.RollbackAsync(); throw; }
    }

    [HttpPut("{maChuyen:long}"), Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Sua(long maChuyen, SuaChuyenDto yeuCau)
    {
        KiemTraThoiGian(yeuCau.GioKhoiHanhUtc, yeuCau.GioDenUtc, yeuCau.GiaVe);
        if (yeuCau.TrangThai is not ("SCHEDULED" or "CANCELLED"))
            throw new NgoaiLeNghiepVu("Cập nhật chuyến chỉ cho phép SCHEDULED/CANCELLED.");
        await using var sql = ketNoi.TaoKetNoi();
        await sql.OpenAsync();
        await using var gd = (SqlTransaction)await sql.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var soDon = await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Bookings WITH (UPDLOCK,HOLDLOCK) WHERE TripId=@maChuyen",
                new { maChuyen }, gd);
            if (soDon != 0) throw new NgoaiLeNghiepVu("Chuyến có đơn đặt vé, không được sửa lịch/đổi xe trực tiếp.", 409);
            await KiemTraXeTaiXe(sql, gd, yeuCau.MaXe, yeuCau.MaTaiXe,
                yeuCau.GioKhoiHanhUtc, yeuCau.GioDenUtc, maChuyen);
            // Phải xóa TripSeats trước khi đổi BusId vì FK (TripId,BusId) trỏ đến Trips.
            await sql.ExecuteAsync("DELETE dbo.TripSeats WHERE TripId=@maChuyen; DELETE dbo.TripStops WHERE TripId=@maChuyen", new { maChuyen }, gd);
            var soDong = await sql.ExecuteAsync("""
                UPDATE dbo.Trips SET RouteId=@maTuyen,BusId=@maXe,DriverId=@maTaiXe,
                  DepartureAtUtc=@gioDi,ArrivalAtUtc=@gioDen,PriceVnd=@giaVe,Status=@trangThai
                WHERE TripId=@maChuyen AND Status IN ('SCHEDULED','CANCELLED')
                """, new { maChuyen, maTuyen = yeuCau.MaTuyen, maXe = yeuCau.MaXe,
                    maTaiXe = yeuCau.MaTaiXe, gioDi = yeuCau.GioKhoiHanhUtc,
                    gioDen = yeuCau.GioDenUtc, giaVe = yeuCau.GiaVe, trangThai = yeuCau.TrangThai }, gd);
            if (soDong == 0) throw new NgoaiLeNghiepVu("Chuyến không tồn tại hoặc đã khởi hành.", 409);
            await sql.ExecuteAsync("""
                INSERT dbo.TripSeats(TripId,BusId,SeatId,FareVnd)
                SELECT @maChuyen,BusId,SeatId,@giaVe FROM dbo.Seats WHERE BusId=@maXe AND IsActive=1;
                INSERT dbo.TripStops(TripId,StationId,StopOrder,PlannedDepartureAtUtc,IsPickup,IsDropoff)
                SELECT @maChuyen,OriginStationId,1,@gioDi,1,0 FROM dbo.Routes WHERE RouteId=@maTuyen;
                INSERT dbo.TripStops(TripId,StationId,StopOrder,PlannedArrivalAtUtc,IsPickup,IsDropoff)
                SELECT @maChuyen,DestinationStationId,3,@gioDen,0,1 FROM dbo.Routes WHERE RouteId=@maTuyen;
                """, new { maChuyen, maXe = yeuCau.MaXe, maTuyen = yeuCau.MaTuyen,
                    giaVe = yeuCau.GiaVe, gioDi = yeuCau.GioKhoiHanhUtc, gioDen = yeuCau.GioDenUtc }, gd);
            await gd.CommitAsync();
            return NoContent();
        }
        catch { await gd.RollbackAsync(); throw; }
    }

    [HttpDelete("{maChuyen:long}"), Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Huy(long maChuyen)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var soDon = await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Bookings WHERE TripId=@maChuyen", new { maChuyen });
        if (soDon > 0) throw new NgoaiLeNghiepVu("Chuyến đã có đơn, phải xử lý vé trước khi hủy.", 409);
        var soDong = await sql.ExecuteAsync("UPDATE dbo.Trips SET Status='CANCELLED' WHERE TripId=@maChuyen AND Status='SCHEDULED'", new { maChuyen });
        return soDong == 0 ? NotFound() : NoContent();
    }

    [HttpPatch("{maChuyen:long}/seats/{maGhe:int}/block"), Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> KhoaGhe(long maChuyen, int maGhe, [FromQuery] bool khoa = true)
    {
        if (await giuGhe.DangDuocGiu(maChuyen, maGhe)) throw new NgoaiLeNghiepVu("Ghế đang được giữ, không thể thay đổi.", 409);
        await using var sql = ketNoi.TaoKetNoi();
        var soDong = await sql.ExecuteAsync("""
            UPDATE dbo.TripSeats SET Status=@sau,BlockReason=@lyDo
            WHERE TripId=@maChuyen AND SeatId=@maGhe AND Status=@truoc
            """, new { maChuyen, maGhe, sau = khoa ? "BLOCKED" : "AVAILABLE",
                truoc = khoa ? "AVAILABLE" : "BLOCKED", lyDo = khoa ? "Khóa bởi quản trị viên" : null });
        return soDong == 0 ? Conflict("Không thể đổi trạng thái ghế đã bán hoặc không tồn tại.") : NoContent();
    }

    private static void KiemTraThoiGian(DateTime gioDi, DateTime gioDen, decimal giaVe)
    {
        if (gioDi <= DateTime.UtcNow || gioDen <= gioDi || giaVe < 0)
            throw new NgoaiLeNghiepVu("Giờ đi phải ở tương lai, giờ đến phải sau giờ đi và giá phải hợp lệ.");
    }
    private static async Task KiemTraXeTaiXe(SqlConnection sql, SqlTransaction gd,
        int maXe, int? maTaiXe, DateTime gioDi, DateTime gioDen, long? boQuaMaChuyen = null)
    {
        // SERIALIZABLE + UPDLOCK; đảm bảo không phân công trùng xe/tài xế trong khoảng thời gian.
        var soTrung = await sql.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.Trips WITH (UPDLOCK,HOLDLOCK)
            WHERE Status IN ('SCHEDULED','BOARDING','DEPARTED') AND (@boQuaMaChuyen IS NULL OR TripId<>@boQuaMaChuyen)
              AND DepartureAtUtc<@gioDen AND ArrivalAtUtc>@gioDi
              AND (BusId=@maXe OR (@maTaiXe IS NOT NULL AND DriverId=@maTaiXe))
            """, new { maXe, maTaiXe, gioDi, gioDen, boQuaMaChuyen }, gd);
        if (soTrung > 0) throw new NgoaiLeNghiepVu("Xe hoặc tài xế đã có chuyến trùng giờ.", 409);
    }
}
