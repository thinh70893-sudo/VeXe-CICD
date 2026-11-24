namespace BusTicket.Api.Options;

public sealed class CauHinhJwt
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string Key { get; set; } = "";
    public int ExpireMinutes { get; set; } = 120;
}
public sealed class CauHinhDatVe
{
    public int SoGiayGiuGhe { get; set; } = 300;
    public int SoGheToiDa { get; set; } = 6;
}
public sealed class CauHinhSmtp
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string EmailGui { get; set; } = "ve@busticket.local";
    public string TenHienThi { get; set; } = "BusTicket Demo";
}
