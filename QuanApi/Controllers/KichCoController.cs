using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KichCoController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public KichCoController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            var query = _context.KichCos.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenKichCo.Contains(keyword) || x.MaKichCo.Contains(keyword));

            var result = await query.ToListAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var kc = await _context.KichCos.FindAsync(id);
            if (kc == null) return NotFound();
            return Ok(kc);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(KichCo kc)
        {
            Console.WriteLine("✅ API nhận Create KichCo:");
            Console.WriteLine($"TenKichCo: {kc.TenKichCo}, MaKichCo: {kc.MaKichCo}, TrangThai: {kc.TrangThai}");

            kc.IDKichCo = Guid.NewGuid();
            kc.NgayTao = DateTime.Now;

            if (string.IsNullOrEmpty(kc.NguoiTao))
                kc.NguoiTao = "unknown";

            _context.KichCos.Add(kc);
            var saved = await _context.SaveChangesAsync();

            Console.WriteLine($"✅ SaveChanges: {saved}");

            return Ok(new { success = true });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] KichCo kc)
        {
            var entity = await _context.KichCos.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenKichCo = kc.TenKichCo;
            entity.MaKichCo = kc.MaKichCo;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(kc.NguoiCapNhat) ? "unknown" : kc.NguoiCapNhat;
            entity.TrangThai = kc.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.KichCos.FindAsync(id);
            if (entity == null) return NotFound();

            _context.KichCos.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            Console.WriteLine($"🌀 API nhận ToggleStatus với ID: {id}");

            var kc = await _context.KichCos.FindAsync(id);
            if (kc == null)
            {
                Console.WriteLine("❌ Không tìm thấy KichCo");
                return NotFound();
            }

            kc.TrangThai = !kc.TrangThai;
            kc.LanCapNhatCuoi = DateTime.Now;
            kc.NguoiCapNhat = "auto-toggle";

            var saved = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Trạng thái mới: {kc.TrangThai}, Save: {saved}");

            return Ok(new { success = true, trangThai = kc.TrangThai });
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string? keyword = null, string? trangThai = null)
        {
            var query = _context.KichCos.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TenKichCo.Contains(keyword) || x.MaKichCo.Contains(keyword));
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