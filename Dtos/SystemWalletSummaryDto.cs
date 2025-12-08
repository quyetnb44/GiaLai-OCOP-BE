namespace GiaLaiOCOP.Api.Dtos
{
    public class SystemWalletSummaryDto
    {
        public decimal TotalSystemBalance { get; set; } // Tổng số tiền trong hệ thống
        public decimal SystemAdminBalance { get; set; } // Số tiền trong ví SystemAdmin
        public decimal AllUsersBalance { get; set; } // Tổng số tiền của tất cả User (Customer + EnterpriseAdmin)
        public int TotalUsers { get; set; } // Tổng số user có ví
        public int TotalCustomers { get; set; } // Số Customer có ví
        public int TotalEnterpriseAdmins { get; set; } // Số EnterpriseAdmin có ví
        public SystemWalletBreakdownDto Breakdown { get; set; } = new SystemWalletBreakdownDto();
    }

    public class SystemWalletBreakdownDto
    {
        public decimal CustomersBalance { get; set; } // Tổng số tiền của Customer
        public decimal EnterpriseAdminsBalance { get; set; } // Tổng số tiền của EnterpriseAdmin
    }
}

