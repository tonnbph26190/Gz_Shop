using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using QuanView.Models;
using System.Threading.Tasks;

public interface IEmailService
{
    Task SendVoucherEmailAsync(string toEmail, string tenKhachHang, string tenVoucher);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailConfig _config;

    public EmailService(ILogger<EmailService> logger, EmailConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task SendVoucherEmailAsync(string toEmail, string tenKhachHang, string tenVoucher)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_config.SenderName, _config.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "🎉 Bạn nhận được mã giảm giá từ cửa hàng!";

        message.Body = new TextPart("html")
        {
            Text = $@"
                <p>Xin chào <strong>{tenKhachHang}</strong>,</p>
                <p>🎉 Chúc mừng bạn đã nhận được một <strong>voucher</strong> từ cửa hàng quần áo của chúng tôi:</p>
                <p><strong>{tenVoucher}</strong></p>
                <p>Hãy nhanh tay sử dụng trước khi hết hạn nhé!</p>
                <p>Trân trọng,<br/>Đội ngũ cửa hàng</p>"
        };

        try
        {
            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync(_config.SmtpServer, _config.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config.Username, _config.Password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation($"📧 Email đã gửi tới {toEmail}");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, $"❌ Gửi email thất bại tới {toEmail}");
        }
    }
}
