using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Dtos;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class DotGiamGiaController : Controller
    {
        private readonly HttpClient _httpClient;

        public DotGiamGiaController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("MyApi");
        }

        // GET: Danh sách
        public async Task<IActionResult> Index(string maDot, string tenDot, int? phanTramGiam, DateTime? tuNgay, DateTime? denNgay, string trangThai, int page = 1, int pageSize = 10)
        {
            var url = $"/api/DotGiamGias?" +
                      $"maDot={maDot}&tenDot={tenDot}&phanTramGiam={phanTramGiam}&" +
                      (tuNgay.HasValue ? $"tuNgay={tuNgay:yyyy-MM-dd}&" : "") +
                      (denNgay.HasValue ? $"denNgay={denNgay:yyyy-MM-dd}&" : "") +
                      $"trangThai={trangThai}&page={page}&pageSize={pageSize}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return View(new PagedResultGeneric<DotGiamGia>());

            var data = await response.Content.ReadFromJsonAsync<PagedResultGeneric<DotGiamGia>>();
            return View(data ?? new PagedResultGeneric<DotGiamGia>());
        }

        // GET: Tạo mới
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(DotGiamGia model, string selectedIds)
        {
            if (!ModelState.IsValid)
                return View(model);

            var ngayHienTai = DateTime.Today;
            
            if (model.NgayBatDau.Date < ngayHienTai)
            {
                ModelState.AddModelError("NgayBatDau", "Ngày bắt đầu không được nhỏ hơn ngày hiện tại!");
                return View(model);
            }
            
            if (model.NgayKetThuc < model.NgayBatDau)
            {
                ModelState.AddModelError("NgayBatDau", "Ngày kết thúc không được nhỏ hơn ngày bắt đầu !");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(selectedIds))
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất 1 sản phẩm.");
                return View(model);
            }

            var idList = selectedIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Where(id => Guid.TryParse(id, out _))
                .Select(Guid.Parse)
                .Distinct()
                .ToList();

            if (!idList.Any())
            {
                ModelState.AddModelError("", "Danh sách sản phẩm chi tiết không hợp lệ.");
                return View(model);
            }

            var dto = new
            {
                dot = model,
                chiTietIds = idList
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(dto),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("DotGiamGias", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (responseData.TryGetProperty("Message", out var messageElement))
                {
                    TempData["SuccessMessage"] = messageElement.GetString();
                }
                else
                {
                    TempData["SuccessMessage"] = "Tạo đợt giảm giá thành công!";
                }
                return RedirectToAction(nameof(Index));
            }

            var errorMessage = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Tạo đợt giảm giá thất bại: {errorMessage}");

            return View(model);
        }


        // GET: Chỉnh sửa
        public async Task<IActionResult> Edit(Guid id)
        {
            var res = await _httpClient.GetAsync($"DotGiamGias/{id}");
            if (!res.IsSuccessStatusCode)
                return NotFound();

            var dot = await res.Content.ReadFromJsonAsync<DotGiamGia>();

            var spRes = await _httpClient.GetAsync($"DotGiamGias/{id}/SanPhams");
            if (spRes.IsSuccessStatusCode)
            {
                var sanPhams = await spRes.Content.ReadFromJsonAsync<List<SelectListItem>>();
                ViewBag.SelectedSanPhamList = sanPhams;
            }
            else
            {
                ViewBag.SelectedSanPhamList = new List<SelectListItem>();
            }

            return View(dot);
        }


        // POST: Cập nhật
        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, DotGiamGia model, [FromForm] List<Guid> SelectedSanPhamChiTietIds)
        {
            if (!ModelState.IsValid) return View(model);
            if (id != model.IDDotGiamGia) return BadRequest();

            var ngayHienTai = DateTime.Today;
            
            if (model.NgayBatDau.Date < ngayHienTai)
            {
                ModelState.AddModelError("NgayBatDau", "Ngày bắt đầu không được nhỏ hơn ngày hiện tại!");
                return View(model);
            }

            if (model.NgayKetThuc < model.NgayBatDau)
            {
                ModelState.AddModelError("NgayBatDau", "Ngày kết thúc không được nhỏ hơn ngày bắt đầu !");
                return View(model);
            }

            var dto = new
            {
                IDDotGiamGia = model.IDDotGiamGia,
                MaDot = model.MaDot,
                TenDot = model.TenDot,
                PhanTramGiam = model.PhanTramGiam,
                NgayBatDau = model.NgayBatDau,
                NgayKetThuc = model.NgayKetThuc,
                TrangThai = model.TrangThai,
                SanPhamChiTietIds = SelectedSanPhamChiTietIds
            };

            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            var res = await _httpClient.PutAsync($"DotGiamGias/{id}", content);

            if (res.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Cập nhật đợt giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Cập nhật thất bại");

            return View(model);
        }






        // GET: Xoá
        public async Task<IActionResult> Delete(Guid id)
        {
            var res = await _httpClient.DeleteAsync($"DotGiamGias/{id}");
            TempData[res.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] =
                res.IsSuccessStatusCode ? "Xóa đợt giảm giá thành công!" : "Xóa đợt giảm giá thất bại!";
            return RedirectToAction(nameof(Index));
        }

        // PUT: Cập nhật trạng thái
        [HttpGet]
        public async Task<IActionResult> UpdateTrangThai(Guid id, bool trangThai) 
        {
            var request = new HttpRequestMessage(HttpMethod.Put,
                $"DotGiamGias/UpdateTrangThai?id={id}&trangThai={trangThai}");

            var response = await _httpClient.SendAsync(request);

            TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] =
                response.IsSuccessStatusCode ? "Cập nhật trạng thái thành công!" : "Thay đổi trạng thái thất bại!";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetSanPhams()
        {
            var response = await _httpClient.GetAsync("SanPhamChiTiets?page=1&pageSize=50");
            if (!response.IsSuccessStatusCode)
                return Json(new List<object>());

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<SanPhamChiTietDto>>();
            if (result == null) return Json(new List<object>());

            var sanPhams = result
                .GroupBy(x => x.IdSanPham)
                .Select(g => new
                {
                    idSanPham = g.Key,
                    maSanPham = g.First().MaSPChiTiet ?? "N/A",
                    tenSanPham = g.First().TenSanPham ?? "Không xác định",
                    trangThai = g.First().TrangThai
                })
                .ToList();

            return Json(sanPhams);
        }

        [HttpGet]
        public async Task<IActionResult> GetChiTietSanPham(Guid idSanPham)
        {
            var response = await _httpClient.GetAsync($"SanPhamChiTiets/bysanpham?idsanpham={idSanPham}");
            if (!response.IsSuccessStatusCode)
                return Json(new List<object>());

            var chiTiet = await response.Content.ReadFromJsonAsync<IEnumerable<SanPhamChiTietDto>>();
            if (chiTiet == null) return Json(new List<object>());

            var result = chiTiet.Select(x => new
            {
                idSanPhamChiTiet = x.IdSanPhamChiTiet,
                tenSanPham = x.TenSanPham ?? "Không xác định",
                kichCo = x.TenKichCo ?? "N/A",
                mauSac = x.TenMauSac ?? "N/A",
                hoaTiet = x.TenHoaTiet ?? "N/A",

                trangThai = x.TrangThai
            }).ToList();

            return Json(result);
        }




    }
}