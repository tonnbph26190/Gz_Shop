using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
			var tenDangNhap = User.Identity?.Name;

			return await _context.KhachHang
				.FirstOrDefaultAsync(x =>
					x.TenKhachHang == tenDangNhap ||
					x.Email == tenDangNhap ||
					x.MaKhachHang == tenDangNhap);
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

			if (string.IsNullOrWhiteSpace(tenKhachHang))
			{
				ModelState.AddModelError("tenKhachHang", "Tên tài khoản không được để trống");
			}

			if (string.IsNullOrWhiteSpace(email))
			{
				ModelState.AddModelError("email", "Email không được để trống");
			}
			else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
			{
				ModelState.AddModelError("email", "Email không đúng định dạng");
			}

			if (string.IsNullOrWhiteSpace(soDienThoai))
			{
				ModelState.AddModelError("soDienThoai", "Số điện thoại không được để trống");
			}
			else if (!Regex.IsMatch(soDienThoai, @"^(03|05|07|08|09)[0-9]{8}$"))
			{
				ModelState.AddModelError("soDienThoai", "Số điện thoại không đúng định dạng");
			}

			if (!ModelState.IsValid)
			{
				return View(khachHang);
			}

			var tenMoi = tenKhachHang.Trim();
			var emailMoi = email.Trim();
			var sdtMoi = soDienThoai.Trim();

			bool khongThayDoi =
				khachHang.TenKhachHang == tenMoi &&
				khachHang.Email == emailMoi &&
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
			var tenDangNhap = User.Identity?.Name;

			var khachHang = await _context.KhachHang
				.FirstOrDefaultAsync(x =>
					x.TenKhachHang == tenDangNhap ||
					x.Email == tenDangNhap ||
					x.MaKhachHang == tenDangNhap);

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
