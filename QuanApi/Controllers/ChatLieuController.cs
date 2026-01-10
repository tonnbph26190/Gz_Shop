using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatLieuController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public ChatLieuController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            var query = _context.ChatLieus.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenChatLieu.Contains(keyword) || x.MaChatLieu.Contains(keyword));

            var result = await query.ToListAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var cl = await _context.ChatLieus.FindAsync(id);
            if (cl == null) return NotFound();
            return Ok(cl);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(ChatLieu cl)
        {
            Console.WriteLine("✅ API nhận Create ChatLieu:");
            Console.WriteLine($"TenChatLieu: {cl.TenChatLieu}, MaChatLieu: {cl.MaChatLieu}, TrangThai: {cl.TrangThai}");

            cl.IDChatLieu = Guid.NewGuid();
            cl.NgayTao = DateTime.Now;

            if (string.IsNullOrEmpty(cl.NguoiTao))
                cl.NguoiTao = "unknown";

            _context.ChatLieus.Add(cl);
            var saved = await _context.SaveChangesAsync();

            Console.WriteLine($"✅ SaveChanges: {saved}");

            return Ok(new { success = true });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ChatLieu cl)
        {
            var entity = await _context.ChatLieus.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenChatLieu = cl.TenChatLieu;
            entity.MaChatLieu = cl.MaChatLieu;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(cl.NguoiCapNhat) ? "unknown" : cl.NguoiCapNhat;
            entity.TrangThai = cl.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.ChatLieus.FindAsync(id);
            if (entity == null) return NotFound();

            _context.ChatLieus.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            Console.WriteLine($"🌀 API nhận ToggleStatus với ID: {id}");

            var cl = await _context.ChatLieus.FindAsync(id);
            if (cl == null)
            {
                Console.WriteLine("❌ Không tìm thấy ChatLieu");
                return NotFound();
            }

            cl.TrangThai = !cl.TrangThai;
            cl.LanCapNhatCuoi = DateTime.Now;
            cl.NguoiCapNhat = "auto-toggle";

            var saved = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Trạng thái mới: {cl.TrangThai}, Save: {saved}");

            return Ok(new { success = true, trangThai = cl.TrangThai });
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string? keyword = null, string? trangThai = null)
        {
            var query = _context.ChatLieus.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TenChatLieu.Contains(keyword) || x.MaChatLieu.Contains(keyword));
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
