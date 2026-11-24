using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/bookings"), Authorize]
public sealed class BookingsController(DichVuDatVe datVe) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Tao(DatVeDto yeuCau) => StatusCode(201,
        await datVe.TaoDon(User.LayMaNguoiDung(), yeuCau));
    [HttpGet("me")]
    public async Task<IActionResult> CuaToi() => Ok(await datVe.DanhSach(User.LayMaNguoiDung()));
    [HttpGet("{maDatVe:long}")]
    public async Task<IActionResult> ChiTiet(long maDatVe) => Ok(await datVe.ChiTiet(maDatVe, User.LayMaNguoiDung(), User.LaNhanVien()));
    [HttpPost("{maDatVe:long}/cancel")]
    public async Task<IActionResult> Huy(long maDatVe)
    { await datVe.HuyDonCho(maDatVe, User.LayMaNguoiDung()); return Ok(new { trangThai = "CANCELLED" }); }
}
