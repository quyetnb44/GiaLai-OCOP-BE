namespace GiaLaiOCOP.Api.Models;

public class Producer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public ICollection<Product>? Products { get; set; }
}
