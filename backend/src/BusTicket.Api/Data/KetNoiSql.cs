using Microsoft.Data.SqlClient;
namespace BusTicket.Api.Data;
public sealed class KetNoiSql(IConfiguration cauHinh)
{
    public SqlConnection TaoKetNoi() => new(cauHinh.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Thiếu ConnectionStrings:SqlServer"));
}
