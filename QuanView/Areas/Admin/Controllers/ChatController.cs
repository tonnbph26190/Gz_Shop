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
        public async Task<IActionResult> PopupRooms()
        {
            var rooms = await _context.PhongTroChuyens
                .AsNoTracking()
                .Include(p => p.KhachHang)
                .Include(p => p.NhanVien)
                .Where(p => p.TrangThai)
                .Select(p => new
                {
                    id = p.IDPhongTroChuyen,
                    customerName = p.KhachHang != null ? p.KhachHang.TenKhachHang : "Khach hang",
                    customerPhone = p.KhachHang != null ? p.KhachHang.SoDienThoai : "",
                    staffName = p.NhanVien != null ? p.NhanVien.TenNhanVien : "Nhan vien",
                    updatedAt = p.LanCapNhatCuoi ?? p.NgayTao
                })
                .OrderByDescending(p => p.updatedAt)
                .ToListAsync();

            var roomIds = rooms.Select(r => r.id).ToList();
            var lastMessages = await _context.TinNhans
                .AsNoTracking()
                .Where(t => t.TrangThai && roomIds.Contains(t.IDPhongTroChuyen))
                .OrderByDescending(t => t.NgayTao)
                .Select(t => new
                {
                    roomId = t.IDPhongTroChuyen,
                    content = t.NoiDung
                })
                .ToListAsync();

            var lastMessageByRoom = lastMessages
                .GroupBy(t => t.roomId)
                .ToDictionary(g => g.Key, g => g.First().content);

            var result = rooms.Select(room => new
            {
                room.id,
                room.customerName,
                room.customerPhone,
                room.staffName,
                room.updatedAt,
                lastMessage = lastMessageByRoom.GetValueOrDefault(room.id)
            });

            return Json(new { success = true, data = result });
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
        public async Task<IActionResult> UnreadSummary()
        {
            var rooms = await _context.PhongTroChuyens
                .AsNoTracking()
                .Where(p => p.TrangThai)
                .Select(p => new
                {
                    roomId = p.IDPhongTroChuyen,
                    customerName = p.KhachHang != null ? p.KhachHang.TenKhachHang : "Khach hang",
                    lastMessage = p.TinNhans != null
                        ? p.TinNhans
                            .Where(t => t.TrangThai)
                            .OrderByDescending(t => t.NgayTao)
                            .Select(t => new
                            {
                                id = t.IDTinNhan,
                                content = t.NoiDung,
                                createdAt = t.NgayTao,
                                senderType = t.IDNhanVien != null ? "admin" : "customer"
                            })
                            .FirstOrDefault()
                        : null
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = rooms
                    .Where(r => r.lastMessage != null && r.lastMessage.senderType == "customer")
                    .Select(r => new
                    {
                        r.roomId,
                        r.customerName,
                        r.lastMessage!.id,
                        r.lastMessage.content,
                        r.lastMessage.createdAt,
                        r.lastMessage.senderType
                    })
            });
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
            var staffId = await GetCurrentStaffIdAsync();
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

        private async Task<Guid?> GetCurrentStaffIdAsync()
        {
            var claimValue = User.FindFirst("custom:id_nhanvien")?.Value;
            if (Guid.TryParse(claimValue, out var id))
            {
                return id;
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var staffByEmail = await _context.NhanViens
                    .AsNoTracking()
                    .Where(n => n.TrangThai && n.Email == email)
                    .Select(n => n.IDNhanVien)
                    .FirstOrDefaultAsync();

                if (staffByEmail != Guid.Empty)
                {
                    return staffByEmail;
                }
            }

            var name = User.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                var staffByName = await _context.NhanViens
                    .AsNoTracking()
                    .Where(n => n.TrangThai && (n.TenNhanVien == name || n.MaNhanVien == name))
                    .Select(n => n.IDNhanVien)
                    .FirstOrDefaultAsync();

                if (staffByName != Guid.Empty)
                {
                    return staffByName;
                }
            }

            return null;
        }
    }
}
