using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    
    [JsonIgnore] // 🔥 Ngăn vòng lặp khi serialize JSON
    public Order? Order { get; set; }
    
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } = "Cash";
}
