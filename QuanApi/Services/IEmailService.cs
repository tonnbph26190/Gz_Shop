using System.Threading.Tasks;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IEmailService
    {
        Task SendOrderStatusChangeEmailAsync(HoaDon hoaDon, string oldStatus, string newStatus, string? fallbackEmail = null);
        Task SendOrderCancellationEmailAsync(HoaDon hoaDon, string reason);
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
