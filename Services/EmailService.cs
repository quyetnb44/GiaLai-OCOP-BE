using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GiaLaiOCOP.Api.Services
{
    public interface IEmailService
    {
        Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string purpose = "Register");
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string purpose = "Register")
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["Email:SmtpUsername"];
                var smtpPassword = _configuration["Email:SmtpPassword"];
                var fromEmail = _configuration["Email:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["Email:FromName"] ?? "GiaLai OCOP";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogError("❌ Email configuration is missing! Please configure Email settings in appsettings.json");
                    _logger.LogError("Required: Email:SmtpHost, Email:SmtpUsername, Email:SmtpPassword");
                    return false;
                }

                // Kiểm tra nếu đang dùng placeholder
                if (smtpUsername.Contains("your-email") || smtpPassword.Contains("your-app-password"))
                {
                    _logger.LogError("❌ Email configuration is using placeholder values! Please update with real Gmail credentials.");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("", toEmail));

                // Subject và body tùy theo purpose
                string subject, body;
                switch (purpose.ToLower())
                {
                    case "login":
                        subject = "Mã OTP đăng nhập - GiaLai OCOP";
                        body = $@"
                            <h2>Mã OTP đăng nhập</h2>
                            <p>Xin chào,</p>
                            <p>Mã OTP của bạn là: <strong style='font-size: 24px; color: #007bff;'>{otpCode}</strong></p>
                            <p>Mã này có hiệu lực trong 10 phút.</p>
                            <p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.</p>
                            <p>Trân trọng,<br/>Đội ngũ GiaLai OCOP</p>
                        ";
                        break;
                    case "resetpassword":
                        subject = "Mã OTP đặt lại mật khẩu - GiaLai OCOP";
                        body = $@"
                            <h2>Mã OTP đặt lại mật khẩu</h2>
                            <p>Xin chào,</p>
                            <p>Mã OTP của bạn là: <strong style='font-size: 24px; color: #007bff;'>{otpCode}</strong></p>
                            <p>Mã này có hiệu lực trong 10 phút.</p>
                            <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                            <p>Trân trọng,<br/>Đội ngũ GiaLai OCOP</p>
                        ";
                        break;
                    default: // Register
                        subject = "Xác thực email đăng ký - GiaLai OCOP";
                        body = $@"
                            <h2>Xác thực email đăng ký</h2>
                            <p>Xin chào,</p>
                            <p>Cảm ơn bạn đã đăng ký tài khoản tại GiaLai OCOP!</p>
                            <p>Mã xác thực của bạn là: <strong style='font-size: 24px; color: #007bff;'>{otpCode}</strong></p>
                            <p>Mã này có hiệu lực trong 10 phút.</p>
                            <p>Vui lòng nhập mã này để hoàn tất đăng ký.</p>
                            <p>Trân trọng,<br/>Đội ngũ GiaLai OCOP</p>
                        ";
                        break;
                }

                message.Subject = subject;
                message.Body = new TextPart("html")
                {
                    Text = body
                };

                using (var client = new SmtpClient())
                {
                    // Hỗ trợ cả port 587 (StartTls) và 465 (SSL)
                    SecureSocketOptions socketOptions = smtpPort == 465 
                        ? SecureSocketOptions.SslOnConnect 
                        : SecureSocketOptions.StartTls;

                    await client.ConnectAsync(smtpHost, smtpPort, socketOptions);
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"OTP email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send OTP email to {toEmail}");
                return false;
            }
        }
    }
}

