using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using QuanView.Areas.Admin.Models;
using System.Net.Http;
using System.Text;
using QuanView.Models;
using System.Diagnostics;
using QuanApi.Dtos;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using QuanApi.Data;
using QuanView.ViewModels;
using System.Net.Http.Json;
using AddAnhSanPhamDto = QuanApi.Dtos.AddAnhSanPhamDto;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class SanPhamController : Controller
    {
        private readonly HttpClient _http;

        public SanPhamController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("MyApi");
        }

        public async Task<IActionResult> Index(
            int page = 1,
            string? keyword = null,
            string? trangThai = null,
            decimal? priceFrom = null,
            decimal? priceTo = null,
            int? qtyFrom = null,
            int? qtyTo = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            // Cố định pageSize = 5
            int pageSize = 5;
            
            // Tạo query string cho API
            var queryParams = new List<string>();
            queryParams.Add($"page={page}");
            queryParams.Add($"pageSize={pageSize}");
            
            if (!string.IsNullOrWhiteSpace(keyword))
                queryParams.Add($"keyword={Uri.EscapeDataString(keyword)}");
            if (!string.IsNullOrWhiteSpace(trangThai))
                queryParams.Add($"trangThai={Uri.EscapeDataString(trangThai)}");
            if (priceFrom.HasValue)
                queryParams.Add($"priceFrom={priceFrom.Value}");
            if (priceTo.HasValue)
                queryParams.Add($"priceTo={priceTo.Value}");
            if (qtyFrom.HasValue)
                queryParams.Add($"qtyFrom={qtyFrom.Value}");
            if (qtyTo.HasValue)
                queryParams.Add($"qtyTo={qtyTo.Value}");
            if (dateFrom.HasValue)
                queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
            if (dateTo.HasValue)
                queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");

            var queryString = string.Join("&", queryParams);
            var response = await _http.GetAsync($"sanphams/paged?{queryString}");
            
            if (!response.IsSuccessStatusCode) 
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

            var json = await response.Content.ReadAsStringAsync();
            var pagedResult = JsonSerializer.Deserialize<dynamic>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            // Parse kết quả từ API
            var total = pagedResult.GetProperty("total").GetInt32();
            var dataArray = pagedResult.GetProperty("data").EnumerateArray();
            var products = new List<QuanView.Areas.Admin.Models.SanPhamDto>();
            
            foreach (var item in dataArray)
            {
                var product = JsonSerializer.Deserialize<QuanView.Areas.Admin.Models.SanPhamDto>(item.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                products.Add(product);
            }

            // Load chi tiết sản phẩm cho từng sản phẩm
            foreach (var sp in products)
            {
                var res = await _http.GetAsync($"sanphamchitiets/bysanpham?idsanpham={sp.IDSanPham}");

                if (res.IsSuccessStatusCode)
                {
                    var ctJson = await res.Content.ReadAsStringAsync();
                    sp.ChiTietSanPhams = JsonSerializer.Deserialize<List<QuanView.Areas.Admin.Models.SanPhamChiTietDto>>(ctJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // ✅ Cập nhật ảnh chính từ SanPhamChiTiet đầu tiên có ảnh
                    if (sp.ChiTietSanPhams != null && sp.ChiTietSanPhams.Any())
                    {
                        var firstWithImage = sp.ChiTietSanPhams.FirstOrDefault(ct => !string.IsNullOrEmpty(ct.AnhDaiDien));
                        if (firstWithImage != null)
                        {
                            sp.AnhChinh = firstWithImage.AnhDaiDien;
                        }
                    }

                    // ✅ Load danh sách ảnh cho từng sản phẩm chi tiết
                    if (sp.ChiTietSanPhams != null)
                    {
                        foreach (var ct in sp.ChiTietSanPhams)
                        {
                            var imagesRes = await _http.GetAsync($"sanphams/chitiet/{ct.IdSanPhamChiTiet}/images");
                            if (imagesRes.IsSuccessStatusCode)
                            {
                                var imagesJson = await imagesRes.Content.ReadAsStringAsync();
                                var apiImages = JsonSerializer.Deserialize<List<QuanApi.Dtos.AnhSanPhamDto>>(imagesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                ct.DanhSachAnh = MapApiImagesToAdminImages(apiImages);

                                // ✅ Cập nhật ảnh đại diện từ danh sách ảnh
                                var mainImage = apiImages?.FirstOrDefault(img => img.LaAnhChinh);
                                if (mainImage != null)
                                {
                                    ct.AnhDaiDien = mainImage.UrlAnh;
                                }
                            }
                        }
                    }
                }
            }

            // Tạo ViewModel với phân trang và bộ lọc
            var viewModel = new SanPhamFilterViewModel
            {
                SanPhams = products,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = total,
                Keyword = keyword,
                TrangThai = trangThai,
                PriceFrom = priceFrom,
                PriceTo = priceTo,
                QtyFrom = qtyFrom,
                QtyTo = qtyTo,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdownData();
            ViewBag.SanPhams = await GetSelectList("sanphams", "idSanPham", "tenSanPham");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuanView.Areas.Admin.Models.SanPhamDto dto, Guid? SanPhamDaCoId)
        {
            // Nếu chọn sản phẩm đã có, xóa lỗi validation các trường sản phẩm chính
            if (SanPhamDaCoId.HasValue && SanPhamDaCoId.Value != Guid.Empty)
            {
                // Xóa lỗi cho các trường sản phẩm chính (tùy model, có thể cần bổ sung thêm)
                ModelState.Remove(nameof(dto.MaSanPham));
                ModelState.Remove(nameof(dto.TenSanPham));
                ModelState.Remove(nameof(dto.IDDanhMuc));
                ModelState.Remove(nameof(dto.IDThuongHieu));
                ModelState.Remove(nameof(dto.IDChatLieu));
                ModelState.Remove(nameof(dto.IDLoaiOng));
                ModelState.Remove(nameof(dto.IDKieuDang));
                ModelState.Remove(nameof(dto.IDLungQuan));
                ModelState.Remove(nameof(dto.CoXepLy));
                ModelState.Remove(nameof(dto.CoGian));
                ModelState.Remove(nameof(dto.TrangThai));
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownData();
                ViewBag.SanPhams = await GetSelectList("sanphams", "idSanPham", "tenSanPham");
                return View(dto);
            }

            if (SanPhamDaCoId.HasValue && SanPhamDaCoId.Value != Guid.Empty)
            {
                // Thêm biến thể cho sản phẩm đã có
                if (dto.ChiTietSanPhams != null && dto.ChiTietSanPhams.Any())
                {
                    foreach (var ct in dto.ChiTietSanPhams)
                    {
                        ct.IdSanPhamChiTiet = Guid.NewGuid();
                        ct.IdSanPham = SanPhamDaCoId.Value;
                        var res = await _http.PostAsJsonAsync("sanphamchitiets", ct);
                        if (!res.IsSuccessStatusCode)
                        {
                            var msg = await res.Content.ReadAsStringAsync();
                            ModelState.AddModelError(string.Empty, $"Lỗi lưu biến thể: {msg}");
                            await LoadDropdownData();
                            ViewBag.SanPhams = await GetSelectList("sanphams", "idSanPham", "tenSanPham");
                            return View(dto);
                        }
                    }
                }
                return RedirectToAction("Index");
            }
            else
            {
                // Tạo sản phẩm mới như hiện tại
                dto.IDSanPham = Guid.NewGuid();
                var response = await _http.PostAsJsonAsync("sanphams", dto);
                if (!response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Lỗi API: {response.StatusCode} - {msg}");
                    await LoadDropdownData();
                    ViewBag.SanPhams = await GetSelectList("sanphams", "idSanPham", "tenSanPham");
                    return View(dto);
                }

                if (dto.ChiTietSanPhams != null && dto.ChiTietSanPhams.Any())
                {
                    foreach (var ct in dto.ChiTietSanPhams)
                    {
                        ct.IdSanPhamChiTiet = Guid.NewGuid();
                        ct.IdSanPham = dto.IDSanPham;
                        var res = await _http.PostAsJsonAsync("sanphamchitiets", ct);
                        if (!res.IsSuccessStatusCode)
                        {
                            var msg = await res.Content.ReadAsStringAsync();
                            ModelState.AddModelError(string.Empty, $"Lỗi lưu biến thể: {msg}");
                            await LoadDropdownData();
                            ViewBag.SanPhams = await GetSelectList("sanphams", "idSanPham", "tenSanPham");
                            return View(dto);
                        }
                    }
                }
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var response = await _http.GetAsync($"sanphams/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();
            var dto = await response.Content.ReadFromJsonAsync<QuanView.Areas.Admin.Models.SanPhamDto>();

            // 🔍 Debug: Kiểm tra thông tin sản phẩm chính
            System.Diagnostics.Debug.WriteLine($"🏷️ SanPham ID: {dto.IDSanPham}, Ten: {dto.TenSanPham}");

            var res = await _http.GetAsync($"sanphamchitiets/bysanpham?idsanpham={id}");
            if (res.IsSuccessStatusCode)
            {
                var ctJson = await res.Content.ReadAsStringAsync();

                // 💥 Kiểm tra JSON trước khi parse
                Console.WriteLine($"👉 JSON: {ctJson}");

                var ctList = JsonSerializer.Deserialize<List<QuanView.Areas.Admin.Models.SanPhamChiTietDto>>(ctJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 🔧 FIX: Đảm bảo IdSanPham được gán đúng cho tất cả chi tiết
                foreach (var item in ctList)
                {
                    // Gán IdSanPham nếu chưa có hoặc bị null/empty
                    if (item.IdSanPham == Guid.Empty || item.IdSanPham == null)
                    {
                        item.IdSanPham = id;
                        System.Diagnostics.Debug.WriteLine($"🔧 Fixed IdSanPham for ChiTiet: {item.IdSanPhamChiTiet}");
                    }

                    // 💥 Kiểm tra sau khi parse và fix
                    System.Diagnostics.Debug.WriteLine($"📦 ID: {item.IdSanPhamChiTiet}, SanPhamID: {item.IdSanPham}, {item.TenKichCo} - SL: {item.SoLuong}, Giá: {item.GiaBan}");
                }

                dto.ChiTietSanPhams = ctList;

                // ✅ Cập nhật ảnh chính từ SanPhamChiTiet đầu tiên có ảnh
                if (ctList != null && ctList.Any())
                {
                    var firstWithImage = ctList.FirstOrDefault(ct => !string.IsNullOrEmpty(ct.AnhDaiDien));
                    if (firstWithImage != null)
                    {
                        dto.AnhChinh = firstWithImage.AnhDaiDien;
                    }
                }

                // ✅ Load danh sách ảnh cho từng sản phẩm chi tiết
                if (ctList != null)
                {
                    foreach (var ct in ctList)
                    {
                        var imagesRes = await _http.GetAsync($"sanphams/chitiet/{ct.IdSanPhamChiTiet}/images");
                        if (imagesRes.IsSuccessStatusCode)
                        {
                            var imagesJson = await imagesRes.Content.ReadAsStringAsync();
                            var apiImages = JsonSerializer.Deserialize<List<QuanApi.Dtos.AnhSanPhamDto>>(imagesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            ct.DanhSachAnh = MapApiImagesToAdminImages(apiImages);
                            
                            // ✅ Cập nhật ảnh đại diện từ danh sách ảnh
                            var mainImage = apiImages?.FirstOrDefault(img => img.LaAnhChinh);
                            if (mainImage != null)
                            {
                                ct.AnhDaiDien = mainImage.UrlAnh;
                            }
                        }
                    }
                }

                // 🔍 Debug: Kiểm tra tổng quan
                System.Diagnostics.Debug.WriteLine($"📊 Tổng số chi tiết: {ctList?.Count ?? 0}");
            }
            else
            {
                // 🔍 Debug: Nếu không load được chi tiết
                System.Diagnostics.Debug.WriteLine($"❌ Không load được chi tiết sản phẩm: {res.StatusCode}");
                dto.ChiTietSanPhams = new List<QuanView.Areas.Admin.Models.SanPhamChiTietDto>();
            }

            await LoadDropdownData();
            return View(dto);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(QuanView.Areas.Admin.Models.SanPhamDto dto)
        {
            // 🔍 Debug: Kiểm tra dữ liệu nhận được
            System.Diagnostics.Debug.WriteLine($"📥 Received IDSanPham: {dto.IDSanPham}");
            System.Diagnostics.Debug.WriteLine($"📥 ChiTietSanPhams count: {dto.ChiTietSanPhams?.Count ?? 0}");

            if (dto.ChiTietSanPhams != null)
            {
                for (int i = 0; i < dto.ChiTietSanPhams.Count; i++)
                {
                    var ct = dto.ChiTietSanPhams[i];
                    System.Diagnostics.Debug.WriteLine($"📦 [{i}] ID: {ct?.IdSanPhamChiTiet}, SL: {ct?.SoLuong}, Giá: {ct?.GiaBan}");
                }
            }

            // Cập nhật sản phẩm chính
            var response = await _http.PutAsJsonAsync($"sanphams/{dto.IDSanPham}", dto);
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Lỗi API: {response.StatusCode} - {msg}");
                await LoadDropdownData();
                return View(dto);
            }

            // Cập nhật chi tiết sản phẩm
            if (dto.ChiTietSanPhams != null)
            {
                foreach (var ct in dto.ChiTietSanPhams)
                {
                    if (ct == null)
                        continue;

                    // 🔧 FIX: Gán IdSanPham nếu bị mất
                    if (ct.IdSanPham == Guid.Empty)
                    {
                        ct.IdSanPham = dto.IDSanPham;
                        System.Diagnostics.Debug.WriteLine($" Fixed IdSanPham: {ct.IdSanPham}");
                    }

                    // 🔍 Debug: Kiểm tra dữ liệu trước khi gửi API
                    System.Diagnostics.Debug.WriteLine($"🔄 Sending to API: ID={ct.IdSanPhamChiTiet}, SanPhamID={ct.IdSanPham}, SL={ct.SoLuong}, Giá={ct.GiaBan}");

                    // 🔍 Debug: Serialize để xem JSON gửi đi
                    var jsonContent = JsonSerializer.Serialize(ct, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    });
                    System.Diagnostics.Debug.WriteLine($" JSON being sent: {jsonContent}");

                    var res = await _http.PutAsJsonAsync($"sanphamchitiets/{ct.IdSanPhamChiTiet}", ct);

                    if (!res.IsSuccessStatusCode)
                    {
                        var msg = await res.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ API Error: {res.StatusCode} - {msg}");
                        ModelState.AddModelError(string.Empty, $"Lỗi cập nhật biến thể: {msg}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Successfully updated ID: {ct.IdSanPhamChiTiet}");
                    }
                }
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var response = await _http.GetAsync($"sanphams/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var dto = await response.Content.ReadFromJsonAsync<QuanView.Areas.Admin.Models.SanPhamDto>();

            var ctRes = await _http.GetAsync($"sanphamchitiets/bysanpham?idsanpham={id}");
            if (ctRes.IsSuccessStatusCode)
            {
                var ctJson = await ctRes.Content.ReadAsStringAsync();
                dto.ChiTietSanPhams = JsonSerializer.Deserialize<List<QuanView.Areas.Admin.Models.SanPhamChiTietDto>>(ctJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                // ✅ Cập nhật ảnh chính từ SanPhamChiTiet đầu tiên có ảnh
                if (dto.ChiTietSanPhams != null && dto.ChiTietSanPhams.Any())
                {
                    var firstWithImage = dto.ChiTietSanPhams.FirstOrDefault(ct => !string.IsNullOrEmpty(ct.AnhDaiDien));
                    if (firstWithImage != null)
                    {
                        dto.AnhChinh = firstWithImage.AnhDaiDien;
                    }
                }

                // ✅ Load danh sách ảnh cho từng sản phẩm chi tiết
                if (dto.ChiTietSanPhams != null)
                {
                    foreach (var ct in dto.ChiTietSanPhams)
                    {
                        var imagesRes = await _http.GetAsync($"sanphams/chitiet/{ct.IdSanPhamChiTiet}/images");
                        if (imagesRes.IsSuccessStatusCode)
                        {
                            var imagesJson = await imagesRes.Content.ReadAsStringAsync();
                            var apiImages = JsonSerializer.Deserialize<List<QuanApi.Dtos.AnhSanPhamDto>>(imagesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            ct.DanhSachAnh = MapApiImagesToAdminImages(apiImages);
                        }
                    }
                }
            }

            return View(dto);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _http.DeleteAsync($"sanphams/{id}");
            return RedirectToAction("Index");
        }
        //load biến thể cần chỉnh sửa hàng loạt 
        [HttpPost]
        [ActionName("TaiBienThe")]
        public async Task<IActionResult> TaiBienThe([FromForm] List<Guid> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm.";
                return RedirectToAction("Index");
            }

            var allVariants = new List<QuanView.Areas.Admin.Models.SanPhamChiTietDto>();

            foreach (var id in selectedIds)
            {
                var response = await _http.GetAsync($"sanphamchitiets/bysanpham?idsanpham={id}");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<List<QuanView.Areas.Admin.Models.SanPhamChiTietDto>>();
                    if (data != null)
                    {
                        allVariants.AddRange(data);
                    }
                }
            }

            var vm = new SanPhamBienTheHangLoatViewModel
            {
                BienThes = allVariants
            };

            return View("ChinhSuaBienThe", vm);
        }


        //trả cập nhật biến thể trở lại api 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChinhSuaBienThe(SanPhamBienTheHangLoatViewModel model)
        {
            if (!ModelState.IsValid || model.BienThes == null || model.BienThes.Count == 0)
            {
                TempData["Error"] = "Dữ liệu cập nhật không hợp lệ.";
                return RedirectToAction("Index");
            }

            // ✅ Serialize thủ công toàn bộ danh sách để đảm bảo decimal không sai
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            var json = JsonSerializer.Serialize(model.BienThes, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync("sanphamchitiets/bulk", content);

            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Lỗi cập nhật hàng loạt: {msg}";
                return RedirectToAction("Index");
            }

            TempData["Success"] = "Cập nhật hàng loạt thành công!";
            return RedirectToAction("Index");
        }





        private async Task LoadDropdownData()
        {
            ViewBag.ChatLieus = await GetSelectList("chatlieu", "idChatLieu", "tenChatLieu");
            ViewBag.DanhMucs = await GetSelectList("danhmucs", "idDanhMuc", "tenDanhMuc");
            ViewBag.ThuongHieus = await GetSelectList("thuonghieu", "idThuongHieu", "tenThuongHieu");
            ViewBag.LoaiOngs = await GetSelectList("loaiong", "idLoaiOng", "tenLoaiOng");
            ViewBag.KieuDangs = await GetSelectList("kieudang", "idKieuDang", "tenKieuDang");
            ViewBag.LungQuans = await GetSelectList("lungquan", "idLungQuan", "tenLungQuan");
            ViewBag.KichCos = await GetSelectList("kichco", "idKichCo", "tenKichCo");
            ViewBag.MauSacs = await GetSelectList("mausac", "idMauSac", "tenMauSac");
            ViewBag.HoaTiets = await GetSelectList("hoatiet", "idHoaTiet", "tenHoaTiet");
        }

        private async Task<List<SelectListItem>> GetSelectList(string url, string idField, string nameField)
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<SelectListItem>();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.EnumerateArray()
                .Select(e => new SelectListItem
                {
                    Value = e.GetProperty(idField).ToString(),
                    Text = e.GetProperty(nameField).GetString()
                }).ToList();
        }

        // Test action cho quản lý ảnh
        public IActionResult TestImageManagement()
        {
            return View();
        }

        // Helper method để map từ API DTO sang Admin DTO
        private List<QuanView.Areas.Admin.Models.AnhSanPhamDto> MapApiImagesToAdminImages(List<QuanApi.Dtos.AnhSanPhamDto> apiImages)
        {
            return apiImages?.Select(img => new QuanView.Areas.Admin.Models.AnhSanPhamDto
            {
                IDAnhSanPham = img.IDAnhSanPham,
                MaAnh = img.MaAnh,
                IDSanPhamChiTiet = img.IDSanPhamChiTiet,
                UrlAnh = img.UrlAnh,
                LaAnhChinh = img.LaAnhChinh,
                NgayTao = img.NgayTao,
                NguoiTao = img.NguoiTao,
                LanCapNhatCuoi = img.LanCapNhatCuoi,
                NguoiCapNhat = img.NguoiCapNhat,
                TrangThai = img.TrangThai
            }).ToList() ?? new List<QuanView.Areas.Admin.Models.AnhSanPhamDto>();
        }

        [HttpPost]
        [Route("Admin/AnhSanPham/UploadImage")]
        public async Task<IActionResult> UploadImage(IFormFile file, Guid sanPhamChiTietId, bool laAnhChinh)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Không có file ảnh." });

            // 1. Lưu file vào wwwroot/uploads/
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(uploadPath));
            using (var stream = new FileStream(uploadPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var urlAnh = $"/uploads/{fileName}";

            // 2. Gọi API backend để lưu thông tin ảnh vào DB
            var dto = new AddAnhSanPhamDto
            {
                UrlAnh = urlAnh,
                LaAnhChinh = laAnhChinh
            };

            // Gọi API backend (MyApi là HttpClient đã cấu hình base address)
            var response = await _http.PostAsJsonAsync($"sanphams/chitiet/{sanPhamChiTietId}/images", dto);

            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = "Lỗi lưu ảnh vào DB: " + msg });
            }

            return Json(new { success = true, urlAnh });
        }
    }
}
