using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GiaLaiOCOP.Api.Services
{
    /// <summary>
    /// Service để cập nhật AverageRating vào database
    /// </summary>
    public interface IRatingService
    {
        Task UpdateProductAverageRatingAsync(int productId);
        Task UpdateEnterpriseAverageRatingAsync(int enterpriseId);
    }

    public class RatingService : IRatingService
    {
        private readonly AppDbContext _context;

        public RatingService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cập nhật AverageRating của Product vào database
        /// </summary>
        public async Task UpdateProductAverageRatingAsync(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return;

            // Tính AverageRating từ Reviews
            double? averageRating = null;
            if (product.Reviews != null && product.Reviews.Any())
            {
                averageRating = Math.Round(product.Reviews.Average(r => (double)r.Rating), 2);
            }

            // Cập nhật vào database
            product.AverageRating = averageRating;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Cập nhật AverageRating của Enterprise
            await UpdateEnterpriseAverageRatingAsync(product.EnterpriseId);
        }

        /// <summary>
        /// Cập nhật AverageRating của Enterprise vào database
        /// Tính từ AverageRating của tất cả sản phẩm Approved của enterprise
        /// </summary>
        public async Task UpdateEnterpriseAverageRatingAsync(int enterpriseId)
        {
            var enterprise = await _context.Enterprises
                .Include(e => e.Products)
                .FirstOrDefaultAsync(e => e.Id == enterpriseId);

            if (enterprise == null)
                return;

            // Lấy tất cả sản phẩm Approved có AverageRating
            var approvedProducts = (enterprise.Products ?? new List<Product>())
                .Where(p => p.Status == "Approved" && p.AverageRating.HasValue)
                .ToList();

            // Tính AverageRating của Enterprise từ AverageRating của các sản phẩm
            double? averageRating = null;
            if (approvedProducts.Any())
            {
                averageRating = Math.Round(approvedProducts.Average(p => p.AverageRating!.Value), 2);
            }

            // Cập nhật vào database
            enterprise.AverageRating = averageRating;
            enterprise.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}

