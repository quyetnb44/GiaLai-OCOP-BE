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
    public class ProducersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProducersController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 GET: api/producers - Xem tất cả nhà sản xuất (public)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProducerDto>>> GetProducers()
        {
            var producers = await _context.Producers.ToListAsync();

            var producerDtos = producers.Select(p => new ProducerDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address
            });

            return Ok(producerDtos);
        }

        // 🔹 GET: api/producers/{id} - Xem chi tiết nhà sản xuất (public)
        [HttpGet("{id}")]
        public async Task<ActionResult<ProducerDto>> GetProducer(int id)
        {
            var producer = await _context.Producers.FindAsync(id);
            if (producer == null) return NotFound("Không tìm thấy nhà sản xuất.");

            var producerDto = new ProducerDto
            {
                Id = producer.Id,
                Name = producer.Name,
                Address = producer.Address
            };

            return Ok(producerDto);
        }

        // 🔹 POST: api/producers - Tạo nhà sản xuất mới (chỉ SystemAdmin)
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost]
        public async Task<ActionResult<ProducerDto>> CreateProducer([FromBody] ProducerRegisterDto dto)
        {
            // 🔹 Validation
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Tên nhà sản xuất là bắt buộc.");

            if (string.IsNullOrWhiteSpace(dto.Address))
                return BadRequest("Địa chỉ nhà sản xuất là bắt buộc.");

            // 🔹 Kiểm tra trùng tên (tùy chọn)
            if (await _context.Producers.AnyAsync(p => p.Name.ToLower() == dto.Name.ToLower()))
                return Conflict("Tên nhà sản xuất đã tồn tại.");

            var producer = new Producer
            {
                Name = dto.Name.Trim(),
                Address = dto.Address.Trim()
            };

            _context.Producers.Add(producer);
            await _context.SaveChangesAsync();

            var producerDto = new ProducerDto
            {
                Id = producer.Id,
                Name = producer.Name,
                Address = producer.Address
            };

            return CreatedAtAction(nameof(GetProducer), new { id = producer.Id }, producerDto);
        }

        // 🔹 PUT: api/producers/{id} - Cập nhật nhà sản xuất (chỉ SystemAdmin)
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProducer(int id, [FromBody] ProducerRegisterDto dto)
        {
            var producer = await _context.Producers.FindAsync(id);
            if (producer == null) return NotFound("Không tìm thấy nhà sản xuất.");

            // 🔹 Validation
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Tên nhà sản xuất là bắt buộc.");

            if (string.IsNullOrWhiteSpace(dto.Address))
                return BadRequest("Địa chỉ nhà sản xuất là bắt buộc.");

            // 🔹 Kiểm tra trùng tên (trừ chính nó)
            if (await _context.Producers.AnyAsync(p => p.Name.ToLower() == dto.Name.ToLower() && p.Id != id))
                return Conflict("Tên nhà sản xuất đã được sử dụng bởi nhà sản xuất khác.");

            producer.Name = dto.Name.Trim();
            producer.Address = dto.Address.Trim();

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 DELETE: api/producers/{id} - Xóa nhà sản xuất (chỉ SystemAdmin)
        [Authorize(Roles = "SystemAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProducer(int id)
        {
            var producer = await _context.Producers
                .Include(p => p.Products)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producer == null) return NotFound("Không tìm thấy nhà sản xuất.");

            // 🔹 Kiểm tra xem có sản phẩm nào đang sử dụng nhà sản xuất này không
            if (producer.Products != null && producer.Products.Any())
                return BadRequest("Không thể xóa nhà sản xuất vì còn sản phẩm đang sử dụng.");

            _context.Producers.Remove(producer);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
