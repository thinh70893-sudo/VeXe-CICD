using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/tickets"), Authorize]
public sealed class TicketsController(DichVuVe ve, DichVuEmail email) : ControllerBase
{
    [HttpGet("{maVe:long}")]
    public async Task<IActionResult> ChiTiet(long maVe) => Ok(await ve.ChiTiet(maVe, User.LayMaNguoiDung(), User.LaNhanVien()));
    [HttpGet("{maVe:long}/qr")]
    public async Task<IActionResult> MaQr(long maVe) => File(await ve.AnhQr(maVe, User.LayMaNguoiDung(), User.LaNhanVien()), "image/png", $"ve-{maVe}.png");
    [HttpPost("verify"), Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> KiemTra(XacThucVeDto yeuCau) => Ok(await ve.KiemTra(DocMaQr(yeuCau.MaQr)));
    [HttpPost("check-in"), Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> SoatVe(XacThucVeDto yeuCau) => Ok(await ve.SoatVe(DocMaQr(yeuCau.MaQr), User.LayMaNguoiDung()));
    [HttpPost("{maVe:long}/resend-email")]
    public async Task<IActionResult> GuiLai(long maVe) => Ok(await email.YeuCauGuiLai(maVe, User.LayMaNguoiDung(), User.LaNhanVien()));

    private static Guid DocMaQr(string noiDung)
    {
        var ma = (noiDung ?? "").Trim();
        if (ma.StartsWith("BTICKET:", StringComparison.OrdinalIgnoreCase)) ma = ma[8..];
        if (!Guid.TryParse(ma, out var ketQua))
            throw new NgoaiLeNghiepVu("Mã QR không đúng định dạng.", 400);
        return ketQua;
    }
}
