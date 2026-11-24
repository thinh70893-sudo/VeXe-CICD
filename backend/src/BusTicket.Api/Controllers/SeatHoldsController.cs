using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/seat-holds"), Authorize]
public sealed class SeatHoldsController(DichVuGiuGhe giuGhe) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Tao(GiuGheDto yeuCau) => StatusCode(201,
        await giuGhe.GiuGhe(User.LayMaNguoiDung(), yeuCau));
    [HttpGet("{maGiuGhe:guid}")]
    public async Task<IActionResult> ChiTiet(Guid maGiuGhe) => Ok(await giuGhe.KiemTra(maGiuGhe, User.LayMaNguoiDung()));
    [HttpDelete("{maGiuGhe:guid}")]
    public async Task<IActionResult> Xoa(Guid maGiuGhe)
    {
        var duLieu = await giuGhe.KiemTra(maGiuGhe, User.LayMaNguoiDung());
        await giuGhe.BoGiuGhe(duLieu);
        return NoContent();
    }
}
