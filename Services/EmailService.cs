using SendGrid;
using SendGrid.Helpers.Mail;
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
                var apiKey = _configuration["Email:SendGridApiKey"];
                var fromEmail = _configuration["Email:FromEmail"];
                var fromName = _configuration["Email:FromName"] ?? "GiaLai OCOP";

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogError("❌ Email configuration is missing! Please configure Email settings in appsettings.json");
                    _logger.LogError("Required: Email:SendGridApiKey, Email:FromEmail");
                    return false;
                }

                // Kiểm tra nếu đang dùng placeholder
                if (apiKey.Contains("your-api-key") || fromEmail.Contains("your-email"))
                {
                    _logger.LogError("❌ Email configuration is using placeholder values! Please update with real SendGrid credentials.");
                    return false;
                }

                var client = new SendGridClient(apiKey);

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

                var msg = new SendGridMessage()
                {
                    From = new EmailAddress(fromEmail, fromName),
                    Subject = subject,
                    HtmlContent = body
                };
                msg.AddTo(new EmailAddress(toEmail));

                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"OTP email sent successfully to {toEmail} from {fromEmail}, API KEY: {apiKey}");
                    return true;
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"Failed to send OTP email to {toEmail}. Status: {response.StatusCode}, Body: {responseBody}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send OTP email to {toEmail}");
                return false;
            }
        }
    }
}

