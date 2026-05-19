using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using System.Security.Claims;

namespace QuanView.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly BanQuanAu1DbContext _context;

        public ChatController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var room = await GetOrCreateCustomerRoomAsync(customer);
            if (room == null)
            {
                ViewBag.ChatError = "Chưa có nhân viên hỗ trợ đang hoạt động.";
            }
            else
            {
                ViewBag.RoomId = room.IDPhongTroChuyen;
                ViewBag.StaffName = room.NhanVien?.TenNhanVien ?? "Nhân viên hỗ trợ";
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Messages()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });
            }

            var room = await GetOrCreateCustomerRoomAsync(customer);
            if (room == null)
            {
                return BadRequest(new { success = false, message = "Chưa có nhân viên hỗ trợ đang hoạt động." });
            }

            var messages = await _context.TinNhans
                .Where(t => t.IDPhongTroChuyen == room.IDPhongTroChuyen && t.TrangThai)
                .OrderBy(t => t.NgayTao)
                .Select(t => new
                {
                    id = t.IDTinNhan,
                    content = t.NoiDung,
                    createdAt = t.NgayTao,
                    senderType = t.IDKhachHang != null ? "customer" : "admin",
                    senderName = t.IDKhachHang != null
                        ? (t.KhachHang != null ? t.KhachHang.TenKhachHang : "Bạn")
                        : (t.NhanVien != null ? t.NhanVien.TenNhanVien : "Nhân viên")
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                room = new
                {
                    id = room.IDPhongTroChuyen,
                    staffName = room.NhanVien?.TenNhanVien ?? "Nhân viên hỗ trợ"
                },
                data = messages
            });
        }

        [HttpPost]
        public async Task<IActionResult> Send(string content)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { success = false, message = "Tin nhắn không được để trống." });
            }

            var room = await GetOrCreateCustomerRoomAsync(customer);
            if (room == null)
            {
                return BadRequest(new { success = false, message = "Chưa có nhân viên hỗ trợ đang hoạt động." });
            }

            room.LanCapNhatCuoi = DateTime.UtcNow;
            room.NguoiCapNhat = customer.TenKhachHang;

            var message = new TinNhan
            {
                IDTinNhan = Guid.NewGuid(),
                MaTinNhan = "TN" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IDPhongTroChuyen = room.IDPhongTroChuyen,
                IDKhachHang = customer.IDKhachHang,
                NoiDung = content.Trim(),
                NgayTao = DateTime.UtcNow,
                NguoiTao = customer.TenKhachHang,
                TrangThai = true
            };

            _context.TinNhans.Add(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private async Task<KhachHang?> GetCurrentCustomerAsync()
        {
            var claimValue = User.FindFirst("custom:id_khachhang")?.Value;
            if (Guid.TryParse(claimValue, out var claimId))
            {
                return await _context.KhachHang.FirstOrDefaultAsync(k => k.IDKhachHang == claimId && k.TrangThai);
            }

            var sessionValue = HttpContext.Session.GetString("CustomerId");
            if (Guid.TryParse(sessionValue, out var sessionId))
            {
                return await _context.KhachHang.FirstOrDefaultAsync(k => k.IDKhachHang == sessionId && k.TrangThai);
            }

            var name = User.Identity?.Name;
            return await _context.KhachHang.FirstOrDefaultAsync(k =>
                k.TrangThai && (k.TenKhachHang == name || k.Email == name || k.MaKhachHang == name));
        }

        private async Task<PhongTroChuyen?> GetOrCreateCustomerRoomAsync(KhachHang customer)
        {
            var room = await _context.PhongTroChuyens
                .Include(p => p.NhanVien)
                .FirstOrDefaultAsync(p => p.IDKhachHang == customer.IDKhachHang && p.TrangThai);

            if (room != null)
            {
                return room;
            }

            var staff = await _context.NhanViens
                .Include(n => n.VaiTro)
                .Where(n => n.TrangThai && n.VaiTro != null &&
                            (n.VaiTro.MaVaiTro == "ADMIN" || n.VaiTro.MaVaiTro == "NHANVIEN"))
                .OrderBy(n => n.NgayTao)
                .FirstOrDefaultAsync();

            if (staff == null)
            {
                return null;
            }

            room = new PhongTroChuyen
            {
                IDPhongTroChuyen = Guid.NewGuid(),
                MaPhongTroChuyen = "CHAT" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IDKhachHang = customer.IDKhachHang,
                IDNhanVien = staff.IDNhanVien,
                NgayTao = DateTime.UtcNow,
                NguoiTao = customer.TenKhachHang,
                TrangThai = true
            };

            _context.PhongTroChuyens.Add(room);
            await _context.SaveChangesAsync();

            room.NhanVien = staff;
            return room;
        }
    }
}
