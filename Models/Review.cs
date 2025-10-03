namespace GiaLaiOCOP.Api.Models;

public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string Comment { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
