using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace BusTicket.Api.Infrastructure;

public sealed class NgoaiLeNghiepVu(string noiDung, int maHttp = 400) : Exception(noiDung)
{
    public int MaHttp { get; } = maHttp;
}

public static class TienIchNguoiDung
{
    public static long LayMaNguoiDung(this ClaimsPrincipal nguoiDung)
    {
        var giaTri = nguoiDung.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(giaTri, out var maNguoiDung))
            throw new NgoaiLeNghiepVu("Vui lòng đăng nhập.", 401);
        return maNguoiDung;
    }
    public static bool LaQuanTri(this ClaimsPrincipal nguoiDung) => nguoiDung.IsInRole("ADMIN");
    public static bool LaNhanVien(this ClaimsPrincipal nguoiDung) => nguoiDung.IsInRole("STAFF") || nguoiDung.LaQuanTri();
}

public sealed class XuLyNgoaiLeMiddleware(RequestDelegate tiepTheo, ILogger<XuLyNgoaiLeMiddleware> nhatKy)
{
    public async Task InvokeAsync(HttpContext nguCanh)
    {
        try { await tiepTheo(nguCanh); }
        catch (NgoaiLeNghiepVu loi)
        {
            nguCanh.Response.StatusCode = loi.MaHttp;
            await nguCanh.Response.WriteAsJsonAsync(new ProblemDetails { Status = loi.MaHttp, Title = "Lỗi nghiệp vụ", Detail = loi.Message });
        }
        catch (Microsoft.Data.SqlClient.SqlException loi) when (loi.Number is 2601 or 2627 or 547 or 50001 or >= 51000 and <= 53999)
        {
            nhatKy.LogWarning(loi, "Ràng buộc Database hoặc stored procedure trả về lỗi");
            nguCanh.Response.StatusCode = (int)HttpStatusCode.Conflict;
            await nguCanh.Response.WriteAsJsonAsync(new ProblemDetails { Status = 409, Title = "Dữ liệu xung đột", Detail = loi.Number is 2601 or 2627 or 547 ? "Dữ liệu trùng hoặc đang được tham chiếu." : loi.Message });
        }
        catch (StackExchange.Redis.RedisException loi)
        {
            nhatKy.LogError(loi, "Redis không khả dụng");
            nguCanh.Response.StatusCode = 503;
            await nguCanh.Response.WriteAsJsonAsync(new ProblemDetails { Status = 503, Title = "Không thể giữ ghế", Detail = "Redis tạm thời không khả dụng; thử lại sau." });
        }
        catch (Exception loi)
        {
            nhatKy.LogError(loi, "Lỗi hệ thống");
            nguCanh.Response.StatusCode = 500;
            await nguCanh.Response.WriteAsJsonAsync(new ProblemDetails { Status = 500, Title = "Lỗi máy chủ", Detail = "Vui lòng thử lại sau." });
        }
    }
}

