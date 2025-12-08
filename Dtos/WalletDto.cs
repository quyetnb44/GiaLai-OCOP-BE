namespace GiaLaiOCOP.Api.Dtos
{
    public class WalletDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTime CreatedAt { get; set; }
    }
}

