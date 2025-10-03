namespace GiaLaiOCOP.Api.Dtos
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = "";
        public DateTime Expires { get; set; }
    }
}