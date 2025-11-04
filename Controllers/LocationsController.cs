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
    public class LocationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LocationsController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 GET: api/locations - Xem tất cả địa điểm (public)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetLocations()
        {
            var locations = await _context.Locations.ToListAsync();

            var locationDtos = locations.Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Address = l.Address,
                Latitude = l.Latitude,
                Longitude = l.Longitude
            });

            return Ok(locationDtos);
        }

        // 🔹 GET: api/locations/{id} - Xem chi tiết địa điểm (public)
        [HttpGet("{id}")]
        public async Task<ActionResult<LocationDto>> GetLocation(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound("Không tìm thấy địa điểm.");

            var locationDto = new LocationDto
            {
                Id = location.Id,
                Name = location.Name,
                Address = location.Address,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            };

            return Ok(locationDto);
        }

        // 🔹 POST: api/locations - Tạo địa điểm mới (chỉ SystemAdmin)
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost]
        public async Task<ActionResult<LocationDto>> CreateLocation([FromBody] CreateLocationDto dto)
        {
            // 🔹 Validation
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Tên địa điểm là bắt buộc.");

            if (string.IsNullOrWhiteSpace(dto.Address))
                return BadRequest("Địa chỉ là bắt buộc.");

            // 🔹 Validation: Latitude (-90 đến 90)
            if (dto.Latitude < -90 || dto.Latitude > 90)
                return BadRequest("Latitude (vĩ độ) phải nằm trong khoảng -90 đến 90.");

            // 🔹 Validation: Longitude (-180 đến 180)
            if (dto.Longitude < -180 || dto.Longitude > 180)
                return BadRequest("Longitude (kinh độ) phải nằm trong khoảng -180 đến 180.");

            var location = new Location
            {
                Name = dto.Name.Trim(),
                Address = dto.Address.Trim(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            var locationDto = new LocationDto
            {
                Id = location.Id,
                Name = location.Name,
                Address = location.Address,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            };

            return CreatedAtAction(nameof(GetLocation), new { id = location.Id }, locationDto);
        }

        // 🔹 PUT: api/locations/{id} - Cập nhật địa điểm (chỉ SystemAdmin)
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] CreateLocationDto dto)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound("Không tìm thấy địa điểm.");

            // 🔹 Validation
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Tên địa điểm là bắt buộc.");

            if (string.IsNullOrWhiteSpace(dto.Address))
                return BadRequest("Địa chỉ là bắt buộc.");

            // 🔹 Validation: Latitude
            if (dto.Latitude < -90 || dto.Latitude > 90)
                return BadRequest("Latitude (vĩ độ) phải nằm trong khoảng -90 đến 90.");

            // 🔹 Validation: Longitude
            if (dto.Longitude < -180 || dto.Longitude > 180)
                return BadRequest("Longitude (kinh độ) phải nằm trong khoảng -180 đến 180.");

            location.Name = dto.Name.Trim();
            location.Address = dto.Address.Trim();
            location.Latitude = dto.Latitude;
            location.Longitude = dto.Longitude;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 DELETE: api/locations/{id} - Xóa địa điểm (chỉ SystemAdmin)
        [Authorize(Roles = "SystemAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound("Không tìm thấy địa điểm.");

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
