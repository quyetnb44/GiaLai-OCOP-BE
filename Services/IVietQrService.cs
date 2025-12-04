namespace GiaLaiOCOP.Api.Services
{
    public interface IVietQrService
    {
        /// <summary>
        /// Tạo QR code base64 chỉ chứa thông tin tài khoản (không có amount, description)
        /// QR này được lưu một lần và tái sử dụng
        /// </summary>
        string GenerateAccountQrCodeBase64(string bankCode, string bankAccount, string accountName);

        /// <summary>
        /// Tạo QR code base64 với đầy đủ thông tin thanh toán (có amount và description)
        /// QR này được tạo động cho mỗi giao dịch
        /// </summary>
        string GeneratePaymentQrCodeBase64(string bankCode, string bankAccount, string accountName, decimal amount, string description);
    }
}

