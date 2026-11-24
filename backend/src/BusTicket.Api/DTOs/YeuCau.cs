using System.ComponentModel.DataAnnotations;
namespace BusTicket.Api.DTOs;
public sealed record DangKyDto([Required] string HoTen, [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string MatKhau, string? SoDienThoai);
public sealed record DangNhapDto([Required] string Email, [Required] string MatKhau);
public sealed record SuaHoSoDto([Required] string HoTen, string? SoDienThoai);
public sealed record DoiMatKhauDto([Required] string MatKhauCu, [Required, MinLength(8)] string MatKhauMoi);
public sealed record GiuGheDto(long MaChuyen, List<int> DanhSachMaGhe);
public sealed record HanhKhachDto([Required] string HoTen, string? SoDienThoai, int MaGhe);
public sealed record DatVeDto(Guid MaGiuGhe, [Required] string TenLienHe, [Required, EmailAddress] string EmailLienHe,
    [Required] string SoDienThoaiLienHe, List<HanhKhachDto> HanhKhach, string? MaKhuyenMai);
public sealed record TaoThanhToanDto(long MaDatVe);
public sealed record XacThucVeDto([Required] string MaQr);
public sealed record YeuCauHoanTienDto(string LyDo);
public sealed record TrangThaiHoanTienDto(string TrangThai);
public sealed record TaoDanhGiaDto(long MaChuyen, byte SoSao, string? BinhLuan);
public sealed record TaoChuyenDto(string MaChuyen, int MaTuyen, int MaXe, int? MaTaiXe,
    DateTime GioKhoiHanhUtc, DateTime GioDenUtc, decimal GiaVe);
public sealed record SuaChuyenDto(int MaTuyen, int MaXe, int? MaTaiXe, DateTime GioKhoiHanhUtc, DateTime GioDenUtc,
    decimal GiaVe, string TrangThai);
