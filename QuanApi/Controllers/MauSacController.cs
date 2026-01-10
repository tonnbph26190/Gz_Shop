using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MauSacController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public MauSacController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            var query = _context.MauSacs.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenMauSac.Contains(keyword) || x.MaMauSac.Contains(keyword));

            var result = await query.ToListAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var ms = await _context.MauSacs.FindAsync(id);
            if (ms == null) return NotFound();
            return Ok(ms);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(MauSac ms)
        {
            Console.WriteLine("✅ API nhận Create MauSac:");
            Console.WriteLine($"TenMauSac: {ms.TenMauSac}, MaMauSac: {ms.MaMauSac}, TrangThai: {ms.TrangThai}");

            ms.IDMauSac = Guid.NewGuid();
            ms.NgayTao = DateTime.Now;

            if (string.IsNullOrEmpty(ms.NguoiTao))
                ms.NguoiTao = "unknown";

            _context.MauSacs.Add(ms);
            var saved = await _context.SaveChangesAsync();

            Console.WriteLine($"✅ SaveChanges: {saved}");

            return Ok(new { success = true });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] MauSac ms)
        {
            var entity = await _context.MauSacs.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenMauSac = ms.TenMauSac;
            entity.MaMauSac = ms.MaMauSac;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(ms.NguoiCapNhat) ? "unknown" : ms.NguoiCapNhat;
            entity.TrangThai = ms.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.MauSacs.FindAsync(id);
            if (entity == null) return NotFound();

            _context.MauSacs.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            Console.WriteLine($"🌀 API nhận ToggleStatus với ID: {id}");

            var ms = await _context.MauSacs.FindAsync(id);
            if (ms == null)
            {
                Console.WriteLine("❌ Không tìm thấy MauSac");
                return NotFound();
            }

            ms.TrangThai = !ms.TrangThai;
            ms.LanCapNhatCuoi = DateTime.Now;
            ms.NguoiCapNhat = "auto-toggle";

            var saved = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Trạng thái mới: {ms.TrangThai}, Save: {saved}");

            return Ok(new { success = true, trangThai = ms.TrangThai });
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string? keyword = null, string? trangThai = null)
        {
            var query = _context.MauSacs.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TenMauSac.Contains(keyword) || x.MaMauSac.Contains(keyword));
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