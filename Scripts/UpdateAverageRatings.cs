using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GiaLaiOCOP.Api.Scripts
{
    /// <summary>
    /// Script để cập nhật AverageRating cho tất cả Products và Enterprises hiện có
    /// Chạy script này sau khi deploy RatingService để đảm bảo dữ liệu nhất quán
    /// 
    /// Cách chạy:
    /// 1. Tạo một endpoint tạm thời trong Program.cs hoặc HomeController
    /// 2. Hoặc chạy trong Package Manager Console:
    ///    dotnet ef migrations script --idempotent
    /// </summary>
    public static class UpdateAverageRatingsScript
    {
        public static async Task RunAsync(AppDbContext context, IRatingService ratingService)
        {
            Console.WriteLine("🔄 Bắt đầu cập nhật AverageRating cho tất cả Products và Enterprises...");

            // 1. Lấy tất cả Products có Reviews
            var productsWithReviews = await context.Products
                .Include(p => p.Reviews)
                .Where(p => p.Reviews != null && p.Reviews.Any())
                .ToListAsync();

            Console.WriteLine($"📦 Tìm thấy {productsWithReviews.Count} sản phẩm có reviews.");

            // 2. Cập nhật AverageRating cho từng Product
            int updatedProducts = 0;
            foreach (var product in productsWithReviews)
            {
                await ratingService.UpdateProductAverageRatingAsync(product.Id);
                updatedProducts++;
            }

            Console.WriteLine($"✅ Đã cập nhật AverageRating cho {updatedProducts} sản phẩm.");

            // 3. Lấy tất cả Enterprises có Products
            var enterprises = await context.Enterprises
                .Include(e => e.Products)
                .ToListAsync();

            Console.WriteLine($"🏢 Tìm thấy {enterprises.Count} doanh nghiệp.");

            // 4. Cập nhật AverageRating cho từng Enterprise
            int updatedEnterprises = 0;
            foreach (var enterprise in enterprises)
            {
                await ratingService.UpdateEnterpriseAverageRatingAsync(enterprise.Id);
                updatedEnterprises++;
            }

            Console.WriteLine($"✅ Đã cập nhật AverageRating cho {updatedEnterprises} doanh nghiệp.");

            // 5. Thống kê
            var productsWithRating = await context.Products
                .CountAsync(p => p.AverageRating.HasValue);
            
            var enterprisesWithRating = await context.Enterprises
                .CountAsync(e => e.AverageRating.HasValue);

            Console.WriteLine("\n📊 Thống kê:");
            Console.WriteLine($"   - Products có AverageRating: {productsWithRating}");
            Console.WriteLine($"   - Enterprises có AverageRating: {enterprisesWithRating}");
            Console.WriteLine("\n✅ Hoàn tất cập nhật AverageRating!");
        }
    }
}

