using BusTicket.Api.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/catalog")]
public sealed class CatalogController(KetNoiSql ketNoi) : ControllerBase
{
    [HttpGet("cities")]
    public async Task<IActionResult> ThanhPho()
    { await using var sql = ketNoi.TaoKetNoi(); return Ok(await sql.QueryAsync("SELECT CityId AS MaThanhPho,CityName AS TenThanhPho FROM dbo.Cities WHERE IsActive=1 ORDER BY CityName")); }
    [HttpGet("stations")]
    public async Task<IActionResult> BenXe([FromQuery] int? maThanhPho)
    { await using var sql = ketNoi.TaoKetNoi(); return Ok(await sql.QueryAsync("""
        SELECT StationId AS MaBenXe,CityId AS MaThanhPho,StationName AS TenBenXe,
            AddressLine AS DiaChi FROM dbo.Stations WHERE IsActive=1 AND (@maThanhPho IS NULL OR CityId=@maThanhPho)
        ORDER BY StationName
        """, new { maThanhPho })); }
    [HttpGet("routes")]
    public async Task<IActionResult> TuyenXe()
    { await using var sql = ketNoi.TaoKetNoi(); return Ok(await sql.QueryAsync("""
        SELECT r.RouteId AS MaTuyen,r.RouteCode AS MaTuyenIn,
            s1.StationName AS BenDi,s2.StationName AS BenDen,
            r.EstimatedMinutes AS SoPhutDuKien,r.DistanceKm AS SoKm
        FROM dbo.Routes r JOIN dbo.Stations s1 ON s1.StationId=r.OriginStationId
        JOIN dbo.Stations s2 ON s2.StationId=r.DestinationStationId
        WHERE r.IsActive=1 ORDER BY r.RouteCode
        """)); }
    [HttpGet("bus-types")]
    public async Task<IActionResult> LoaiXe()
    { await using var sql = ketNoi.TaoKetNoi(); return Ok(await sql.QueryAsync("SELECT BusTypeId AS MaLoaiXe,TypeName AS TenLoaiXe,DefaultCapacity AS SoCho FROM dbo.BusTypes")); }
    [HttpGet("promotions")]
    public async Task<IActionResult> KhuyenMai()
    { await using var sql = ketNoi.TaoKetNoi(); return Ok(await sql.QueryAsync("""
        SELECT PromoCode AS MaKhuyenMai,Description AS MoTa,DiscountType AS LoaiGiam,
          DiscountValue AS MucGiam,MinOrderVnd AS DonToiThieu,EndsAtUtc AS HetHanUtc
        FROM dbo.Promotions WHERE IsActive=1 AND SYSUTCDATETIME() BETWEEN StartsAtUtc AND EndsAtUtc
        AND (MaxUses IS NULL OR UsedCount<MaxUses)
        """)); }
}
