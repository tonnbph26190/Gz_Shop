using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using System.Security.Claims;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ChatController : Controller
    {
        private readonly BanQuanAu1DbContext _context;

        public ChatController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Rooms()
        {
            var rooms = await _context.PhongTroChuyens
                .Include(p => p.KhachHang)
                .Include(p => p.NhanVien)
                .Where(p => p.TrangThai)
                .Select(p => new
                {
                    id = p.IDPhongTroChuyen,
                    customerName = p.KhachHang != null ? p.KhachHang.TenKhachHang : "Khách hàng",
                    customerPhone = p.KhachHang != null ? p.KhachHang.SoDienThoai : "",
                    staffName = p.NhanVien != null ? p.NhanVien.TenNhanVien : "Nhân viên",
                    updatedAt = p.LanCapNhatCuoi ?? p.NgayTao,
                    lastMessage = p.TinNhans != null
                        ? p.TinNhans
                            .Where(t => t.TrangThai)
                            .OrderByDescending(t => t.NgayTao)
                            .Select(t => t.NoiDung)
                            .FirstOrDefault()
                        : null
                })
                .OrderByDescending(p => p.updatedAt)
                .ToListAsync();

            return Json(new { success = true, data = rooms });
        }

        [HttpGet]
        public async Task<IActionResult> Messages(Guid id)
        {
            var room = await _context.PhongTroChuyens
                .Include(p => p.KhachHang)
                .Include(p => p.NhanVien)
                .FirstOrDefaultAsync(p => p.IDPhongTroChuyen == id && p.TrangThai);

            if (room == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy phòng trò chuyện." });
            }

            var messages = await _context.TinNhans
                .Where(t => t.IDPhongTroChuyen == id && t.TrangThai)
                .OrderBy(t => t.NgayTao)
                .Select(t => new
                {
                    id = t.IDTinNhan,
                    content = t.NoiDung,
                    createdAt = t.NgayTao,
                    senderType = t.IDNhanVien != null ? "admin" : "customer",
                    senderName = t.IDNhanVien != null
                        ? (t.NhanVien != null ? t.NhanVien.TenNhanVien : "Nhân viên")
                        : (t.KhachHang != null ? t.KhachHang.TenKhachHang : "Khách hàng")
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                room = new
                {
                    id = room.IDPhongTroChuyen,
                    customerName = room.KhachHang?.TenKhachHang ?? "Khách hàng",
                    customerPhone = room.KhachHang?.SoDienThoai ?? "",
                    staffName = room.NhanVien?.TenNhanVien ?? "Nhân viên"
                },
                data = messages
            });
        }

        [HttpPost]
        public async Task<IActionResult> Send(Guid id, string content)
        {
            var staffId = GetCurrentStaffId();
            if (staffId == null)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên đang đăng nhập." });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { success = false, message = "Tin nhắn không được để trống." });
            }

            var room = await _context.PhongTroChuyens
                .FirstOrDefaultAsync(p => p.IDPhongTroChuyen == id && p.TrangThai);

            if (room == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy phòng trò chuyện." });
            }

            room.IDNhanVien = staffId.Value;
            room.LanCapNhatCuoi = DateTime.UtcNow;
            room.NguoiCapNhat = User.Identity?.Name;

            var message = new TinNhan
            {
                IDTinNhan = Guid.NewGuid(),
                MaTinNhan = "TN" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IDPhongTroChuyen = room.IDPhongTroChuyen,
                IDNhanVien = staffId.Value,
                NoiDung = content.Trim(),
                NgayTao = DateTime.UtcNow,
                NguoiTao = User.Identity?.Name,
                TrangThai = true
            };

            _context.TinNhans.Add(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private Guid? GetCurrentStaffId()
        {
            var claimValue = User.FindFirst("custom:id_nhanvien")?.Value;
            return Guid.TryParse(claimValue, out var id) ? id : null;
        }
    }
}
