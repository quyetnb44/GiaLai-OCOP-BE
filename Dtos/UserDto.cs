namespace GiaLaiOCOP.Api.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";            // thêm Role
    public int? EnterpriseId { get; set; }            // thêm EnterpriseId (nullable)
    public EnterpriseDto? Enterprise { get; set; }    // thêm Enterprise (nullable)
}
