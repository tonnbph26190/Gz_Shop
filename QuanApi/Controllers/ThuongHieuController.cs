using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThuongHieuController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public ThuongHieuController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            var query = _context.ThuongHieus.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenThuongHieu.Contains(keyword) || x.MaThuongHieu.Contains(keyword));

            var result = await query.ToListAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var th = await _context.ThuongHieus.FindAsync(id);
            if (th == null) return NotFound();
            return Ok(th);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(ThuongHieu th)
        {
            Console.WriteLine("✅ API nhận Create ThuongHieu:");
            Console.WriteLine($"TenThuongHieu: {th.TenThuongHieu}, MaThuongHieu: {th.MaThuongHieu}, TrangThai: {th.TrangThai}");

            th.IDThuongHieu = Guid.NewGuid();
            th.NgayTao = DateTime.Now;

            if (string.IsNullOrEmpty(th.NguoiTao))
                th.NguoiTao = "unknown";

            _context.ThuongHieus.Add(th);
            var saved = await _context.SaveChangesAsync();

            Console.WriteLine($"✅ SaveChanges: {saved}");

            return Ok(new { success = true });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ThuongHieu th)
        {
            var entity = await _context.ThuongHieus.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenThuongHieu = th.TenThuongHieu;
            entity.MaThuongHieu = th.MaThuongHieu;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(th.NguoiCapNhat) ? "unknown" : th.NguoiCapNhat;
            entity.TrangThai = th.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.ThuongHieus.FindAsync(id);
            if (entity == null) return NotFound();

            _context.ThuongHieus.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            Console.WriteLine($"🌀 API nhận ToggleStatus với ID: {id}");

            var th = await _context.ThuongHieus.FindAsync(id);
            if (th == null)
            {
                Console.WriteLine("❌ Không tìm thấy ThuongHieu");
                return NotFound();
            }

            th.TrangThai = !th.TrangThai;
            th.LanCapNhatCuoi = DateTime.Now;
            th.NguoiCapNhat = "auto-toggle";

            var saved = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Trạng thái mới: {th.TrangThai}, Save: {saved}");

            return Ok(new { success = true, trangThai = th.TrangThai });
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string? keyword = null, string? trangThai = null)
        {
            var query = _context.ThuongHieus.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TenThuongHieu.Contains(keyword) || x.MaThuongHieu.Contains(keyword));
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
