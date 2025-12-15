using System;

namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO cho thông tin thanh toán trong chi tiết giao dịch
    /// </summary>
    public class PaymentInfoDto
    {
        public string Method { get; set; } = string.Empty;        // COD, BankTransfer, Wallet
        public string Status { get; set; } = string.Empty;        // Paid, Pending, AwaitingTransfer, Cancelled
        public string Reference { get; set; } = string.Empty;     // Mã tham chiếu
        public string? MaskedBankAccount { get; set; }            // Số TK ẩn: **** **** 1234
        public string? BankName { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
