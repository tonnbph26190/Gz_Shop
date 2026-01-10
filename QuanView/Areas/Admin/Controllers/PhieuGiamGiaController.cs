using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanApi.Data;
using QuanApi.Dtos;
using static QuanApi.Controllers.PhieuGiamGiasController;
using System.Linq; // Added for .Where() and .ToList()
using Microsoft.AspNetCore.Authorization;
using QuanView.ViewModels;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class PhieuGiamGiaController : Controller
    {
        private readonly HttpClient _http;
        private readonly ILogger<PhieuGiamGiaController> _logger;

        private readonly IEmailService _emailService;

        public PhieuGiamGiaController(IHttpClientFactory factory, ILogger<PhieuGiamGiaController> logger, IEmailService emailService)
        {
            _http = factory.CreateClient("MyApi");
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index(string keyword, string trangThai, int page = 1, int pageSize = 10)
        {
            try
            {
                var list = await _http.GetFromJsonAsync<List<PhieuGiamGia>>("PhieuGiamGias")
                           ?? new List<PhieuGiamGia>();

                // Lọc theo từ khóa
                if (!string.IsNullOrEmpty(keyword))
                {
                    list = list.Where(p => 
                        p.MaCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        p.TenPhieu.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // Lọc theo trạng thái
                if (!string.IsNullOrEmpty(trangThai))
                {
                    var now = DateTime.Now;
                    switch (trangThai)
                    {
                        case "sapdienra":
                            list = list.Where(p => p.TrangThai && now < p.NgayBatDau).ToList();
                            break;
                        case "danghoatdong":
                            list = list.Where(p => p.TrangThai && now >= p.NgayBatDau && now <= p.NgayKetThuc).ToList();
                            break;
                        case "hethang":
                            list = list.Where(p => p.TrangThai && now > p.NgayKetThuc).ToList();
                            break;
                        case "ngungapdung":
                            list = list.Where(p => !p.TrangThai).ToList();
                            break;
                    }
                }

                // Tính toán phân trang
                var totalItems = list.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                page = Math.Max(1, Math.Min(page, totalPages));

                var pagedList = list
                    .OrderByDescending(p => p.NgayTao)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Truyền dữ liệu cho View
                ViewBag.Keyword = keyword;
                ViewBag.Status = trangThai;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = totalItems;

                return View(pagedList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh sách phiếu giảm giá.");
                TempData["ErrorMessage"] = "Không thể tải danh sách.";
                return View(new List<PhieuGiamGia>());
            }
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var phieu = await _http.GetFromJsonAsync<PhieuGiamGia>($"PhieuGiamGias/{id}");
            return phieu == null ? NotFound() : View(phieu);
        }

        public async Task<IActionResult> Create()
        {
            return View(new PhieuGiamGia
            {
                NgayBatDau = DateTime.Today,
                NgayKetThuc = DateTime.Today.AddDays(7),
                NguoiTao = User.Identity?.Name ?? "Admin"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhieuGiamGia model, bool GuiEmail = false)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Tạo DTO để gửi lên API
            var createDto = new CreatePhieuGiamGiaDto
            {
                MaCode = model.MaCode,
                TenPhieu = model.TenPhieu,
                GiaTriGiam = model.GiaTriGiam,
                GiaTriGiamToiDa = model.GiaTriGiamToiDa,
                DonToiThieu = model.DonToiThieu,
                SoLuong = model.SoLuong,
                LaCongKhai = model.LaCongKhai,
                NgayBatDau = model.NgayBatDau,
                NgayKetThuc = model.NgayKetThuc,
                TrangThai = model.TrangThai,
                NguoiTao = model.NguoiTao
            };

            var response = await _http.PostAsJsonAsync("PhieuGiamGias", createDto);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Tạo phiếu giảm giá thành công! Tất cả khách hàng đã được nhận 1 phiếu mỗi người.";
                return RedirectToAction(nameof(Index));
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Tạo thất bại: {error}");
            return View(model);
        }


        // GET: Admin/PhieuGiamGia/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            var phieu = await _http.GetFromJsonAsync<PhieuGiamGia>($"PhieuGiamGias/{id}");
            if (phieu == null) return NotFound();

            return View(phieu);
        }

        // POST: Admin/PhieuGiamGia/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PhieuGiamGia model)
        {
            if (id != model.IDPhieuGiamGia) return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.LaCongKhai = true; // Luôn là công khai
            model.SoLuong = 1; // Mỗi khách hàng 1 phiếu
            model.NguoiCapNhat = User.Identity?.Name ?? "Admin";
            model.LanCapNhatCuoi = DateTime.UtcNow;

            var payload = new
            {
                phieu = model,
                khachHangId = (Guid?)null // Không cần khách hàng cụ thể
            };

            var res = await _http.PutAsJsonAsync($"PhieuGiamGias/{id}", payload);
            if (res.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", $"Cập nhật thất bại: {await res.Content.ReadAsStringAsync()}");
            return View(model);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var phieu = await _http.GetFromJsonAsync<PhieuGiamGia>($"PhieuGiamGias/{id}");
            return phieu == null ? NotFound() : View(phieu);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var res = await _http.DeleteAsync($"PhieuGiamGias/{id}");

            TempData[res.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] =
                res.IsSuccessStatusCode ? "Đã xoá thành công." : "Xoá thất bại.";

            return RedirectToAction(nameof(Index));
        }


    }
}
