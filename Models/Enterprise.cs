namespace GiaLaiOCOP.Api.Models
{
    public class Enterprise
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        // 1 doanh nghiệp có nhiều sản phẩm
        public ICollection<Product>? Products { get; set; }
        // 1 doanh nghiệp có nhiều người dùng (bao gồm admin & customer)
        public ICollection<User>? Users { get; set; }
    }
}
