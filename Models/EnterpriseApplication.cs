namespace GiaLaiOCOP.Api.Models
{
    public class EnterpriseApplication
    {
        public int Id { get; set; }
        public int UserId { get; set; }              // Người nộp đơn (Customer)

        // Thông tin doanh nghiệp
        public string EnterpriseName { get; set; } = string.Empty;     // Tên doanh nghiệp
        public string BusinessType { get; set; } = string.Empty;       // Loại hình doanh nghiệp (Công ty TNHH, HTX, Hộ kinh doanh...)
        public string TaxCode { get; set; } = string.Empty;            // Mã số thuế
        public string BusinessLicenseNumber { get; set; } = string.Empty; // Số giấy phép đăng ký kinh doanh
        public DateTime? LicenseIssuedDate { get; set; }               // Ngày cấp giấy phép
        public string LicenseIssuedBy { get; set; } = string.Empty;    // Nơi cấp giấy phép

        // Thông tin liên hệ
        public string Address { get; set; } = string.Empty;            // Địa chỉ trụ sở chính
        public string Ward { get; set; } = string.Empty;               // Phường / Xã
        public string District { get; set; } = string.Empty;           // Quận / Huyện
        public string Province { get; set; } = string.Empty;           // Tỉnh / Thành phố
        public string PhoneNumber { get; set; } = string.Empty;        // Số điện thoại liên hệ
        public string EmailContact { get; set; } = string.Empty;       // Email liên hệ
        public string Website { get; set; } = string.Empty;            // Trang web (nếu có)

        // Người đại diện pháp luật
        public string RepresentativeName { get; set; } = string.Empty; // Họ và tên người đại diện pháp luật
        public string RepresentativePosition { get; set; } = string.Empty; // Chức vụ
        public string RepresentativeIdNumber { get; set; } = string.Empty; // Số CCCD / CMND
        public DateTime? RepresentativeIdIssuedDate { get; set; }      // Ngày cấp CCCD / CMND
        public string RepresentativeIdIssuedBy { get; set; } = string.Empty; // Nơi cấp CCCD / CMND

        // Thông tin sản xuất
        public string ProductionLocation { get; set; } = string.Empty; // Địa điểm sản xuất
        public string NumberOfEmployees { get; set; } = string.Empty;  // Số lượng lao động
        public string ProductionScale { get; set; } = string.Empty;    // Quy mô sản xuất
        public string BusinessField { get; set; } = string.Empty;      // Ngành nghề sản xuất kinh doanh

        // Sản phẩm đăng ký OCOP
        public string ProductName { get; set; } = string.Empty;        // Tên sản phẩm đăng ký OCOP
        public string ProductCategory { get; set; } = string.Empty;    // Nhóm sản phẩm (Thực phẩm, đồ uống, thảo dược...)
        public string ProductDescription { get; set; } = string.Empty; // Mô tả sản phẩm
        public string ProductOrigin { get; set; } = string.Empty;      // Nguồn gốc nguyên liệu
        public string ProductCertifications { get; set; } = string.Empty; // Các chứng nhận (VSATTP, VietGAP, OCOP cũ...)
        public string ProductImages { get; set; } = string.Empty;      // Link ảnh sản phẩm (nếu có)

        // Hồ sơ kèm theo
        public string AttachedDocuments { get; set; } = string.Empty;  // Tài liệu đính kèm (file path hoặc JSON list)
        public string AdditionalNotes { get; set; } = string.Empty;    // Ghi chú thêm của doanh nghiệp

        // Trạng thái hồ sơ
        public string Status { get; set; } = "Pending";                // Pending / Approved / Rejected / Returned
        public string? AdminComment { get; set; }                      // Nhận xét hoặc yêu cầu bổ sung từ cơ quan duyệt
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;     // Ngày nộp hồ sơ
        public DateTime? UpdatedAt { get; set; }                       // Ngày cập nhật hồ sơ

        // Quan hệ với bảng người dùng
        public User? User { get; set; }
    }
}
