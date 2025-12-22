using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    /// <summary>
    /// Track trạng thái xác nhận của từng Enterprise trong một đơn hàng
    /// Khi một đơn hàng có sản phẩm từ nhiều enterprise, mỗi enterprise có thể xác nhận độc lập
    /// </summary>
    public class OrderEnterpriseStatus
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [JsonIgnore]
        public Order Order { get; set; } = null!;

        public int EnterpriseId { get; set; }

        [JsonIgnore]
        public Enterprise Enterprise { get; set; } = null!;

        /// <summary>
        /// Trạng thái của enterprise này trong đơn hàng: Pending, Processing, Shipped, Completed
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Thời điểm enterprise này cập nhật trạng thái lần cuối
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// UserId của EnterpriseAdmin đã cập nhật trạng thái này
        /// </summary>
        public int? UpdatedBy { get; set; }

        /// <summary>
        /// Ghi chú từ enterprise (nếu có)
        /// </summary>
        public string? Notes { get; set; }
    }
}






