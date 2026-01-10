using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhuongThucThanhToansController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public PhuongThucThanhToansController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        // GET: api/PhuongThucThanhToans
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhuongThucThanhToan>>> GetPhuongThucThanhToans()
        {
            return await _context.PhuongThucThanhToans
                .Where(p => p.TrangThai)
                .ToListAsync();
        }

        // GET: api/PhuongThucThanhToans/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PhuongThucThanhToan>> GetPhuongThucThanhToan(Guid id)
        {
            var phuongThucThanhToan = await _context.PhuongThucThanhToans.FindAsync(id);

            if (phuongThucThanhToan == null)
            {
                return NotFound();
            }

            return phuongThucThanhToan;
        }

        // POST: api/PhuongThucThanhToans
        [HttpPost]
        public async Task<ActionResult<PhuongThucThanhToan>> CreatePhuongThucThanhToan(PhuongThucThanhToan phuongThucThanhToan)
        {
            if (phuongThucThanhToan.IDPhuongThucThanhToan == Guid.Empty)
                phuongThucThanhToan.IDPhuongThucThanhToan = Guid.NewGuid();

            _context.PhuongThucThanhToans.Add(phuongThucThanhToan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPhuongThucThanhToan), new { id = phuongThucThanhToan.IDPhuongThucThanhToan }, phuongThucThanhToan);
        }

        // PUT: api/PhuongThucThanhToans/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePhuongThucThanhToan(Guid id, PhuongThucThanhToan phuongThucThanhToan)
        {
            if (id != phuongThucThanhToan.IDPhuongThucThanhToan)
            {
                return BadRequest();
            }

            _context.Entry(phuongThucThanhToan).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PhuongThucThanhToanExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/PhuongThucThanhToans/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhuongThucThanhToan(Guid id)
        {
            var phuongThucThanhToan = await _context.PhuongThucThanhToans.FindAsync(id);
            if (phuongThucThanhToan == null)
            {
                return NotFound();
            }

            _context.PhuongThucThanhToans.Remove(phuongThucThanhToan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PhuongThucThanhToanExists(Guid id)
        {
            return _context.PhuongThucThanhToans.Any(e => e.IDPhuongThucThanhToan == id);
        }
    }
} 