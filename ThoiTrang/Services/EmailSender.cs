using System.Net;
using System.Net.Mail;

namespace ThoiTrang.Services
{
    public interface IEmailSender
    {
        /// <summary>Gửi email. Trả về true nếu gửi thành công.</summary>
        Task<bool> SendAsync(string toEmail, string subject, string htmlBody);
        bool IsEnabled { get; }
    }

    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public bool IsEnabled => _config.GetValue("Smtp:Enabled", false)
            && !string.IsNullOrWhiteSpace(_config["Smtp:Password"]);

        public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody)
        {
            if (!IsEnabled)
            {
                _logger.LogInformation("SMTP tắt — bỏ qua gửi email tới {Email}", toEmail);
                return false;
            }
            if (string.IsNullOrWhiteSpace(toEmail)) return false;

            try
            {
                var host = _config["Smtp:Host"] ?? "smtp.gmail.com";
                var port = _config.GetValue("Smtp:Port", 587);
                var user = _config["Smtp:User"]!;
                var pass = _config["Smtp:Password"]!;
                var fromName = _config["Smtp:FromName"] ?? "MONO.WEAR";
                var fromEmail = _config["Smtp:FromEmail"] ?? user;

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(user, pass),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };
                await client.SendMailAsync(message);
                _logger.LogInformation("Đã gửi email tới {Email}: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi email tới {Email}", toEmail);
                return false;
            }
        }
    }

    /// <summary>Bọc nội dung text thành email HTML có thương hiệu MONO.WEAR.</summary>
    public static class EmailTemplate
    {
        public static string Wrap(string title, string bodyText)
        {
            var safeBody = System.Net.WebUtility.HtmlEncode(bodyText).Replace("\n", "<br>");
            return $@"
<div style='max-width:600px;margin:0 auto;font-family:Arial,Helvetica,sans-serif;background:#fafafa;padding:0;'>
  <div style='background:#1a1a1a;padding:24px;text-align:center;'>
    <span style='color:#fff;font-size:24px;font-weight:bold;letter-spacing:1px;'>MONO<span style='color:#d4537e;'>.WEAR</span></span>
  </div>
  <div style='background:#fff;padding:32px;'>
    <h2 style='color:#1a1a1a;margin-top:0;font-size:20px;'>{System.Net.WebUtility.HtmlEncode(title)}</h2>
    <div style='color:#444;font-size:15px;line-height:1.7;'>{safeBody}</div>
  </div>
  <div style='padding:20px;text-align:center;color:#999;font-size:12px;'>
    © 2026 MONO.WEAR — Thời trang tối giản<br>
    Email này được gửi tự động, vui lòng không trả lời.
  </div>
</div>";
        }
    }
}
