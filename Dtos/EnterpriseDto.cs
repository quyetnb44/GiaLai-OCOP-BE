namespace GiaLaiOCOP.Api.Dtos;

public class EnterpriseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<ProductDto> Products { get; set; } = new List<ProductDto>();
    public List<UserDto> Users { get; set; } = new List<UserDto>();
    public string? Description { get; set; }  // thêm Description
}
