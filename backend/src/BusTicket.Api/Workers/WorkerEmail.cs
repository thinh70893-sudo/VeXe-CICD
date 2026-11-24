using BusTicket.Api.Data;
using BusTicket.Api.Services;
using Dapper;

namespace BusTicket.Api.Workers;
public sealed class WorkerEmail(IServiceScopeFactory phamVi, ILogger<WorkerEmail> nhatKy) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken dung)
    {
        while (!dung.IsCancellationRequested)
        {
            try
            {
                using var scope = phamVi.CreateScope();
                var sqlFactory = scope.ServiceProvider.GetRequiredService<KetNoiSql>();
                var email = scope.ServiceProvider.GetRequiredService<DichVuEmail>();
                await using var sql = sqlFactory.TaoKetNoi();
                // Atomically claim messages. Expired processing leases can be retried.
                var hangDoi = (await sql.QueryAsync<dynamic>("""
                    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
                    ;WITH s AS (
                        SELECT TOP (5) * FROM dbo.EmailOutbox WITH (UPDLOCK, READPAST, READCOMMITTEDLOCK)
                        WHERE (Status IN ('PENDING','RETRY') AND NextAttemptAtUtc<=SYSUTCDATETIME())
                           OR (Status='PROCESSING' AND LockedUntilUtc<SYSUTCDATETIME())
                        ORDER BY EmailOutboxId
                    )
                    UPDATE s SET Status='PROCESSING',LockedUntilUtc=DATEADD(MINUTE,2,SYSUTCDATETIME())
                    OUTPUT inserted.EmailOutboxId,inserted.AttemptCount
                    """)).ToList();
                foreach (var duLieu in hangDoi)
                {
                    long maEmail = duLieu.EmailOutboxId;
                    int lan = (int)duLieu.AttemptCount + 1;
                    bool thanhCong = false;
                    string? maThu = null;
                    string? loi = null;
                    try { maThu = await email.GuiEmail(maEmail); thanhCong = true; }
                    catch (Exception e) { loi = e.Message; nhatKy.LogWarning(e, "Email {MaEmail} gửi thất bại", maEmail); }
                    int giayCho = Math.Min(3600, (int)Math.Pow(2, lan) * 30);
                    await sql.ExecuteAsync("""
                        BEGIN TRANSACTION;
                        INSERT dbo.EmailDeliveryAttempts(EmailOutboxId,AttemptNo,IsSuccess,ProviderMessageId,ErrorText)
                        VALUES(@maEmail,@lan,@thanhCong,@maThu,@loi);
                        UPDATE dbo.EmailOutbox
                        SET Status=CASE WHEN @thanhCong=1 THEN 'SENT' WHEN @lan>=5 THEN 'DEAD' ELSE 'RETRY' END,
                            AttemptCount=@lan,SentAtUtc=CASE WHEN @thanhCong=1 THEN SYSUTCDATETIME() ELSE NULL END,
                            NextAttemptAtUtc=DATEADD(SECOND,@giayCho,SYSUTCDATETIME()),
                            LockedUntilUtc=NULL,LastError=@loi
                        WHERE EmailOutboxId=@maEmail;
                        COMMIT TRANSACTION;
                        """, new { maEmail, lan, thanhCong, maThu, loi, giayCho });
                }
            }
            catch (Exception loi) { nhatKy.LogError(loi, "Lỗi worker gửi email; sẽ thử lại"); }
            await Task.Delay(TimeSpan.FromSeconds(5), dung);
        }
    }
}
