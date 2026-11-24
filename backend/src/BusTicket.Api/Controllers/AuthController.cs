using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/auth")]
public sealed class AuthController(DichVuXacThuc xacThuc) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> DangKy(DangKyDto yeuCau) => StatusCode(201, await xacThuc.DangKy(yeuCau));
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> DangNhap(DangNhapDto yeuCau) => Ok(await xacThuc.DangNhap(yeuCau));
    [HttpGet("me"), Authorize]
    public async Task<IActionResult> ThongTin() => Ok(await xacThuc.ThongTin(User.LayMaNguoiDung()));
    [HttpPut("profile"), Authorize]
    public async Task<IActionResult> SuaHoSo(SuaHoSoDto yeuCau)
    { await xacThuc.SuaHoSo(User.LayMaNguoiDung(), yeuCau); return NoContent(); }
    [HttpPut("password"), Authorize]
    public async Task<IActionResult> DoiMatKhau(DoiMatKhauDto yeuCau)
    { await xacThuc.DoiMatKhau(User.LayMaNguoiDung(), yeuCau); return NoContent(); }
}
