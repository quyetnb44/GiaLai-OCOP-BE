using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class CreatePaymentRequestDto
    {
        [Required(ErrorMessage = "OrderId là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "OrderId phải lớn hơn 0.")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Method là bắt buộc.")]
        [RegularExpression("^(COD|BankTransfer)$", ErrorMessage = "Method chỉ chấp nhận: COD hoặc BankTransfer.")]
        public string Method { get; set; } = "COD"; // COD, BankTransfer
    }
}

