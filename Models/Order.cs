namespace GiaLaiOCOP.Api.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public ICollection<OrderItem>? OrderItems { get; set; }
}
