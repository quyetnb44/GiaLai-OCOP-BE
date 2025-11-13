using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class EnterpriseApplicationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        // 🏢 Thông tin doanh nghiệp
        public string EnterpriseName { get; set; } = string.Empty;
        public string BusinessType { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string BusinessLicenseNumber { get; set; } = string.Empty;
        public DateTime? LicenseIssuedDate { get; set; }
        public string LicenseIssuedBy { get; set; } = string.Empty;

        // 📍 Địa chỉ & liên hệ
        public string Address { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string EmailContact { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;

        // 👤 Người đại diện
        public string RepresentativeName { get; set; } = string.Empty;
        public string RepresentativePosition { get; set; } = string.Empty;
        public string RepresentativeIdNumber { get; set; } = string.Empty;
        public DateTime? RepresentativeIdIssuedDate { get; set; }
        public string RepresentativeIdIssuedBy { get; set; } = string.Empty;

        // ⚙️ Sản xuất & quy mô
        public string ProductionLocation { get; set; } = string.Empty;
        public string NumberOfEmployees { get; set; } = string.Empty;
        public string ProductionScale { get; set; } = string.Empty;
        public string BusinessField { get; set; } = string.Empty;

        // 🍶 Sản phẩm đăng ký OCOP
        public string ProductName { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public string ProductOrigin { get; set; } = string.Empty;
        public string ProductCertifications { get; set; } = string.Empty;
        public string ProductImages { get; set; } = string.Empty;

        // 📎 Hồ sơ kèm theo
        public string AttachedDocuments { get; set; } = string.Empty;
        public string AdditionalNotes { get; set; } = string.Empty;

        // ⚖️ Trạng thái & thời gian
        public string Status { get; set; } = "Pending";
        public string? AdminComment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateEnterpriseApplicationDto
    {
        // 🏢 Thông tin doanh nghiệp
        [Required(ErrorMessage = "Tên doanh nghiệp là bắt buộc.")]
        [MaxLength(255)]
        public string EnterpriseName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại hình doanh nghiệp là bắt buộc.")]
        [MaxLength(100)]
        public string BusinessType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã số thuế là bắt buộc.")]
        [MaxLength(50)]
        public string TaxCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số giấy phép kinh doanh là bắt buộc.")]
        [MaxLength(100)]
        public string BusinessLicenseNumber { get; set; } = string.Empty;

        public DateTime? LicenseIssuedDate { get; set; }
        [MaxLength(255)]
        public string LicenseIssuedBy { get; set; } = string.Empty;

        // 📍 Địa chỉ & liên hệ
        [Required(ErrorMessage = "Địa chỉ là bắt buộc.")]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        public string Ward { get; set; } = string.Empty;
        [Required(ErrorMessage = "Quận/Huyện là bắt buộc.")]
        [MaxLength(100)]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tỉnh/Thành phố là bắt buộc.")]
        [MaxLength(100)]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [MaxLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email liên hệ là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email liên hệ không hợp lệ.")]
        [MaxLength(255)]
        public string EmailContact { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Website { get; set; } = string.Empty;

        // 👤 Người đại diện
        [Required(ErrorMessage = "Tên người đại diện là bắt buộc.")]
        [MaxLength(255)]
        public string RepresentativeName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string RepresentativePosition { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số CCCD/CMND của người đại diện là bắt buộc.")]
        [MaxLength(50)]
        public string RepresentativeIdNumber { get; set; } = string.Empty;

        public DateTime? RepresentativeIdIssuedDate { get; set; }
        [MaxLength(255)]
        public string RepresentativeIdIssuedBy { get; set; } = string.Empty;

        // ⚙️ Sản xuất & quy mô
        [MaxLength(500)]
        public string ProductionLocation { get; set; } = string.Empty;

        [MaxLength(100)]
        public string NumberOfEmployees { get; set; } = string.Empty;

        [MaxLength(255)]
        public string ProductionScale { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngành nghề kinh doanh là bắt buộc.")]
        [MaxLength(255)]
        public string BusinessField { get; set; } = string.Empty;

        // 🍶 Sản phẩm OCOP
        [Required(ErrorMessage = "Tên sản phẩm OCOP là bắt buộc.")]
        [MaxLength(255)]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhóm sản phẩm là bắt buộc.")]
        [MaxLength(255)]
        public string ProductCategory { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả sản phẩm là bắt buộc.")]
        [MaxLength(2000)]
        public string ProductDescription { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ProductOrigin { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string ProductCertifications { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string ProductImages { get; set; } = string.Empty;

        // 📎 Hồ sơ kèm theo
        [MaxLength(2000)]
        public string AttachedDocuments { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string AdditionalNotes { get; set; } = string.Empty;
    }
}
