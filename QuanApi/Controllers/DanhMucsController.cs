using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DanhMucsController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public DanhMucsController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            var query = _context.DanhMucs.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenDanhMuc.Contains(keyword));

            var result = await query.ToListAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var dm = await _context.DanhMucs.FindAsync(id);
            if (dm == null) return NotFound();
            return Ok(dm);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(DanhMuc dm)
        {
            Console.WriteLine("✅ API nhận Create:");
            Console.WriteLine($"TenDanhMuc: {dm.TenDanhMuc}, MaDanhMuc: {dm.MaDanhMuc}, TrangThai: {dm.TrangThai}");

            dm.IDDanhMuc = Guid.NewGuid();
            dm.NgayTao = DateTime.Now;

            if (string.IsNullOrEmpty(dm.NguoiTao))
                dm.NguoiTao = "unknown";

            _context.DanhMucs.Add(dm);
            var saved = await _context.SaveChangesAsync();

            Console.WriteLine($"✅ SaveChanges: {saved}");

            return Ok(new { success = true });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] DanhMuc dm)
        {
            var entity = await _context.DanhMucs.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenDanhMuc = dm.TenDanhMuc;
            entity.MaDanhMuc = dm.MaDanhMuc;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(dm.NguoiCapNhat) ? "unknown" : dm.NguoiCapNhat;
            entity.TrangThai = dm.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.DanhMucs.FindAsync(id);
            if (entity == null) return NotFound();

            _context.DanhMucs.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            Console.WriteLine($"🌀 API nhận ToggleStatus với ID: {id}");

            var dm = await _context.DanhMucs.FindAsync(id);
            if (dm == null)
            {
                Console.WriteLine("❌ Không tìm thấy DanhMuc");
                return NotFound();
            }

            dm.TrangThai = !dm.TrangThai;
            dm.LanCapNhatCuoi = DateTime.Now;
            dm.NguoiCapNhat = "auto-toggle"; // có thể sửa lại sau

            var saved = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Trạng thái mới: {dm.TrangThai}, Save: {saved}");

            return Ok(new { success = true, trangThai = dm.TrangThai });
        }


        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string? keyword = null, string? trangThai = null)
        {
            var query = _context.DanhMucs.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TenDanhMuc.Contains(keyword) || x.MaDanhMuc.Contains(keyword));
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
