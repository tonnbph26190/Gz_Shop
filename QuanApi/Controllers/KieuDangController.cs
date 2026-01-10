using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KieuDangController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public KieuDangController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            var query = _context.KieuDangs.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenKieuDang.Contains(keyword) || x.MaKieuDang.Contains(keyword));

            var result = await query.ToListAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var kd = await _context.KieuDangs.FindAsync(id);
            if (kd == null) return NotFound();
            return Ok(kd);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(KieuDang kd)
        {
            Console.WriteLine("✅ API nhận Create KieuDang:");
            Console.WriteLine($"TenKieuDang: {kd.TenKieuDang}, MaKieuDang: {kd.MaKieuDang}, TrangThai: {kd.TrangThai}");

            kd.IDKieuDang = Guid.NewGuid();
            kd.NgayTao = DateTime.Now;

            if (string.IsNullOrEmpty(kd.NguoiTao))
                kd.NguoiTao = "unknown";

            _context.KieuDangs.Add(kd);
            var saved = await _context.SaveChangesAsync();

            Console.WriteLine($"✅ SaveChanges: {saved}");

            return Ok(new { success = true });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] KieuDang kd)
        {
            var entity = await _context.KieuDangs.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenKieuDang = kd.TenKieuDang;
            entity.MaKieuDang = kd.MaKieuDang;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(kd.NguoiCapNhat) ? "unknown" : kd.NguoiCapNhat;
            entity.TrangThai = kd.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.KieuDangs.FindAsync(id);
            if (entity == null) return NotFound();

            _context.KieuDangs.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            Console.WriteLine($"🌀 API nhận ToggleStatus với ID: {id}");

            var kd = await _context.KieuDangs.FindAsync(id);
            if (kd == null)
            {
                Console.WriteLine("❌ Không tìm thấy KieuDang");
                return NotFound();
            }

            kd.TrangThai = !kd.TrangThai;
            kd.LanCapNhatCuoi = DateTime.Now;
            kd.NguoiCapNhat = "auto-toggle";

            var saved = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Trạng thái mới: {kd.TrangThai}, Save: {saved}");

            return Ok(new { success = true, trangThai = kd.TrangThai });
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string? keyword = null, string? trangThai = null)
        {
            var query = _context.KieuDangs.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TenKieuDang.Contains(keyword) || x.MaKieuDang.Contains(keyword));
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
