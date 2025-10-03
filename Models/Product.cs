namespace GiaLaiOCOP.Api.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int ProducerId { get; set; }
    public Producer? Producer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
