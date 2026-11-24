using System.Text.Json;
using BusTicket.Api.Data;
using BusTicket.Api.Infrastructure;
using Dapper;

namespace BusTicket.Api.Services;

public sealed record CotDuLieu(string CotSql, string KieuDuLieu, bool BatBuoc = false);
public sealed record DinhNghiaBang(string TenBang, string KhoaChinh,
    IReadOnlyDictionary<string, CotDuLieu> CotChoPhep, string? CotXoaMem = "IsActive", string? GiaTriXoaMem = "0");

public sealed class DichVuQuanTri(KetNoiSql ketNoi)
{
    // Danh sách trắng: không có tên bảng/tên cột nào được lấy trực tiếp từ người dùng.
    public static readonly IReadOnlyDictionary<string, DinhNghiaBang> Bang =
        new Dictionary<string, DinhNghiaBang>(StringComparer.OrdinalIgnoreCase)
        {
            ["cities"] = new("Cities", "CityId", new Dictionary<string, CotDuLieu> {
                ["maThanhPho"] = new("CityCode","string",true), ["tenThanhPho"] = new("CityName","string",true),
                ["hoatDong"] = new("IsActive","bool") }),
            ["stations"] = new("Stations", "StationId", new Dictionary<string, CotDuLieu> {
                ["maThanhPho"] = new("CityId","int",true),["maBenXe"] = new("StationCode","string",true),
                ["tenBenXe"] = new("StationName","string",true),["diaChi"] = new("AddressLine","string"),
                ["hoatDong"] = new("IsActive","bool") }),
            ["routes"] = new("Routes", "RouteId", new Dictionary<string, CotDuLieu> {
                ["maTuyen"] = new("RouteCode","string",true),["maBenDi"] = new("OriginStationId","int",true),
                ["maBenDen"] = new("DestinationStationId","int",true),["soKm"] = new("DistanceKm","decimal"),
                ["soPhutDuKien"] = new("EstimatedMinutes","int",true),["hoatDong"] = new("IsActive","bool") }),
            ["bus-types"] = new("BusTypes", "BusTypeId", new Dictionary<string, CotDuLieu> {
                ["maLoaiXe"] = new("TypeCode","string",true),["tenLoaiXe"] = new("TypeName","string",true),
                ["soChoMacDinh"] = new("DefaultCapacity","int",true) }, null),
            ["buses"] = new("Buses", "BusId", new Dictionary<string, CotDuLieu> {
                ["maLoaiXe"] = new("BusTypeId","int",true),["bienSo"] = new("PlateNumber","string",true),
                ["hangXe"] = new("Manufacturer","string"),["soCho"] = new("Capacity","int",true),
                ["trangThai"] = new("Status","string") },"Status","'INACTIVE'"),
            ["seats"] = new("Seats", "SeatId", new Dictionary<string, CotDuLieu> {
                ["maXe"] = new("BusId","int",true),["maGhe"] = new("SeatCode","string",true),
                ["tang"] = new("FloorNumber","int"),["hoatDong"] = new("IsActive","bool") }),
            ["drivers"] = new("Drivers", "DriverId", new Dictionary<string, CotDuLieu> {
                ["maTaiXe"] = new("DriverCode","string",true),["hoTen"] = new("FullName","string",true),
                ["soDienThoai"] = new("PhoneNumber","string",true),["hangBangLai"] = new("LicenseClass","string"),
                ["hoatDong"] = new("IsActive","bool") }),
            ["trip-stops"] = new("TripStops", "TripStopId", new Dictionary<string, CotDuLieu> {
                ["maChuyen"] = new("TripId","long",true),["maBenXe"] = new("StationId","int",true),
                ["thuTu"] = new("StopOrder","int",true),["gioDenUtc"] = new("PlannedArrivalAtUtc","datetime"),
                ["gioDiUtc"] = new("PlannedDepartureAtUtc","datetime"),["duocDon"] = new("IsPickup","bool"),
                ["duocTra"] = new("IsDropoff","bool") },null),
            ["promotions"] = new("Promotions", "PromotionId", new Dictionary<string, CotDuLieu> {
                ["maKhuyenMai"] = new("PromoCode","string",true),["moTa"] = new("Description","string"),
                ["loaiGiam"] = new("DiscountType","string",true),["mucGiam"] = new("DiscountValue","decimal",true),
                ["giamToiDa"] = new("MaxDiscountVnd","decimal"),["donToiThieu"] = new("MinOrderVnd","decimal"),
                ["luotToiDa"] = new("MaxUses","int"),["batDauUtc"] = new("StartsAtUtc","datetime",true),
                ["ketThucUtc"] = new("EndsAtUtc","datetime",true),["hoatDong"] = new("IsActive","bool") })
        };

    private static DinhNghiaBang LayBang(string taiNguyen) => Bang.TryGetValue(taiNguyen, out var bang) ? bang :
        throw new NgoaiLeNghiepVu("Tài nguyên CRUD không tồn tại.", 404);

    private static object? ChuyenGiaTri(JsonElement giaTri, string kieu)
    {
        if (giaTri.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
        try
        {
            return kieu switch
            {
                "int" => giaTri.GetInt32(),
                "long" => giaTri.GetInt64(),
                "decimal" => giaTri.GetDecimal(),
                "bool" => giaTri.GetBoolean(),
                "datetime" => DateTime.SpecifyKind(DateTime.Parse(giaTri.GetString()!, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal), DateTimeKind.Utc),
                _ => giaTri.GetString()
            };
        }
        catch (Exception) { throw new NgoaiLeNghiepVu($"Giá trị '{giaTri}' không đúng kiểu {kieu}."); }
    }

