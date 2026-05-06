using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QuanView.Models;
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

		// Fixed code
		public IActionResult GoogleLogin(string? returnUrl = "/")
		{
			var props = new AuthenticationProperties
			{
				RedirectUri = Url.Action("GoogleResponse", new { returnUrl })
			};
			return Challenge(props, GoogleDefaults.AuthenticationScheme);
		}

		public IActionResult GoogleSignUp(string? returnUrl = "/")
		{
			var props = new AuthenticationProperties
			{
				RedirectUri = Url.Action("GoogleResponse", new { returnUrl, isSignUp = true })
			};

			props.Items.Add("prompt", "select_account");

			return Challenge(props, GoogleDefaults.AuthenticationScheme);
		}

		public async Task<IActionResult> GoogleResponse(string? returnUrl = "/", bool isSignUp = false)
		{
			// 1. Get the external login info
			var result = await HttpContext.AuthenticateAsync("ExternalCookie");

			if (!result.Succeeded || result.Principal == null)
			{
				TempData["Error"] = "Đăng nhập Google thất bại.";
				return RedirectToAction("Index");
			}

			// 2. Extract claims
			var email = result.Principal.FindFirstValue(ClaimTypes.Email)?.ToLower().Trim();
			var name = result.Principal.FindFirstValue(ClaimTypes.Name);

			if (string.IsNullOrEmpty(email))
			{
				TempData["Error"] = "Không lấy được email từ Google.";
				return RedirectToAction("Index");
			}

			// 3. Logic for Registration vs Login
			var existingUser = await _context.KhachHang.AnyAsync(kh => kh.Email == email);

			if (isSignUp && existingUser)
			{
				TempData["Error"] = "Email này đã được đăng ký. Vui lòng đăng nhập.";
				await HttpContext.SignOutAsync("ExternalCookie");
				return RedirectToAction("Index");
			}

			// 4. Check Database for NhanVien (Admin/Staff)
			var nhanVien = await _context.NhanViens
				.Include(nv => nv.VaiTro)
				.FirstOrDefaultAsync(nv => nv.Email.ToLower() == email && nv.TrangThai);

			if (nhanVien != null && nhanVien.VaiTro != null)
			{
				var claims = new List<Claim>
		{
			new Claim(ClaimTypes.Name, nhanVien.TenNhanVien ?? name),
			new Claim(ClaimTypes.Email, nhanVien.Email),
			new Claim(ClaimTypes.Role, nhanVien.VaiTro.MaVaiTro),
			new Claim("custom:id_nhanvien", nhanVien.IDNhanVien.ToString())
		};

				await SignInUser(claims);
				await HttpContext.SignOutAsync("ExternalCookie"); // Cleanup
				return RedirectToAction("Index", "ProductManage", new { area = "Admin" });
			}

			// 5. Check/Create KhachHang (Customer)
			var khachHang = await _context.KhachHang
				.FirstOrDefaultAsync(kh => kh.Email.ToLower() == email && kh.TrangThai);

			if (khachHang == null)
			{
				khachHang = new KhachHang
				{
					IDKhachHang = Guid.NewGuid(),
					MaKhachHang = $"KH{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
					Email = email,
					TenKhachHang = name ?? email.Split('@')[0],
					SoDienThoai = "0000000000",
					NgayTao = DateTime.UtcNow,
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

			await SignInUser(khachClaims);
			HttpContext.Session.SetString("CustomerId", khachHang.IDKhachHang.ToString());

			// Always sign out of the temporary cookie at the end
			await HttpContext.SignOutAsync("ExternalCookie");

			return LocalRedirect(returnUrl ?? "/");
		}

		// Helper method to sign into the local cookie
		private async Task SignInUser(List<Claim> claims)
		{
			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var principal = new ClaimsPrincipal(identity);
			var authProperties = new AuthenticationProperties { IsPersistent = true };

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
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
				return RedirectToAction("Index", "ThongKe", new { area = "Admin" });
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
            if (await _context.KhachHang.AnyAsync(kh => kh.SoDienThoai == model.SoDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại đã được sử dụng.");
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
                NgayTao = DateTime.UtcNow,
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

