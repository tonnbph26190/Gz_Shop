using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using QuanView.Models; 
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace QuanView.Controllers
{
    public class LoginController : Controller
    {
        private readonly BanQuanAu1DbContext _context;

        public LoginController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.Error = TempData["Error"];
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View();
        }

        public IActionResult Login(string? returnUrl = "/")
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", new { returnUrl })
            };
            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse(string? returnUrl = "/")
        {
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var avatar = User.FindFirst("picture")?.Value;

            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                TempData["Error"] = "Đăng nhập thất bại hoặc bị hủy. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }

             email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Không thể lấy email từ tài khoản Google.";
                return RedirectToAction("Index");
            }

            email = email.Trim().ToLower();
            Console.WriteLine($"Email Google: {email}");

            var nhanVien = await _context.NhanViens
                .Include(nv => nv.VaiTro)
                .FirstOrDefaultAsync(nv => nv.Email != null && nv.Email.Trim().ToLower() == email && nv.TrangThai);

            if (nhanVien != null)
            {
                Console.WriteLine($"Tìm thấy NhanVien: {nhanVien.Email}, IDVaiTro: {nhanVien.IDVaiTro}, VaiTro: {(nhanVien.VaiTro != null ? nhanVien.VaiTro.MaVaiTro : "null")}");

                if (nhanVien.VaiTro != null && 
                    (nhanVien.VaiTro.MaVaiTro?.ToLower() == "admin" || nhanVien.VaiTro.MaVaiTro?.ToLower() == "nhanvien"))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, nhanVien.TenNhanVien ?? email.Split('@')[0]),
                        new Claim(ClaimTypes.Email, nhanVien.Email),
                        new Claim(ClaimTypes.Role, nhanVien.VaiTro.MaVaiTro),
                        new Claim("custom:id_nhanvien", nhanVien.IDNhanVien.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                    return RedirectToAction("Index", "ProductManage", new { area = "Admin" });

                }
                else
                {
                    Console.WriteLine($"NhanVien không có vai trò admin. VaiTro: {(nhanVien.VaiTro != null ? nhanVien.VaiTro.MaVaiTro : "null")}");
                }
            }
            else
            {
                Console.WriteLine($"Không tìm thấy NhanVien cho email: {email}");
            }

            // Tìm hoặc tạo khách hàng
            var khachHang = await _context.KhachHang
                .FirstOrDefaultAsync(kh => kh.Email != null && kh.Email.Trim().ToLower() == email && kh.TrangThai);

            if (khachHang == null)
            {
                khachHang = new KhachHang
                {
                    MaKhachHang = $"KH{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                    Email = email,
                    TenKhachHang = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? email.Split('@')[0],
                    SoDienThoai = "0000000000",
                    NgayTao = DateTime.Now,
                    TrangThai = true
                };
                _context.KhachHang.Add(khachHang);
                await _context.SaveChangesAsync();
            }

            var khachClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, khachHang.TenKhachHang),
                new Claim(ClaimTypes.Email, khachHang.Email),
                new Claim(ClaimTypes.Role, "KhachHang"),
                new Claim("custom:id_khachhang", khachHang.IDKhachHang.ToString())
            };

            var khachIdentity = new ClaimsIdentity(khachClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false, // Không lưu trữ lâu dài
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2) // Hết hạn sau 2 giờ
            };
            
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(khachIdentity), authProperties);

            // Lưu thông tin khách hàng vào session
            HttpContext.Session.SetString("CustomerId", khachHang.IDKhachHang.ToString());

            Console.WriteLine($"Đăng nhập thành công: {khachHang.TenKhachHang} - {khachHang.Email}");
            Console.WriteLine($"Claims: {string.Join(", ", khachClaims.Select(c => $"{c.Type}={c.Value}"))}");

            return LocalRedirect(returnUrl ?? "/");
        }

        // Đăng nhập bằng form (GET)
        [HttpGet]
        public IActionResult FormLogin()
        {
            ViewBag.Error = TempData["Error"];
            return View(); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormLogin(LoginViewModel model)
        {
            Console.WriteLine($"🔍 Đang xử lý đăng nhập với email: {model.Email}");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("❌ ModelState không hợp lệ");
                return View("Index", model);
            }

            // Check nhân viên
            var nhanVien = await _context.NhanViens
                .Include(nv => nv.VaiTro)
                .FirstOrDefaultAsync(nv => nv.Email == model.Email && nv.MatKhau == model.Password && nv.TrangThai);

            Console.WriteLine($"🔍 Tìm thấy nhân viên: {(nhanVien != null ? "Có" : "Không")}");
            if (nhanVien != null)
            {
                Console.WriteLine($"🔍 Vai trò nhân viên: {(nhanVien.VaiTro != null ? nhanVien.VaiTro.MaVaiTro : "NULL")}");
            }

            if (nhanVien != null && nhanVien.VaiTro != null &&
                (nhanVien.VaiTro.MaVaiTro?.ToLower() == "admin" || nhanVien.VaiTro.MaVaiTro?.ToLower() == "nhanvien"))
            {
                Console.WriteLine("✅ Đăng nhập thành công với vai trò admin/nhân viên");
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, nhanVien.TenNhanVien ?? model.Email.Split('@')[0]),
                    new Claim(ClaimTypes.Email, nhanVien.Email),
                    new Claim(ClaimTypes.Role, nhanVien.VaiTro.MaVaiTro),
                    new Claim("custom:id_nhanvien", nhanVien.IDNhanVien.ToString())
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                return RedirectToAction("Index", "ProductManage", new { area = "Admin" });
            }

            var khachHang = await _context.KhachHang
                .FirstOrDefaultAsync(kh => kh.Email == model.Email && kh.MatKhau == model.Password && kh.TrangThai);

            Console.WriteLine($"🔍 Tìm thấy khách hàng: {(khachHang != null ? "Có" : "Không")}");

            if (khachHang != null)
            {
                Console.WriteLine("✅ Đăng nhập thành công với vai trò khách hàng");
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, khachHang.TenKhachHang),
                    new Claim(ClaimTypes.Email, khachHang.Email),
                    new Claim(ClaimTypes.Role, "KhachHang"),
                    new Claim("custom:id_khachhang", khachHang.IDKhachHang.ToString())
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false, // Không lưu trữ lâu dài
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2) // Hết hạn sau 2 giờ
                };
                
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);
                
                // Lưu thông tin khách hàng vào session
                HttpContext.Session.SetString("CustomerId", khachHang.IDKhachHang.ToString());
                
                return RedirectToAction("Index", "Home");
            }

            Console.WriteLine("❌ Đăng nhập thất bại - Email hoặc mật khẩu không đúng");
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View("Index", model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Error = TempData["Error"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _context.KhachHang.AnyAsync(kh => kh.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
                return View(model);
            }

            var khachHang = new KhachHang
            {
                IDKhachHang = Guid.NewGuid(),
                MaKhachHang = "KH" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                TenKhachHang = model.TenKhachHang,
                Email = model.Email,
                MatKhau = model.Password,
                SoDienThoai = model.SoDienThoai,
                NgayTao = DateTime.Now,
                TrangThai = true
            };
            _context.KhachHang.Add(khachHang);
            await _context.SaveChangesAsync();

            // Lưu thông báo thành công vào TempData
            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập để tiếp tục.";
            
            return RedirectToAction("Index", "Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Xóa thông tin khách hàng khỏi session
            HttpContext.Session.Remove("CustomerId");
            
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult TestAuth()
        {
            var result = new
            {
                IsAuthenticated = User.Identity.IsAuthenticated,
                UserName = User.Identity.Name,
                Role = User.FindFirst(ClaimTypes.Role)?.Value,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                CustomerId = User.FindFirst("custom:id_khachhang")?.Value,
                SessionCustomerId = HttpContext.Session.GetString("CustomerId"),
                AllClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            };
            
            return Json(result);
        }

        [HttpGet]
        [Authorize]
        public IActionResult CheckAdminAccess()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var isAdmin = role == "admin" || role == "nhanvien";
            
            var result = new
            {
                CanAccessAdmin = isAdmin,
                Role = role,
                UserName = User.Identity.Name,
                Message = isAdmin ? "Có quyền truy cập Admin" : "Không có quyền truy cập Admin"
            };
            
            return Json(result);
        }
    }
}

