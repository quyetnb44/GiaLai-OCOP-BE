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

        // 🍶 Sản phẩm OCOP
        public string ProductName { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public string ProductOrigin { get; set; } = string.Empty;
        public string ProductCertifications { get; set; } = string.Empty;
        public string ProductImages { get; set; } = string.Empty;

        // 📎 Hồ sơ kèm theo
        public string AttachedDocuments { get; set; } = string.Empty;
        public string AdditionalNotes { get; set; } = string.Empty;
    }
}
