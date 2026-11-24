using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusTicket.Api.Data;
using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Options;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BusTicket.Api.Services;

public sealed class DichVuXacThuc(KetNoiSql ketNoi, IOptions<CauHinhJwt> caiDatJwt)
{
    private readonly PasswordHasher<object> maHoa = new();
    public async Task<object> DangKy(DangKyDto yeuCau)
    {
        var email = yeuCau.Email.Trim().ToLowerInvariant();
        if (yeuCau.HoTen.Trim().Length < 2) throw new NgoaiLeNghiepVu("Họ tên quá ngắn.");
        if (yeuCau.MatKhau.Length < 8) throw new NgoaiLeNghiepVu("Mật khẩu phải có ít nhất 8 ký tự.");
        await using var sql = ketNoi.TaoKetNoi();
        var maVaiTro = await sql.ExecuteScalarAsync<int?>("SELECT RoleId FROM dbo.Roles WHERE RoleCode='CUSTOMER'");
        if (!maVaiTro.HasValue) throw new NgoaiLeNghiepVu("Chưa có vai trò CUSTOMER trong cơ sở dữ liệu.", 503);
        var hash = maHoa.HashPassword(new object(), yeuCau.MatKhau);
        var maNguoiDung = await sql.ExecuteScalarAsync<long>("""
            INSERT dbo.Users(RoleId,FullName,Email,PhoneNumber,PasswordHash)
            OUTPUT INSERTED.UserId
            VALUES (@maVaiTro,@hoTen,@email,@soDienThoai,@hash)
            """, new { maVaiTro, hoTen = yeuCau.HoTen.Trim(), email, soDienThoai = yeuCau.SoDienThoai, hash });
        return new { maNguoiDung, email, thongBao = "Đăng ký thành công, vui lòng đăng nhập." };
    }

    public async Task<object> DangNhap(DangNhapDto yeuCau)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var nguoiDung = await sql.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT u.UserId, u.FullName, u.Email, u.PasswordHash, u.IsActive, r.RoleCode
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId
            WHERE u.Email=@email
            """, new { email = yeuCau.Email.Trim().ToLowerInvariant() });
        if (nguoiDung is null || !nguoiDung.IsActive ||
            maHoa.VerifyHashedPassword(new object(), (string)nguoiDung.PasswordHash, yeuCau.MatKhau) == PasswordVerificationResult.Failed)
            throw new NgoaiLeNghiepVu("Email hoặc mật khẩu không chính xác.", 401);
        string maVaiTro = nguoiDung.RoleCode;
        long maNguoiDung = nguoiDung.UserId;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, maNguoiDung.ToString()),
            new Claim(ClaimTypes.Name, (string)nguoiDung.FullName),
            new Claim(ClaimTypes.Email, (string)nguoiDung.Email),
            new Claim(ClaimTypes.Role, maVaiTro)
        };
        var caiDat = caiDatJwt.Value;
        var hetHan = DateTime.UtcNow.AddMinutes(caiDat.ExpireMinutes);
        var thongTinToken = new JwtSecurityToken(caiDat.Issuer, caiDat.Audience, claims,
            expires: hetHan, signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(caiDat.Key)), SecurityAlgorithms.HmacSha256));
        return new { accessToken = new JwtSecurityTokenHandler().WriteToken(thongTinToken), loaiToken = "Bearer",
            hetHanUtc = hetHan, maNguoiDung, hoTen = (string)nguoiDung.FullName, email = (string)nguoiDung.Email, vaiTro = maVaiTro };
    }

    public async Task<object> ThongTin(long maNguoiDung)
    {
        await using var sql = ketNoi.TaoKetNoi();
        return await sql.QuerySingleOrDefaultAsync("""
            SELECT u.UserId AS MaNguoiDung,u.FullName AS HoTen,u.Email,u.PhoneNumber AS SoDienThoai,
                u.IsEmailVerified AS EmailDaXacThuc,u.IsActive AS DangHoatDong,r.RoleCode AS VaiTro
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId WHERE u.UserId=@maNguoiDung
            """, new { maNguoiDung }) ?? throw new NgoaiLeNghiepVu("Tài khoản không tồn tại.", 404);
    }

    public async Task SuaHoSo(long maNguoiDung, SuaHoSoDto yeuCau)
    {
        if (yeuCau.HoTen.Trim().Length < 2) throw new NgoaiLeNghiepVu("Họ tên quá ngắn.");
        await using var sql = ketNoi.TaoKetNoi();
        await sql.ExecuteAsync("""
            UPDATE dbo.Users SET FullName=@hoTen,PhoneNumber=@soDienThoai,UpdatedAtUtc=SYSUTCDATETIME()
            WHERE UserId=@maNguoiDung
            """, new { maNguoiDung, hoTen = yeuCau.HoTen.Trim(), soDienThoai = yeuCau.SoDienThoai });
    }

    public async Task DoiMatKhau(long maNguoiDung, DoiMatKhauDto yeuCau)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var hashCu = await sql.ExecuteScalarAsync<string?>("SELECT PasswordHash FROM dbo.Users WHERE UserId=@maNguoiDung", new { maNguoiDung });
        if (hashCu is null || maHoa.VerifyHashedPassword(new object(), hashCu, yeuCau.MatKhauCu) == PasswordVerificationResult.Failed)
            throw new NgoaiLeNghiepVu("Mật khẩu hiện tại không đúng.", 400);
        var hashMoi = maHoa.HashPassword(new object(), yeuCau.MatKhauMoi);
        await sql.ExecuteAsync("UPDATE dbo.Users SET PasswordHash=@hashMoi,UpdatedAtUtc=SYSUTCDATETIME() WHERE UserId=@maNguoiDung",
            new { maNguoiDung, hashMoi });
    }
}
