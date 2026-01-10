using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json; 
using System.Threading.Tasks;
using QuanApi.Dtos; 
using System.Net.Http.Json; 
using System;

namespace BanQuanAu1.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class KhachHangController : Controller
    {
        private readonly HttpClient _httpClient;

        public KhachHangController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("MyApi");
        }

        // GET: Admin/KhachHang
        public async Task<IActionResult> Index(
            string? search = null,
            int pageNumber = 1,
            int pageSize = 10,
            string? sortBy = "NgayTao",
            bool sortAscending = false)
        {

            string requestUrl = $"KhachHang?pageNumber={pageNumber}&pageSize={pageSize}&sortBy={sortBy}&sortAscending={sortAscending}";
            if (!string.IsNullOrEmpty(search))
            {
                requestUrl += $"&search={Uri.EscapeDataString(search)}";
            }

            try
            {
                var response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
 
                    var khachHangs = await response.Content.ReadFromJsonAsync<List<KhachHangDto>>();

                    ViewBag.CurrentSearch = search;
                    ViewBag.CurrentPage = pageNumber;
                    ViewBag.PageSize = pageSize;
                    ViewBag.TotalCount = khachHangs?.Count ?? 0;
                    ViewBag.TotalPages = (int)Math.Ceiling((double)(khachHangs?.Count ?? 0) / pageSize);
                    ViewBag.SortBy = sortBy;
                    ViewBag.SortAscending = sortAscending;

                    return View(khachHangs);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi tải danh sách khách hàng từ API: {response.ReasonPhrase}. Chi tiết: {errorContent}";
                    return View(new List<KhachHangDto>()); 
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Không thể kết nối tới API khách hàng. Vui lòng kiểm tra API đang chạy: {ex.Message}";
                return View(new List<KhachHangDto>());
            }
            catch (JsonException ex)
            {
                TempData["ErrorMessage"] = $"Lỗi đọc dữ liệu từ API khách hàng: {ex.Message}";
                return View(new List<KhachHangDto>());
            }
        }

        // GET: Admin/KhachHang/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var response = await _httpClient.GetAsync($"KhachHang/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var khachHangDto = await response.Content.ReadFromJsonAsync<KhachHangDto>();
                    if (khachHangDto == null)
                    {
                        return NotFound();
                    }
                    return View(khachHangDto);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi lấy chi tiết khách hàng ID: {id}. Chi tiết: {errorContent}";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Không thể kết nối tới API khách hàng để lấy chi tiết: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/KhachHang/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/KhachHang/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaKhachHang,Email,MatKhau,TenKhachHang,SoDienThoai,NguoiTao,DiaChis")] CreateKhachHangDto createKhachHangDto)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync("KhachHang", createKhachHangDto);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Thêm khách hàng thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {

                        var errorContent = await response.Content.ReadAsStringAsync();

                        if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
                        {
                            var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
                            if (problemDetails?.Extensions?.TryGetValue("errors", out var errorsObj) == true && errorsObj is JsonElement errorsElement)
                            {
                                foreach (var prop in errorsElement.EnumerateObject())
                                {
                                    foreach (var value in prop.Value.EnumerateArray())
                                    {
                                        ModelState.AddModelError(prop.Name, value.GetString() ?? "Lỗi không xác định.");
                                    }
                                }
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, problemDetails?.Detail ?? $"Lỗi API: {errorContent}");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, $"Lỗi khi tạo khách hàng: {response.ReasonPhrase}. Nội dung: {errorContent}");
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    ModelState.AddModelError(string.Empty, $"Không thể kết nối tới API khách hàng: {ex.Message}");
                }
                catch (JsonException ex)
                {
                    ModelState.AddModelError(string.Empty, $"Lỗi xử lý phản hồi từ API: {ex.Message}");
                }
            }
            return View(createKhachHangDto); 
        }

        // GET: Admin/KhachHang/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var response = await _httpClient.GetAsync($"KhachHang/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var khachHangDto = await response.Content.ReadFromJsonAsync<KhachHangDto>();
                    if (khachHangDto == null)
                    {
                        return NotFound();
                    }
                    var updateKhachHangDto = new UpdateKhachHangDto
                    {
                        IDKhachHang = khachHangDto.IDKhachHang,
                        MaKhachHang = khachHangDto.MaKhachHang,
                        Email = khachHangDto.Email,
                        TenKhachHang = khachHangDto.TenKhachHang,
                        SoDienThoai = khachHangDto.SoDienThoai,
                        TrangThai = khachHangDto.TrangThai,
                        NguoiCapNhat = khachHangDto.NguoiCapNhat,
                        DiaChis = khachHangDto.DiaChis // truyền địa chỉ sang view
                    };
                    return View(updateKhachHangDto);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi lấy thông tin khách hàng ID: {id} để chỉnh sửa. Chi tiết: {errorContent}";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Không thể kết nối tới API khách hàng để chỉnh sửa: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/KhachHang/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("IDKhachHang,MaKhachHang,Email,TenKhachHang,SoDienThoai,TrangThai,NguoiCapNhat,DiaChis")] UpdateKhachHangDto updateKhachHangDto)
        {
            if (id != updateKhachHangDto.IDKhachHang)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var response = await _httpClient.PutAsJsonAsync($"KhachHang/{id}", updateKhachHangDto);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Cập nhật khách hàng thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
                        {
                            var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
                            if (problemDetails?.Extensions?.TryGetValue("errors", out var errorsObj) == true && errorsObj is JsonElement errorsElement)
                            {
                                foreach (var prop in errorsElement.EnumerateObject())
                                {
                                    foreach (var value in prop.Value.EnumerateArray())
                                    {
                                        ModelState.AddModelError(prop.Name, value.GetString() ?? "Lỗi không xác định.");
                                    }
                                }
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, problemDetails?.Detail ?? $"Lỗi API: {errorContent}");
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            return NotFound("Khách hàng không tồn tại hoặc đã bị xóa.");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, $"Lỗi khi cập nhật khách hàng: {response.ReasonPhrase}. Nội dung: {errorContent}");
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    ModelState.AddModelError(string.Empty, $"Không thể kết nối tới API khách hàng: {ex.Message}");
                }
                catch (JsonException ex)
                {
                    ModelState.AddModelError(string.Empty, $"Lỗi xử lý phản hồi từ API: {ex.Message}");
                }
            }
            return View(updateKhachHangDto);
        }

        // GET: Admin/KhachHang/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var response = await _httpClient.GetAsync($"KhachHang/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var khachHangDto = await response.Content.ReadFromJsonAsync<KhachHangDto>();
                    if (khachHangDto == null)
                    {
                        return NotFound();
                    }
                    return View(khachHangDto);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi lấy thông tin khách hàng ID: {id} để xóa. Chi tiết: {errorContent}";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Không thể kết nối tới API khách hàng để xóa: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/KhachHang/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"KhachHang/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Xóa khách hàng thành công!";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy khách hàng để xóa.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi xóa khách hàng: {response.ReasonPhrase}. Chi tiết: {errorContent}";
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Không thể kết nối tới API khách hàng để xóa: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

   
    }
}