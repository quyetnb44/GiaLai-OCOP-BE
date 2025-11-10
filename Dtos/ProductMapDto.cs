namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO cho sản phẩm hiển thị trên map
    /// </summary>
    public class ProductMapDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int? OCOPRating { get; set; }
        public string StockStatus { get; set; } = "InStock";
        public double? AverageRating { get; set; }
        public int EnterpriseId { get; set; }
    }
}

