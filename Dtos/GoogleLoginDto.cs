using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class GoogleLoginDto
    {
        [Required(ErrorMessage = "Google id_token là bắt buộc.")]
        public string IdToken { get; set; } = string.Empty;
    }
}


