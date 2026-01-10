using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HoaTietController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public HoaTietController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            var query = _context.HoaTiet.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenHoaTiet.Contains(keyword) || x.MaHoaTiet.Contains(keyword));

            var result = await query.ToListAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var ht = await _context.HoaTiet.FindAsync(id);
            if (ht == null) return NotFound();
            return Ok(ht);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(HoaTiet ht)
        {
            Console.WriteLine("✅ API nhận Create HoaTiet:");
            Console.WriteLine($"TenHoaTiet: {ht.TenHoaTiet}, MaHoaTiet: {ht.MaHoaTiet}, TrangThai: {ht.TrangThai}");

            ht.IDHoaTiet = Guid.NewGuid();
            ht.NgayTao = DateTime.Now;

            if (string.IsNullOrEmpty(ht.NguoiTao))
                ht.NguoiTao = "unknown";

            _context.HoaTiet.Add(ht);
            var saved = await _context.SaveChangesAsync();

            Console.WriteLine($"✅ SaveChanges: {saved}");

            return Ok(new { success = true });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] HoaTiet ht)
        {
            var entity = await _context.HoaTiet.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenHoaTiet = ht.TenHoaTiet;
            entity.MaHoaTiet = ht.MaHoaTiet;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(ht.NguoiCapNhat) ? "unknown" : ht.NguoiCapNhat;
            entity.TrangThai = ht.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.HoaTiet.FindAsync(id);
            if (entity == null) return NotFound();

            _context.HoaTiet.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            Console.WriteLine($"🌀 API nhận ToggleStatus với ID: {id}");

            var ht = await _context.HoaTiet.FindAsync(id);
            if (ht == null)
            {
                Console.WriteLine("❌ Không tìm thấy HoaTiet");
                return NotFound();
            }

            ht.TrangThai = !ht.TrangThai;
            ht.LanCapNhatCuoi = DateTime.Now;
            ht.NguoiCapNhat = "auto-toggle";

            var saved = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Trạng thái mới: {ht.TrangThai}, Save: {saved}");

            return Ok(new { success = true, trangThai = ht.TrangThai });
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string? keyword = null, string? trangThai = null)
        {
            var query = _context.HoaTiet.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TenHoaTiet.Contains(keyword) || x.MaHoaTiet.Contains(keyword));
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