    public async Task<IEnumerable<dynamic>> DanhSach(string taiNguyen)
    {
        var bang = LayBang(taiNguyen);
        await using var sql = ketNoi.TaoKetNoi();
        return await sql.QueryAsync($"SELECT TOP (200) * FROM dbo.[{bang.TenBang}] ORDER BY [{bang.KhoaChinh}] DESC");
    }
    public async Task<object> ChiTiet(string taiNguyen, long ma)
    {
        var bang = LayBang(taiNguyen);
        await using var sql = ketNoi.TaoKetNoi();
        return await sql.QuerySingleOrDefaultAsync($"SELECT * FROM dbo.[{bang.TenBang}] WHERE [{bang.KhoaChinh}]=@ma", new { ma })
            ?? throw new NgoaiLeNghiepVu("Dữ liệu không tồn tại.", 404);
    }
    public async Task<object> TaoMoi(string taiNguyen, Dictionary<string, JsonElement> duLieu)
    {
        var bang = LayBang(taiNguyen);
        var duLieuChuan = new Dictionary<string, JsonElement>(duLieu, StringComparer.OrdinalIgnoreCase);
        foreach (var (ten, cot) in bang.CotChoPhep)
            if (cot.BatBuoc && (!duLieuChuan.TryGetValue(ten, out var v) || v.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined))
                throw new NgoaiLeNghiepVu($"Thiếu trường bắt buộc: {ten}");
        var (danhSachCot, danhSachThamSo) = TaoThamSo(bang, duLieuChuan);
        if (danhSachCot.Count == 0) throw new NgoaiLeNghiepVu("Thiếu dữ liệu cần tạo.");
        var cotSql = string.Join(',', danhSachCot.Select(x => $"[{x}]"));
        var tenThamSo = string.Join(',', danhSachCot.Select((_, i) => $"@p{i}"));
        await using var sql = ketNoi.TaoKetNoi();
        var ma = await sql.ExecuteScalarAsync<long>($"""
            INSERT dbo.[{bang.TenBang}] ({cotSql}) OUTPUT INSERTED.[{bang.KhoaChinh}]
            VALUES ({tenThamSo})
            """, danhSachThamSo);
        return await ChiTiet(taiNguyen, ma);
    }
    public async Task Sua(string taiNguyen, long ma, Dictionary<string, JsonElement> duLieu)
    {
        var bang = LayBang(taiNguyen);
        var (danhSachCot, thamSo) = TaoThamSo(bang, new Dictionary<string, JsonElement>(duLieu, StringComparer.OrdinalIgnoreCase));
        if (danhSachCot.Count == 0) throw new NgoaiLeNghiepVu("Không có trường nào để cập nhật.");
        thamSo.Add("ma", ma);
        var tapLenh = string.Join(',', danhSachCot.Select((cot, i) => $"[{cot}]=@p{i}"));
        await using var sql = ketNoi.TaoKetNoi();
        var soDong = await sql.ExecuteAsync($"UPDATE dbo.[{bang.TenBang}] SET {tapLenh} WHERE [{bang.KhoaChinh}]=@ma", thamSo);
        if (soDong == 0) throw new NgoaiLeNghiepVu("Không tìm thấy dữ liệu.", 404);
    }
    public async Task Xoa(string taiNguyen, long ma)
    {
        var bang = LayBang(taiNguyen);
        await using var sql = ketNoi.TaoKetNoi();
        int soDong;
        if (bang.CotXoaMem is null)
        {
            // Chỉ thực hiện xóa cứng nếu SQL không có ràng buộc đang tham chiếu.
            soDong = await sql.ExecuteAsync($"DELETE dbo.[{bang.TenBang}] WHERE [{bang.KhoaChinh}]=@ma", new { ma });
        }
        else
            soDong = await sql.ExecuteAsync($"UPDATE dbo.[{bang.TenBang}] SET [{bang.CotXoaMem}]={bang.GiaTriXoaMem} WHERE [{bang.KhoaChinh}]=@ma", new { ma });
        if (soDong == 0) throw new NgoaiLeNghiepVu("Không tìm thấy dữ liệu.", 404);
    }
    public async Task GhiNhatKy(long maNguoiDung, string taiNguyen, string maBanGhi,
        string thaoTac, object? chiTiet = null)
    {
        await using var sql = ketNoi.TaoKetNoi();
        await sql.ExecuteAsync("""
            INSERT dbo.AuditLogs(UserId,EntityName,EntityKey,ActionName,DetailJson)
            VALUES(@maNguoiDung,@taiNguyen,@maBanGhi,@thaoTac,@json)
            """, new { maNguoiDung, taiNguyen, maBanGhi, thaoTac,
                json = chiTiet is null ? null : JsonSerializer.Serialize(chiTiet) });
    }

    private static (List<string>, DynamicParameters) TaoThamSo(DinhNghiaBang bang,
        Dictionary<string, JsonElement> duLieu)
    {
        var cot = new List<string>();
        var thamSo = new DynamicParameters();
        foreach (var (ten, giaTri) in duLieu)
        {
            if (!bang.CotChoPhep.TryGetValue(ten, out var dinhNghia))
                throw new NgoaiLeNghiepVu($"Tên trường không được phép: {ten}");
            thamSo.Add($"p{cot.Count}", ChuyenGiaTri(giaTri, dinhNghia.KieuDuLieu));
            cot.Add(dinhNghia.CotSql);
        }
        return (cot, thamSo);
    }
}
