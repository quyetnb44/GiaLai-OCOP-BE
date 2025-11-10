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
    public double? AverageRating { get; set; }

    // Thêm property Enterprise để có thể dùng trong DTO
    public EnterpriseDto? Enterprise { get; set; }
}
