using System;
using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class InventoryHistoryDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int EnterpriseId { get; set; }
        public string EnterpriseName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal PreviousQuantity { get; set; }
        public decimal NewQuantity { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public string? CreatedByName { get; set; }
    }

    public class AdjustInventoryDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [RegularExpression("^(import|export|adjustment)$", ErrorMessage = "Type chỉ chấp nhận: import, export, adjustment.")]
        public string Type { get; set; } = "adjustment";

        [Range(-1000000.0, 1000000.0)]
        public decimal Quantity { get; set; }

        public string? Reason { get; set; }
        public decimal LowStockThreshold { get; set; } = 10;
    }
}

