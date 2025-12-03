namespace GiaLaiOCOP.Api.Dtos
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = "";
        public DateTime Expires { get; set; }
        public string? Message { get; set; }
        public UserDto? User { get; set; } // Thông tin user (optional để tương thích với code cũ)
    }
}