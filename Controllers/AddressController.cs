using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AddressController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/address/provinces
        /// <summary>
        /// Lấy danh sách tất cả tỉnh/thành phố
        /// </summary>
        [HttpGet("provinces")]
        [ProducesResponseType(typeof(IEnumerable<ProvinceDto>), 200)]
        public async Task<ActionResult<IEnumerable<ProvinceDto>>> GetProvinces()
        {
            var provinces = await _context.Provinces
                .OrderBy(p => p.Name)
                .Select(p => new ProvinceDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code
                })
                .ToListAsync();

            return Ok(provinces);
        }

        // GET: api/address/districts?provinceId=1
        /// <summary>
        /// Lấy danh sách quận/huyện theo tỉnh/thành phố
        /// </summary>
        [HttpGet("districts")]
        [ProducesResponseType(typeof(IEnumerable<DistrictDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<DistrictDto>>> GetDistricts([FromQuery] int provinceId)
        {
            if (provinceId <= 0)
            {
                return BadRequest("ProvinceId phải lớn hơn 0.");
            }

            // Kiểm tra tỉnh có tồn tại không
            var provinceExists = await _context.Provinces.AnyAsync(p => p.Id == provinceId);
            if (!provinceExists)
            {
                return NotFound($"Không tìm thấy tỉnh/thành phố với Id = {provinceId}.");
            }

            var districts = await _context.Districts
                .Where(d => d.ProvinceId == provinceId)
                .OrderBy(d => d.Name)
                .Select(d => new DistrictDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Code = d.Code,
                    ProvinceId = d.ProvinceId
                })
                .ToListAsync();

            return Ok(districts);
        }

        // GET: api/address/wards?districtId=1
        /// <summary>
        /// Lấy danh sách phường/xã theo quận/huyện
        /// </summary>
        [HttpGet("wards")]
        [ProducesResponseType(typeof(IEnumerable<WardDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<WardDto>>> GetWards([FromQuery] int districtId)
        {
            if (districtId <= 0)
            {
                return BadRequest("DistrictId phải lớn hơn 0.");
            }

            // Kiểm tra quận/huyện có tồn tại không
            var districtExists = await _context.Districts.AnyAsync(d => d.Id == districtId);
            if (!districtExists)
            {
                return NotFound($"Không tìm thấy quận/huyện với Id = {districtId}.");
            }

            var wards = await _context.Wards
                .Where(w => w.DistrictId == districtId)
                .OrderBy(w => w.Name)
                .Select(w => new WardDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Code = w.Code,
                    DistrictId = w.DistrictId
                })
                .ToListAsync();

            return Ok(wards);
        }
    }
}


