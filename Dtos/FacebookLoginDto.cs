using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class FacebookLoginDto
    {
        [Required(ErrorMessage = "Facebook access_token là bắt buộc.")]
        public string AccessToken { get; set; } = string.Empty;
    }
}


