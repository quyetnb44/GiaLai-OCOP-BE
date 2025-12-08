namespace GiaLaiOCOP.Api.Services
{
    public interface IVietQRPaymentService
    {
        string GeneratePaymentQrCodeUrl(decimal amount, string description, string? reference = null);
        string GenerateQrCodeUrlForBankAccount(string bankCode, string accountNumber, decimal? amount = null, string? description = null);
    }
}

