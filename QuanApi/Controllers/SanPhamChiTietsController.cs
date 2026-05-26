using BanQuanAu1.Web.Data; // Namespace chứa DbContext
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;
using QuanApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace QuanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SanPhamChiTietsController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly ILogger<SanPhamChiTietsController> _logger;
        private readonly SanPhamValidationService _validationService;

        public SanPhamChiTietsController(BanQuanAu1DbContext context, ILogger<SanPhamChiTietsController> logger)
        {
            _context = context;
            _logger = logger;
            _validationService = new SanPhamValidationService();
        }

        // GET: api/sanphamchitiets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SanPhamChiTietDto>>> GetAll()
        {
            try
            {
                var result = await _context.SanPhamChiTiets
                    .Include(ct => ct.KichCo)
                    .Include(ct => ct.MauSac)
                    .Include(ct => ct.HoaTiet)
                    .Include(ct => ct.SanPham)
                        .ThenInclude(s => s.DanhMuc) // Thêm include này để tránh lỗi
                    .Include(ct => ct.AnhSanPhams.Where(a => a.TrangThai))
                    .Select(ct => new SanPhamChiTietDto
                    {
                        IdSanPhamChiTiet = ct.IDSanPhamChiTiet,
                        IdSanPham = ct.IDSanPham,
                        IdKichCo = ct.IDKichCo,
                        IdMauSac = ct.IDMauSac,
                        IdHoaTiet = ct.IDHoaTiet ?? Guid.Empty,
                        SoLuong = ct.SoLuong,
                        SoLuongVatLy = ct.SoLuong,
                        SoLuongDatCho = ct.SoLuongDatCho,
                        SoLuongKhaDung = Math.Max(0, ct.SoLuong - ct.SoLuongDatCho),
                        GiaBan = ct.GiaBan,
                        MaSPChiTiet = ct.MaSPChiTiet,
                        QrCode = ct.QRCode,
                        TenKichCo = ct.KichCo.TenKichCo,
                        TenMauSac = ct.MauSac.TenMauSac,
                        TenHoaTiet = ct.HoaTiet != null ? ct.HoaTiet.TenHoaTiet : "N/A",
                        TenSanPham = ct.SanPham.TenSanPham,
                        TrangThai = ct.TrangThai,
                        originalPrice = ct.GiaBan,
                        price = (
                            (from dgg in _context.DotGiamGias
                             join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
                             where sp.IDSanPhamChiTiet == ct.IDSanPhamChiTiet
                                && dgg.TrangThai == true
                                && dgg.NgayBatDau <= DateTime.UtcNow
                                && dgg.NgayKetThuc >= DateTime.UtcNow
							 select dgg.PhanTramGiam
                            ).FirstOrDefault() > 0
                            ? ct.GiaBan * (1 - (decimal)(
                                (from dgg in _context.DotGiamGias
                                 join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
                                 where sp.IDSanPhamChiTiet == ct.IDSanPhamChiTiet
                                    && dgg.TrangThai == true
                                    && dgg.NgayBatDau <= DateTime.UtcNow
									&& dgg.NgayKetThuc >= DateTime.UtcNow
								 select dgg.PhanTramGiam
                                ).FirstOrDefault() / 100.0m))
                            : ct.GiaBan
                        ),
                        TenDanhMuc = ct.SanPham.DanhMuc.TenDanhMuc,
                        AnhDaiDien = ct.AnhSanPhams
                            .Where(a => a.LaAnhChinh && a.TrangThai)
                            .Select(a => a.UrlAnh)
                            .FirstOrDefault() ??
                            ct.AnhSanPhams
                            .Where(a => a.TrangThai)
                            .Select(a => a.UrlAnh)
                            .FirstOrDefault() ?? ""
                    })
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm chi tiết: {Message}", ex.Message);
                return StatusCode(500, "Lỗi khi tải danh sách sản phẩm chi tiết");
            }
        }

        // GET: api/sanphamchitiets/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SanPhamChiTietDto>> GetById(Guid id)
        {
            try
            {
                var result = await _context.SanPhamChiTiets
                    .Include(ct => ct.KichCo)
                    .Include(ct => ct.MauSac)
                    .Include(ct => ct.HoaTiet)
                    .Include(ct => ct.SanPham)
                        .ThenInclude(s => s.DanhMuc) // Thêm include này để tránh lỗi
                    .Include(ct => ct.AnhSanPhams.Where(a => a.TrangThai))
                    .Where(ct => ct.IDSanPhamChiTiet == id)
                    .Select(ct => new SanPhamChiTietDto
                    {
                        IdSanPhamChiTiet = ct.IDSanPhamChiTiet,
                        IdSanPham = ct.IDSanPham,
                        IdKichCo = ct.IDKichCo,
                        IdMauSac = ct.IDMauSac,
                        IdHoaTiet = ct.IDHoaTiet ?? Guid.Empty,
                        SoLuong = ct.SoLuong,
                        SoLuongVatLy = ct.SoLuong,
                        SoLuongDatCho = ct.SoLuongDatCho,
                        SoLuongKhaDung = Math.Max(0, ct.SoLuong - ct.SoLuongDatCho),
                        GiaBan = ct.GiaBan,
                        MaSPChiTiet = ct.MaSPChiTiet,
                        QrCode = ct.QRCode,
                        TenKichCo = ct.KichCo.TenKichCo,
                        TenMauSac = ct.MauSac.TenMauSac,
                        TenHoaTiet = ct.HoaTiet != null ? ct.HoaTiet.TenHoaTiet : "N/A",
                        TenSanPham = ct.SanPham.TenSanPham,
                        TrangThai = ct.TrangThai,
                        originalPrice = ct.GiaBan,
                        price = (
                            (from dgg in _context.DotGiamGias
                             join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
                             where sp.IDSanPhamChiTiet == ct.IDSanPhamChiTiet
                                && dgg.TrangThai == true
                                && dgg.NgayBatDau <= DateTime.UtcNow
                                && dgg.NgayKetThuc >= DateTime.UtcNow
                             select dgg.PhanTramGiam
                            ).FirstOrDefault() > 0
                            ? ct.GiaBan * (1 - (decimal)(
                                (from dgg in _context.DotGiamGias
                                 join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
                                 where sp.IDSanPhamChiTiet == ct.IDSanPhamChiTiet
                                    && dgg.TrangThai == true
                                    && dgg.NgayBatDau <= DateTime.UtcNow
                                    && dgg.NgayKetThuc >= DateTime.UtcNow
                                 select dgg.PhanTramGiam
                                ).FirstOrDefault() / 100.0m))
                            : ct.GiaBan
                        ),
                        TenDanhMuc = ct.SanPham.DanhMuc.TenDanhMuc,
                        AnhDaiDien = ct.AnhSanPhams
                            .Where(a => a.LaAnhChinh && a.TrangThai)
                            .Select(a => a.UrlAnh)
                            .FirstOrDefault() ??
                            ct.AnhSanPhams
                            .Where(a => a.TrangThai)
                            .Select(a => a.UrlAnh)
                            .FirstOrDefault() ?? ""
                    })
                    .FirstOrDefaultAsync();

                if (result == null)
                    return NotFound("Không tìm thấy sản phẩm chi tiết.");


                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy sản phẩm chi tiết: {Message}", ex.Message);
                return StatusCode(500, "Lỗi khi tải sản phẩm chi tiết");
            }
        }

        // GET: api/sanphamchitiets/bysanpham?idsanpham=...
        [HttpGet("bysanpham")]
        public async Task<ActionResult<IEnumerable<SanPhamChiTietDto>>> GetBySanPham([FromQuery] Guid idsanpham)
        {
            try
            {
                if (idsanpham == Guid.Empty)
                    return BadRequest("ID sản phẩm không hợp lệ.");


                var list = await _context.SanPhamChiTiets
                    .Where(ct => ct.IDSanPham == idsanpham)
                    .Include(ct => ct.KichCo)
                    .Include(ct => ct.MauSac)
                    .Include(ct => ct.HoaTiet)
                    .Include(ct => ct.SanPham)
                    .Include(ct => ct.AnhSanPhams.Where(a => a.TrangThai))
                    .ToListAsync();

                if (!list.Any())
                    return Ok(new List<SanPhamChiTietDto>());

                var result = list.Select(ct => new SanPhamChiTietDto
                {
                    IdSanPhamChiTiet = ct.IDSanPhamChiTiet,
                    IdSanPham = ct.IDSanPham,
                    IdKichCo = ct.IDKichCo,
                    IdMauSac = ct.IDMauSac,
                    IdHoaTiet = ct.IDHoaTiet ?? Guid.Empty,
                    SoLuong = ct.SoLuong,
                    SoLuongVatLy = ct.SoLuong,
                    SoLuongDatCho = ct.SoLuongDatCho,
                    SoLuongKhaDung = Math.Max(0, ct.SoLuong - ct.SoLuongDatCho),
                    GiaBan = ct.GiaBan,
                    MaSPChiTiet = ct.MaSPChiTiet,
                    QrCode = ct.QRCode,
                    TenKichCo = ct.KichCo?.TenKichCo ?? "N/A",
                    TenMauSac = ct.MauSac?.TenMauSac ?? "N/A",
                    TenHoaTiet = ct.HoaTiet?.TenHoaTiet ?? "N/A",
                    TenSanPham = ct.SanPham?.TenSanPham ?? "Không xác định",
                    TrangThai = ct.TrangThai,
                    originalPrice = ct.GiaBan,
                    price = (
                        (from dgg in _context.DotGiamGias
                         join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
                         where sp.IDSanPhamChiTiet == ct.IDSanPhamChiTiet
                            && dgg.TrangThai == true
                            && dgg.NgayBatDau <= DateTime.UtcNow
                            && dgg.NgayKetThuc >= DateTime.UtcNow
                         select dgg.PhanTramGiam
                        ).FirstOrDefault() > 0
                        ? ct.GiaBan * (1 - (decimal)(
                            (from dgg in _context.DotGiamGias
                             join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
                             where sp.IDSanPhamChiTiet == ct.IDSanPhamChiTiet
                                && dgg.TrangThai == true
                                && dgg.NgayBatDau <= DateTime.UtcNow
                                && dgg.NgayKetThuc >= DateTime.UtcNow
                             select dgg.PhanTramGiam
                            ).FirstOrDefault() / 100.0m))
                        : ct.GiaBan
                    ),
                    TenDanhMuc = ct.SanPham?.DanhMuc?.TenDanhMuc ?? "",
                    AnhDaiDien = ct.AnhSanPhams != null ? ct.AnhSanPhams.Where(a => a.LaAnhChinh).Select(a => a.UrlAnh).FirstOrDefault() ?? "" : "",
				}).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

		[HttpGet("getDetailSp")]
		public async Task<ActionResult<SanPhamDetailDto>> GetDetailSanPham([FromQuery] Guid idsanpham)
		{
			if (idsanpham == Guid.Empty)
				return BadRequest("ID sản phẩm không hợp lệ.");

			var list = await _context.SanPhamChiTiets
				.Where(ct => ct.IDSanPham == idsanpham)
				.Include(ct => ct.KichCo)
				.Include(ct => ct.MauSac)
				.Include(ct => ct.HoaTiet)
				.Include(ct => ct.SanPham)
				.Include(ct => ct.AnhSanPhams)
				.ToListAsync();

			if (!list.Any())
				return NotFound();

			// 👉 Chỉ lấy ảnh của biến thể đang hoạt động
			var allImages = list
				.Where(ct => ct.TrangThai)
				.SelectMany(ct => ct.AnhSanPhams
					.Where(a => a.TrangThai)
					.Select(a => new
					{
						ct.IDSanPhamChiTiet,
						a.UrlAnh,
						a.LaAnhChinh,
						a.NgayTao
					}))
				.OrderByDescending(a => a.LaAnhChinh)
				.ThenBy(a => a.NgayTao)
				.Select(a => a.UrlAnh)
				.Distinct()
				.ToList();

			var result = new SanPhamDetailDto
			{
				IdSanPham = idsanpham,
				TenSanPham = list.First().SanPham?.TenSanPham ?? "",
				TenDanhMuc = list.First().SanPham?.DanhMuc?.TenDanhMuc ?? "",

				DanhSachAnh = allImages,

				BienThes = list
	.Where(ct => ct.TrangThai)
	.Select(ct => new SanPhamChiTietDto
	{
					IdSanPhamChiTiet = ct.IDSanPhamChiTiet,
					IdSanPham = ct.IDSanPham,
					IdKichCo = ct.IDKichCo,
					IdMauSac = ct.IDMauSac,
					IdHoaTiet = ct.IDHoaTiet ?? Guid.Empty,
					SoLuong = ct.SoLuong,
                    SoLuongVatLy = ct.SoLuong,
                    SoLuongDatCho = ct.SoLuongDatCho,
                    SoLuongKhaDung = Math.Max(0, ct.SoLuong - ct.SoLuongDatCho),
					GiaBan = ct.GiaBan,
					QrCode = ct.QRCode,
					TenKichCo = ct.KichCo?.TenKichCo,
					TenMauSac = ct.MauSac?.TenMauSac,
					TenHoaTiet = ct.HoaTiet?.TenHoaTiet,
					TrangThai = ct.TrangThai,
					price = (
						(from dgg in _context.DotGiamGias
						 join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
						 where sp.IDSanPhamChiTiet == ct.IDSanPhamChiTiet
							&& dgg.TrangThai == true
							&& dgg.NgayBatDau <= DateTime.UtcNow
							&& dgg.NgayKetThuc >= DateTime.UtcNow
						 select dgg.PhanTramGiam
						).FirstOrDefault() > 0
						? ct.GiaBan * (1 - (decimal)(
							(from dgg in _context.DotGiamGias
							 join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
							 where sp.IDSanPhamChiTiet == ct.IDSanPhamChiTiet
								&& dgg.TrangThai == true
								&& dgg.NgayBatDau <= DateTime.UtcNow
								&& dgg.NgayKetThuc >= DateTime.UtcNow
							 select dgg.PhanTramGiam
							).FirstOrDefault() / 100.0m))
						: ct.GiaBan
					),
		MaSPChiTiet = ct.MaSPChiTiet,

		AnhDaiDien = ct.AnhSanPhams
	.Where(a => a.TrangThai && a.LaAnhChinh)
	.Select(a => a.UrlAnh)
	.FirstOrDefault()
	?? ct.AnhSanPhams
		.Where(a => a.TrangThai)
		.Select(a => a.UrlAnh)
		.FirstOrDefault()
	?? "/img/default-product.jpg",

		DanhSachAnh = ct.AnhSanPhams
	.Where(a => a.TrangThai)
	.OrderByDescending(a => a.LaAnhChinh)
	.ThenBy(a => a.NgayTao)
	.Select(a => a.UrlAnh)
	.Distinct()
	.ToList()
	}).ToList()
			};

			return Ok(result);
		}

		// POST: api/sanphamchitiets
		[HttpPost]
        public async Task<IActionResult> Post(SanPhamChiTietDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (dto.IdSanPham == Guid.Empty || dto.IdKichCo == Guid.Empty || dto.IdMauSac == Guid.Empty)
                    return BadRequest("ID sản phẩm, kích cỡ hoặc màu sắc không hợp lệ.");

                // Validate quantity and price
                if (dto.SoLuong < 0)
                    return BadRequest("Số lượng không được là số âm.");

                if (dto.GiaBan < 0)
                    return BadRequest("Giá bán không được là số âm.");

                if (dto.GiaBan == 0)
                    return BadRequest("Giá bán phải lớn hơn 0.");

                // ✅ Check for existing variant with same combination
                var existingVariant = await _context.SanPhamChiTiets
                    .FirstOrDefaultAsync(ct =>
                        ct.IDSanPham == dto.IdSanPham &&
                        ct.IDKichCo == dto.IdKichCo &&
                        ct.IDMauSac == dto.IdMauSac &&
                        ct.IDHoaTiet == (dto.IdHoaTiet == Guid.Empty ? null : dto.IdHoaTiet));

                if (existingVariant != null)
                {
                    // Merge quantities for existing variant
                    existingVariant.SoLuong += dto.SoLuong;
                    existingVariant.GiaBan = dto.GiaBan; // Update price
                    existingVariant.QRCode = string.IsNullOrWhiteSpace(dto.QrCode)
                        ? BuildVariantQrCode(existingVariant.MaSPChiTiet, existingVariant.IDSanPhamChiTiet)
                        : dto.QrCode;
                    _context.Entry(existingVariant).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Merged duplicate variant: ID={Id}, NewQuantity={Quantity}",
                        existingVariant.IDSanPhamChiTiet, existingVariant.SoLuong);

                    return Ok(new
                    {
                        message = "Biến thể đã tồn tại. Số lượng đã được cộng dồn.",
                        id = existingVariant.IDSanPhamChiTiet,
                        totalQuantity = existingVariant.SoLuong
                    });
                }

                var entity = new SanPhamChiTiet
                {
                    IDSanPhamChiTiet = dto.IdSanPhamChiTiet == Guid.Empty ? Guid.NewGuid() : dto.IdSanPhamChiTiet,
                    IDSanPham = dto.IdSanPham,
                    IDKichCo = dto.IdKichCo,
                    IDMauSac = dto.IdMauSac,
                    IDHoaTiet = dto.IdHoaTiet == Guid.Empty ? null : dto.IdHoaTiet,
                    SoLuong = dto.SoLuong,
                    GiaBan = dto.GiaBan,
                    MaSPChiTiet = dto.MaSPChiTiet ?? $"CT_{DateTime.UtcNow.Ticks.ToString()[^6..]}",
                    QRCode = dto.QrCode,
                    TrangThai = true // Biến thể mới luôn ở trạng thái hoạt động (đang bán)
                };

                entity.QRCode = string.IsNullOrWhiteSpace(entity.QRCode)
                    ? BuildVariantQrCode(entity.MaSPChiTiet, entity.IDSanPhamChiTiet)
                    : entity.QRCode;

                _context.SanPhamChiTiets.Add(entity);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = entity.IDSanPhamChiTiet }, entity);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // PUT: api/sanphamchitiets/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, SanPhamChiTietDto dto)
        {
            try
            {
                if (id != dto.IdSanPhamChiTiet)
                {
                    _logger.LogWarning("ID không khớp: id={Id}, dto.IdSanPhamChiTiet={DtoId}", id, dto.IdSanPhamChiTiet);
                    return BadRequest("ID sản phẩm chi tiết không khớp.");
                }

                // Tìm bản ghi cũ trong database
                var entity = await _context.SanPhamChiTiets.FindAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning("Không tìm thấy sản phẩm chi tiết với ID={Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("[PUT] Trước update: ID={ID}, SoLuong={SoLuong}, GiaBan={GiaBan}", entity.IDSanPhamChiTiet, entity.SoLuong, entity.GiaBan);

                // Validate quantity and price before updating
                if (dto.SoLuong < 0)
                    return BadRequest("Số lượng không được là số âm.");

                if (dto.GiaBan < 0)
                    return BadRequest("Giá bán không được là số âm.");

                if (dto.GiaBan == 0)
                    return BadRequest("Giá bán phải lớn hơn 0.");


                // Cập nhật các trường cần thiết
                entity.IDSanPham = dto.IdSanPham;
                entity.IDKichCo = dto.IdKichCo;
                entity.IDMauSac = dto.IdMauSac;
                entity.IDHoaTiet = dto.IdHoaTiet == Guid.Empty ? null : dto.IdHoaTiet;
                entity.SoLuong = dto.SoLuong;
                entity.GiaBan = dto.GiaBan;
                entity.MaSPChiTiet = string.IsNullOrEmpty(dto.MaSPChiTiet) ? entity.MaSPChiTiet : dto.MaSPChiTiet;
                entity.QRCode = string.IsNullOrWhiteSpace(dto.QrCode)
                    ? BuildVariantQrCode(entity.MaSPChiTiet, entity.IDSanPhamChiTiet)
                    : dto.QrCode;
                entity.TrangThai = dto.TrangThai; // Cho phép cập nhật trạng thái (hoạt động / ngưng bán)
                entity.LanCapNhatCuoi = DateTime.UtcNow;
                entity.NguoiCapNhat = User.Identity?.Name ?? "System";

                var result = await _context.SaveChangesAsync();

                _logger.LogInformation("[PUT] SaveChangesAsync result: {Result}", result);

                // Kiểm tra lại trực tiếp trên context
                var ctCheck = await _context.SanPhamChiTiets.FindAsync(id);
                if (ctCheck != null)
                {
                    _logger.LogInformation("[PUT] Sau update DB: ID={ID}, SoLuong={SoLuong}, GiaBan={GiaBan}", ctCheck.IDSanPhamChiTiet, ctCheck.SoLuong, ctCheck.GiaBan);
                }
                else
                {
                    _logger.LogWarning("[PUT] Không tìm thấy lại entity sau update với ID={ID}", id);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PUT] Exception khi update sản phẩm chi tiết: {Message}", ex.Message);
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPost("{id}/generate-qr")]
        public async Task<IActionResult> GenerateQr(Guid id)
        {
            try
            {
                var entity = await _context.SanPhamChiTiets.FindAsync(id);
                if (entity == null)
                    return NotFound("Không tìm thấy sản phẩm chi tiết.");

                if (string.IsNullOrWhiteSpace(entity.MaSPChiTiet))
                    entity.MaSPChiTiet = $"CT_{DateTime.UtcNow.Ticks.ToString()[^6..]}";

                entity.QRCode = BuildVariantQrCode(entity.MaSPChiTiet, entity.IDSanPhamChiTiet);
                entity.LanCapNhatCuoi = DateTime.UtcNow;
                entity.NguoiCapNhat = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    id = entity.IDSanPhamChiTiet,
                    maSPChiTiet = entity.MaSPChiTiet,
                    qrCode = entity.QRCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POST generate-qr] Exception khi tạo QR: {Message}", ex.Message);
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPost("reservation-details")]
        public async Task<IActionResult> GetReservationDetails([FromBody] ReservationDetailsRequest request)
        {
            try
            {
                if (request?.VariantIds == null || request.VariantIds.Count == 0)
                    return Ok(new List<VariantReservationDetailDto>());

                var variantIds = request.VariantIds
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (variantIds.Count == 0)
                    return Ok(new List<VariantReservationDetailDto>());

                var reservedStatuses = OrderActiveStatusRules.ActiveOrderStatuses;

                var rows = await _context.ChiTietHoaDons
                    .AsNoTracking()
                    .Include(ct => ct.HoaDon)
                        .ThenInclude(h => h.KhachHang)
                    .Where(ct => variantIds.Contains(ct.IDSanPhamChiTiet)
                                 && ct.TrangThai
                                 && ct.HoaDon != null
                                 && ct.HoaDon.TrangThaiHoaDon
                                 && ct.HoaDon.DaDatChoTonKho
                                 && ct.HoaDon.TrangThai != "Đã hủy")
                    .Select(ct => new
                    {
                        ct.IDSanPhamChiTiet,
                        IdHoaDon = ct.HoaDon!.IDHoaDon,
                        MaDonHang = ct.HoaDon!.MaHoaDon,
                        KhachHangTen = ct.HoaDon.KhachHang != null ? ct.HoaDon.KhachHang.TenKhachHang : null,
                        TenNguoiNhan = ct.HoaDon.TenNguoiNhan,
                        SoLuongDangGiu = ct.SoLuong,
                        TrangThaiDon = ct.HoaDon.TrangThai,
                        NgayTaoDon = ct.HoaDon.NgayTao
                    })
                    .ToListAsync();

                var result = rows
                    .Where(x => !string.IsNullOrWhiteSpace(x.TrangThaiDon) && reservedStatuses.Contains(x.TrangThaiDon))
                    .OrderByDescending(x => x.NgayTaoDon)
                    .ThenBy(x => x.MaDonHang)
                    .Select(x => new VariantReservationDetailDto
                    {
                        IdSanPhamChiTiet = x.IDSanPhamChiTiet,
                        IdHoaDon = x.IdHoaDon,
                        MaDonHang = x.MaDonHang ?? string.Empty,
                        TenKhachHang = !string.IsNullOrWhiteSpace(x.KhachHangTen) ? x.KhachHangTen : x.TenNguoiNhan,
                        SoLuongDangGiu = x.SoLuongDangGiu,
                        TrangThaiDon = x.TrangThaiDon ?? string.Empty
                    })
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy reservation details cho biến thể.");
                return StatusCode(500, "Lỗi khi tải chi tiết số lượng đặt trước");
            }
        }

        /// <summary>Đếm số đơn đang xử lý có chứa biến thể này.</summary>
        [HttpGet("{id}/active-orders-count")]
        public async Task<IActionResult> GetActiveOrdersCount(Guid id)
        {
            if (!await _context.SanPhamChiTiets.AnyAsync(s => s.IDSanPhamChiTiet == id))
                return NotFound("Không tìm thấy sản phẩm chi tiết.");

            var count = await _context.ChiTietHoaDons
                .AsNoTracking()
                .Where(ct => ct.TrangThai
                    && ct.IDSanPhamChiTiet == id
                    && ct.HoaDon != null
                    && ct.HoaDon.TrangThaiHoaDon
                    && OrderActiveStatusRules.ActiveOrderStatuses.Contains(ct.HoaDon.TrangThai))
                .Select(ct => ct.IDHoaDon)
                .Distinct()
                .CountAsync();

            return Ok(new { count });
        }

        // DELETE: api/sanphamchitiets/{id} — luôn xóa mềm (TrangThai = false), không xóa hẳn bản ghi.
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var ct = await _context.SanPhamChiTiets.FindAsync(id);
                if (ct == null)
                    return NotFound("Không tìm thấy sản phẩm chi tiết.");

                ct.TrangThai = false;
                ct.LanCapNhatCuoi = DateTime.UtcNow;
                ct.NguoiCapNhat = User.Identity?.Name ?? "System";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Đã xóa mềm biến thể (chuyển sang ngưng bán).",
                    softDelete = true,
                    id = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DELETE] Exception khi xóa mềm sản phẩm chi tiết: {Message}", ex.Message);
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // PUT: api/sanphamchitiets/bulk
        [HttpPut("bulk")]
        public async Task<IActionResult> PutBulk([FromBody] List<SanPhamChiTietDto> dtos)
        {
            _logger.LogWarning("Received bulk update:\n{Data}", JsonSerializer.Serialize(dtos, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            if (dtos == null || !dtos.Any())
                return BadRequest("Không có dữ liệu cập nhật.");


            foreach (var dto in dtos)
            {
                _logger.LogInformation("✅ DTO nhận được - ID: {Id}, GiaBan: {GiaBan}, SoLuong: {SoLuong}", dto.IdSanPhamChiTiet, dto.GiaBan, dto.SoLuong);

                // Validate each item in bulk update
                if (dto.SoLuong < 0)
                    return BadRequest($"Số lượng không được là số âm cho sản phẩm ID: {dto.IdSanPhamChiTiet}");

                if (dto.GiaBan < 0)
                    return BadRequest($"Giá bán không được là số âm cho sản phẩm ID: {dto.IdSanPhamChiTiet}");

                if (dto.GiaBan == 0)
                    return BadRequest($"Giá bán phải lớn hơn 0 cho sản phẩm ID: {dto.IdSanPhamChiTiet}");


                var ct = await _context.SanPhamChiTiets.FindAsync(dto.IdSanPhamChiTiet);
                if (ct == null)
                    continue;

                ct.SoLuong = dto.SoLuong;
                ct.GiaBan = dto.GiaBan;

                _context.Entry(ct).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static string BuildVariantQrCode(string? maSpChiTiet, Guid idSanPhamChiTiet)
        {
            var variantCode = string.IsNullOrWhiteSpace(maSpChiTiet)
                ? $"SPCT-{idSanPhamChiTiet:N}"
                : maSpChiTiet.Trim();

            return $"SPCT|{variantCode}|{idSanPhamChiTiet:D}";
        }

        public class ReservationDetailsRequest
        {
            public List<Guid> VariantIds { get; set; } = new();
        }

        public class VariantReservationDetailDto
        {
            public Guid IdSanPhamChiTiet { get; set; }
            public Guid IdHoaDon { get; set; }
            public string MaDonHang { get; set; } = string.Empty;
            public string? TenKhachHang { get; set; }
            public int SoLuongDangGiu { get; set; }
            public string TrangThaiDon { get; set; } = string.Empty;
        }
    }
}
