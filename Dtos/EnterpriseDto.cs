namespace GiaLaiOCOP.Api.Dtos;

public class EnterpriseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ProductDto> Products { get; set; } = new List<ProductDto>();
    public List<UserDto> Users { get; set; } = new List<UserDto>();
    
    // 🔹 Thông tin địa chỉ và tọa độ
    public string Address { get; set; } = "";
    public string Ward { get; set; } = "";
    public string District { get; set; } = "";
    public string Province { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // 🔹 Thông tin liên hệ
    public string PhoneNumber { get; set; } = "";
    public string EmailContact { get; set; } = "";
    public string Website { get; set; } = "";
    
    // 🔹 Thông tin OCOP
    public int? OCOPRating { get; set; }
    public string BusinessField { get; set; } = "";
    public string? ImageUrl { get; set; }
    public double? AverageRating { get; set; }
}
