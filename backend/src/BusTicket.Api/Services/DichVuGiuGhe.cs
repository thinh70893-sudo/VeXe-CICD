using System.Text.Json;
using BusTicket.Api.Data;
using BusTicket.Api.DTOs;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Options;
using Dapper;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BusTicket.Api.Services;
public sealed record ThongTinGiuGhe(Guid MaGiuGhe, long MaNguoiDung, long MaChuyen,
    int[] DanhSachMaGhe, DateTime HetHanUtc);

public sealed class DichVuGiuGhe(KetNoiSql ketNoi, IConnectionMultiplexer redis,
    IOptions<CauHinhDatVe> cauHinh)
{
    private readonly IDatabase khoRedis = redis.GetDatabase();
    private const string LuaGiuGhe = """
        for i=2,#KEYS do
            if redis.call('EXISTS',KEYS[i]) == 1 then return 0 end
        end
        for i=2,#KEYS do
            redis.call('SET',KEYS[i],ARGV[1],'EX',ARGV[2])
        end
        redis.call('SET',KEYS[1],ARGV[3],'EX',ARGV[2])
        return 1
        """;
    private const string LuaBoGiuGhe = """
        if redis.call('GET',KEYS[1]) == false then return 0 end
        for i=2,#KEYS do
            if redis.call('GET',KEYS[i]) == ARGV[1] then redis.call('DEL',KEYS[i]) end
        end
        redis.call('DEL',KEYS[1])
        return 1
        """;

    private static RedisKey KhoaGiuGhe(Guid maGiuGhe) => $"hold:{maGiuGhe:N}";
    private static RedisKey KhoaGhe(long maChuyen, int maGhe) => $"trip:{maChuyen}:seat:{maGhe}";

    public async Task<ThongTinGiuGhe> GiuGhe(long maNguoiDung, GiuGheDto yeuCau)
    {
        var dsGhe = yeuCau.DanhSachMaGhe?.Distinct().ToArray() ?? [];
        if (dsGhe.Length < 1 || dsGhe.Length > cauHinh.Value.SoGheToiDa ||
            dsGhe.Length != (yeuCau.DanhSachMaGhe?.Count ?? 0))
            throw new NgoaiLeNghiepVu($"Chọn từ 1 đến {cauHinh.Value.SoGheToiDa} ghế, không trùng mã ghế.");
        await using var sql = ketNoi.TaoKetNoi();
        var chuyen = await sql.QuerySingleOrDefaultAsync<dynamic>("SELECT TripId, Status, DepartureAtUtc FROM dbo.Trips WHERE TripId=@maChuyen",
            new { maChuyen = yeuCau.MaChuyen });
        if (chuyen is null || chuyen.Status != "SCHEDULED" || (DateTime)chuyen.DepartureAtUtc <= DateTime.UtcNow)
            throw new NgoaiLeNghiepVu("Chuyến xe không mở bán hoặc đã khởi hành.", 409);
        var soGheTrong = await sql.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.TripSeats
            WHERE TripId=@maChuyen AND SeatId IN @dsGhe AND Status='AVAILABLE'
            """, new { maChuyen = yeuCau.MaChuyen, dsGhe });
        if (soGheTrong != dsGhe.Length)
            throw new NgoaiLeNghiepVu("Một hoặc nhiều ghế không tồn tại hoặc đã bán.", 409);
        var maGiuGhe = Guid.NewGuid();
        var hetHan = DateTime.UtcNow.AddSeconds(cauHinh.Value.SoGiayGiuGhe);
        var duLieu = new ThongTinGiuGhe(maGiuGhe, maNguoiDung, yeuCau.MaChuyen, dsGhe, hetHan);
        var khoa = new RedisKey[] { KhoaGiuGhe(maGiuGhe) }
            .Concat(dsGhe.Select(maGhe => KhoaGhe(yeuCau.MaChuyen, maGhe))).ToArray();
        var token = maGiuGhe.ToString("N");
        var ketQua = (int)(long)await khoRedis.ScriptEvaluateAsync(LuaGiuGhe, khoa,
            [token, cauHinh.Value.SoGiayGiuGhe.ToString(), JsonSerializer.Serialize(duLieu)]);
        if (ketQua != 1) throw new NgoaiLeNghiepVu("Ghế đang được khách khác giữ. Hãy chọn ghế khác.", 409);
        // Tránh trả hold thành công nếu ghế vừa bán trong khoảng kiểm tra SQL -> SET Redis.
        var vanTrong = await sql.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.TripSeats
            WHERE TripId=@maChuyen AND SeatId IN @dsGhe AND Status='AVAILABLE'
            """, new { maChuyen = yeuCau.MaChuyen, dsGhe });
        if (vanTrong != dsGhe.Length)
        {
            await BoGiuGhe(duLieu);
            throw new NgoaiLeNghiepVu("Ghế vừa được bán, vui lòng chọn lại.", 409);
        }
        return duLieu;
    }

    public async Task<ThongTinGiuGhe> KiemTra(Guid maGiuGhe, long maNguoiDung)
    {
        var json = await khoRedis.StringGetAsync(KhoaGiuGhe(maGiuGhe));
        if (json.IsNullOrEmpty) throw new NgoaiLeNghiepVu("Phiên giữ ghế đã hết hạn.", 410);
        var duLieu = JsonSerializer.Deserialize<ThongTinGiuGhe>((string)json!)
            ?? throw new NgoaiLeNghiepVu("Phiên giữ ghế bị lỗi.", 409);
        if (duLieu.MaNguoiDung != maNguoiDung)
            throw new NgoaiLeNghiepVu("Bạn không có quyền sử dụng phiên giữ ghế này.", 403);
        if (duLieu.HetHanUtc <= DateTime.UtcNow)
            throw new NgoaiLeNghiepVu("Phiên giữ ghế đã hết hạn.", 410);
        var token = maGiuGhe.ToString("N");
        foreach (var maGhe in duLieu.DanhSachMaGhe)
        {
            if (await khoRedis.StringGetAsync(KhoaGhe(duLieu.MaChuyen, maGhe)) != token)
                throw new NgoaiLeNghiepVu("Phiên giữ ghế không còn hợp lệ.", 409);
        }
        return duLieu;
    }

    public async Task BoGiuGhe(ThongTinGiuGhe duLieu)
    {
        var khoa = new RedisKey[] { KhoaGiuGhe(duLieu.MaGiuGhe) }
            .Concat(duLieu.DanhSachMaGhe.Select(maGhe => KhoaGhe(duLieu.MaChuyen, maGhe))).ToArray();
        await khoRedis.ScriptEvaluateAsync(LuaBoGiuGhe, khoa, [duLieu.MaGiuGhe.ToString("N")]);
    }

    public async Task<bool> DangDuocGiu(long maChuyen, int maGhe) =>
        await khoRedis.KeyExistsAsync(KhoaGhe(maChuyen, maGhe));

    public async Task BoGiuTheoMa(Guid? maGiuGhe)
    {
        if (!maGiuGhe.HasValue) return;
        var json = await khoRedis.StringGetAsync(KhoaGiuGhe(maGiuGhe.Value));
        if (json.IsNullOrEmpty) return;
        var duLieu = JsonSerializer.Deserialize<ThongTinGiuGhe>((string)json!);
        if (duLieu is not null) await BoGiuGhe(duLieu);
    }
}
