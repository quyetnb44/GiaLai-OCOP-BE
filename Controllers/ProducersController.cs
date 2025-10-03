using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;

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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producer>>> GetProducers()
        {
            return await _context.Producers.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Producer>> GetProducer(int id)
        {
            var producer = await _context.Producers.FindAsync(id);
            if (producer == null) return NotFound();
            return producer;
        }

        [HttpPost]
        public async Task<ActionResult<Producer>> PostProducer(Producer producer)
        {
            _context.Producers.Add(producer);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProducer), new { id = producer.Id }, producer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProducer(int id, Producer producer)
        {
            if (id != producer.Id) return BadRequest();
            _context.Entry(producer).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProducer(int id)
        {
            var producer = await _context.Producers.FindAsync(id);
            if (producer == null) return NotFound();
            _context.Producers.Remove(producer);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
