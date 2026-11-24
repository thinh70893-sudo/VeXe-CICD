using Dapper;
using System.Security.Claims;
using System.Text;
using BusTicket.Api.Data;
using BusTicket.Api.Infrastructure;
using BusTicket.Api.Options;
using BusTicket.Api.Services;
using BusTicket.Api.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var xayDung = WebApplication.CreateBuilder(args);
xayDung.Services.AddControllers().AddJsonOptions(tuyChon =>
{
    tuyChon.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    // DapperRow được serialize như Dictionary nên cần quy tắc đặt tên khóa riêng.
    tuyChon.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
xayDung.Services.AddOpenApi();
xayDung.Services.Configure<CauHinhJwt>(xayDung.Configuration.GetSection("Jwt"));
xayDung.Services.Configure<CauHinhDatVe>(xayDung.Configuration.GetSection("DatVe"));
xayDung.Services.Configure<CauHinhSmtp>(xayDung.Configuration.GetSection("Smtp"));
var khoaJwt = xayDung.Configuration["Jwt:Key"] ?? "";
if (Encoding.UTF8.GetByteCount(khoaJwt) < 32)
    throw new InvalidOperationException("Phải cấu hình Jwt:Key có ít nhất 32 byte bằng user-secrets / environment variables.");
xayDung.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(tuyChon =>
    {
        tuyChon.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = xayDung.Configuration["Jwt:Issuer"],
            ValidAudience = xayDung.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(khoaJwt)),
            ClockSkew = TimeSpan.FromSeconds(15)
        };
        tuyChon.Events = new JwtBearerEvents
        {
            OnTokenValidated = async nguCanh =>
            {
                var ma = nguCanh.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var vaiTro = nguCanh.Principal?.FindFirstValue(ClaimTypes.Role);
                if (!long.TryParse(ma, out var maNguoiDung)) { nguCanh.Fail("Token thiếu UserId."); return; }
                var sqlFactory = nguCanh.HttpContext.RequestServices.GetRequiredService<KetNoiSql>();
                await using var sql = sqlFactory.TaoKetNoi();
                var dangHoatDong = await sql.ExecuteScalarAsync<int>("""
                    SELECT COUNT(*) FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId
                    WHERE u.UserId=@maNguoiDung AND u.IsActive=1 AND r.RoleCode=@vaiTro
                    """, new { maNguoiDung, vaiTro });
                if (dangHoatDong != 1) nguCanh.Fail("Tài khoản đã bị vô hiệu hóa hoặc đổi vai trò.");
            }
        };
    });
xayDung.Services.AddAuthorization();
xayDung.Services.AddCors(tuyChon => tuyChon.AddPolicy("ReactLocal", chinhSach =>
    chinhSach.WithOrigins(xayDung.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
        .AllowAnyHeader().AllowAnyMethod()));
xayDung.Services.AddSingleton<KetNoiSql>();
xayDung.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(
    xayDung.Configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false"));
xayDung.Services.AddScoped<DichVuXacThuc>();
xayDung.Services.AddScoped<DichVuGiuGhe>();
xayDung.Services.AddScoped<DichVuDatVe>();
xayDung.Services.AddScoped<DichVuThanhToan>();
xayDung.Services.AddScoped<DichVuVe>();
xayDung.Services.AddScoped<DichVuEmail>();
xayDung.Services.AddScoped<DichVuQuanTri>();
xayDung.Services.AddScoped<DichVuViHoanVe>();
xayDung.Services.AddHostedService<WorkerEmail>();
xayDung.Services.AddHostedService<WorkerHetHan>();
var ungDung = xayDung.Build();
ungDung.UseMiddleware<XuLyNgoaiLeMiddleware>();
if (ungDung.Environment.IsDevelopment()) ungDung.MapOpenApi();
ungDung.UseCors("ReactLocal");
ungDung.UseAuthentication();
ungDung.UseAuthorization();
ungDung.MapControllers();
ungDung.MapGet("/health", () => Results.Ok(new { trangThai = "OK", thoiGianUtc = DateTime.UtcNow }));
ungDung.Run();

