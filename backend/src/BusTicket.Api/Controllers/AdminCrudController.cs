using System.Text.Json;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Controllers;
[ApiController, Route("api/admin/crud/{taiNguyen}"), Authorize(Roles = "ADMIN")]
public sealed class AdminCrudController(DichVuQuanTri quanTri, ILogger<AdminCrudController> nhatKy) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> DanhSach(string taiNguyen) => Ok(await quanTri.DanhSach(taiNguyen));
    [HttpGet("{ma:long}")]
    public async Task<IActionResult> ChiTiet(string taiNguyen, long ma) => Ok(await quanTri.ChiTiet(taiNguyen, ma));
    [HttpPost]
    public async Task<IActionResult> Tao(string taiNguyen, [FromBody] Dictionary<string, JsonElement> duLieu)
    {
        var ketQua = await quanTri.TaoMoi(taiNguyen, duLieu);
        await GhiAudit(taiNguyen, "new", "CREATE", duLieu);
        return StatusCode(201, ketQua);
    }
    [HttpPut("{ma:long}"), HttpPatch("{ma:long}")]
    public async Task<IActionResult> Sua(string taiNguyen, long ma, [FromBody] Dictionary<string, JsonElement> duLieu)
    {
        await quanTri.Sua(taiNguyen, ma, duLieu);
        await GhiAudit(taiNguyen, ma.ToString(), "UPDATE", duLieu);
        return NoContent();
    }
    [HttpDelete("{ma:long}")]
    public async Task<IActionResult> Xoa(string taiNguyen, long ma)
    {
        await quanTri.Xoa(taiNguyen, ma);
        await GhiAudit(taiNguyen, ma.ToString(), "DELETE");
        return NoContent();
    }
    private async Task GhiAudit(string taiNguyen, string ma, string thaoTac, object? duLieu = null)
    {
        // Audit là best effort để không trả lỗi 500 sau khi CRUD đã commit thành công.
        try { await quanTri.GhiNhatKy(User.LayMaNguoiDung(), taiNguyen, ma, thaoTac, duLieu); }
        catch (Exception loi) { nhatKy.LogWarning(loi, "Không ghi được AuditLogs cho {TaiNguyen}", taiNguyen); }
    }
}
