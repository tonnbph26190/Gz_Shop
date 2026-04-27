using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Dtos;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;
using System.Collections.Generic; // Added for List
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BanHangTaiQuayController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly IShippingService _shippingService;
        private readonly IGHNService _ghnService;
        private readonly IShippingPolicyService _shippingPolicyService;
        private readonly ILoyaltyService _loyaltyService;
        private readonly IInventoryReservationService _inventoryReservationService;

        public BanHangTaiQuayController(
            BanQuanAu1DbContext context,
            IShippingService shippingService,
            IGHNService ghnService,
            IShippingPolicyService shippingPolicyService,
            ILoyaltyService loyaltyService,
            IInventoryReservationService inventoryReservationService)
        {
            _context = context;
            _shippingService = shippingService;
            _ghnService = ghnService;
            _shippingPolicyService = shippingPolicyService;
            _loyaltyService = loyaltyService;
            _inventoryReservationService = inventoryReservationService;
        }

        // 1. Tạo đơn hàng mới
        [HttpPost("tao-don")]
        public async Task<IActionResult> TaoDonHang([FromBody] TaoDonHangDto dto)
        {
            // Kiểm tra số lượng đơn chưa thanh toán của nhân viên
            var soDonChuaThanhToan = await _context.HoaDons.CountAsync(h => h.IDNhanVien == dto.IDNhanVien && h.TrangThai == "ChuaThanhToan");
            if (soDonChuaThanhToan >= 5)
            {
                return BadRequest(new { message = "Bạn chỉ được tạo tối đa 5 đơn hàng chưa thanh toán cùng lúc." });
            }

            var hoaDon = new HoaDon
            {
                IDHoaDon = Guid.NewGuid(),
                MaHoaDon = $"HD{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                IDNhanVien = dto.IDNhanVien,
                IDKhachHang = dto.IDKhachHang,
                IDPhuongThucThanhToan = Guid.Empty, // Chưa chọn
                TongTien = 0,
                TrangThai = "ChuaThanhToan",
                BanTaiQuay = true,
                NgayTao = DateTime.UtcNow,
                TrangThaiHoaDon = true
            };
            _context.HoaDons.Add(hoaDon);
            var directCommitLines = new List<InventoryLine>();
            await _context.SaveChangesAsync();

            // Thêm sản phẩm nếu có
            if (dto.SanPhams != null && dto.SanPhams.Count > 0)
            {
                foreach (var sp in dto.SanPhams)
                {
                    var spct = await _context.SanPhamChiTiets.FindAsync(sp.IDSanPhamChiTiet);
                    if (spct == null)
                        return BadRequest(new { message = $"Không tìm thấy sản phẩm chi tiết: {sp.IDSanPhamChiTiet}" });

                    // Kiểm tra số lượng tồn kho khả dụng (đã trừ phần đặt chỗ)
                    var soLuongKhaDung = spct.SoLuong - spct.SoLuongDatCho;
                    if (soLuongKhaDung <= 0)
                        return BadRequest(new { message = $"Sản phẩm {spct.MaSPChiTiet} đã hết hàng." });
                    if (sp.SoLuong <= 0)
                        return BadRequest(new { message = $"Số lượng phải lớn hơn 0 cho sản phẩm: {spct.MaSPChiTiet}" });
                    if (sp.SoLuong > soLuongKhaDung)
                        return BadRequest(new { message = $"Số lượng vượt quá tồn khả dụng cho sản phẩm {spct.MaSPChiTiet}. Hiện có: {soLuongKhaDung}" });

                    // Tính giá sau khi áp dụng đợt giảm giá
                    var giaSauGiam = await TinhGiaSauGiam(spct.IDSanPhamChiTiet, spct.GiaBan);

                    var cthd = new ChiTietHoaDon
                    {
                        IDChiTietHoaDon = Guid.NewGuid(),
                        MaChiTietHoaDon = $"CTHD{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                        IDHoaDon = hoaDon.IDHoaDon,
                        IDSanPhamChiTiet = sp.IDSanPhamChiTiet,
                        SoLuong = sp.SoLuong,
                        DonGia = giaSauGiam,
                        ThanhTien = giaSauGiam * sp.SoLuong,
                        NgayTao = DateTime.UtcNow,
                        TrangThai = true
                    };
                    _context.ChiTietHoaDons.Add(cthd);
                    hoaDon.TongTien += cthd.ThanhTien;
                }
                await _context.SaveChangesAsync();
            }
            return Ok(new { hoaDon.IDHoaDon, hoaDon.MaHoaDon });
        }

        // 2. Thêm sản phẩm vào đơn
        [HttpPost("them-san-pham")]
        public async Task<IActionResult> ThemSanPham([FromBody] ThemSanPhamDto dto)
        {
            var hoaDon = await _context.HoaDons.FindAsync(dto.IDHoaDon);
            if (hoaDon == null || hoaDon.TrangThai != "ChuaThanhToan")
                return BadRequest(new { message = "Đơn hàng không tồn tại hoặc đã thanh toán." });
            var spct = await _context.SanPhamChiTiets.FindAsync(dto.IDSanPhamChiTiet);
            if (spct == null)
                return BadRequest(new { message = "Không tìm thấy sản phẩm chi tiết." });

            // Kiểm tra số lượng tồn kho khả dụng
            var soLuongKhaDung = spct.SoLuong - spct.SoLuongDatCho;
            if (soLuongKhaDung <= 0)
                return BadRequest(new { message = "Sản phẩm đã hết hàng." });
            if (dto.SoLuong <= 0)
                return BadRequest(new { message = "Số lượng phải lớn hơn 0." });
            if (dto.SoLuong > soLuongKhaDung)
                return BadRequest(new { message = $"Số lượng vượt quá tồn khả dụng. Hiện có: {soLuongKhaDung}" });

            // Tính giá sau khi áp dụng đợt giảm giá
            var giaSauGiam = await TinhGiaSauGiam(spct.IDSanPhamChiTiet, spct.GiaBan);

            var cthd = await _context.ChiTietHoaDons.FirstOrDefaultAsync(x => x.IDHoaDon == dto.IDHoaDon && x.IDSanPhamChiTiet == dto.IDSanPhamChiTiet);
            if (cthd != null)
            {
                // Kiểm tra tổng số lượng sau khi thêm có vượt quá tồn kho không
                if (cthd.SoLuong + dto.SoLuong > soLuongKhaDung)
                    return BadRequest(new { message = $"Tổng số lượng vượt quá tồn khả dụng. Hiện có: {soLuongKhaDung}, Đã chọn: {cthd.SoLuong}" });

                cthd.SoLuong += dto.SoLuong;
                cthd.ThanhTien = cthd.SoLuong * giaSauGiam;
            }
            else
            {
                cthd = new ChiTietHoaDon
                {
                    IDChiTietHoaDon = Guid.NewGuid(),
                    MaChiTietHoaDon = $"CTHD{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    IDHoaDon = dto.IDHoaDon,
                    IDSanPhamChiTiet = dto.IDSanPhamChiTiet,
                    SoLuong = dto.SoLuong,
                    DonGia = giaSauGiam,
                    ThanhTien = giaSauGiam * dto.SoLuong,
                    NgayTao = DateTime.UtcNow,
                    TrangThai = true
                };
                _context.ChiTietHoaDons.Add(cthd);
            }
            // Cập nhật tổng tiền hóa đơn
            hoaDon.TongTien = await _context.ChiTietHoaDons.Where(x => x.IDHoaDon == dto.IDHoaDon).SumAsync(x => x.ThanhTien);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm sản phẩm vào giỏ hàng thành công!" });
        }

        // 3. Cập nhật/xóa sản phẩm trong đơn
        [HttpPost("cap-nhat-san-pham")]
        public async Task<IActionResult> CapNhatSanPham([FromBody] CapNhatSanPhamDto dto)
        {
            var hoaDon = await _context.HoaDons.FindAsync(dto.IDHoaDon);
            if (hoaDon == null || hoaDon.TrangThai != "ChuaThanhToan")
                return BadRequest(new { message = "Đơn hàng không tồn tại hoặc đã thanh toán." });
            var cthd = await _context.ChiTietHoaDons.FirstOrDefaultAsync(x => x.IDHoaDon == dto.IDHoaDon && x.IDSanPhamChiTiet == dto.IDSanPhamChiTiet);
            if (cthd == null)
                return BadRequest(new { message = "Sản phẩm không tồn tại trong đơn hàng." });
            if (dto.SoLuongMoi <= 0)
            {
                _context.ChiTietHoaDons.Remove(cthd);
                // Cập nhật tổng tiền hóa đơn
                hoaDon.TongTien = await _context.ChiTietHoaDons.Where(x => x.IDHoaDon == dto.IDHoaDon).SumAsync(x => x.ThanhTien);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã xóa sản phẩm khỏi giỏ hàng!" });
            }
            else
            {
                var spct = await _context.SanPhamChiTiets.FindAsync(dto.IDSanPhamChiTiet);
                if (spct == null)
                    return BadRequest(new { message = "Không tìm thấy sản phẩm chi tiết." });

                // Kiểm tra số lượng tồn kho khả dụng
                var soLuongKhaDungMoi = spct.SoLuong - spct.SoLuongDatCho;
                if (soLuongKhaDungMoi <= 0)
                    return BadRequest(new { message = "Sản phẩm đã hết hàng." });
                if (dto.SoLuongMoi > soLuongKhaDungMoi)
                    return BadRequest(new { message = $"Số lượng vượt quá tồn khả dụng. Hiện có: {soLuongKhaDungMoi}" });

                // Tính giá sau khi áp dụng đợt giảm giá
                var giaSauGiam = await TinhGiaSauGiam(spct.IDSanPhamChiTiet, spct.GiaBan);

                cthd.SoLuong = dto.SoLuongMoi;
                cthd.ThanhTien = cthd.SoLuong * giaSauGiam;
            }
            // Cập nhật tổng tiền hóa đơn
            hoaDon.TongTien = await _context.ChiTietHoaDons.Where(x => x.IDHoaDon == dto.IDHoaDon).SumAsync(x => x.ThanhTien);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật số lượng sản phẩm thành công!" });
        }

        // 4. Chọn khách hàng (tìm theo SĐT)
        [HttpPost("chon-khach-hang")]
        public async Task<IActionResult> ChonKhachHang([FromBody] ChonKhachHangDto dto)
        {
            var kh = await _context.KhachHang.FirstOrDefaultAsync(x => x.SoDienThoai == dto.SoDienThoai);
            if (kh == null)
                return NotFound("Không tìm thấy khách hàng với số điện thoại này.");
            return Ok(new { kh.IDKhachHang, kh.TenKhachHang, kh.SoDienThoai });
        }

        // 5. Tạo khách hàng mới nhanh
        [HttpPost("tao-khach-hang")]
        public async Task<IActionResult> TaoKhachHang([FromBody] TaoKhachHangDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenKhachHang) || string.IsNullOrWhiteSpace(dto.SoDienThoai))
                return BadRequest("Tên và số điện thoại không được để trống.");
            var existed = await _context.KhachHang.AnyAsync(x => x.SoDienThoai == dto.SoDienThoai);
            if (existed)
                return BadRequest("Số điện thoại đã tồn tại.");
            var kh = new KhachHang
            {
                IDKhachHang = Guid.NewGuid(),
                MaKhachHang = $"KH{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                TenKhachHang = dto.TenKhachHang,
                SoDienThoai = dto.SoDienThoai,
                NgayTao = DateTime.UtcNow,
                TrangThai = true
            };
            _context.KhachHang.Add(kh);
            await _context.SaveChangesAsync();
            return Ok(new { kh.IDKhachHang, kh.TenKhachHang, kh.SoDienThoai });
        }

        [HttpGet("danh-sach-san-pham")]
        public async Task<IActionResult> GetProducts()
        {
            var now = DateTime.UtcNow;
            var products = await _context.SanPhamChiTiets
                .Include(x => x.SanPham)
                .Include(x => x.AnhSanPhams.Where(a => a.TrangThai))
                .Include(x => x.KichCo)
                .Include(x => x.MauSac)
                .Include(x => x.HoaTiet)
                .Where(x => x.TrangThai && x.SanPham.TrangThai)
                .Select(x => new
                {
                    id = x.IDSanPhamChiTiet,
					ma = x.SanPham.MaSanPham,
					name = x.SanPham.TenSanPham + $" [{x.KichCo.TenKichCo} - {x.MauSac.TenMauSac}" + (x.HoaTiet != null ? $" - {x.HoaTiet.TenHoaTiet}" : "") + "]",
                    qrCode = x.QRCode,
                    // Giá gốc
                    originalPrice = x.GiaBan,
                    // Tính giá giảm nếu có đợt giảm giá đang áp dụng
                    price = (
                        (from dgg in _context.DotGiamGias
                         join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
                         where sp.IDSanPhamChiTiet == x.IDSanPhamChiTiet
                            && dgg.TrangThai == true
                            && dgg.NgayBatDau <= now
                            && dgg.NgayKetThuc >= now
                         select dgg.PhanTramGiam
                        ).FirstOrDefault() > 0
                        ? x.GiaBan * (1 - (decimal)(
                            (from dgg in _context.DotGiamGias
                             join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
                             where sp.IDSanPhamChiTiet == x.IDSanPhamChiTiet
                                && dgg.TrangThai == true
                                && dgg.NgayBatDau <= now
                                && dgg.NgayKetThuc >= now
                             select dgg.PhanTramGiam
                            ).FirstOrDefault() / 100.0m))
                        : x.GiaBan
                    ),
                    size = x.KichCo.TenKichCo,
                    color = x.MauSac.TenMauSac,
                    pattern = x.HoaTiet != null ? x.HoaTiet.TenHoaTiet : "Không có",
                    // Lấy ảnh chính hoặc ảnh đầu tiên
                    img = x.AnhSanPhams
                        .Where(a => a.TrangThai)
                        .OrderByDescending(a => a.LaAnhChinh)
                        .ThenBy(a => a.NgayTao)
                        .Select(a => a.UrlAnh)
                        .FirstOrDefault() ?? "/img/default-product.jpg",
                    // Thêm ảnh chính riêng biệt
                    mainImage = x.AnhSanPhams
                        .Where(a => a.TrangThai && a.LaAnhChinh)
                        .Select(a => a.UrlAnh)
                        .FirstOrDefault() ?? "/img/default-product.jpg",
                    stock = x.SoLuong - x.SoLuongDatCho,
                    // Thêm thông tin sản phẩm gốc
                    productId = x.IDSanPham,
                    productName = x.SanPham.TenSanPham,
                    // Thêm danh sách ảnh
                    images = x.AnhSanPhams
                        .Where(a => a.TrangThai)
                        .OrderByDescending(a => a.LaAnhChinh)
                        .ThenBy(a => a.NgayTao)
                        .Select(a => new
                        {
                            id = a.IDAnhSanPham,
                            url = a.UrlAnh,
                            isMain = a.LaAnhChinh
                        }).ToList()
                }).ToListAsync();
            return Ok(products);
        }

        [HttpGet("tim-san-pham-theo-qr")]
        public async Task<IActionResult> FindProductByQr([FromQuery] string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
                return BadRequest(new { message = "Mã QR không được để trống." });

            var now = DateTime.UtcNow;
            var normalizedQrCode = qrCode.Trim();
            var (sanPhamChiTietId, maSanPhamChiTiet) = ParseQrCode(normalizedQrCode);

            var product = await _context.SanPhamChiTiets
                .Include(x => x.SanPham)
                .Include(x => x.AnhSanPhams.Where(a => a.TrangThai))
                .Include(x => x.KichCo)
                .Include(x => x.MauSac)
                .Include(x => x.HoaTiet)
                .Where(x => x.TrangThai && x.SanPham.TrangThai)
                .Where(x =>
                    x.QRCode == normalizedQrCode ||
                    (sanPhamChiTietId.HasValue && x.IDSanPhamChiTiet == sanPhamChiTietId.Value) ||
                    (!string.IsNullOrWhiteSpace(maSanPhamChiTiet) && x.MaSPChiTiet == maSanPhamChiTiet))
                .Select(x => new
                {
                    id = x.IDSanPhamChiTiet,
                    name = x.SanPham.TenSanPham + $" [{x.KichCo.TenKichCo} - {x.MauSac.TenMauSac}" + (x.HoaTiet != null ? $" - {x.HoaTiet.TenHoaTiet}" : "") + "]",
                    qrCode = x.QRCode,
                    originalPrice = x.GiaBan,
                    price = (
                        (from dgg in _context.DotGiamGias
                         join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
                         where sp.IDSanPhamChiTiet == x.IDSanPhamChiTiet
                            && dgg.TrangThai == true
                            && dgg.NgayBatDau <= now
                            && dgg.NgayKetThuc >= now
                         select dgg.PhanTramGiam
                        ).FirstOrDefault() > 0
                        ? x.GiaBan * (1 - (decimal)(
                            (from dgg in _context.DotGiamGias
                             join sp in _context.SanPhamDotGiams on dgg.IDDotGiamGia equals sp.IDDotGiamGia
                             where sp.IDSanPhamChiTiet == x.IDSanPhamChiTiet
                                && dgg.TrangThai == true
                                && dgg.NgayBatDau <= now
                                && dgg.NgayKetThuc >= now
                             select dgg.PhanTramGiam
                            ).FirstOrDefault() / 100.0m))
                        : x.GiaBan
                    ),
                    size = x.KichCo.TenKichCo,
                    color = x.MauSac.TenMauSac,
                    pattern = x.HoaTiet != null ? x.HoaTiet.TenHoaTiet : "Không có",
                    img = x.AnhSanPhams
                        .Where(a => a.TrangThai)
                        .OrderByDescending(a => a.LaAnhChinh)
                        .ThenBy(a => a.NgayTao)
                        .Select(a => a.UrlAnh)
                        .FirstOrDefault() ?? "/img/default-product.jpg",
                    mainImage = x.AnhSanPhams
                        .Where(a => a.TrangThai && a.LaAnhChinh)
                        .Select(a => a.UrlAnh)
                        .FirstOrDefault() ?? "/img/default-product.jpg",
                    stock = x.SoLuong - x.SoLuongDatCho,
                    productId = x.IDSanPham,
                    productName = x.SanPham.TenSanPham
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm đang kinh doanh từ mã QR." });

            if (product.stock <= 0)
                return BadRequest(new { message = "Sản phẩm đã hết hàng, không thể thêm vào giỏ." });

            return Ok(product);
        }

        [HttpGet("danh-sach-khach-hang")]
        public async Task<IActionResult> GetCustomers()
        {

            var customers = await _context.KhachHang
				 //.Where(x => x.TrangThai)
				 .OrderByDescending(x => x.SoDiemHienTai)
				.Select(x => new
                {
                    id = x.IDKhachHang,
                    name = x.TenKhachHang,
                    email = x.Email,
                    phone = x.SoDienThoai,
                    point = x.SoDiemHienTai,
					rankName = _context.HangKhachHangs
			.Where(h => h.TrangThai == true
				&& x.SoDiemHienTai >= h.DiemTu   // ✅ QUAN TRỌNG
				&& x.SoDiemHienTai <= h.DiemDen)
			.Select(h => h.TenHang)
			.FirstOrDefault(),
					img = "/img/default-user.png" // Nếu có trường ảnh thì thay thế
                }).ToListAsync();
            return Ok(customers);
        }

        [HttpGet("tim-kiem-khach-hang")]
        public async Task<IActionResult> SearchCustomer(string query)
        {
            var customers = await _context.KhachHang
                .Where(x => x.TenKhachHang.Contains(query) || x.SoDienThoai.Contains(query))
                .Select(x => new
                {
                    id = x.IDKhachHang,
                    name = x.TenKhachHang,
                    email = x.Email,
                    phone = x.SoDienThoai,
                    point = x.SoDiemHienTai,
                    img = "/img/default-user.png"
                }).ToListAsync();
            return Ok(customers);
        }

        [HttpPost("them-khach-hang")]
        public async Task<IActionResult> AddCustomer([FromBody] TaoKhachHangDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenKhachHang) || string.IsNullOrWhiteSpace(dto.SoDienThoai))
                return BadRequest("Tên và số điện thoại không được để trống.");
            var existed = await _context.KhachHang.AnyAsync(x => x.SoDienThoai == dto.SoDienThoai);
            if (existed)
                return BadRequest("Số điện thoại đã tồn tại.");
            var kh = new KhachHang
            {
                IDKhachHang = Guid.NewGuid(),
                MaKhachHang = $"KH{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                TenKhachHang = dto.TenKhachHang,
                SoDienThoai = dto.SoDienThoai,
                NgayTao = DateTime.UtcNow,
                TrangThai = true
            };
            _context.KhachHang.Add(kh);
            await _context.SaveChangesAsync();
            return Ok(new { kh.IDKhachHang, kh.TenKhachHang, kh.SoDienThoai, kh.Email });
        }

        [HttpGet("kiem-tra-ma-giam-gia")]
        public async Task<IActionResult> CheckDiscount(string code)
        {
            var discount = await _context.PhieuGiamGias
                .FirstOrDefaultAsync(x => x.MaCode == code && x.TrangThai);
            if (discount == null) return Ok(new { success = false });
            return Ok(new { success = true, value = discount.GiaTriGiam, max = discount.GiaTriGiamToiDa });
        }

        public class InvoiceProductDto
        {
            public Guid ProductDetailId { get; set; }
            public int Quantity { get; set; }
        }

        public class InvoiceDto
        {
            public Guid? CustomerId { get; set; }
            public string? CustomerName { get; set; }
            public string? CustomerPhone { get; set; }
            public string? CustomerEmail { get; set; }
            public string? Address { get; set; }
            public List<InvoiceProductDto> Products { get; set; }
            public string? DiscountCode { get; set; }
            public bool UsePoint { get; set; }
            public int? RequestedUsedPoints { get; set; }
            public bool Shipping { get; set; }
            public string? PaymentMethod { get; set; } // Mã phương thức thanh toán ("cash", "bank", ...)
            public decimal? CustomerPaid { get; set; }
            public decimal? ShippingFee { get; set; } // Phí vận chuyển
            public string? Province { get; set; }
            public string? District { get; set; }
            public int? ToDistrictId { get; set; }
            public string? ToWardCode { get; set; }
            public int? Weight { get; set; }
            public string? ShippingFeeSource { get; set; }
        }

        [HttpPost("thanh-toan")]
        public async Task<IActionResult> PayInvoice([FromBody] InvoiceDto dto)
        {
			// ===== VALIDATE SHIPPING INFO =====
			var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀ-ỹ\s]+$");
			var phoneRegex = new System.Text.RegularExpressions.Regex(@"^(0|\+84)[0-9]{9}$");
			var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");

			string name = (dto.CustomerName ?? "").Trim();
			string phone = (dto.CustomerPhone ?? "").Trim();
			string email = (dto.CustomerEmail ?? "").Trim();

            if (dto.Shipping)
            {
			    // Đơn giao hàng: bắt buộc tên + SĐT + địa chỉ
			    if (string.IsNullOrEmpty(name) || !nameRegex.IsMatch(name))
			    {
				    return BadRequest(new { message = "Tên không hợp lệ (không chứa số)." });
			    }

			    if (string.IsNullOrEmpty(phone) || !phoneRegex.IsMatch(phone))
			    {
				    return BadRequest(new { message = "Số điện thoại không hợp lệ." });
			    }

			    if (string.IsNullOrWhiteSpace(dto.Address))
			    {
				    return BadRequest(new { message = "Vui lòng nhập địa chỉ giao hàng." });
			    }
            }
            else
            {
                // Đơn tại quầy: cho phép khách vãng lai không cần tài khoản/SĐT.
                if (string.IsNullOrEmpty(name))
                {
                    name = "Khách vãng lai";
                }
                else if (!nameRegex.IsMatch(name))
                {
                    return BadRequest(new { message = "Tên không hợp lệ (không chứa số)." });
                }

                if (!string.IsNullOrEmpty(phone) && !phoneRegex.IsMatch(phone))
                {
                    return BadRequest(new { message = "Số điện thoại không hợp lệ." });
                }
            }

			// Email không bắt buộc, nhưng nếu nhập thì phải đúng định dạng
			if (!string.IsNullOrEmpty(email) && !emailRegex.IsMatch(email))
			{
				return BadRequest(new { message = "Email không đúng định dạng." });
			}

            dto.CustomerName = name;
            dto.CustomerPhone = string.IsNullOrEmpty(phone) ? null : phone;
            dto.CustomerEmail = string.IsNullOrEmpty(email) ? null : email;
			
            await using var tx = await _context.Database.BeginTransactionAsync();
            // Map code sang ID phương thức thanh toán
            Guid paymentMethodId = Guid.Empty;
            if (!string.IsNullOrEmpty(dto.PaymentMethod))
            {
                var method = await _context.PhuongThucThanhToans
                    .FirstOrDefaultAsync(x => x.MaPhuongThuc == dto.PaymentMethod && x.TrangThai);
                if (method == null)
                    return BadRequest("Phương thức thanh toán không hợp lệ.");
                paymentMethodId = method.IDPhuongThucThanhToan;
            }
            else
                return BadRequest("Chưa chọn phương thức thanh toán.");

            string trangThaiHoaDon;
            var cashPaymentMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
             {
    "cash", "Tiền mặt", "tiền mặt"
    };
            if (dto.Shipping && !string.IsNullOrWhiteSpace(dto.Address) && cashPaymentMethods.Contains(dto.PaymentMethod))
            {
                trangThaiHoaDon = "Đã xác nhận";
            }
            else
            {
                trangThaiHoaDon = "DaThanhToan";
            }

            // Khách vãng lai chọn giao hàng: tạo KhachHang + địa chỉ trong DB rồi gán vào hóa đơn
            Guid? customerIdForInvoice = dto.CustomerId;
            if (!dto.CustomerId.HasValue && dto.Shipping && !string.IsNullOrWhiteSpace(dto.CustomerName) && !string.IsNullOrWhiteSpace(dto.CustomerPhone))
            {
                var guest = await TaoKhachHangVangLaiAsync(dto.CustomerName!, dto.CustomerPhone, dto.CustomerEmail, dto.Address);
                customerIdForInvoice = guest.IDKhachHang;
            }

            // 1. Tạo hóa đơn
            var hoaDon = new HoaDon
            {
                IDHoaDon = Guid.NewGuid(),
                MaHoaDon = $"HD{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                IDKhachHang = customerIdForInvoice,
                TenNguoiNhan = dto.CustomerName,
                SoDienThoaiNguoiNhan = dto.CustomerPhone,
                DiaChiGiaoHang = dto.Address,
                TongTien = 0,
                TrangThai = trangThaiHoaDon,
                DaDatChoTonKho = false,
                DaTruTonKho = true,
                BanTaiQuay = true,
                NgayTao = DateTime.UtcNow,
                TrangThaiHoaDon = true,
                IDPhuongThucThanhToan = paymentMethodId
            };
            _context.HoaDons.Add(hoaDon);
            var directCommitLines = new List<InventoryLine>();

            // 2. Thêm chi tiết hóa đơn và kiểm tra tồn kho
            foreach (var p in dto.Products)
            {
                var spct = await _context.SanPhamChiTiets.FindAsync(p.ProductDetailId);
                if (spct == null)
                    return BadRequest(new { message = "Sản phẩm không tồn tại" });
                if (!spct.TrangThai)
                    return BadRequest(new { message = $"Sản phẩm {spct.MaSPChiTiet ?? spct.IDSanPhamChiTiet.ToString()} đã ngưng bán, không thể thanh toán." });

                // Kiểm tra số lượng tồn kho
                var soLuongKhaDung = spct.SoLuong - spct.SoLuongDatCho;
                if (soLuongKhaDung <= 0)
                    return BadRequest(new { message = $"Sản phẩm {spct.IDSanPhamChiTiet} đã hết hàng" });
                if (p.Quantity <= 0)
                    return BadRequest(new { message = $"Số lượng phải lớn hơn 0 cho sản phẩm {spct.IDSanPhamChiTiet}" });
                if (p.Quantity > soLuongKhaDung)
                    return BadRequest(new { message = $"Sản phẩm {spct.IDSanPhamChiTiet} vượt quá tồn khả dụng ({soLuongKhaDung})" });
                // Tính giá sau khi áp dụng đợt giảm giá
                var giaSauGiam = await TinhGiaSauGiam(spct.IDSanPhamChiTiet, spct.GiaBan);

                var cthd = new ChiTietHoaDon
                {
                    IDChiTietHoaDon = Guid.NewGuid(),
                    MaChiTietHoaDon = $"CTHD{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    IDHoaDon = hoaDon.IDHoaDon,
                    IDSanPhamChiTiet = p.ProductDetailId,
                    SoLuong = p.Quantity,
                    DonGia = giaSauGiam,
                    ThanhTien = giaSauGiam * p.Quantity,
                    NgayTao = DateTime.UtcNow,
                    TrangThai = true
                };
                hoaDon.TongTien += cthd.ThanhTien;
                _context.ChiTietHoaDons.Add(cthd);
                directCommitLines.Add(new InventoryLine(p.ProductDetailId, p.Quantity));
            }

            // 3. Áp dụng mã giảm giá nếu có
            if (!string.IsNullOrEmpty(dto.DiscountCode))
            {
                var discount = await _context.PhieuGiamGias.FirstOrDefaultAsync(x => x.MaCode == dto.DiscountCode);
                if (discount != null)
                {
                    var now = DateTime.UtcNow;
                    if (!discount.TrangThai || discount.NgayBatDau > now || discount.NgayKetThuc < now)
                    {
                        return BadRequest(new { message = "Mã giảm giá không còn hiệu lực." });
                    }

                    if (discount.DonToiThieu.HasValue && hoaDon.TongTien < discount.DonToiThieu.Value)
                    {
                        return BadRequest(new { message = $"Đơn hàng chưa đạt giá trị tối thiểu {discount.DonToiThieu.Value:n0}." });
                    }

                    // Kiểm tra xem khách hàng có phiếu này không và còn số lượng không
                    if (dto.CustomerId.HasValue)
                    {
                        var customerVoucher = await _context.KhachHangPhieuGiams
                            .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == discount.IDPhieuGiamGia &&
                                                     x.IDKhachHang == dto.CustomerId.Value);

                        if (customerVoucher == null)
                        {
                            customerVoucher = new KhachHangPhieuGiam
                            {
                                IDKhachHangPhieuGiam = Guid.NewGuid(),
                                MaKhachHangPhieuGiam = $"KHPG_{DateTime.UtcNow:yyyyMMddHHmmss}_{dto.CustomerId.Value.ToString().Substring(0, 8)}",
                                IDKhachHang = dto.CustomerId.Value,
                                IDPhieuGiamGia = discount.IDPhieuGiamGia,
                                SoLuong = discount.SoLuong > 0 ? discount.SoLuong : (short)1,
                                SoLuongDaSuDung = 0,
                                NgayTao = DateTime.UtcNow,
                                NguoiTao = "System",
                                TrangThai = true
                            };

                            _context.KhachHangPhieuGiams.Add(customerVoucher);
                        }

                        if (!customerVoucher.TrangThai || customerVoucher.SoLuongDaSuDung >= customerVoucher.SoLuong)
                            return BadRequest(new { message = "Phiếu giảm giá không hợp lệ hoặc đã được sử dụng hết." });

                        // Tăng số lượng đã sử dụng
                        customerVoucher.SoLuongDaSuDung++;
                        customerVoucher.LanCapNhatCuoi = DateTime.UtcNow;
                        customerVoucher.NguoiCapNhat = "System";

                        // Nếu đã sử dụng hết, vô hiệu hóa
                        if (customerVoucher.SoLuongDaSuDung >= customerVoucher.SoLuong)
                        {
                            customerVoucher.TrangThai = false;
                        }
                    }

                    // Tính số tiền giảm theo phần trăm
                    var tienGiam = hoaDon.TongTien * (discount.GiaTriGiam / 100m);
                    // Nếu có giá trị giảm tối đa, lấy min
                    if (discount.GiaTriGiamToiDa.HasValue)
                        tienGiam = Math.Min(tienGiam, discount.GiaTriGiamToiDa.Value);

                    hoaDon.TienGiam = tienGiam;
                    hoaDon.IDPhieuGiamGia = discount.IDPhieuGiamGia;
                    hoaDon.TongTien -= tienGiam;
                }
                else
                    return BadRequest(new { message = "Mã giảm giá không hợp lệ." });
            }

            // 4. Áp dụng điểm nếu có
            var loyaltyResult = await _loyaltyService.BuildCheckoutResultAsync(
                customerIdForInvoice,
                hoaDon.TongTien,
                dto.UsePoint,
                dto.RequestedUsedPoints);
            // Luôn lưu snapshot điểm / tỷ lệ quy đổi (khớp dữ liệu hiển thị khi tạo đơn); chỉ trừ TongTien khi có giảm từ điểm
            hoaDon.DiemDaDung = loyaltyResult.UsedPoints;
            hoaDon.SoTienGiamTuDiem = loyaltyResult.DiscountFromPoints;
            hoaDon.TyLeQuyDoiDiem = loyaltyResult.PointConversionRate;
            if (loyaltyResult.DiscountFromPoints > 0)
            {
                hoaDon.TongTien = Math.Max(hoaDon.TongTien - loyaltyResult.DiscountFromPoints, 0);
            }

            hoaDon.DiemCong = loyaltyResult.EarnedPoints;

            // 5. Thêm phí vận chuyển sau khi tính giảm theo hạng điểm
            if (dto.Shipping)
            {
                var shippingCalc = await TinhPhiVanChuyenChoThanhToanAsync(
                    dto.Province,
                    dto.District,
                    dto.ToDistrictId,
                    dto.ToWardCode,
                    dto.Weight,
                    hoaDon.TongTien,
                    customerIdForInvoice,
                    dto.ShippingFee,
                    dto.ShippingFeeSource);

                hoaDon.PhiVanChuyenGoc = shippingCalc.OriginalFee;
                hoaDon.PhiVanChuyen = shippingCalc.FinalFee;
                hoaDon.SoTienGiamPhiVanChuyen = shippingCalc.DiscountAmount;
                hoaDon.TongTien += shippingCalc.FinalFee;
            }

            if (customerIdForInvoice.HasValue)
            {
                await _loyaltyService.ApplyCheckoutPointChangesAsync(customerIdForInvoice.Value, hoaDon.IDHoaDon, loyaltyResult, "POS");
            }

            var directCommitResult = await _inventoryReservationService.CommitDirectAsync(directCommitLines, "POS");
            if (!directCommitResult.Success)
            {
                return BadRequest(new { message = directCommitResult.ErrorMessage ?? "Không thể trừ tồn kho khi thanh toán." });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { hoaDon.IDHoaDon, hoaDon.MaHoaDon });
        }

        [HttpGet("danh-sach-phuong-thuc-thanh-toan")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var methods = await _context.PhuongThucThanhToans
                .Where(x => x.TrangThai)
                .Select(x => new { x.IDPhuongThucThanhToan, x.MaPhuongThuc, x.TenPhuongThuc })
                .ToListAsync();
            return Ok(methods);
        }

		[HttpGet("danh-sach-phieu-giam-gia-khach-hang")]
		public async Task<IActionResult> GetCustomerDiscountVouchers(Guid? customerId, decimal tongTien)
		{
			var now = DateTime.UtcNow;
			IQueryable<KhachHangPhieuGiam> customerVoucherQuery = _context.KhachHangPhieuGiams
				.AsNoTracking()
				.Where(x => false);

			if (customerId.HasValue)
			{
				customerVoucherQuery = _context.KhachHangPhieuGiams
					.AsNoTracking()
					.Where(x => x.IDKhachHang == customerId.Value);
			}

			var raw = await _context.PhieuGiamGias
				.AsNoTracking()
				.Where(p => p.TrangThai &&
							p.NgayBatDau <= now &&
							p.NgayKetThuc >= now)
				.GroupJoin(
					customerVoucherQuery,
					p => p.IDPhieuGiamGia,
					x => x.IDPhieuGiamGia,
					(p, links) => new
					{
						Voucher = p,
						CustomerVoucher = links.FirstOrDefault()
					})
				.Where(x => x.Voucher.LaCongKhai ||
							(x.CustomerVoucher != null &&
							 x.CustomerVoucher.TrangThai &&
							 x.CustomerVoucher.SoLuongDaSuDung < x.CustomerVoucher.SoLuong))
				.Select(x => new
				{
					id = x.Voucher.IDPhieuGiamGia,
					maCode = x.Voucher.MaCode,
					tenPhieu = x.Voucher.TenPhieu,
					giaTriGiam = x.Voucher.GiaTriGiam,
					giaTriGiamToiDa = x.Voucher.GiaTriGiamToiDa,
					donToiThieu = x.Voucher.DonToiThieu,
					ngayBatDau = x.Voucher.NgayBatDau,
					ngayKetThuc = x.Voucher.NgayKetThuc,
					soLuong = x.CustomerVoucher != null ? x.CustomerVoucher.SoLuong : x.Voucher.SoLuong,
					soLuongDaSuDung = x.CustomerVoucher != null ? x.CustomerVoucher.SoLuongDaSuDung : 0,
					soLuongConLai = x.CustomerVoucher != null
						? x.CustomerVoucher.SoLuong - x.CustomerVoucher.SoLuongDaSuDung
						: x.Voucher.SoLuong
				})
				.ToListAsync(); // 🔥 lấy về trước

			// 👉 xử lý tại C#
			var vouchers = raw.Select(x =>
			{
				var hopLe = tongTien >= (x.donToiThieu ?? 0);

				decimal tienGiam = 0;

				if (x.giaTriGiam <= 100)
				{
					tienGiam = tongTien * x.giaTriGiam / 100;
				}
				else
				{
					tienGiam = x.giaTriGiam;
				}

				if (x.giaTriGiamToiDa.HasValue)
				{
					tienGiam = Math.Min(tienGiam, x.giaTriGiamToiDa.Value);
				}

				return new
				{
					x.id,
					x.maCode,
					x.tenPhieu,
					x.giaTriGiam,
					x.giaTriGiamToiDa,
					x.donToiThieu,
					x.ngayBatDau,
					x.ngayKetThuc,
					x.soLuong,
					x.soLuongDaSuDung,
					x.soLuongConLai,
					hopLe,
					tienGiamThucTe = tienGiam,
					doLech = Math.Abs((x.donToiThieu ?? 0) - tongTien)
				};
			})
			.OrderByDescending(x => x.hopLe)
			.ThenByDescending(x => x.tienGiamThucTe) // 🔥 giờ mới đúng
			.ThenBy(x => x.doLech)
			.ThenByDescending(x => x.soLuongConLai)
			.ThenBy(x => x.ngayKetThuc)
			.ToList();

			return Ok(vouchers);
		}


		// Lấy chi tiết sản phẩm với đầy đủ ảnh
		[HttpGet("chi-tiet-san-pham/{id}")]
        public async Task<IActionResult> GetProductDetail(Guid id)
        {
            var product = await _context.SanPhamChiTiets
                .Include(x => x.SanPham)
                    .ThenInclude(s => s.ChatLieu)
                .Include(x => x.SanPham)
                    .ThenInclude(s => s.DanhMuc)
                .Include(x => x.SanPham)
                    .ThenInclude(s => s.ThuongHieu)
                .Include(x => x.AnhSanPhams.Where(a => a.TrangThai))
                .Include(x => x.KichCo)
                .Include(x => x.MauSac)
                .Include(x => x.HoaTiet) // Thêm include này để tránh lỗi
                .Where(x => x.IDSanPhamChiTiet == id && x.TrangThai)
                .Select(x => new
                {
                    id = x.IDSanPhamChiTiet,
                    productId = x.IDSanPham,
                    name = x.SanPham.TenSanPham,
                    description = $"Kích cỡ: {x.KichCo.TenKichCo}, Màu sắc: {x.MauSac.TenMauSac}" +
                                 (x.HoaTiet != null ? $", Họa tiết: {x.HoaTiet.TenHoaTiet}" : ""),
                    price = x.GiaBan,
                    stock = x.SoLuong - x.SoLuongDatCho,
                    size = x.KichCo.TenKichCo,
                    color = x.MauSac.TenMauSac,
                    pattern = x.HoaTiet != null ? x.HoaTiet.TenHoaTiet : null,
                    // Thông tin sản phẩm gốc
                    material = x.SanPham.ChatLieu.TenChatLieu,
                    category = x.SanPham.DanhMuc.TenDanhMuc,
                    brand = x.SanPham.ThuongHieu.TenThuongHieu,
                    hasPleats = x.SanPham.CoXepLy,
                    hasElastic = x.SanPham.CoGian,
                    // Ảnh chính
                    mainImage = x.AnhSanPhams
                        .Where(a => a.TrangThai)
                        .OrderByDescending(a => a.LaAnhChinh)
                        .ThenBy(a => a.NgayTao)
                        .Select(a => a.UrlAnh)
                        .FirstOrDefault() ?? "/img/default-product.jpg",
                    // Danh sách tất cả ảnh
                    images = x.AnhSanPhams
                        .Where(a => a.TrangThai)
                        .OrderByDescending(a => a.LaAnhChinh)
                        .ThenBy(a => a.NgayTao)
                        .Select(a => new
                        {
                            id = a.IDAnhSanPham,
                            url = a.UrlAnh,
                            isMain = a.LaAnhChinh,
                            createdAt = a.NgayTao
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            return Ok(product);
        }

        // Lấy địa chỉ của khách hàng
        [HttpGet("dia-chi-khach-hang")]
        public async Task<IActionResult> GetCustomerAddress(Guid customerId)
        {
            try
            {
                // Lấy địa chỉ mặc định của khách hàng
                var diaChi = await _context.DiaChis
                    .Where(x => x.IDKhachHang == customerId && x.TrangThai)
                    .OrderByDescending(x => x.LaMacDinh) // Ưu tiên địa chỉ mặc định
                    .ThenBy(x => x.NgayTao) // Sau đó theo ngày tạo
                    .FirstOrDefaultAsync();

                if (diaChi == null)
                {
                    return Ok(null); // Không có địa chỉ
                }

                return Ok(new
                {
                    tenNguoiNhan = diaChi.TenNguoiNhan,
                    sdtNguoiNhan = diaChi.SdtNguoiNhan,
                    diaChiChiTiet = diaChi.DiaChiChiTiet,
                    laMacDinh = diaChi.LaMacDinh
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy địa chỉ khách hàng: {ex.Message}");
                return StatusCode(500, "Lỗi khi lấy địa chỉ khách hàng");
            }
        }

        // Tạo địa chỉ mới cho khách hàng
        [HttpPost("tao-dia-chi")]
        public async Task<IActionResult> TaoDiaChi([FromBody] TaoDiaChiDto dto)
        {
            try
            {
                // Kiểm tra khách hàng có tồn tại không
                var khachHang = await _context.KhachHang.FindAsync(dto.IDKhachHang);
                if (khachHang == null)
                {
                    return BadRequest("Khách hàng không tồn tại.");
                }

				// 🔥 normalize để so sánh
				string Normalize(string s) => (s ?? "").Trim().ToLower();

				var diaChiMoi = Normalize(dto.DiaChiChiTiet);
				var tenMoi = Normalize(dto.TenNguoiNhan ?? khachHang.TenKhachHang);
				var sdtMoi = Normalize(dto.SdtNguoiNhan ?? khachHang.SoDienThoai);

				// 🚫 CHECK TRÙNG - Lấy dữ liệu trước rồi so sánh trong bộ nhớ
				var existingAddresses = await _context.DiaChis
					.Where(x =>
						x.IDKhachHang == dto.IDKhachHang &&
						x.TrangThai)
					.ToListAsync();

				var isExist = existingAddresses.Any(x =>
					Normalize(x.DiaChiChiTiet) == diaChiMoi &&
					Normalize(x.TenNguoiNhan) == tenMoi &&
					Normalize(x.SdtNguoiNhan) == sdtMoi
				);

				if (isExist)
				{
					return BadRequest("Địa chỉ đã tồn tại.");
				}

				// 🔥 Nếu là mặc định → bỏ mặc định cũ
				if (dto.LaMacDinh)
				{
					var diaChiMacDinhKhac = await _context.DiaChis
						.Where(x => x.IDKhachHang == dto.IDKhachHang && x.LaMacDinh && x.TrangThai)
						.ToListAsync();

					foreach (var dc in diaChiMacDinhKhac)
					{
						dc.LaMacDinh = false;
					}
				}

				// ✅ tạo mới
				var diaChi = new DiaChi
				{
					IDDiaChi = Guid.NewGuid(),
					MaDiaChi = $"DC{DateTime.UtcNow:yyyyMMddHHmmssfff}",
					IDKhachHang = dto.IDKhachHang,
					DiaChiChiTiet = dto.DiaChiChiTiet.Trim(),
					LaMacDinh = dto.LaMacDinh,
					TenNguoiNhan = dto.TenNguoiNhan ?? khachHang.TenKhachHang,
					SdtNguoiNhan = dto.SdtNguoiNhan ?? khachHang.SoDienThoai,
					NgayTao = DateTime.UtcNow,
					NguoiTao = "System",
					TrangThai = true
				};

				_context.DiaChis.Add(diaChi);
				await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Tạo địa chỉ thành công",
                    diaChi = new
                    {
                        id = diaChi.IDDiaChi,
                        maDiaChi = diaChi.MaDiaChi,
                        diaChiChiTiet = diaChi.DiaChiChiTiet,
                        tenNguoiNhan = diaChi.TenNguoiNhan,
                        sdtNguoiNhan = diaChi.SdtNguoiNhan,
                        laMacDinh = diaChi.LaMacDinh
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tạo địa chỉ: {ex.Message}");
                return StatusCode(500, "Lỗi khi tạo địa chỉ");
            }
        }

        // Lấy danh sách địa chỉ của khách hàng
        [HttpGet("danh-sach-dia-chi-khach-hang")]
        public async Task<IActionResult> GetCustomerAddresses(Guid customerId)
        {
            try
            {
                var diaChis = await _context.DiaChis
                    .Where(x => x.IDKhachHang == customerId && x.TrangThai)
                    .OrderByDescending(x => x.LaMacDinh)
                    .ThenBy(x => x.NgayTao)
                    .Select(x => new
                    {
                        id = x.IDDiaChi,
                        tenNguoiNhan = x.TenNguoiNhan,
                        sdtNguoiNhan = x.SdtNguoiNhan,
                        diaChiChiTiet = x.DiaChiChiTiet,
                        laMacDinh = x.LaMacDinh,
                        ngayTao = x.NgayTao
                    })
                    .ToListAsync();

                return Ok(diaChis);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy danh sách địa chỉ khách hàng: {ex.Message}");
                return StatusCode(500, "Lỗi khi lấy danh sách địa chỉ khách hàng");
            }
        }

        // Xóa (vô hiệu hóa) địa chỉ của khách hàng
        [HttpDelete("xoa-dia-chi/{id}")]
        public async Task<IActionResult> XoaDiaChi(Guid id)
        {
            try
            {
                var diaChi = await _context.DiaChis.FindAsync(id);
                if (diaChi == null)
                {
                    return NotFound("Không tìm thấy địa chỉ.");
                }

                // Soft delete địa chỉ
                bool wasDefault = diaChi.LaMacDinh;
                diaChi.TrangThai = false;
                diaChi.LaMacDinh = false;
                diaChi.LanCapNhatCuoi = DateTime.UtcNow;
                diaChi.NguoiCapNhat = "System";

                // Nếu đây là địa chỉ mặc định, gán địa chỉ khác làm mặc định (nếu có)
                if (wasDefault)
                {
                    var other = await _context.DiaChis
                        .Where(x => x.IDKhachHang == diaChi.IDKhachHang && x.TrangThai && x.IDDiaChi != diaChi.IDDiaChi)
                        .OrderByDescending(x => x.NgayTao)
                        .FirstOrDefaultAsync();

                    if (other != null)
                    {
                        other.LaMacDinh = true;
                        other.LanCapNhatCuoi = DateTime.UtcNow;
                        other.NguoiCapNhat = "System";
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Xóa địa chỉ thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xóa địa chỉ: {ex.Message}");
                return StatusCode(500, "Lỗi khi xóa địa chỉ");
            }
        }


        // Phương thức tính giá sau khi áp dụng đợt giảm giá
        private async Task<decimal> TinhGiaSauGiam(Guid sanPhamChiTietId, decimal giaGoc)
        {
            try
            {
                var now = DateTime.UtcNow;
                // Tìm đợt giảm giá đang áp dụng cho sản phẩm này
                var dotGiamGia = await _context.DotGiamGias
                    .Join(_context.SanPhamDotGiams,
                          dgg => dgg.IDDotGiamGia,
                          spdg => spdg.IDDotGiamGia,
                          (dgg, spdg) => new { dgg, spdg })
                    .Where(x => x.spdg.IDSanPhamChiTiet == sanPhamChiTietId &&
                               x.dgg.TrangThai == true &&
                               x.dgg.NgayBatDau <= now &&
                               x.dgg.NgayKetThuc >= now)
                    .Select(x => x.dgg)
                    .FirstOrDefaultAsync();

                if (dotGiamGia != null)
                {
                    // Tính giá sau khi giảm
                    var phanTramGiam = (decimal)dotGiamGia.PhanTramGiam;
                    var giaSauGiam = giaGoc * (1 - phanTramGiam / 100);
                    return Math.Round(giaSauGiam, 2);
                }

                // Nếu không có đợt giảm giá, trả về giá gốc
                return giaGoc;
            }
            catch (Exception ex)
            {
                // Log lỗi nếu có
                Console.WriteLine($"Lỗi khi tính giá sau giảm: {ex.Message}");
                return giaGoc; // Trả về giá gốc nếu có lỗi
            }
        }

        // Tạo giỏ hàng mới cho bán hàng tại quầy
        [HttpPost("tao-gio-hang")]
        public async Task<IActionResult> TaoGioHang([FromBody] TaoGioHangDto dto)
        {
            var gioHang = new GioHang
            {
                IDGioHang = Guid.NewGuid(),
                MaGioHang = $"GH{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                IDKhachHang = dto.IDKhachHang,
                NgayTao = DateTime.UtcNow,
                NguoiTao = dto.NguoiTao ?? "System",
                TrangThai = true
            };
            _context.GioHangs.Add(gioHang);
            await _context.SaveChangesAsync();
            return Ok(new { gioHang.IDGioHang, gioHang.MaGioHang });
        }

        // Thêm sản phẩm vào giỏ hàng (trừ tồn kho ngay lập tức)
        [HttpPost("them-vao-gio-hang")]
        public async Task<IActionResult> ThemVaoGioHang([FromBody] ThemVaoGioHangDto dto)
        {
            var gioHang = await _context.GioHangs.FindAsync(dto.IDGioHang);
            if (gioHang == null || !gioHang.TrangThai)
                return BadRequest(new { message = "Giỏ hàng không tồn tại hoặc đã bị vô hiệu hóa." });

            var spct = await _context.SanPhamChiTiets.FindAsync(dto.IDSanPhamChiTiet);
            if (spct == null || !spct.TrangThai)
                return BadRequest(new { message = "Sản phẩm không tồn tại hoặc đã bị vô hiệu hóa." });

            var soLuongKhaDung = spct.SoLuong - spct.SoLuongDatCho;
            if (soLuongKhaDung < dto.SoLuong)
                return BadRequest(new { message = $"Số lượng vượt quá tồn khả dụng. Hiện có: {soLuongKhaDung}" });

            // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
            var cthd = await _context.ChiTietGioHangs
                .FirstOrDefaultAsync(x => x.IDGioHang == dto.IDGioHang &&
                                         x.IDSanPhamChiTiet == dto.IDSanPhamChiTiet &&
                                         x.TrangThai);

            if (cthd != null)
            {
                var reserveResult = await _inventoryReservationService.ReserveAsync(
                    new[] { new InventoryLine(dto.IDSanPhamChiTiet, dto.SoLuong) },
                    dto.NguoiCapNhat ?? "System");
                if (!reserveResult.Success)
                    return BadRequest(new { message = reserveResult.ErrorMessage ?? "Không thể giữ chỗ tồn kho." });

                // Cập nhật số lượng và đồng bộ giá theo sản phẩm hiện tại
                var giaSauGiam = await TinhGiaSauGiam(spct.IDSanPhamChiTiet, spct.GiaBan);
                cthd.GiaBan = giaSauGiam;
                cthd.SoLuong += dto.SoLuong;
                cthd.SoLuongDatCho += dto.SoLuong;
                cthd.LanCapNhatCuoi = DateTime.UtcNow;
                cthd.NguoiCapNhat = dto.NguoiCapNhat ?? "System";
            }
            else
            {
                // Tính giá sau khi áp dụng đợt giảm giá
                var giaSauGiam = await TinhGiaSauGiam(spct.IDSanPhamChiTiet, spct.GiaBan);

                // Tạo chi tiết giỏ hàng mới
                cthd = new ChiTietGioHang
                {
                    IDChiTietGioHang = Guid.NewGuid(),
                    MaChiTietGioHang = $"CTGH{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    IDGioHang = dto.IDGioHang,
                    IDSanPhamChiTiet = dto.IDSanPhamChiTiet,
                    SoLuong = dto.SoLuong,
                    SoLuongDatCho = dto.SoLuong,
                    GiaBan = giaSauGiam,
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = dto.NguoiCapNhat ?? "System",
                    TrangThai = true
                };

                var reserveResult = await _inventoryReservationService.ReserveAsync(
                    new[] { new InventoryLine(dto.IDSanPhamChiTiet, dto.SoLuong) },
                    dto.NguoiCapNhat ?? "System");
                if (!reserveResult.Success)
                    return BadRequest(new { message = reserveResult.ErrorMessage ?? "Không thể giữ chỗ tồn kho." });

                _context.ChiTietGioHangs.Add(cthd);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm vào giỏ hàng thành công", soLuongConLai = spct.SoLuong - spct.SoLuongDatCho });
        }

        // Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost("cap-nhat-so-luong-gio-hang")]
        public async Task<IActionResult> CapNhatSoLuongGioHang([FromBody] CapNhatSoLuongGioHangDto dto)
        {
            var gioHang = await _context.GioHangs.FindAsync(dto.IDGioHang);
            if (gioHang == null || !gioHang.TrangThai)
                return BadRequest(new { message = "Giỏ hàng không tồn tại hoặc đã bị vô hiệu hóa." });

            var cthd = await _context.ChiTietGioHangs
                .FirstOrDefaultAsync(x => x.IDGioHang == dto.IDGioHang &&
                                         x.IDSanPhamChiTiet == dto.IDSanPhamChiTiet &&
                                         x.TrangThai);

            if (cthd == null)
                return BadRequest(new { message = "Sản phẩm không tồn tại trong giỏ hàng." });

            var spct = await _context.SanPhamChiTiets.FindAsync(dto.IDSanPhamChiTiet);
            if (spct == null || !spct.TrangThai)
                return BadRequest(new { message = "Sản phẩm không tồn tại hoặc đã bị vô hiệu hóa." });

            int soLuongCu = cthd.SoLuong;
            int soLuongMoi = dto.SoLuongMoi;
            int chenhLech = soLuongMoi - soLuongCu;

            if (soLuongMoi <= 0)
            {
                if (cthd.SoLuongDatCho > 0)
                {
                    var releaseResult = await _inventoryReservationService.ReleaseAsync(
                        new[] { new InventoryLine(dto.IDSanPhamChiTiet, cthd.SoLuongDatCho) },
                        dto.NguoiCapNhat ?? "System");
                    if (!releaseResult.Success)
                        return BadRequest(new { message = releaseResult.ErrorMessage ?? "Không thể nhả giữ chỗ tồn kho." });
                }
                _context.ChiTietGioHangs.Remove(cthd);
            }
            else
            {
                if (chenhLech > 0)
                {
                    var reserveResult = await _inventoryReservationService.ReserveAsync(
                        new[] { new InventoryLine(dto.IDSanPhamChiTiet, chenhLech) },
                        dto.NguoiCapNhat ?? "System");
                    if (!reserveResult.Success)
                        return BadRequest(new { message = reserveResult.ErrorMessage ?? "Không thể tăng giữ chỗ tồn kho." });
                }
                else if (chenhLech < 0)
                {
                    var releaseResult = await _inventoryReservationService.ReleaseAsync(
                        new[] { new InventoryLine(dto.IDSanPhamChiTiet, -chenhLech) },
                        dto.NguoiCapNhat ?? "System");
                    if (!releaseResult.Success)
                        return BadRequest(new { message = releaseResult.ErrorMessage ?? "Không thể giảm giữ chỗ tồn kho." });
                }

                cthd.SoLuong = soLuongMoi;
                cthd.SoLuongDatCho = soLuongMoi;
                cthd.LanCapNhatCuoi = DateTime.UtcNow;
                cthd.NguoiCapNhat = dto.NguoiCapNhat ?? "System";
            }

            spct.LanCapNhatCuoi = DateTime.UtcNow;
            spct.NguoiCapNhat = dto.NguoiCapNhat ?? "System";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật số lượng thành công", soLuongConLai = spct.SoLuong - spct.SoLuongDatCho });
        }

        // Xóa sản phẩm khỏi giỏ hàng (trả lại tồn kho)
        [HttpPost("xoa-khoi-gio-hang")]
        public async Task<IActionResult> XoaKhoiGioHang([FromBody] XoaKhoiGioHangDto dto)
        {
            var gioHang = await _context.GioHangs.FindAsync(dto.IDGioHang);
            if (gioHang == null || !gioHang.TrangThai)
                return BadRequest(new { message = "Giỏ hàng không tồn tại hoặc đã bị vô hiệu hóa." });

            var cthd = await _context.ChiTietGioHangs
                .FirstOrDefaultAsync(x => x.IDGioHang == dto.IDGioHang &&
                                         x.IDSanPhamChiTiet == dto.IDSanPhamChiTiet &&
                                         x.TrangThai);

            if (cthd == null)
                return BadRequest(new { message = "Sản phẩm không tồn tại trong giỏ hàng." });

            if (cthd.SoLuongDatCho > 0)
            {
                // Xóa khỏi giỏ phải luôn ưu tiên thành công; dữ liệu giữ chỗ có thể lệch khi tồn kho đã thay đổi.
                var spct = await _context.SanPhamChiTiets
                    .FirstOrDefaultAsync(x => x.IDSanPhamChiTiet == dto.IDSanPhamChiTiet);

                if (spct != null)
                {
                    spct.SoLuongDatCho = Math.Max(spct.SoLuongDatCho - cthd.SoLuongDatCho, 0);
                    spct.LanCapNhatCuoi = DateTime.UtcNow;
                    spct.NguoiCapNhat = dto.NguoiCapNhat ?? "System";
                }
            }

            // Xóa sản phẩm khỏi giỏ
            _context.ChiTietGioHangs.Remove(cthd);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa sản phẩm khỏi giỏ hàng thành công" });
        }

        // Lấy thông tin giỏ hàng
        [HttpGet("gio-hang/{idGioHang}")]
        public async Task<IActionResult> GetGioHang(Guid idGioHang)
        {
            var gioHang = await _context.GioHangs
                .Include(g => g.KhachHang)
                .Include(g => g.ChiTietGioHangs.Where(ct => ct.TrangThai))
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.SanPham)
                .Include(g => g.ChiTietGioHangs.Where(ct => ct.TrangThai))
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.KichCo)
                .Include(g => g.ChiTietGioHangs.Where(ct => ct.TrangThai))
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.MauSac)
                .Include(g => g.ChiTietGioHangs.Where(ct => ct.TrangThai))
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.HoaTiet) // Thêm include này để tránh lỗi
                .Include(g => g.ChiTietGioHangs.Where(ct => ct.TrangThai))
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.AnhSanPhams.Where(a => a.TrangThai))
                .FirstOrDefaultAsync(g => g.IDGioHang == idGioHang && g.TrangThai);

            if (gioHang == null)
                return NotFound("Không tìm thấy giỏ hàng.");

            // Cập nhật giá từ sản phẩm hiện tại (giá có thể đã đổi sau khi thêm vào giỏ)
            foreach (var ct in gioHang.ChiTietGioHangs)
            {
                var giaSauGiam = await TinhGiaSauGiam(ct.IDSanPhamChiTiet, ct.SanPhamChiTiet.GiaBan);
                ct.GiaBan = giaSauGiam;
                ct.LanCapNhatCuoi = DateTime.UtcNow;
                ct.NguoiCapNhat = "System";
            }
            await _context.SaveChangesAsync();

            var result = new
            {
                idGioHang = gioHang.IDGioHang,
                maGioHang = gioHang.MaGioHang,
                khachHang = gioHang.KhachHang != null ? new
                {
                    id = gioHang.KhachHang.IDKhachHang,
                    ten = gioHang.KhachHang.TenKhachHang,
                    sdt = gioHang.KhachHang.SoDienThoai,
                    email = gioHang.KhachHang.Email
                } : null,
                sanPhams = gioHang.ChiTietGioHangs.Select(ct => new
                {
                    id = ct.IDSanPhamChiTiet,
					ma = ct.SanPhamChiTiet.SanPham.MaSanPham,

					ten = ct.SanPhamChiTiet.SanPham.TenSanPham,
                    kichCo = ct.SanPhamChiTiet.KichCo.TenKichCo,
                    mauSac = ct.SanPhamChiTiet.MauSac.TenMauSac,
                    hoaTiet = ct.SanPhamChiTiet.HoaTiet != null ? ct.SanPhamChiTiet.HoaTiet.TenHoaTiet : null,
                    soLuong = ct.SoLuong,
                    giaBan = ct.GiaBan,
                    giaGoc = ct.SanPhamChiTiet.GiaBan,
                    thanhTien = ct.SoLuong * ct.GiaBan,
                    anh = ct.SanPhamChiTiet.AnhSanPhams
                        .OrderByDescending(a => a.LaAnhChinh)
                        .ThenBy(a => a.NgayTao)
                        .Select(a => a.UrlAnh)
                        .FirstOrDefault() ?? "/img/default-product.jpg"
                }).ToList(),
                tongTien = gioHang.ChiTietGioHangs.Sum(ct => ct.SoLuong * ct.GiaBan),
                ngayTao = gioHang.NgayTao
            };

            return Ok(result);
        }

        // Xóa giỏ hàng (trả lại tất cả tồn kho)
        [HttpPost("xoa-gio-hang")]
        public async Task<IActionResult> XoaGioHang([FromBody] XoaGioHangDto dto)
        {
            var gioHang = await _context.GioHangs
                .Include(g => g.ChiTietGioHangs.Where(ct => ct.TrangThai))
                .FirstOrDefaultAsync(g => g.IDGioHang == dto.IDGioHang && g.TrangThai);

            if (gioHang == null)
                return BadRequest(new { message = "Giỏ hàng không tồn tại hoặc đã bị vô hiệu hóa." });

            var lines = gioHang.ChiTietGioHangs
                .Where(x => x.SoLuongDatCho > 0)
                .Select(x => new InventoryLine(x.IDSanPhamChiTiet, x.SoLuongDatCho))
                .ToList();

            if (lines.Count > 0)
            {
                var releaseResult = await _inventoryReservationService.ReleaseAsync(lines, dto.NguoiCapNhat ?? "System");
                if (!releaseResult.Success)
                    return BadRequest(new { message = releaseResult.ErrorMessage ?? "Không thể nhả tồn giữ chỗ cho giỏ hàng." });
            }

            _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
            _context.GioHangs.Remove(gioHang);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa giỏ hàng thành công" });
        }

        // Chuyển giỏ hàng thành hóa đơn
        [HttpPost("chuyen-gio-hang-thanh-hoa-don")]
        public async Task<IActionResult> ChuyenGioHangThanhHoaDon([FromBody] ChuyenGioHangThanhHoaDonDto dto)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            var gioHang = await _context.GioHangs
                .Include(g => g.ChiTietGioHangs.Where(ct => ct.TrangThai))
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                .FirstOrDefaultAsync(g => g.IDGioHang == dto.IDGioHang && g.TrangThai);

            if (gioHang == null)
                return BadRequest(new { message = "Giỏ hàng không tồn tại hoặc đã bị vô hiệu hóa." });

            if (!gioHang.ChiTietGioHangs.Any())
                return BadRequest(new { message = "Giỏ hàng không có sản phẩm nào." });
			// ===== VALIDATE THÔNG TIN KHÁCH =====
			var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀ-ỹ\s]+$");

			// ✅ SĐT chuẩn VN + không cho toàn số 0
			var phoneRegex = new System.Text.RegularExpressions.Regex(
		@"^(0|\+84)(3|5|7|8|9)[0-9]{8}$"
	);
			// ✅ Email chuẩn hơn (chặt hơn chút)
			var emailRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");

			string name = (dto.CustomerName ?? "").Trim();
			string phone = (dto.CustomerPhone ?? "").Trim();
			string email = (dto.CustomerEmail ?? "").Trim();

            if (dto.Shipping)
            {
			    // Đơn giao hàng: bắt buộc tên + SĐT + địa chỉ
			    if (string.IsNullOrEmpty(name) || !nameRegex.IsMatch(name))
			    {
				    return BadRequest(new { message = "Tên không hợp lệ (không chứa số)." });
			    }

			    if (string.IsNullOrEmpty(phone) || !phoneRegex.IsMatch(phone))
			    {
				    return BadRequest(new { message = "Số điện thoại không hợp lệ." });
			    }

			    if (string.IsNullOrWhiteSpace(dto.Address))
			    {
				    return BadRequest(new { message = "Vui lòng nhập địa chỉ giao hàng." });
			    }
            }
            else
            {
                // Đơn tại quầy: cho phép khách vãng lai không cần tài khoản/SĐT.
                if (string.IsNullOrEmpty(name))
                {
                    name = "Khách vãng lai";
                }
                else if (!nameRegex.IsMatch(name))
                {
                    return BadRequest(new { message = "Tên không hợp lệ (không chứa số)." });
                }

                if (!string.IsNullOrEmpty(phone) && !phoneRegex.IsMatch(phone))
                {
                    return BadRequest(new { message = "Số điện thoại không hợp lệ." });
                }
            }

			// Email không bắt buộc, nhưng nếu nhập thì phải đúng định dạng
			if (!string.IsNullOrEmpty(email) && !emailRegex.IsMatch(email))
			{
				return BadRequest(new { message = "Email không đúng định dạng." });
			}

            dto.CustomerName = name;
            dto.CustomerPhone = string.IsNullOrEmpty(phone) ? null : phone;
            dto.CustomerEmail = string.IsNullOrEmpty(email) ? null : email;
			// Map code sang ID phương thức thanh toán
			Guid paymentMethodId = Guid.Empty;
            if (!string.IsNullOrEmpty(dto.PaymentMethod))
            {
                var method = await _context.PhuongThucThanhToans
                    .FirstOrDefaultAsync(x => x.MaPhuongThuc == dto.PaymentMethod && x.TrangThai);
                if (method == null)
                    return BadRequest(new { message = "Phương thức thanh toán không hợp lệ." });
                paymentMethodId = method.IDPhuongThucThanhToan;
            }
            else
            {
                return BadRequest(new { message = "Chưa chọn phương thức thanh toán." });
            }

            string trangThaiHoaDon;
            var cashPaymentMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "cash", "Tiền mặt", "tiền mặt"
                };
            if (dto.Shipping && !string.IsNullOrWhiteSpace(dto.Address) && cashPaymentMethods.Contains(dto.PaymentMethod))
            {
                trangThaiHoaDon = "Đã xác nhận";
            }
            else
            {
                trangThaiHoaDon = "DaThanhToan";
            }

            // Khách có tài khoản: ưu tiên CustomerId từ POS (chọn khách sau khi tạo giỏ thì GioHang.IDKhachHang thường vẫn null)
            var registeredCustomerId = dto.CustomerId ?? gioHang.IDKhachHang;
            Guid? customerIdForInvoice = registeredCustomerId;
            if (!customerIdForInvoice.HasValue && dto.Shipping && !string.IsNullOrWhiteSpace(dto.CustomerName) && !string.IsNullOrWhiteSpace(dto.CustomerPhone))
            {
                var guest = await TaoKhachHangVangLaiAsync(dto.CustomerName, dto.CustomerPhone, dto.CustomerEmail, dto.Address);
                customerIdForInvoice = guest.IDKhachHang;
            }

            // Tạo hóa đơn
            var hoaDon = new HoaDon
            {
                IDHoaDon = Guid.NewGuid(),
                MaHoaDon = $"HD{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                IDKhachHang = customerIdForInvoice,
                TenNguoiNhan = dto.CustomerName,
                SoDienThoaiNguoiNhan = dto.CustomerPhone,
                DiaChiGiaoHang = dto.Address,
                TongTien = 0,
                TrangThai = trangThaiHoaDon,
                BanTaiQuay = true,
                NgayTao = DateTime.UtcNow,
                TrangThaiHoaDon = true,
                IDPhuongThucThanhToan = paymentMethodId
            };
            _context.HoaDons.Add(hoaDon);
            var committedLines = new List<InventoryLine>();

            // Chuyển chi tiết giỏ hàng thành chi tiết hóa đơn (dùng giá hiện tại, chỉ với SP đang bán)
            foreach (var cthd in gioHang.ChiTietGioHangs)
            {
                var spct = cthd.SanPhamChiTiet;

                var giaSauGiam = await TinhGiaSauGiam(cthd.IDSanPhamChiTiet, spct.GiaBan);

                var cthdHoaDon = new ChiTietHoaDon
                {
                    IDChiTietHoaDon = Guid.NewGuid(),
                    MaChiTietHoaDon = $"CTHD{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    IDHoaDon = hoaDon.IDHoaDon,
                    IDSanPhamChiTiet = cthd.IDSanPhamChiTiet,
                    SoLuong = cthd.SoLuong,
                    DonGia = giaSauGiam,
                    ThanhTien = cthd.SoLuong * giaSauGiam,
                    NgayTao = DateTime.UtcNow,
                    TrangThai = true
                };

                _context.ChiTietHoaDons.Add(cthdHoaDon);

                hoaDon.TongTien += cthdHoaDon.ThanhTien;
                committedLines.Add(new InventoryLine(cthd.IDSanPhamChiTiet, cthd.SoLuongDatCho > 0 ? cthd.SoLuongDatCho : cthd.SoLuong));
            }

            // Áp dụng mã giảm giá nếu có
            if (!string.IsNullOrEmpty(dto.DiscountCode))
            {
                var discount = await _context.PhieuGiamGias.FirstOrDefaultAsync(x => x.MaCode == dto.DiscountCode);
                if (discount != null)
                {
                    var now = DateTime.UtcNow;
                    if (!discount.TrangThai || discount.NgayBatDau > now || discount.NgayKetThuc < now)
                    {
                        return BadRequest(new { message = "Mã giảm giá không còn hiệu lực." });
                    }

                    if (discount.DonToiThieu.HasValue && hoaDon.TongTien < discount.DonToiThieu.Value)
                    {
                        return BadRequest(new { message = $"Đơn hàng chưa đạt giá trị tối thiểu {discount.DonToiThieu.Value:n0}." });
                    }

                    // Kiểm tra xem khách hàng có phiếu này không và còn số lượng không (dùng registeredCustomerId: khớp khách đang chọn trên POS)
                    if (registeredCustomerId.HasValue)
                    {
                        var customerVoucher = await _context.KhachHangPhieuGiams
                            .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == discount.IDPhieuGiamGia &&
                                                     x.IDKhachHang == registeredCustomerId.Value);

                        if (customerVoucher == null)
                        {
                            customerVoucher = new KhachHangPhieuGiam
                            {
                                IDKhachHangPhieuGiam = Guid.NewGuid(),
                                MaKhachHangPhieuGiam = $"KHPG_{DateTime.UtcNow:yyyyMMddHHmmss}_{registeredCustomerId.Value.ToString().Substring(0, 8)}",
                                IDKhachHang = registeredCustomerId.Value,
                                IDPhieuGiamGia = discount.IDPhieuGiamGia,
                                SoLuong = discount.SoLuong > 0 ? discount.SoLuong : (short)1,
                                SoLuongDaSuDung = 0,
                                NgayTao = DateTime.UtcNow,
                                NguoiTao = "System",
                                TrangThai = true
                            };

                            _context.KhachHangPhieuGiams.Add(customerVoucher);
                        }

                        if (!customerVoucher.TrangThai || customerVoucher.SoLuongDaSuDung >= customerVoucher.SoLuong)
                        {
                            return BadRequest(new { message = "Phiếu giảm giá không hợp lệ hoặc đã được sử dụng hết." });
                        }

                        // Tăng số lượng đã sử dụng
                        customerVoucher.SoLuongDaSuDung++;
                        customerVoucher.LanCapNhatCuoi = DateTime.UtcNow;
                        customerVoucher.NguoiCapNhat = "System";

                        // Nếu đã sử dụng hết, vô hiệu hóa
                        if (customerVoucher.SoLuongDaSuDung >= customerVoucher.SoLuong)
                        {
                            customerVoucher.TrangThai = false;
                        }
                    }

                    // Tính số tiền giảm theo phần trăm
                    var tienGiam = hoaDon.TongTien * (discount.GiaTriGiam / 100m);
                    // Nếu có giá trị giảm tối đa, lấy min
                    if (discount.GiaTriGiamToiDa.HasValue)
                        tienGiam = Math.Min(tienGiam, discount.GiaTriGiamToiDa.Value);

                    hoaDon.TienGiam = tienGiam;
                    hoaDon.IDPhieuGiamGia = discount.IDPhieuGiamGia;
                    hoaDon.TongTien -= tienGiam;
                }
                else
                {
                    return BadRequest(new { message = "Mã giảm giá không hợp lệ." });
                }
            }

            // Loyalty: tính điểm / giảm điểm theo khách trên hóa đơn (đã gồm CustomerId từ POS)
            var loyaltyResult = await _loyaltyService.BuildCheckoutResultAsync(
                customerIdForInvoice,
                hoaDon.TongTien,
                dto.UsePoint,
                dto.RequestedUsedPoints);
            hoaDon.DiemDaDung = loyaltyResult.UsedPoints;
            hoaDon.SoTienGiamTuDiem = loyaltyResult.DiscountFromPoints;
            hoaDon.TyLeQuyDoiDiem = loyaltyResult.PointConversionRate;
            if (loyaltyResult.DiscountFromPoints > 0)
            {
                hoaDon.TongTien = Math.Max(hoaDon.TongTien - loyaltyResult.DiscountFromPoints, 0);
            }

            hoaDon.DiemCong = loyaltyResult.EarnedPoints;

            // Thêm phí vận chuyển sau giảm theo hạng
            if (dto.Shipping)
            {
                var shippingCalc = await TinhPhiVanChuyenChoThanhToanAsync(
                    dto.Province,
                    dto.District,
                    dto.ToDistrictId,
                    dto.ToWardCode,
                    dto.Weight,
                    hoaDon.TongTien,
                    customerIdForInvoice,
                    dto.ShippingFee,
                    dto.ShippingFeeSource);

                hoaDon.PhiVanChuyenGoc = shippingCalc.OriginalFee;
                hoaDon.PhiVanChuyen = shippingCalc.FinalFee;
                hoaDon.SoTienGiamPhiVanChuyen = shippingCalc.DiscountAmount;
                hoaDon.TongTien += shippingCalc.FinalFee;
            }

            if (customerIdForInvoice.HasValue)
            {
                await _loyaltyService.ApplyCheckoutPointChangesAsync(customerIdForInvoice.Value, hoaDon.IDHoaDon, loyaltyResult, "POS");
            }

            var hasFullReservation = gioHang.ChiTietGioHangs.All(x => x.SoLuongDatCho >= x.SoLuong);
            var commitResult = hasFullReservation
                ? await _inventoryReservationService.CommitReservedAsync(committedLines, "POS")
                : await _inventoryReservationService.CommitDirectAsync(
                    gioHang.ChiTietGioHangs.Select(x => new InventoryLine(x.IDSanPhamChiTiet, x.SoLuong)),
                    "POS");
            if (!commitResult.Success)
            {
                return BadRequest(new { message = commitResult.ErrorMessage ?? "Không thể hoàn tất thanh toán từ phần tồn đã giữ chỗ." });
            }

            // Xóa giỏ hàng và chi tiết giỏ hàng
            _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
            _context.GioHangs.Remove(gioHang);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { hoaDon.IDHoaDon, hoaDon.MaHoaDon, message = "Chuyển giỏ hàng thành hóa đơn thành công" });
        }

        private class ShippingCheckoutResult
        {
            public decimal OriginalFee { get; set; }
            public decimal FinalFee { get; set; }
            public decimal DiscountAmount { get; set; }
        }

        private async Task<ShippingCheckoutResult> TinhPhiVanChuyenChoThanhToanAsync(
            string? province,
            string? district,
            int? toDistrictId,
            string? toWardCode,
            int? weight,
            decimal orderValue,
            Guid? customerId,
            decimal? clientShippingFee,
            string? shippingFeeSource)
        {
            var shippingConfig = await _shippingPolicyService.GetActiveShippingConfigAsync();
            var resolvedSource = _shippingPolicyService.ResolveShippingFeeSourceFromConfig(shippingConfig);
            decimal originalFee;
            var ghnRequested = resolvedSource == ShippingFeeSources.Ghn;
            var hasCompleteGhnAddress = toDistrictId.HasValue
                && toDistrictId.Value > 0
                && !string.IsNullOrWhiteSpace(toWardCode);

            async Task<decimal> ResolveConfigFeeAsync()
            {
                if (!string.IsNullOrWhiteSpace(province))
                {
                    return _shippingService.CalculateShippingFee(
                        province,
                        district ?? string.Empty,
                        orderValue,
                        0,
                        shippingConfig);
                }

                return await _shippingPolicyService.ResolveDefaultShippingFeeAsync();
            }

            if (ghnRequested)
            {
                if (hasCompleteGhnAddress && await _ghnService.IsConfiguredAsync())
                {
                    var ghnFee = await _ghnService.GetFeeAsync(toDistrictId.Value, toWardCode.Trim(), weight ?? 500);
                    originalFee = ghnFee?.Total ?? await ResolveConfigFeeAsync();
                }
                else
                {
                    originalFee = await ResolveConfigFeeAsync();
                }
            }
            else
            {
                originalFee = await ResolveConfigFeeAsync();
            }

            originalFee = Math.Max(originalFee, 0);
            var policy = await _shippingPolicyService.ResolveCustomerDiscountAsync(customerId);
            var finalFee = _shippingService.ApplyShippingDiscount(originalFee, policy.Percent);

            return new ShippingCheckoutResult
            {
                OriginalFee = originalFee,
                FinalFee = finalFee,
                DiscountAmount = Math.Max(originalFee - finalFee, 0)
            };
        }

        /// <summary>
        /// Tạo khách hàng vãng lai khi chọn giao hàng: lưu tên, SĐT, email, địa chỉ vào DB.
        /// </summary>
        private async Task<KhachHang> TaoKhachHangVangLaiAsync(string ten, string soDienThoai, string? email, string? diaChiGiaoHang)
        {
            var maKhachHang = "GUEST-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var khachHang = new KhachHang
            {
                IDKhachHang = Guid.NewGuid(),
                MaKhachHang = maKhachHang,
                TenKhachHang = ten?.Trim() ?? "Khách vãng lai",
                SoDienThoai = soDienThoai?.Trim() ?? "",
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                MatKhau = null,
                NgayTao = DateTime.UtcNow,
                NguoiTao = "POS",
                TrangThai = true
            };
            _context.KhachHang.Add(khachHang);

            if (!string.IsNullOrWhiteSpace(diaChiGiaoHang))
            {
                var diaChi = new DiaChi
                {
                    IDDiaChi = Guid.NewGuid(),
                    MaDiaChi = "DC-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
                    IDKhachHang = khachHang.IDKhachHang,
                    DiaChiChiTiet = diaChiGiaoHang.Trim(),
                    LaMacDinh = true,
                    TenNguoiNhan = khachHang.TenKhachHang,
                    SdtNguoiNhan = khachHang.SoDienThoai,
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = "POS",
                    TrangThai = true
                };
                _context.DiaChis.Add(diaChi);
            }

            await _context.SaveChangesAsync();
            return khachHang;
        }

        private static (Guid? sanPhamChiTietId, string? maSanPhamChiTiet) ParseQrCode(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
                return (null, null);

            var normalized = qrCode.Trim();
            if (Guid.TryParse(normalized, out var directId))
                return (directId, null);

            var segments = normalized
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (segments.Length >= 3 && segments[0].Equals("SPCT", StringComparison.OrdinalIgnoreCase))
            {
                Guid? parsedId = Guid.TryParse(segments[^1], out var qrId) ? qrId : null;
                var parsedCode = string.IsNullOrWhiteSpace(segments[1]) ? null : segments[1];
                return (parsedId, parsedCode);
            }

            return (null, normalized);
        }
    }
}
