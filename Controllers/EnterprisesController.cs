using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SystemAdmin")]
    public class EnterprisesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public EnterprisesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Enterprises
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EnterpriseDto>>> GetEnterprises()
        {
            var enterprises = await _context.Enterprises
                                            .Include(e => e.Products)
                                            .Include(e => e.Users)
                                            .ToListAsync();

            var enterpriseDtos = enterprises.Select(e => new EnterpriseDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Address = e.Address,
                Ward = e.Ward,
                District = e.District,
                Province = e.Province,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                PhoneNumber = e.PhoneNumber,
                EmailContact = e.EmailContact,
                Website = e.Website,
                OCOPRating = e.OCOPRating,
                BusinessField = e.BusinessField,
                ImageUrl = e.ImageUrl,
                AverageRating = e.AverageRating,
                Products = (e.Products ?? new List<Product>()).Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    EnterpriseId = e.Id,
                    ImageUrl = p.ImageUrl,
                    OCOPRating = p.OCOPRating,
                    StockStatus = p.StockStatus,
                    AverageRating = p.AverageRating
                }).ToList(),
                Users = (e.Users ?? new List<User>()).Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email
                }).ToList()
            }).ToList();

            return Ok(enterpriseDtos);
        }

        // GET: api/Enterprises/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EnterpriseDto>> GetEnterprise(int id)
        {
            var enterprise = await _context.Enterprises
                                           .Include(e => e.Products)
                                           .Include(e => e.Users)
                                           .FirstOrDefaultAsync(e => e.Id == id);

            if (enterprise == null) return NotFound();

            var enterpriseDto = new EnterpriseDto
            {
                Id = enterprise.Id,
                Name = enterprise.Name,
                Description = enterprise.Description,
                Address = enterprise.Address,
                Ward = enterprise.Ward,
                District = enterprise.District,
                Province = enterprise.Province,
                Latitude = enterprise.Latitude,
                Longitude = enterprise.Longitude,
                PhoneNumber = enterprise.PhoneNumber,
                EmailContact = enterprise.EmailContact,
                Website = enterprise.Website,
                OCOPRating = enterprise.OCOPRating,
                BusinessField = enterprise.BusinessField,
                ImageUrl = enterprise.ImageUrl,
                AverageRating = enterprise.AverageRating,
                Products = (enterprise.Products ?? new List<Product>()).Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    EnterpriseId = enterprise.Id,
                    ImageUrl = p.ImageUrl,
                    OCOPRating = p.OCOPRating,
                    StockStatus = p.StockStatus,
                    AverageRating = p.AverageRating
                }).ToList(),
                Users = (enterprise.Users ?? new List<User>()).Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email
                }).ToList()
            };

            return Ok(enterpriseDto);
        }

        // POST: api/Enterprises
        [HttpPost]
        public async Task<ActionResult<EnterpriseDto>> CreateEnterprise([FromBody] Enterprise enterprise)
        {
            _context.Enterprises.Add(enterprise);
            await _context.SaveChangesAsync();

            var enterpriseDto = new EnterpriseDto
            {
                Id = enterprise.Id,
                Name = enterprise.Name,
                Description = enterprise.Description,
                Address = enterprise.Address,
                Ward = enterprise.Ward,
                District = enterprise.District,
                Province = enterprise.Province,
                Latitude = enterprise.Latitude,
                Longitude = enterprise.Longitude,
                PhoneNumber = enterprise.PhoneNumber,
                EmailContact = enterprise.EmailContact,
                Website = enterprise.Website,
                OCOPRating = enterprise.OCOPRating,
                BusinessField = enterprise.BusinessField,
                ImageUrl = enterprise.ImageUrl,
                AverageRating = enterprise.AverageRating,
                Products = new List<ProductDto>(),
                Users = new List<UserDto>()
            };

            return CreatedAtAction(nameof(GetEnterprise), new { id = enterprise.Id }, enterpriseDto);
        }

        // PUT: api/Enterprises/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEnterprise(int id, [FromBody] Enterprise enterprise)
        {
            if (id != enterprise.Id) return BadRequest();

            _context.Entry(enterprise).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Enterprises/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEnterprise(int id)
        {
            var enterprise = await _context.Enterprises.FindAsync(id);
            if (enterprise == null) return NotFound();

            _context.Enterprises.Remove(enterprise);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
