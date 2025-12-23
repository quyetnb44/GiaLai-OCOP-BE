using GiaLaiOCOP.Api.Models;

namespace GiaLaiOCOP.Api.Data
{
    /// <summary>
    /// Seed data mẫu cho Map - Doanh nghiệp OCOP tại Gia Lai
    /// </summary>
    public static class MapSeedData
    {
        public static void SeedMapData(AppDbContext context)
        {
            // Chỉ seed nếu chưa có dữ liệu
            if (context.Enterprises.Any(e => e.Latitude != null && e.Longitude != null))
                return;

            var enterprises = new List<Enterprise>
            {
                new Enterprise
                {
                    Name = "HTX Nông nghiệp Cà Phê Pleiku",
                    Description = "Chuyên sản xuất và chế biến cà phê Robusta chất lượng cao tại Pleiku, Gia Lai",
                    Address = "123 Đường Lê Lợi",
                    Ward = "Phường Hội Thương",
                    District = "Pleiku",
                    Province = "Gia Lai",
                    Latitude = 13.9833, // Tọa độ Pleiku
                    Longitude = 108.0000,
                    PhoneNumber = "0269.1234567",
                    EmailContact = "contact@cafepleiku.vn",
                    Website = "https://cafepleiku.vn",
                    OCOPRating = 5,
                    BusinessField = "Cà phê",
                    ImageUrl = "https://example.com/images/cafe-pleiku.jpg",
                    CreatedAt = DateTime.UtcNow
                },
                new Enterprise
                {
                    Name = "Công ty TNHH Hồng Sâm Gia Lai",
                    Description = "Sản xuất và phân phối các sản phẩm từ hồng sâm, thảo dược quý",
                    Address = "456 Đường Nguyễn Du",
                    Ward = "Phường Yên Đỗ",
                    District = "Pleiku",
                    Province = "Gia Lai",
                    Latitude = 13.9850,
                    Longitude = 108.0020,
                    PhoneNumber = "0269.2345678",
                    EmailContact = "info@hongsamgialai.com",
                    Website = "https://hongsamgialai.com",
                    OCOPRating = 4,
                    BusinessField = "Thảo dược",
                    ImageUrl = "https://example.com/images/hong-sam.jpg",
                    CreatedAt = DateTime.UtcNow
                },
                new Enterprise
                {
                    Name = "HTX Mật ong rừng Tây Nguyên",
                    Description = "Thu hoạch và chế biến mật ong rừng nguyên chất từ các vùng núi Gia Lai",
                    Address = "789 Đường Trần Hưng Đạo",
                    Ward = "Xã Ia Kênh",
                    District = "Pleiku",
                    Province = "Gia Lai",
                    Latitude = 13.9800,
                    Longitude = 107.9980,
                    PhoneNumber = "0269.3456789",
                    EmailContact = "matong@taynguyen.vn",
                    OCOPRating = 5,
                    BusinessField = "Mật ong",
                    ImageUrl = "https://example.com/images/mat-ong.jpg",
                    CreatedAt = DateTime.UtcNow
                },
                new Enterprise
                {
                    Name = "Cơ sở Sản xuất Rượu cần Gia Lai",
                    Description = "Sản xuất rượu cần truyền thống của người dân tộc Tây Nguyên",
                    Address = "321 Đường Phạm Văn Đồng",
                    Ward = "Xã Chư Á",
                    District = "Pleiku",
                    Province = "Gia Lai",
                    Latitude = 13.9900,
                    Longitude = 108.0050,
                    PhoneNumber = "0269.4567890",
                    EmailContact = "ruoucan@gialai.vn",
                    OCOPRating = 3,
                    BusinessField = "Đồ uống",
                    ImageUrl = "https://example.com/images/ruou-can.jpg",
                    CreatedAt = DateTime.UtcNow
                },
                new Enterprise
                {
                    Name = "HTX Rau củ quả sạch An Khê",
                    Description = "Trồng và cung cấp rau củ quả sạch, đạt tiêu chuẩn VietGAP",
                    Address = "654 Đường Quốc lộ 19",
                    Ward = "Phường An Bình",
                    District = "An Khê",
                    Province = "Gia Lai",
                    Latitude = 13.9500, // Tọa độ An Khê
                    Longitude = 108.6500,
                    PhoneNumber = "0269.5678901",
                    EmailContact = "raucuqua@ankhe.vn",
                    OCOPRating = 4,
                    BusinessField = "Rau củ quả",
                    ImageUrl = "https://example.com/images/rau-cu-qua.jpg",
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Enterprises.AddRange(enterprises);
            context.SaveChanges();

            // Thêm sản phẩm mẫu cho doanh nghiệp đầu tiên
            var cafeEnterprise = enterprises[0];
            var products = new List<Product>
            {
                new Product
                {
                    Name = "Cà phê Robusta hạt rang xay",
                    Description = "Cà phê Robusta rang xay nguyên chất, đóng gói 500g",
                    Price = 150000,
                    EnterpriseId = cafeEnterprise.Id,
                    OCOPRating = 5,
                    StockStatus = "InStock",
                    ImageUrl = "https://example.com/images/cafe-hat.jpg",
                    CreatedAt = DateTime.UtcNow,
                    Status = "Approved",
                    ApprovedAt = DateTime.UtcNow,
                    Unit = "kg", // 🔹 Cà phê hạt/xay tính theo kg
                    StockQuantity = 100
                },
                new Product
                {
                    Name = "Cà phê phin truyền thống",
                    Description = "Cà phê phin đóng gói sẵn, tiện lợi, 20 gói/hộp",
                    Price = 120000,
                    EnterpriseId = cafeEnterprise.Id,
                    OCOPRating = 4,
                    StockStatus = "InStock",
                    ImageUrl = "https://example.com/images/cafe-phin.jpg",
                    CreatedAt = DateTime.UtcNow,
                    Status = "Approved",
                    ApprovedAt = DateTime.UtcNow,
                    Unit = "hộp", // Cafe phin thì tính theo hộp
                    StockQuantity = 50
                }
            };

            context.Products.AddRange(products);
            context.SaveChanges();

            // Thêm sản phẩm Mật ong cho "HTX Mật ong rừng Tây Nguyên" (index 2)
            var honeyEnterprise = enterprises[2];
            var honeyProducts = new List<Product>
            {
                new Product
                {
                    Name = "Mật ong rừng nguyên chất",
                    Description = "Mật ong rừng già khai thác tự nhiên, đậm đặc",
                    Price = 250000,
                    EnterpriseId = honeyEnterprise.Id,
                    OCOPRating = 5,
                    StockStatus = "InStock",
                    ImageUrl = "https://example.com/images/mat-ong.jpg",
                    CreatedAt = DateTime.UtcNow,
                    Status = "Approved",
                    ApprovedAt = DateTime.UtcNow,
                    Unit = "lít", // 🔹 Đơn vị lít
                    StockQuantity = 100 // Tồn kho mẫu
                },
                 new Product
                {
                    Name = "Mật ong hoa cà phê",
                    Description = "Mật ong nuôi hút phấn hoa cà phê, thơm nhẹ",
                    Price = 120000,
                    EnterpriseId = honeyEnterprise.Id,
                    OCOPRating = 4,
                    StockStatus = "InStock",
                    ImageUrl = "https://example.com/images/mat-ong-cafe.jpg", // Ảnh minh họa
                    CreatedAt = DateTime.UtcNow,
                    Status = "Approved",
                    ApprovedAt = DateTime.UtcNow,
                    Unit = "lít", // 🔹 Đơn vị lít
                    StockQuantity = 200
                }
            };

            context.Products.AddRange(honeyProducts);
            context.SaveChanges();

            Console.WriteLine($"✅ Đã seed {enterprises.Count} doanh nghiệp và {products.Count} sản phẩm mẫu cho Map.");
        }
    }
}

