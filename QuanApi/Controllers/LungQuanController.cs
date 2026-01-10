using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LungQuanController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public LungQuanController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            var query = _context.LungQuans.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenLungQuan.Contains(keyword) || x.MaLungQuan.Contains(keyword));

            var result = await query.ToListAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var lq = await _context.LungQuans.FindAsync(id);
            if (lq == null) return NotFound();
            return Ok(lq);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(LungQuan lq)
        {
            Console.WriteLine("✅ API nhận Create LungQuan:");
            Console.WriteLine($"TenLungQuan: {lq.TenLungQuan}, MaLungQuan: {lq.MaLungQuan}, TrangThai: {lq.TrangThai}");

            lq.IDLungQuan = Guid.NewGuid();
            lq.NgayTao = DateTime.Now;

            if (string.IsNullOrEmpty(lq.NguoiTao))
                lq.NguoiTao = "unknown";

            _context.LungQuans.Add(lq);
            var saved = await _context.SaveChangesAsync();

            Console.WriteLine($"✅ SaveChanges: {saved}");

            return Ok(new { success = true });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] LungQuan lq)
        {
            var entity = await _context.LungQuans.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenLungQuan = lq.TenLungQuan;
            entity.MaLungQuan = lq.MaLungQuan;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(lq.NguoiCapNhat) ? "unknown" : lq.NguoiCapNhat;
            entity.TrangThai = lq.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.LungQuans.FindAsync(id);
            if (entity == null) return NotFound();

            _context.LungQuans.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            Console.WriteLine($"🌀 API nhận ToggleStatus với ID: {id}");

            var lq = await _context.LungQuans.FindAsync(id);
            if (lq == null)
            {
                Console.WriteLine("❌ Không tìm thấy LungQuan");
                return NotFound();
            }

            lq.TrangThai = !lq.TrangThai;
            lq.LanCapNhatCuoi = DateTime.Now;
            lq.NguoiCapNhat = "auto-toggle";

            var saved = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Trạng thái mới: {lq.TrangThai}, Save: {saved}");

            return Ok(new { success = true, trangThai = lq.TrangThai });
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string? keyword = null, string? trangThai = null)
        {
            var query = _context.LungQuans.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TenLungQuan.Contains(keyword) || x.MaLungQuan.Contains(keyword));
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
