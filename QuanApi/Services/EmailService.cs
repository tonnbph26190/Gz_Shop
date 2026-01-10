using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;
using QuanApi.Data;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;
using QuanApi.Models;
using Microsoft.Extensions.Logging;

namespace QuanApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly BanQuanAu1DbContext _context;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, BanQuanAu1DbContext context, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        public async Task SendOrderStatusChangeEmailAsync(HoaDon hoaDon, string oldStatus, string newStatus)
        {
            try
            {
                // Load đầy đủ thông tin đơn hàng
                var fullOrder = await _context.HoaDons
                    .Include(h => h.KhachHang)
                    .Include(h => h.ChiTietHoaDons)
                        .ThenInclude(ct => ct.SanPhamChiTiet)
                            .ThenInclude(sp => sp.SanPham)
                    .FirstOrDefaultAsync(h => h.IDHoaDon == hoaDon.IDHoaDon);

                if (fullOrder?.KhachHang?.Email == null)
                {
                    _logger.LogWarning($"Không thể gửi email: Khách hàng không có email cho đơn hàng {hoaDon.MaHoaDon}");
                    return;
                }

                var subject = GetEmailSubject(newStatus, hoaDon.MaHoaDon);
                var body = GenerateStatusChangeEmailBody(fullOrder, oldStatus, newStatus);

                await SendEmailAsync(fullOrder.KhachHang.Email, subject, body);
                
                _logger.LogInformation($"Đã gửi email thông báo thay đổi trạng thái từ '{oldStatus}' sang '{newStatus}' cho đơn hàng {hoaDon.MaHoaDon}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email thông báo trạng thái cho đơn hàng {hoaDon.MaHoaDon}: {ex.Message}");
            }
        }

        public async Task SendOrderCancellationEmailAsync(HoaDon hoaDon, string reason)
        {
            try
            {
                var fullOrder = await _context.HoaDons
                    .Include(h => h.KhachHang)
                    .Include(h => h.ChiTietHoaDons)
                        .ThenInclude(ct => ct.SanPhamChiTiet)
                            .ThenInclude(sp => sp.SanPham)
                    .FirstOrDefaultAsync(h => h.IDHoaDon == hoaDon.IDHoaDon);

                if (fullOrder?.KhachHang?.Email == null) return;

                var subject = $"Thông báo hủy đơn hàng #{hoaDon.MaHoaDon}";
                var body = GenerateCancellationEmailBody(fullOrder, reason);

                await SendEmailAsync(fullOrder.KhachHang.Email, subject, body);
                
                _logger.LogInformation($"Đã gửi email thông báo hủy đơn hàng {hoaDon.MaHoaDon}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email hủy đơn cho đơn hàng {hoaDon.MaHoaDon}: {ex.Message}");
                throw new ApplicationException($"Lỗi khi gửi email hủy đơn: {ex.Message}", ex);
            }
        }


        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromPassword = _configuration["EmailSettings:FromPassword"];
                var fromName = _configuration["EmailSettings:FromName"];

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email đã gửi thành công tới {toEmail} với chủ đề: {subject}");
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, $"Lỗi SMTP khi gửi email tới {toEmail}. Mã lỗi: {smtpEx.StatusCode}. Chi tiết: {smtpEx.Message}");
                throw new ApplicationException($"Lỗi cấu hình hoặc kết nối SMTP: {smtpEx.Message}", smtpEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi không xác định khi gửi email tới {toEmail}. Chi tiết: {ex.Message}");
                throw new ApplicationException($"Lỗi khi gửi email: {ex.Message}", ex);
            }
        }

        private string GetEmailSubject(string status, string orderCode)
        {
            return status switch
            {
                "Đã xác nhận" => $"Đơn hàng #{orderCode} đã được xác nhận",
                "Đang chuẩn bị" => $"Đơn hàng #{orderCode} đang được chuẩn bị",
                "Đang giao hàng" => $"Đơn hàng #{orderCode} đang được giao",
                "Đã giao hàng" => $"Đơn hàng #{orderCode} đã được giao thành công",
                "Đã hủy" => $"Đơn hàng #{orderCode} đã bị hủy",
                _ => $"Cập nhật trạng thái đơn hàng #{orderCode}"
            };
        }

        private string GenerateStatusChangeEmailBody(HoaDon hoaDon, string oldStatus, string newStatus)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='UTF-8'></head><body>");
            sb.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>");
            
            // Header
            sb.AppendLine("<div style='background-color: #007bff; color: white; padding: 20px; text-align: center;'>");
            sb.AppendLine("<h1>Cập nhật trạng thái đơn hàng</h1>");
            sb.AppendLine("</div>");
            
            // Content
            sb.AppendLine("<div style='padding: 20px;'>");
            sb.AppendLine($"<p>Xin chào <strong>{hoaDon.KhachHang?.TenKhachHang ?? hoaDon.TenNguoiNhan}</strong>,</p>");
            sb.AppendLine($"<p>Đơn hàng <strong>#{hoaDon.MaHoaDon}</strong> của bạn đã được cập nhật trạng thái:</p>");
            
            // Status change
            sb.AppendLine("<div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>");
            sb.AppendLine($"<p><strong>Trạng thái cũ:</strong> <span style='color: #6c757d;'>{oldStatus}</span></p>");
            sb.AppendLine($"<p><strong>Trạng thái mới:</strong> <span style='color: #28a745; font-weight: bold;'>{newStatus}</span></p>");
            sb.AppendLine($"<p><strong>Thời gian cập nhật:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
            sb.AppendLine("</div>");

            // Order details
            sb.AppendLine("<h3>Chi tiết đơn hàng:</h3>");
            sb.AppendLine("<table style='width: 100%; border-collapse: collapse;'>");
            sb.AppendLine("<tr style='background-color: #f8f9fa;'>");
            sb.AppendLine("<th style='border: 1px solid #dee2e6; padding: 8px; text-align: left;'>Sản phẩm</th>");
            sb.AppendLine("<th style='border: 1px solid #dee2e6; padding: 8px; text-align: center;'>Số lượng</th>");
            sb.AppendLine("<th style='border: 1px solid #dee2e6; padding: 8px; text-align: right;'>Đơn giá</th>");
            sb.AppendLine("<th style='border: 1px solid #dee2e6; padding: 8px; text-align: right;'>Thành tiền</th>");
            sb.AppendLine("</tr>");

            foreach (var item in hoaDon.ChiTietHoaDons ?? new List<ChiTietHoaDon>())
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td style='border: 1px solid #dee2e6; padding: 8px;'>{item.SanPhamChiTiet?.SanPham?.TenSanPham}</td>");
                sb.AppendLine($"<td style='border: 1px solid #dee2e6; padding: 8px; text-align: center;'>{item.SoLuong}</td>");
                sb.AppendLine($"<td style='border: 1px solid #dee2e6; padding: 8px; text-align: right;'>{item.DonGia:N0} VNĐ</td>");
                sb.AppendLine($"<td style='border: 1px solid #dee2e6; padding: 8px; text-align: right;'>{item.ThanhTien:N0} VNĐ</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine($"<p style='text-align: right; font-weight: bold; font-size: 16px; margin-top: 10px;'>Tổng tiền: {hoaDon.TongTien:N0} VNĐ</p>");

            // Status-specific messages
            sb.AppendLine(GetStatusSpecificMessage(newStatus));

            sb.AppendLine("<p>Cảm ơn bạn đã mua sắm tại cửa hàng của chúng tôi!</p>");
            sb.AppendLine("</div>");
            
            // Footer
            sb.AppendLine("<div style='background-color: #f8f9fa; padding: 15px; text-align: center; color: #6c757d;'>");
            sb.AppendLine("<p>Đây là email tự động, vui lòng không trả lời email này.</p>");
            sb.AppendLine("<p>Nếu có thắc mắc, vui lòng liên hệ: support@example.com | 0123-456-789</p>");
            sb.AppendLine("</div>");
            
            sb.AppendLine("</div></body></html>");
            
            return sb.ToString();
        }

        private string GenerateCancellationEmailBody(HoaDon hoaDon, string reason)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='UTF-8'></head><body>");
            sb.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>");
            
            // Header
            sb.AppendLine("<div style='background-color: #dc3545; color: white; padding: 20px; text-align: center;'>");
            sb.AppendLine("<h1>Thông báo hủy đơn hàng</h1>");
            sb.AppendLine("</div>");
            
            // Content
            sb.AppendLine("<div style='padding: 20px;'>");
            sb.AppendLine($"<p>Xin chào <strong>{hoaDon.KhachHang?.TenKhachHang ?? hoaDon.TenNguoiNhan}</strong>,</p>");
            sb.AppendLine($"<p>Chúng tôi rất tiếc phải thông báo rằng đơn hàng <strong>#{hoaDon.MaHoaDon}</strong> của bạn đã bị hủy.</p>");
            
            if (!string.IsNullOrEmpty(reason))
            {
                sb.AppendLine("<div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0;'>");
                sb.AppendLine($"<p><strong>Lý do hủy:</strong> {reason}</p>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine($"<p><strong>Thời gian hủy:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
            sb.AppendLine($"<p><strong>Tổng tiền đơn hàng:</strong> {hoaDon.TongTien:N0} VNĐ</p>");
            
            sb.AppendLine("<p>Nếu bạn đã thanh toán, chúng tôi sẽ hoàn tiền trong vòng 3-7 ngày làm việc.</p>");
            sb.AppendLine("<p>Chúng tôi xin lỗi vì sự bất tiện này và hy vọng được phục vụ bạn trong tương lai.</p>");
            sb.AppendLine("</div>");
            
            // Footer
            sb.AppendLine("<div style='background-color: #f8f9fa; padding: 15px; text-align: center; color: #6c757d;'>");
            sb.AppendLine("<p>Nếu có thắc mắc, vui lòng liên hệ: support@example.com | 0123-456-789</p>");
            sb.AppendLine("</div>");
            
            sb.AppendLine("</div></body></html>");
            
            return sb.ToString();
        }

        private string GetStatusSpecificMessage(string status)
        {
            return status switch
            {
                "Đã xác nhận" => "<div style='background-color: #d4edda; border: 1px solid #c3e6cb; padding: 10px; border-radius: 5px; margin: 15px 0;'><p><strong>Đơn hàng của bạn đã được xác nhận!</strong> Chúng tôi sẽ chuẩn bị và giao hàng sớm nhất có thể.</p></div>",
                "Đang chuẩn bị" => "<div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 5px; margin: 15px 0;'><p><strong>Đơn hàng đang được chuẩn bị!</strong> Chúng tôi sẽ thông báo khi đơn hàng được giao cho đơn vị vận chuyển.</p></div>",
                "Đang giao hàng" => "<div style='background-color: #cce5ff; border: 1px solid #99d6ff; padding: 10px; border-radius: 5px; margin: 15px 0;'><p><strong>Đơn hàng đang trên đường giao đến bạn!</strong> Vui lòng chú ý điện thoại để nhận hàng.</p></div>",
                "Đã giao hàng" => "<div style='background-color: #d4edda; border: 1px solid #c3e6cb; padding: 10px; border-radius: 5px; margin: 15px 0;'><p><strong>Đơn hàng đã được giao thành công!</strong> Cảm ơn bạn đã mua sắm tại cửa hàng chúng tôi.</p></div>",
                _ => ""
            };
        }

        Task IEmailService.SendEmailAsync(string toEmail, string subject, string body)
        {
            return SendEmailAsync(toEmail, subject, body);
        }
    }
}