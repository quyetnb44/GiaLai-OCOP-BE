using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            return await _context.Transactions.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var txn = await _context.Transactions.FindAsync(id);
            if (txn == null) return NotFound();
            return txn;
        }

        [HttpPost]
        public async Task<ActionResult<Transaction>> PostTransaction(Transaction txn)
        {
            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTransaction), new { id = txn.Id }, txn);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTransaction(int id, Transaction txn)
        {
            if (id != txn.Id) return BadRequest();
            _context.Entry(txn).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var txn = await _context.Transactions.FindAsync(id);
            if (txn == null) return NotFound();
            _context.Transactions.Remove(txn);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
