using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Services;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔥 Thêm authorization
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IRatingService _ratingService;

        public ReviewsController(AppDbContext context, IRatingService ratingService)
        {
            _context = context;
            _ratingService = ratingService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Review>> GetReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();
            return review;
        }

        [HttpPost]
        public async Task<ActionResult<Review>> PostReview(Review review)
        {
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            
            // 🔹 Cập nhật AverageRating vào database
            await _ratingService.UpdateProductAverageRatingAsync(review.ProductId);
            
            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutReview(int id, Review review)
        {
            if (id != review.Id) return BadRequest();
            
            // Lấy ProductId trước khi update
            var existingReview = await _context.Reviews.FindAsync(id);
            if (existingReview == null) return NotFound();
            
            var productId = existingReview.ProductId;
            
            _context.Entry(review).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            // 🔹 Cập nhật AverageRating vào database
            await _ratingService.UpdateProductAverageRatingAsync(productId);
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();
            
            // Lấy ProductId trước khi xóa
            var productId = review.ProductId;
            
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            
            // 🔹 Cập nhật AverageRating vào database
            await _ratingService.UpdateProductAverageRatingAsync(productId);
            
            return NoContent();
        }
    }
}
