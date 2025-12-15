using System;

namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO cho tìm kiếm và lọc giao dịch
    /// </summary>
    public class TransactionFilterDto
    {
        public string? SearchTerm { get; set; }         // Tìm theo mã giao dịch/đơn hàng
        public DateTime? StartDate { get; set; }        // Lọc từ ngày
        public DateTime? EndDate { get; set; }          // Lọc đến ngày
        public string? Status { get; set; }             // Lọc theo trạng thái
        public string? PaymentMethod { get; set; }      // Lọc theo phương thức thanh toán
        public string? Type { get; set; }               // Lọc theo loại giao dịch
        public string SortBy { get; set; } = "date_desc"; // Sắp xếp: date_desc, date_asc, amount_desc, amount_asc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
