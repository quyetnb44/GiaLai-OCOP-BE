namespace GiaLaiOCOP.Api.Dtos;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int? EnterpriseId { get; set; }

    // 🔹 Thông tin OCOP và hình ảnh
    public string? ImageUrl { get; set; }
    public int? OCOPRating { get; set; }
    public string StockStatus { get; set; } = "InStock";
    public int StockQuantity { get; set; }
    public double? AverageRating { get; set; }
    public string Status { get; set; } = "PendingApproval";
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedByUserId { get; set; }

    // Thêm property Enterprise để có thể dùng trong DTO
    public EnterpriseDto? Enterprise { get; set; }
}
