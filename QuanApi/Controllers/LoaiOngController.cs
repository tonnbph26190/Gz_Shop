using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoaiOngController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public LoaiOngController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            var query = _context.LoaiOngs.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenLoaiOng.Contains(keyword) || x.MaLoaiOng.Contains(keyword));

            var result = await query.ToListAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var lo = await _context.LoaiOngs.FindAsync(id);
            if (lo == null) return NotFound();
            return Ok(lo);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(LoaiOng lo)
        {
            Console.WriteLine("✅ API nhận Create LoaiOng:");
            Console.WriteLine($"TenLoaiOng: {lo.TenLoaiOng}, MaLoaiOng: {lo.MaLoaiOng}, TrangThai: {lo.TrangThai}");

            lo.IDLoaiOng = Guid.NewGuid();
            lo.NgayTao = DateTime.Now;

            if (string.IsNullOrEmpty(lo.NguoiTao))
                lo.NguoiTao = "unknown";

            _context.LoaiOngs.Add(lo);
            var saved = await _context.SaveChangesAsync();

            Console.WriteLine($"✅ SaveChanges: {saved}");

            return Ok(new { success = true });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] LoaiOng lo)
        {
            var entity = await _context.LoaiOngs.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenLoaiOng = lo.TenLoaiOng;
            entity.MaLoaiOng = lo.MaLoaiOng;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(lo.NguoiCapNhat) ? "unknown" : lo.NguoiCapNhat;
            entity.TrangThai = lo.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.LoaiOngs.FindAsync(id);
            if (entity == null) return NotFound();

            _context.LoaiOngs.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            Console.WriteLine($"🌀 API nhận ToggleStatus với ID: {id}");

            var lo = await _context.LoaiOngs.FindAsync(id);
            if (lo == null)
            {
                Console.WriteLine("❌ Không tìm thấy LoaiOng");
                return NotFound();
            }

            lo.TrangThai = !lo.TrangThai;
            lo.LanCapNhatCuoi = DateTime.Now;
            lo.NguoiCapNhat = "auto-toggle";

            var saved = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Trạng thái mới: {lo.TrangThai}, Save: {saved}");

            return Ok(new { success = true, trangThai = lo.TrangThai });
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string? keyword = null, string? trangThai = null)
        {
            var query = _context.LoaiOngs.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TenLoaiOng.Contains(keyword) || x.MaLoaiOng.Contains(keyword));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active")
                {
                    query = query.Where(x => x.TrangThai == true);
                }
                else if (trangThai == "inactive")
                {
                    query = query.Where(x => x.TrangThai == false);
                }
            }

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new { total, data });
        }
    }
}
