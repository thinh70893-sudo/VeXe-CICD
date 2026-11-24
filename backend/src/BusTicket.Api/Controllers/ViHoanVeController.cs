using BusTicket.Api.Infrastructure;
using BusTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Controllers;

public sealed record HoanVeTheoLuaChonDto(
    List<long> DanhSachMaVe,
    string LyDo,
    Guid MaLanYeuCau);
public sealed record ThanhToanBangViDto(Guid MaLanYeuCau);

[ApiController]
[Authorize]
[Route("api/vi-busgo")]
public sealed class ViHoanVeController(DichVuViHoanVe dichVu):ControllerBase
{
    [HttpGet("so-du")]
    public async Task<IActionResult> SoDu() => Ok(await dichVu.XemVi(User.LayMaNguoiDung()));

    [HttpGet("lich-su")]
    public async Task<IActionResult> LichSu() => Ok(await dichVu.LichSuVi(User.LayMaNguoiDung()));

    [HttpGet("hoan-ve/du-kien/{maDatVe:long}")]
    public async Task<IActionResult> DuKien(long maDatVe) =>
        Ok(await dichVu.DuKienHoanVe(maDatVe,User.LayMaNguoiDung()));

    [HttpPost("hoan-ve/theo-don/{maDatVe:long}")]
    public async Task<IActionResult> Hoan(long maDatVe, HoanVeTheoLuaChonDto yeuCau) =>
        Ok(await dichVu.HoanNhieuVe(maDatVe,User.LayMaNguoiDung(),
            yeuCau.DanhSachMaVe,yeuCau.LyDo,yeuCau.MaLanYeuCau));

    [HttpGet("hoan-ve/lich-su")]
    public async Task<IActionResult> LichSuHoan() =>
        Ok(await dichVu.LichSuHoan(User.LayMaNguoiDung(),false));

    [HttpGet("hoan-ve/quan-tri")]
    [Authorize(Roles="STAFF,ADMIN")]
    public async Task<IActionResult> QuanTriHoan() =>
        Ok(await dichVu.LichSuHoan(User.LayMaNguoiDung(),true));

    [HttpPost("thanh-toan/{maDatVe:long}")]
    public async Task<IActionResult> ThanhToan(long maDatVe,ThanhToanBangViDto yeuCau) =>
        Ok(await dichVu.ThanhToanBangVi(maDatVe,User.LayMaNguoiDung(),yeuCau.MaLanYeuCau));
}
