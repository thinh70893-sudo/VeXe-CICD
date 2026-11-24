using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/payments"), Authorize]
public sealed class PaymentsController(DichVuThanhToan thanhToan) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Tao(TaoThanhToanDto yeuCau) => Ok(
        await thanhToan.KhoiTao(yeuCau.MaDatVe, User.LayMaNguoiDung()));
    [HttpPost("{maThanhToan:long}/sandbox-confirm")]
    public async Task<IActionResult> ThanhToanThu(long maThanhToan) => Ok(
        await thanhToan.XacNhanSandbox(maThanhToan, User.LayMaNguoiDung(), User.LaNhanVien()));
    [HttpGet("by-booking/{maDatVe:long}")]
    public async Task<IActionResult> TheoDon(long maDatVe) => Ok(
        await thanhToan.DanhSachGiaoDich(maDatVe, User.LayMaNguoiDung(), User.LaNhanVien()));
}
