using QRCoder;
using System.Text;

namespace GiaLaiOCOP.Api.Services
{
    public class VietQrService : IVietQrService
    {
        private readonly ILogger<VietQrService> _logger;

        public VietQrService(ILogger<VietQrService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Tạo QR code base64 chỉ chứa thông tin tài khoản (theo chuẩn VietQR)
        /// Format: https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact.png
        /// </summary>
        public string GenerateAccountQrCodeBase64(string bankCode, string bankAccount, string accountName)
        {
            try
            {
                // Tạo EMVCo string cho VietQR (chỉ thông tin tài khoản, không có amount)
                var emvcoString = BuildEmvcoString(bankCode, bankAccount, accountName, null, null);

                // Generate QR code từ EMVCo string
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(emvcoString, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);

                // Convert sang base64
                return Convert.ToBase64String(qrCodeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating account QR code for BankCode: {BankCode}, Account: {Account}", bankCode, bankAccount);
                throw;
            }
        }

        /// <summary>
        /// Tạo QR code base64 với đầy đủ thông tin thanh toán (theo chuẩn VietQR/EMVCo)
        /// </summary>
        public string GeneratePaymentQrCodeBase64(string bankCode, string bankAccount, string accountName, decimal amount, string description)
        {
            try
            {
                // Tạo EMVCo string cho VietQR với amount và description
                var emvcoString = BuildEmvcoString(bankCode, bankAccount, accountName, amount, description);

                // Generate QR code từ EMVCo string
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(emvcoString, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);

                // Convert sang base64
                return Convert.ToBase64String(qrCodeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payment QR code for BankCode: {BankCode}, Account: {Account}, Amount: {Amount}", 
                    bankCode, bankAccount, amount);
                throw;
            }
        }

        /// <summary>
        /// Xây dựng EMVCo string theo chuẩn VietQR
        /// Format theo EMVCo Merchant-Presented QR Code Specification
        /// </summary>
        private string BuildEmvcoString(string bankCode, string bankAccount, string accountName, decimal? amount, string? description)
        {
            var sb = new StringBuilder();

            // 00: Payload Format Indicator (Fixed: "01")
            sb.Append("000201");

            // 01: Point of Initiation Method
            // "11" = Static (không có amount), "12" = Dynamic (có amount)
            sb.Append(amount.HasValue ? "010212" : "010211");

            // 38: Merchant Account Information (VietQR)
            var merchantInfo = BuildMerchantAccountInfo(bankCode, bankAccount);
            sb.Append($"38{merchantInfo.Length:D2}{merchantInfo}");

            // 52: Merchant Category Code (Fixed: "0000" = Not specified)
            sb.Append("52040000");

            // 53: Transaction Currency (VND = 704)
            sb.Append("5303704");

            // 54: Transaction Amount (chỉ có khi amount != null)
            if (amount.HasValue)
            {
                var amountStr = ((int)amount.Value).ToString();
                sb.Append($"54{amountStr.Length:D2}{amountStr}");
            }

            // 58: Country Code (VN)
            sb.Append("5802VN");

            // 59: Merchant Name
            var merchantName = accountName.Length > 25 ? accountName.Substring(0, 25) : accountName;
            sb.Append($"59{merchantName.Length:D2}{merchantName}");

            // 60: Merchant City
            sb.Append("6002VN");

            // 62: Additional Data Field Template (chứa description nếu có)
            if (!string.IsNullOrWhiteSpace(description))
            {
                var addInfo = BuildAdditionalData(description);
                sb.Append($"62{addInfo.Length:D2}{addInfo}");
            }

            // CRC (Cyclic Redundancy Check) - 4 digits
            // Tính CRC cho string hiện tại, sau đó append "6304" + CRC
            var dataString = sb.ToString();
            var crc = CalculateCRC(dataString + "6304");
            sb.Append($"6304{crc}");

            return sb.ToString();
        }

        /// <summary>
        /// Xây dựng Merchant Account Information theo chuẩn VietQR
        /// Format: 00 (Guid) + 01 (Bank Code) + 02 (Account Number)
        /// </summary>
        private string BuildMerchantAccountInfo(string bankCode, string bankAccount)
        {
            var sb = new StringBuilder();

            // 00: GUID (VietQR: "A000000775")
            sb.Append("00" + "10" + "A000000775");

            // 01: Bank Code
            sb.Append($"01{bankCode.Length:D2}{bankCode}");

            // 02: Account Number
            sb.Append($"02{bankAccount.Length:D2}{bankAccount}");

            return sb.ToString();
        }

        /// <summary>
        /// Xây dựng Additional Data Field Template
        /// Format: 08 (Description)
        /// </summary>
        private string BuildAdditionalData(string description)
        {
            // 08: Description
            var desc = description.Length > 25 ? description.Substring(0, 25) : description;
            return $"08{desc.Length:D2}{desc}";
        }

        /// <summary>
        /// Tính CRC16-CCITT cho EMVCo string (theo chuẩn ISO/IEC 13239)
        /// </summary>
        private string CalculateCRC(string data)
        {
            const ushort polynomial = 0x1021;
            ushort crc = 0xFFFF;
            byte[] bytes = Encoding.UTF8.GetBytes(data);

            foreach (byte b in bytes)
            {
                for (int i = 0; i < 8; i++)
                {
                    bool bit = ((b >> (7 - i) & 1) == 1);
                    bool c15 = ((crc >> 15 & 1) == 1);
                    crc <<= 1;
                    if (c15 ^ bit)
                        crc ^= polynomial;
                }
            }

            crc ^= 0xFFFF;
            return crc.ToString("X4");
        }
    }
}

