using System.ComponentModel.DataAnnotations;
namespace BusTicket.Api.DTOs;
public sealed record WebhookSandboxDto(
    [Required] string MaSuKien, [Required] string MaYeuCau,
    [Required] string MaGiaoDich, decimal SoTien, [Required] string TrangThai);
