using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace QuanView.Controllers
{
	[Authorize]
	public class UserController : Controller
	{
		private readonly BanQuanAu1DbContext _context;
		private readonly IWebHostEnvironment _environment;

		public UserController(BanQuanAu1DbContext context, IWebHostEnvironment environment)
		{
			_context = context;
			_environment = environment;
		}

		private async Task<KhachHang?> GetCurrentKhachHang()
		{
			var id =
				User.FindFirst("custom:id_khachhang")?.Value ??
				User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
				HttpContext.Session.GetString("CustomerId");

			if (Guid.TryParse(id, out var idKhachHang))
			{
				return await _context.KhachHang
					.FirstOrDefaultAsync(x => x.IDKhachHang == idKhachHang);
			}

			var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
			var name = User.Identity?.Name;

			return await _context.KhachHang.FirstOrDefaultAsync(x =>
				(!string.IsNullOrEmpty(email) && x.Email == email) ||
				(!string.IsNullOrEmpty(name) &&
					(x.TenKhachHang == name || x.Email == name || x.MaKhachHang == name)));
		}

		public async Task<IActionResult> Profile()
		{
			var khachHang = await GetCurrentKhachHang();

			if (khachHang == null)
			{
				return NotFound();
			}

			return View(khachHang);
		}

		[HttpPost]
		public async Task<IActionResult> Profile(
			string tenKhachHang,
			string email,
			string soDienThoai,
			IFormFile? avatar)
		{
			var khachHang = await GetCurrentKhachHang();

			if (khachHang == null)
			{
				return NotFound();
			}

			var tenMoi = tenKhachHang?.Trim() ?? string.Empty;
			var emailMoi = email?.Trim().ToLowerInvariant() ?? string.Empty;
			var sdtMoi = soDienThoai?.Trim() ?? string.Empty;

			if (string.IsNullOrWhiteSpace(tenMoi))
			{
				ModelState.AddModelError("tenKhachHang", "Tên tài khoản không được để trống");
			}

			if (string.IsNullOrWhiteSpace(emailMoi))
			{
				ModelState.AddModelError("email", "Email không được để trống");
			}
			else if (!Regex.IsMatch(emailMoi, @"^[a-z0-9](?:[a-z0-9._%+-]{0,62}[a-z0-9])?@gmail\.com$"))
			{
				ModelState.AddModelError("email", "Email không đúng định dạng");
			}

			if (string.IsNullOrWhiteSpace(sdtMoi))
			{
				ModelState.AddModelError("soDienThoai", "Số điện thoại không được để trống");
			}
			else if (!Regex.IsMatch(sdtMoi, @"^(03|05|07|08|09)[0-9]{8}$"))
			{
				ModelState.AddModelError("soDienThoai", "Số điện thoại không đúng định dạng");
			}

			if (!ModelState.IsValid)
			{
				return View(khachHang);
			}

			if ((khachHang.Email ?? "").ToLower() != emailMoi)
			{
				var emailDaTonTai = await _context.KhachHang.AnyAsync(x =>
					x.Email.ToLower() == emailMoi &&
					x.IDKhachHang != khachHang.IDKhachHang);

				if (emailDaTonTai)
				{
					ModelState.AddModelError("email", "Email này đã được sử dụng bởi tài khoản khác");
					return View(khachHang);
				}
			}
			if (khachHang.SoDienThoai != sdtMoi)
			{
				var sdtDaTonTai = await _context.KhachHang.AnyAsync(x =>
					x.SoDienThoai == sdtMoi &&
					x.IDKhachHang != khachHang.IDKhachHang);

				if (sdtDaTonTai)
				{
					ModelState.AddModelError("soDienThoai", "Số điện thoại này đã được sử dụng bởi tài khoản khác");
					return View(khachHang);
				}
			}
			bool khongThayDoi =
	khachHang.TenKhachHang == tenMoi &&
	(khachHang.Email ?? "").ToLower() == emailMoi &&
	khachHang.SoDienThoai == sdtMoi &&
	avatar == null;

			if (khongThayDoi)
			{
				TempData["Error"] = "Bạn chưa thay đổi thông tin nào";
				return RedirectToAction("Profile");
			}

			khachHang.TenKhachHang = tenMoi;
			khachHang.Email = emailMoi;
			khachHang.SoDienThoai = sdtMoi;
			khachHang.LanCapNhatCuoi = DateTime.UtcNow;
			khachHang.NguoiCapNhat = tenMoi;
			if (avatar != null && avatar.Length > 0)
			{
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
				var extension = Path.GetExtension(avatar.FileName).ToLower();

				if (!allowedExtensions.Contains(extension))
				{
					ModelState.AddModelError("avatar", "Chỉ được chọn ảnh .jpg, .jpeg, .png hoặc .webp");
					return View(khachHang);
				}

				var folder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");

				if (!Directory.Exists(folder))
				{
					Directory.CreateDirectory(folder);
				}

				var fileName = Guid.NewGuid() + extension;
				var filePath = Path.Combine(folder, fileName);

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await avatar.CopyToAsync(stream);
				}

				khachHang.AnhDaiDien = "/uploads/avatars/" + fileName;
			}

			await _context.SaveChangesAsync();

			var claims = new List<Claim>
{
	new Claim(ClaimTypes.NameIdentifier, khachHang.IDKhachHang.ToString()),
	new Claim(ClaimTypes.Name, khachHang.TenKhachHang),
	new Claim(ClaimTypes.Email, khachHang.Email ?? ""),
	new Claim(ClaimTypes.Role, "KhachHang"),
	new Claim("custom:id_khachhang", khachHang.IDKhachHang.ToString())
};

			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

			await HttpContext.SignInAsync(
				CookieAuthenticationDefaults.AuthenticationScheme,
				new ClaimsPrincipal(identity));

			TempData["Success"] = "Cập nhật thông tin thành công";
			return RedirectToAction("Profile");
		}
		public IActionResult ChangePassword()
		{
			return View();
		}
		[HttpPost]
		public async Task<IActionResult> ChangePassword(
	string matKhauCu,
	string matKhauMoi,
	string xacNhanMatKhau)
		{
			var khachHang = await GetCurrentKhachHang();

			if (khachHang == null)
			{
				return NotFound();
			}

			if (string.IsNullOrWhiteSpace(matKhauCu))
			{
				ModelState.AddModelError("matKhauCu", "Vui lòng nhập mật khẩu cũ");
			}

			if (string.IsNullOrWhiteSpace(matKhauMoi))
			{
				ModelState.AddModelError("matKhauMoi", "Vui lòng nhập mật khẩu mới");
			}
			else if (matKhauMoi.Length < 6)
			{
				ModelState.AddModelError("matKhauMoi", "Mật khẩu mới phải có ít nhất 6 ký tự");
			}

			if (matKhauMoi != xacNhanMatKhau)
			{
				ModelState.AddModelError("xacNhanMatKhau", "Xác nhận mật khẩu không khớp");
			}

			if (!ModelState.IsValid)
			{
				return View();
			}

			if (khachHang.MatKhau != matKhauCu)
			{
				ModelState.AddModelError("matKhauCu", "Mật khẩu cũ không đúng");
				return View();
			}

			khachHang.MatKhau = matKhauMoi;
			khachHang.LanCapNhatCuoi = DateTime.UtcNow;
			khachHang.NguoiCapNhat = khachHang.TenKhachHang;

			await _context.SaveChangesAsync();

			TempData["Success"] = "Đổi mật khẩu thành công";

			return RedirectToAction("Index", "Home");
		}
	}
}
