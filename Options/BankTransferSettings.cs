namespace GiaLaiOCOP.Api.Options
{
    public class BankTransferSettings
    {
        public string BankCode { get; set; } = string.Empty; // Ví dụ: "970415" (MB Bank)
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string Template { get; set; } = "compact";
        public string BaseUrl { get; set; } = "https://img.vietqr.io/image";
        public string Description { get; set; } = "Thanh toan don hang OCOP";
    }
}




