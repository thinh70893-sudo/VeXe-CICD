using BusTicket.Api.Data;
using BusTicket.Api.Services;
using Dapper;

namespace BusTicket.Api.Workers;
public sealed class WorkerHetHan(IServiceScopeFactory phamVi, ILogger<WorkerHetHan> nhatKy) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken dung)
    {
        while (!dung.IsCancellationRequested)
        {
            try
            {
                using var scope = phamVi.CreateScope();
                var sqlFactory = scope.ServiceProvider.GetRequiredService<KetNoiSql>();
                var redis = scope.ServiceProvider.GetRequiredService<DichVuGiuGhe>();
                await using var sql = sqlFactory.TaoKetNoi();
                // Hết hạn theo transaction: trả lại lượt coupon chưa sử dụng, không đụng ghế SOLD.
                var dsHetHan = (await sql.QueryAsync<Guid?>("""
                    SET XACT_ABORT ON;
                    BEGIN TRANSACTION;
                    DECLARE @HetHan TABLE (BookingId BIGINT, PromotionId INT, HoldId UNIQUEIDENTIFIER);
                    UPDATE dbo.Bookings SET Status='EXPIRED'
                    OUTPUT inserted.BookingId,inserted.PromotionId,inserted.HoldId
                    INTO @HetHan(BookingId,PromotionId,HoldId)
                    WHERE Status='PENDING' AND ExpiresAtUtc<=SYSUTCDATETIME();
                    UPDATE p SET Status='EXPIRED',FailureReason=N'Hết hạn thanh toán'
                    FROM dbo.Payments p JOIN @HetHan e ON e.BookingId=p.BookingId
                    WHERE p.Status='PENDING';
                    ;WITH UuDai AS (
                        SELECT PromotionId,COUNT(*) AS SoLuong
                        FROM @HetHan WHERE PromotionId IS NOT NULL GROUP BY PromotionId
                    )
                    UPDATE p SET UsedCount=CASE WHEN p.UsedCount>u.SoLuong THEN p.UsedCount-u.SoLuong ELSE 0 END
                    FROM dbo.Promotions p JOIN UuDai u ON u.PromotionId=p.PromotionId;
                    SELECT HoldId FROM @HetHan WHERE HoldId IS NOT NULL;
                    COMMIT TRANSACTION;
                    """)).ToList();
                foreach (var maGiu in dsHetHan)
                    try { await redis.BoGiuTheoMa(maGiu); }
                    catch (Exception loi) { nhatKy.LogWarning(loi, "Không giải phóng được Redis hold {MaGiu}", maGiu); }
            }
            catch (Exception loi) { nhatKy.LogError(loi, "Lỗi kiểm tra booking hết hạn"); }
            await Task.Delay(TimeSpan.FromSeconds(15), dung);
        }
    }
}
