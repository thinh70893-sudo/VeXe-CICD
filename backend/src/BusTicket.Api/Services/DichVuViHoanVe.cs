using System.Data;
using System.Text.Json;
using BusTicket.Api.Data;
using BusTicket.Api.Infrastructure;
using Dapper;
using StackExchange.Redis;

namespace BusTicket.Api.Services;

public sealed class DichVuViHoanVe(
    KetNoiSql ketNoi,
    DichVuGiuGhe giuGhe,
    ILogger<DichVuViHoanVe> nhatKy)
{
    public async Task<object> XemVi(long maNguoiDung)
    {
        await using var sql = ketNoi.TaoKetNoi();
        var soDu = await sql.ExecuteScalarAsync<decimal?>("""
            SELECT SoDu FROM dbo.ViTaiKhoan WHERE MaNguoiDung=@maNguoiDung
            """, new { maNguoiDung }) ?? 0;
        return new { soDu, donVi = "VND", duocRut = false };
    }

    public async Task<IEnumerable<dynamic>> LichSuVi(long maNguoiDung)
    {
        await using var sql = ketNoi.TaoKetNoi();
        return await sql.QueryAsync("""
            SELECT TOP(100) gd.MaGiaoDichVi AS maGiaoDich,
                gd.LoaiGiaoDich AS loaiGiaoDich,
                gd.SoTien AS soTien,gd.SoDuSau AS soDuSau,
                gd.MaDatVe AS maDatVe,gd.MaDotHoan AS maDotHoan,
                gd.NgayTaoUtc AS ngayTaoUtc
            FROM dbo.GiaoDichVi gd
            JOIN dbo.ViTaiKhoan w ON w.MaVi=gd.MaVi
            WHERE w.MaNguoiDung=@maNguoiDung
            ORDER BY gd.MaGiaoDichVi DESC
            """, new { maNguoiDung });
    }

    public async Task<IEnumerable<dynamic>> DuKienHoanVe(long maDatVe, long maNguoiDung)
    {
        await using var sql = ketNoi.TaoKetNoi();
        // Chi xem du kien (khong INSERT), so tien phan bo khop logic trong SP hoan ve.
        var ketQua = await sql.QueryAsync("""
            WITH PhanNguyen AS (
                SELECT bi.BookingItemId,bi.BookingId,bi.PriceVnd,
                    CONVERT(DECIMAL(14,0),CASE WHEN b.SubtotalVnd=0 THEN 0 ELSE
                      FLOOR((CONVERT(DECIMAL(28,0),bi.PriceVnd)*b.TotalVnd)/NULLIF(b.SubtotalVnd,0)) END) AS TienNguyen,
                    CASE WHEN b.SubtotalVnd=0 THEN CONVERT(DECIMAL(28,0),0) ELSE
                      (CONVERT(DECIMAL(28,0),bi.PriceVnd)*b.TotalVnd)%NULLIF(b.SubtotalVnd,0) END AS PhanDu,
                    b.TotalVnd
                FROM dbo.BookingItems bi
                JOIN dbo.Bookings b ON b.BookingId=bi.BookingId
                WHERE b.BookingId=@maDatVe AND b.UserId=@maNguoiDung
            ), XepHang AS (
                SELECT *,ROW_NUMBER() OVER(ORDER BY PhanDu DESC,BookingItemId) AS ThuTu,
                    SUM(TienNguyen) OVER() AS TongNguyen
                FROM PhanNguyen
            )
            SELECT ti.TicketId AS maVe, ti.Status AS trangThaiVe,
                bp.FullName AS hoTen,s.SeatCode AS maGhe,
                COALESCE(pb.SoTienThucTra,
                    x.TienNguyen+CASE WHEN x.ThuTu<=x.TotalVnd-x.TongNguyen THEN 1 ELSE 0 END
                ) AS soTienHoan,
                CONVERT(bit,CASE WHEN ti.Status='ISSUED' AND ti.CheckedInAtUtc IS NULL
                     AND b.Status='PAID' AND tr.Status IN ('SCHEDULED','BOARDING')
                     AND tr.DepartureAtUtc>DATEADD(MINUTE,60,SYSUTCDATETIME())
                     AND NOT EXISTS (SELECT 1 FROM dbo.Refunds rf
                                      WHERE rf.BookingId=b.BookingId AND rf.Status IN ('REQUESTED','APPROVED'))
                   THEN 1 ELSE 0 END) AS duDieuKien,
                tr.DepartureAtUtc AS gioKhoiHanhUtc
            FROM XepHang x
            JOIN dbo.BookingItems bi ON bi.BookingItemId=x.BookingItemId
            JOIN dbo.Bookings b ON b.BookingId=bi.BookingId
            JOIN dbo.Trips tr ON tr.TripId=b.TripId
            JOIN dbo.BookingPassengers bp ON bp.PassengerId=bi.PassengerId
            JOIN dbo.Seats s ON s.SeatId=bi.SeatId
            JOIN dbo.Tickets ti ON ti.BookingItemId=bi.BookingItemId
            LEFT JOIN dbo.PhanBoTienVe pb ON pb.MaChiTietDon=bi.BookingItemId
            ORDER BY s.SeatCode
            """, new { maDatVe, maNguoiDung });
        return ketQua;
    }

    public async Task<object> HoanNhieuVe(long maDatVe,long maNguoiDung,
        IReadOnlyCollection<long> danhSachMaVe,string lyDo,Guid maLanYeuCau)
    {
        if (danhSachMaVe is null || danhSachMaVe.Count<1 || danhSachMaVe.Count>50 ||
            danhSachMaVe.Any(x=>x<=0) || danhSachMaVe.Distinct().Count()!=danhSachMaVe.Count)
            throw new NgoaiLeNghiepVu("Chọn từ 1 đến 50 vé hợp lệ, không trùng nhau.");
        if (string.IsNullOrWhiteSpace(lyDo) || lyDo.Trim().Length<3 || lyDo.Length>300)
            throw new NgoaiLeNghiepVu("Lý do hoàn vé phải từ 3 đến 300 ký tự.");
        if (maLanYeuCau==Guid.Empty)
            throw new NgoaiLeNghiepVu("Thiếu mã yêu cầu chống gửi trùng.");
        await using var sql = ketNoi.TaoKetNoi();
        return await sql.QuerySingleAsync("dbo.usp_HoanNhieuVeVaoVi", new
        {
            MaDatVe=maDatVe, MaNguoiDung=maNguoiDung,
            DanhSachMaVeJson=JsonSerializer.Serialize(danhSachMaVe),
            LyDo=lyDo.Trim(), MaLanYeuCau=maLanYeuCau
        }, commandType:CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<dynamic>> LichSuHoan(long maNguoiDung, bool nhanVien)
    {
        await using var sql = ketNoi.TaoKetNoi();
        return await sql.QueryAsync("""
            SELECT TOP(200) h.MaDotHoan AS maDotHoan,h.MaDatVe AS maDatVe,
                h.MaNguoiDung AS maNguoiDung,h.LyDo AS lyDo,
                h.SoTienHoan AS soTienHoan,h.TrangThai AS trangThai,
                h.NgayHoanUtc AS ngayHoanUtc,
                (SELECT COUNT(*) FROM dbo.ChiTietHoanVe c WHERE c.MaDotHoan=h.MaDotHoan) AS soVe,
                (SELECT STRING_AGG(CONVERT(NVARCHAR(MAX),v.TicketCode),N', ')
                 FROM dbo.ChiTietHoanVe c JOIN dbo.Tickets v ON v.TicketId=c.MaVe
                 WHERE c.MaDotHoan=h.MaDotHoan) AS danhSachVe
            FROM dbo.DotHoanVe h
            WHERE @nhanVien=1 OR h.MaNguoiDung=@maNguoiDung
            ORDER BY h.MaDotHoan DESC
            """, new { maNguoiDung, nhanVien });
    }

    public async Task<object> ThanhToanBangVi(long maDatVe,long maNguoiDung,Guid maLanYeuCau)
    {
        if (maLanYeuCau==Guid.Empty)
            throw new NgoaiLeNghiepVu("Thiếu mã yêu cầu thanh toán.");

        // Kiem tra Redis truoc khi ghi SQL, giong DichVuThanhToan.XacNhanSandbox.
        await using var sql = ketNoi.TaoKetNoi();
        var don=await sql.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT BookingId,UserId,Status,HoldId
            FROM dbo.Bookings WHERE BookingId=@maDatVe AND UserId=@maNguoiDung
            """,new {maDatVe,maNguoiDung});
        if (don is null) throw new NgoaiLeNghiepVu("Không tìm thấy đơn của tài khoản.",404);
        Guid? maGiu = (Guid?)don.HoldId;
        if ((string)don.Status=="PENDING")
        {
            if (!maGiu.HasValue) throw new NgoaiLeNghiepVu("Đơn không có phiên giữ ghế.",409);
            await giuGhe.KiemTra(maGiu.Value,maNguoiDung);
        }

        var ketQua=await sql.QuerySingleAsync("dbo.usp_ThanhToanDonBangVi",new
        {
            MaDatVe=maDatVe,MaNguoiDung=maNguoiDung,MaLanYeuCau=maLanYeuCau
        },commandType:CommandType.StoredProcedure);

        // COMMIT da thanh cong, Redis co loi thi khong rollback tien/ve.
        try { await giuGhe.BoGiuTheoMa(maGiu); }
        catch (RedisException loi) { nhatKy.LogWarning(loi,"Thanh toán ví thành công, Redis sẽ hết hạn tự nhiên."); }
        return ketQua;
    }
}
