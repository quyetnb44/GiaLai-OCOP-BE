using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models;

public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    [JsonIgnore] // 🔥 Ngăn vòng lặp khi serialize JSON
    public User? User { get; set; }
    
    public int ProductId { get; set; }
    
    [JsonIgnore] // 🔥 Ngăn vòng lặp khi serialize JSON
    public Product? Product { get; set; }
    
    public string Comment { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
