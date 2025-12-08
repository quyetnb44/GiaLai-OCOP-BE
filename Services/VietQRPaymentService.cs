using GiaLaiOCOP.Api.Options;
using Microsoft.Extensions.Options;

namespace GiaLaiOCOP.Api.Services
{
    public class VietQRPaymentService : IVietQRPaymentService
    {
        private readonly BankTransferSettings _settings;

        public VietQRPaymentService(IOptions<BankTransferSettings> settings)
        {
            _settings = settings.Value;
        }

        public string GeneratePaymentQrCodeUrl(decimal amount, string description, string? reference = null)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_settings.BaseUrl)
                ? "https://img.vietqr.io/image"
                : _settings.BaseUrl.TrimEnd('/');

            var template = string.IsNullOrWhiteSpace(_settings.Template) ? "compact" : _settings.Template;
            
            // Tạo URL QR code theo chuẩn VietQR.io
            // Format: {BaseUrl}/{BankCode}-{AccountNumber}-{Template}.png?amount={Amount}&addInfo={Description}
            
            var amountParam = amount > 0 ? $"&amount={(int)amount}" : string.Empty;
            var addInfo = !string.IsNullOrWhiteSpace(reference) 
                ? Uri.EscapeDataString($"{description} - {reference}")
                : Uri.EscapeDataString(description ?? string.Empty);

            var qrUrl = $"{baseUrl}/{_settings.BankCode}-{_settings.AccountNumber}-{template}.png?addInfo={addInfo}{amountParam}";

            return qrUrl;
        }

        public string GenerateQrCodeUrlForBankAccount(string bankCode, string accountNumber, decimal? amount = null, string? description = null)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_settings.BaseUrl)
                ? "https://img.vietqr.io/image"
                : _settings.BaseUrl.TrimEnd('/');

            var template = string.IsNullOrWhiteSpace(_settings.Template) ? "compact" : _settings.Template;

            var amountParam = amount.HasValue && amount.Value > 0 
                ? $"&amount={(int)amount.Value}" 
                : string.Empty;
            
            var addInfo = !string.IsNullOrWhiteSpace(description)
                ? $"&addInfo={Uri.EscapeDataString(description)}"
                : string.Empty;

            var qrUrl = $"{baseUrl}/{bankCode}-{accountNumber}-{template}.png{addInfo}{amountParam}";

            return qrUrl;
        }
    }
}

