using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using QuanApi.Dtos;
using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class NhanViensController : Controller
    {

        private readonly BanQuanAu1DbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<NhanViensController> _logger;

        public NhanViensController(IHttpClientFactory httpClientFactory, ILogger<NhanViensController> logger, BanQuanAu1DbContext context)
        {
            _httpClient = httpClientFactory.CreateClient("MyApi");
            _logger = logger;
            _context = context;
        }

        // GET: Admin/NhanViens
        public async Task<IActionResult> Index(string? searchTerm, Guid? idVaiTro, bool? trangThai, int pageNumber = 1, int pageSize = 10)
        {

            _logger.LogInformation("Truy cập trang Index của NhanViensController với filter: searchTerm={SearchTerm}, idVaiTro={IDVaiTro}, trangThai={TrangThai}, pageNumber={PageNumber}, pageSize={PageSize}.",
                                    searchTerm, idVaiTro, trangThai, pageNumber, pageSize);
            try
            {
                var filter = new NhanVienFilterDto
                {
                    SearchTerm = searchTerm,
                    IDVaiTro = idVaiTro,
                    TrangThai = trangThai,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var response = await _httpClient.GetAsync($"NhanVien?{filter.ToQueryString()}");

                if (response.IsSuccessStatusCode)
                {
                    var pagedResult = await response.Content.ReadFromJsonAsync<PagedResultGeneric<NhanVienResponseDto>>();

                    ViewBag.SearchTerm = searchTerm;
                    ViewBag.IDVaiTro = idVaiTro;
                    ViewBag.TrangThai = trangThai;
                    ViewBag.PageNumber = pageNumber;
                    ViewBag.PageSize = pageSize;

                    return View(pagedResult);
                }
                else
                {
                    _logger.LogError("Lỗi khi lấy danh sách nhân viên từ API: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    TempData["ErrorMessage"] = "Không thể tải danh sách nhân viên từ API.";
                    return View(new PagedResultGeneric<NhanVienResponseDto>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception khi lấy danh sách nhân viên.");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu nhân viên.";
                return View(new PagedResultGeneric<NhanVienResponseDto>());
            }
        }

        // GET: Admin/NhanViens/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details: ID nhân viên là null.");
                return NotFound();
            }

            try
            {
                var response = await _httpClient.GetAsync($"NhanVien/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var nhanVien = await response.Content.ReadFromJsonAsync<NhanVienResponseDto>();
                    if (nhanVien == null)
                    {
                        _logger.LogWarning("Details: Không tìm thấy nhân viên với ID: {Id} từ API.", id);
                        return NotFound();
                    }
                    return View(nhanVien);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Details: API trả về 404 cho ID: {Id}.", id);
                    return NotFound();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Lỗi khi lấy chi tiết nhân viên từ API: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    TempData["ErrorMessage"] = "Không thể tải chi tiết nhân viên từ API.";
                    return BadRequest("Không thể tải thông tin nhân viên");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception khi lấy chi tiết nhân viên {Id}.", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu chi tiết nhân viên.";
                return BadRequest("Có lỗi xảy ra khi tải dữ liệu");
            }
        }

        // GET: Admin/NhanViens/Create
        public IActionResult Create()
        {
   
            return View(new NhanVienCreateDto());
        }

        // POST: Admin/NhanViens/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaNhanVien,TenNhanVien,Email,MatKhau,SoDienThoai,NgaySinh,GioiTinh,QueQuan,CCCD,IDVaiTro,TrangThai,IDNguoiTao")] NhanVienCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                var modelErrors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("Create: ModelState không hợp lệ. Lỗi: {Errors}", modelErrors);
                return View(createDto);
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("NhanVien", createDto);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Tạo nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Lỗi khi tạo nhân viên qua API: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    try
                    {
                        var apiError = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(errorContent);
                        if (apiError != null && apiError.ContainsKey("errors"))
                        {
                            foreach (var propError in apiError["errors"].EnumerateObject())
                            {
                                foreach (var errorValue in propError.Value.EnumerateArray())
                                {
                                    ModelState.AddModelError(propError.Name, errorValue.GetString() ?? "Lỗi không xác định");
                                }
                            }
                        }
                        else if (apiError != null && apiError.ContainsKey("message"))
                        {
                            ModelState.AddModelError(string.Empty, apiError["message"].GetString() ?? "Có lỗi xảy ra khi tạo nhân viên");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, $"Có lỗi xảy ra khi tạo nhân viên: {errorContent}");
                        }
                    }
                    catch (JsonException)
                    {
                        ModelState.AddModelError(string.Empty, $"Có lỗi xảy ra khi tạo nhân viên: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception khi tạo nhân viên.");
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi tạo nhân viên.");
            }

            return View(createDto);
        }
        // GET: Admin/NhanViens/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit: ID nhân viên là null.");
                return NotFound();
            }

            try
            {
                var response = await _httpClient.GetAsync($"NhanVien/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var nhanVienResponseDto = await response.Content.ReadFromJsonAsync<NhanVienResponseDto>();
                    if (nhanVienResponseDto == null)
                    {
                        _logger.LogWarning("Edit: Không tìm thấy nhân viên với ID: {Id} từ API.", id);
                        return NotFound();
                    }

                    var updateDto = new NhanVienUpdateDto
                    {
                        MaNhanVien = nhanVienResponseDto.MaNhanVien,
                        TenNhanVien = nhanVienResponseDto.TenNhanVien,
                        Email = nhanVienResponseDto.Email,
                        SoDienThoai = nhanVienResponseDto.SoDienThoai,
                        NgaySinh = nhanVienResponseDto.NgaySinh,
                        GioiTinh = nhanVienResponseDto.GioiTinh,
                        QueQuan = nhanVienResponseDto.QueQuan,
                        CCCD = nhanVienResponseDto.CCCD,
                        IDVaiTro = nhanVienResponseDto.IDVaiTro,
                        TrangThai = nhanVienResponseDto.TrangThai,
                        MatKhau = null
                    };

                    var currentUserId = GetCurrentUserId();
                    ViewBag.IsCurrentUser = currentUserId.HasValue && currentUserId.Value == id;
                    _logger.LogInformation("Is current user editing their own profile? {IsCurrentUser}", (bool?)ViewBag.IsCurrentUser);

                    await LoadVaiTroDropdown();
                    ViewBag.NhanVienId = id;
                    return View(updateDto);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Edit: API trả về 404 cho ID: {Id}.", id);
                    return NotFound();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Lỗi khi lấy chi tiết nhân viên để chỉnh sửa từ API: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    TempData["ErrorMessage"] = "Không thể tải thông tin nhân viên để chỉnh sửa từ API.";
                    return BadRequest("Không thể tải thông tin nhân viên");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception khi tải trang chỉnh sửa nhân viên {Id}.", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chỉnh sửa nhân viên.";
                return BadRequest("Có lỗi xảy ra khi tải dữ liệu");
            }
        }

        // POST: Admin/NhanViens/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("MaNhanVien,TenNhanVien,Email,MatKhau,SoDienThoai,NgaySinh,GioiTinh,QueQuan,CCCD,IDVaiTro,TrangThai,IDNguoiCapNhat")] NhanVienUpdateDto updateDto)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue && id == currentUserId.Value)
            {
                var nhanVienHienTai = await _httpClient.GetFromJsonAsync<NhanVienResponseDto>($"NhanVien/{id}");
                if (nhanVienHienTai != null && nhanVienHienTai.TrangThai != updateDto.TrangThai)
                {
                    ModelState.AddModelError(string.Empty, "Bạn không thể tự thay đổi trạng thái của mình.");
                    await LoadVaiTroDropdown();
                    ViewBag.NhanVienId = id;
                    ViewBag.IsCurrentUser = true;
                    return View(updateDto);
                }
            }

            if (!ModelState.IsValid)
            {
                var modelErrors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("Edit: ModelState không hợp lệ cho nhân viên ID: {Id}. Lỗi: {Errors}", id, modelErrors);
                await LoadVaiTroDropdown();
                ViewBag.NhanVienId = id;
                return View(updateDto);
            }

            try
            {
                var response = await _httpClient.PutAsJsonAsync($"NhanVien/{id}", updateDto);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật nhân viên thành công!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Edit: API trả về 404 khi cập nhật nhân viên ID: {Id}.", id);
                    TempData["ErrorMessage"] = "Không tìm thấy nhân viên để cập nhật.";
                    return NotFound();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Lỗi khi cập nhật nhân viên qua API: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    try
                    {
                        var apiError = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(errorContent);
                        if (apiError != null && apiError.ContainsKey("errors"))
                        {
                            foreach (var propError in apiError["errors"].EnumerateObject())
                            {
                                foreach (var errorValue in propError.Value.EnumerateArray())
                                {
                                    ModelState.AddModelError(propError.Name, errorValue.GetString() ?? "Lỗi không xác định");
                                }
                            }
                        }
                        else if (apiError != null && apiError.ContainsKey("message"))
                        {
                            ModelState.AddModelError(string.Empty, apiError["message"].GetString() ?? "Có lỗi xảy ra khi cập nhật nhân viên");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, $"Có lỗi xảy ra khi cập nhật nhân viên: {errorContent}");
                        }
                    }
                    catch (JsonException)
                    {
                        ModelState.AddModelError(string.Empty, $"Có lỗi xảy ra khi cập nhật nhân viên: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception khi cập nhật nhân viên {Id}.", id);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật nhân viên.");
            }

            await LoadVaiTroDropdown();
            ViewBag.NhanVienId = id;
            return View(updateDto);
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                return userId;
            }
            return null;
        }

        private async Task LoadVaiTroDropdown()
        {
            try
            {
                var response = await _httpClient.GetAsync("VaiTro");
                if (response.IsSuccessStatusCode)
                {
                    var vaiTros = await response.Content.ReadFromJsonAsync<List<VaiTroDto>>();
                    ViewData["IDVaiTro"] = new SelectList(vaiTros, "IDVaiTro", "TenVaiTro");
                }
                else
                {
                    _logger.LogWarning("Không thể tải danh sách vai trò từ API: {StatusCode}", response.StatusCode);
                    ViewData["IDVaiTro"] = new SelectList(new List<VaiTroDto>(), "IDVaiTro", "TenVaiTro");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách vai trò cho dropdown.");
                ViewData["IDVaiTro"] = new SelectList(new List<VaiTroDto>(), "IDVaiTro", "TenVaiTro");
            }
        }
    }
}
