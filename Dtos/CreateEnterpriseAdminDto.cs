namespace GiaLaiOCOP.Api.Dtos
{
    public class CreateEnterpriseAdminDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int EnterpriseId { get; set; }
    }
}