using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Dtos;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;
using System.Collections.Generic; // Added for List

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BanHangTaiQuayController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;
        public BanHangTaiQuayController(BanQuanAu1DbContext context)
        {
            _context = context;
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
            await _context.SaveChangesAsync();

            // Thêm sản phẩm nếu có
            if (dto.SanPhams != null && dto.SanPhams.Count > 0)
            {
                foreach (var sp in dto.SanPhams)
                {
                    var spct = await _context.SanPhamChiTiets.FindAsync(sp.IDSanPhamChiTiet);
                    if (spct == null)
                        return BadRequest(new { message = $"Không tìm thấy sản phẩm chi tiết: {sp.IDSanPhamChiTiet}" });

                    // Kiểm tra số lượng tồn kho
                    if (spct.SoLuong <= 0)
                        return BadRequest(new { message = $"Sản phẩm {spct.MaSPChiTiet} đã hết hàng." });
                    if (sp.SoLuong <= 0)
                        return BadRequest(new { message = $"Số lượng phải lớn hơn 0 cho sản phẩm: {spct.MaSPChiTiet}" });
                    if (sp.SoLuong > spct.SoLuong)
                        return BadRequest(new { message = $"Số lượng vượt quá tồn kho cho sản phẩm {spct.MaSPChiTiet}. Hiện có: {spct.SoLuong}" });

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

            // Kiểm tra số lượng tồn kho
            if (spct.SoLuong <= 0)
                return BadRequest(new { message = "Sản phẩm đã hết hàng." });
            if (dto.SoLuong <= 0)
                return BadRequest(new { message = "Số lượng phải lớn hơn 0." });
            if (dto.SoLuong > spct.SoLuong)
                return BadRequest(new { message = $"Số lượng vượt quá tồn kho. Hiện có: {spct.SoLuong}" });

            // Tính giá sau khi áp dụng đợt giảm giá
            var giaSauGiam = await TinhGiaSauGiam(spct.IDSanPhamChiTiet, spct.GiaBan);

            var cthd = await _context.ChiTietHoaDons.FirstOrDefaultAsync(x => x.IDHoaDon == dto.IDHoaDon && x.IDSanPhamChiTiet == dto.IDSanPhamChiTiet);
            if (cthd != null)
            {
                // Kiểm tra tổng số lượng sau khi thêm có vượt quá tồn kho không
                if (cthd.SoLuong + dto.SoLuong > spct.SoLuong)
                    return BadRequest(new { message = $"Tổng số lượng vượt quá tồn kho. Hiện có: {spct.SoLuong}, Đã chọn: {cthd.SoLuong}" });

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

                // Kiểm tra số lượng tồn kho
                if (spct.SoLuong <= 0)
                    return BadRequest(new { message = "Sản phẩm đã hết hàng." });
                if (dto.SoLuongMoi > spct.SoLuong)
                    return BadRequest(new { message = $"Số lượng vượt quá tồn kho. Hiện có: {spct.SoLuong}" });

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
                .Where(x => x.TrangThai)
                .Select(x => new
                {
                    id = x.IDSanPhamChiTiet,
                    name = x.SanPham.TenSanPham + $" [{x.KichCo.TenKichCo} - {x.MauSac.TenMauSac}" + (x.HoaTiet != null ? $" - {x.HoaTiet.TenHoaTiet}" : "") + "]",
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
                    stock = x.SoLuong,
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

        [HttpGet("danh-sach-khach-hang")]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _context.KhachHang
                .Where(x => x.TrangThai)
                .Select(x => new
                {
                    id = x.IDKhachHang,
                    name = x.TenKhachHang,
                    email = x.Email,
                    phone = x.SoDienThoai,
                    point = 0, // Nếu có trường điểm thì thay thế
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
                    point = 0, // Nếu có trường điểm thì thay thế
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
            public bool Shipping { get; set; }
            public string? PaymentMethod { get; set; } // Mã phương thức thanh toán ("cash", "bank", ...)
            public decimal? CustomerPaid { get; set; }
            public decimal? ShippingFee { get; set; } // Phí vận chuyển
        }

        [HttpPost("thanh-toan")]
        public async Task<IActionResult> PayInvoice([FromBody] InvoiceDto dto)
        {
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
                BanTaiQuay = true,
                NgayTao = DateTime.UtcNow,
                TrangThaiHoaDon = true,
                IDPhuongThucThanhToan = paymentMethodId
            };
            _context.HoaDons.Add(hoaDon);

            // 2. Thêm chi tiết hóa đơn và kiểm tra tồn kho
            foreach (var p in dto.Products)
            {
                var spct = await _context.SanPhamChiTiets.FindAsync(p.ProductDetailId);
                if (spct == null)
                    return BadRequest(new { message = "Sản phẩm không tồn tại" });
                if (!spct.TrangThai)
                    return BadRequest(new { message = $"Sản phẩm {spct.MaSPChiTiet ?? spct.IDSanPhamChiTiet.ToString()} đã ngưng bán, không thể thanh toán." });

                // Kiểm tra số lượng tồn kho
                if (spct.SoLuong <= 0)
                    return BadRequest(new { message = $"Sản phẩm {spct.IDSanPhamChiTiet} đã hết hàng" });
                if (p.Quantity <= 0)
                    return BadRequest(new { message = $"Số lượng phải lớn hơn 0 cho sản phẩm {spct.IDSanPhamChiTiet}" });
                if (p.Quantity > spct.SoLuong)
                    return BadRequest(new { message = $"Sản phẩm {spct.IDSanPhamChiTiet} vượt quá tồn kho ({spct.SoLuong})" });
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

                // Trừ tồn kho
                spct.SoLuong -= p.Quantity;
            }

            // 3. Áp dụng mã giảm giá nếu có
            if (!string.IsNullOrEmpty(dto.DiscountCode))
            {
                var discount = await _context.PhieuGiamGias.FirstOrDefaultAsync(x => x.MaCode == dto.DiscountCode && x.TrangThai);
                if (discount != null)
                {
                    // Kiểm tra xem khách hàng có phiếu này không và còn số lượng không
                    if (dto.CustomerId.HasValue)
                    {
                        var customerVoucher = await _context.KhachHangPhieuGiams
                            .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == discount.IDPhieuGiamGia &&
                                                     x.IDKhachHang == dto.CustomerId.Value &&
                                                     x.TrangThai &&
                                                     x.SoLuongDaSuDung < x.SoLuong);

                        if (customerVoucher != null)
                        {
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
                        else
                            return BadRequest(new { message = "Phiếu giảm giá không hợp lệ hoặc đã được sử dụng hết." });
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

            // 4. Thêm phí vận chuyển nếu có
            if (dto.Shipping && dto.ShippingFee.HasValue && dto.ShippingFee.Value > 0)
            {
                hoaDon.PhiVanChuyen = dto.ShippingFee.Value;
                hoaDon.TongTien += dto.ShippingFee.Value;
            }

            await _context.SaveChangesAsync();
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
		public async Task<IActionResult> GetCustomerDiscountVouchers(Guid customerId, decimal tongTien)
		{
			var now = DateTime.UtcNow;

			var raw = await _context.KhachHangPhieuGiams
				.Include(k => k.PhieuGiamGia)
				.Where(x => x.IDKhachHang == customerId &&
							x.TrangThai &&
							x.PhieuGiamGia.TrangThai &&
							x.SoLuongDaSuDung < x.SoLuong &&
							x.PhieuGiamGia.NgayBatDau <= now &&
							x.PhieuGiamGia.NgayKetThuc >= now)
				.Select(x => new
				{
					id = x.IDPhieuGiamGia,
					maCode = x.PhieuGiamGia.MaCode,
					tenPhieu = x.PhieuGiamGia.TenPhieu,
					giaTriGiam = x.PhieuGiamGia.GiaTriGiam,
					giaTriGiamToiDa = x.PhieuGiamGia.GiaTriGiamToiDa,
					donToiThieu = x.PhieuGiamGia.DonToiThieu,
					ngayBatDau = x.PhieuGiamGia.NgayBatDau,
					ngayKetThuc = x.PhieuGiamGia.NgayKetThuc,
					soLuong = x.SoLuong,
					soLuongDaSuDung = x.SoLuongDaSuDung,
					soLuongConLai = x.SoLuong - x.SoLuongDaSuDung
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
                    stock = x.SoLuong,
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

                // Nếu đây là địa chỉ mặc định, bỏ mặc định các địa chỉ khác
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

                // Tạo địa chỉ mới
                var diaChi = new DiaChi
                {
                    IDDiaChi = Guid.NewGuid(),
                    MaDiaChi = $"DC{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    IDKhachHang = dto.IDKhachHang,
                    DiaChiChiTiet = dto.DiaChiChiTiet,
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

            // Kiểm tra tồn kho
            if (spct.SoLuong < dto.SoLuong)
                return BadRequest(new { message = $"Số lượng vượt quá tồn kho. Hiện có: {spct.SoLuong}" });

            // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
            var cthd = await _context.ChiTietGioHangs
                .FirstOrDefaultAsync(x => x.IDGioHang == dto.IDGioHang &&
                                         x.IDSanPhamChiTiet == dto.IDSanPhamChiTiet &&
                                         x.TrangThai);

            if (cthd != null)
            {
                // Kiểm tra tổng số lượng sau khi thêm
                if (spct.SoLuong < cthd.SoLuong + dto.SoLuong)
                    return BadRequest(new { message = $"Tổng số lượng vượt quá tồn kho. Hiện có: {spct.SoLuong}, Đã chọn: {cthd.SoLuong}" });

                // Cập nhật số lượng và đồng bộ giá theo sản phẩm hiện tại
                var giaSauGiam = await TinhGiaSauGiam(spct.IDSanPhamChiTiet, spct.GiaBan);
                cthd.GiaBan = giaSauGiam;
                cthd.SoLuong += dto.SoLuong;
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
                    GiaBan = giaSauGiam,
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = dto.NguoiCapNhat ?? "System",
                    TrangThai = true
                };
                _context.ChiTietGioHangs.Add(cthd);
            }

            //// Trừ tồn kho ngay lập tức
            //spct.SoLuong -= dto.SoLuong;
            //spct.LanCapNhatCuoi = DateTime.UtcNow;
            //spct.NguoiCapNhat = dto.NguoiCapNhat ?? "System";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm vào giỏ hàng thành công", soLuongConLai = spct.SoLuong });
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

            if (soLuongMoi <= 0)
            {
                _context.ChiTietGioHangs.Remove(cthd);
            }
            else
            {
                if (soLuongMoi > spct.SoLuong)
                    return BadRequest(new { message = $"Số lượng vượt quá tồn kho. Hiện có: {spct.SoLuong}" });

                cthd.SoLuong = soLuongMoi;
                cthd.LanCapNhatCuoi = DateTime.UtcNow;
                cthd.NguoiCapNhat = dto.NguoiCapNhat ?? "System";
            }

            spct.LanCapNhatCuoi = DateTime.UtcNow;
            spct.NguoiCapNhat = dto.NguoiCapNhat ?? "System";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật số lượng thành công", soLuongConLai = spct.SoLuong });
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

            // Trả lại tồn kho cho tất cả sản phẩm
            _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
            _context.GioHangs.Remove(gioHang);
            await _context.SaveChangesAsync();

            // Xóa tất cả chi tiết giỏ hàng
            _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);

            // Xóa giỏ hàng
            _context.GioHangs.Remove(gioHang);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa giỏ hàng thành công" });
        }

        // Chuyển giỏ hàng thành hóa đơn
        [HttpPost("chuyen-gio-hang-thanh-hoa-don")]
        public async Task<IActionResult> ChuyenGioHangThanhHoaDon([FromBody] ChuyenGioHangThanhHoaDonDto dto)
        {
            var gioHang = await _context.GioHangs
                .Include(g => g.ChiTietGioHangs.Where(ct => ct.TrangThai))
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                .FirstOrDefaultAsync(g => g.IDGioHang == dto.IDGioHang && g.TrangThai);

            if (gioHang == null)
                return BadRequest(new { message = "Giỏ hàng không tồn tại hoặc đã bị vô hiệu hóa." });

            if (!gioHang.ChiTietGioHangs.Any())
                return BadRequest(new { message = "Giỏ hàng không có sản phẩm nào." });

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

            // Khách vãng lai (giỏ hàng không có IDKhachHang) chọn giao hàng: tạo KhachHang + địa chỉ trong DB
            Guid? customerIdForInvoice = gioHang.IDKhachHang;
            if (!gioHang.IDKhachHang.HasValue && dto.Shipping && !string.IsNullOrWhiteSpace(dto.CustomerName) && !string.IsNullOrWhiteSpace(dto.CustomerPhone))
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

            // Chuyển chi tiết giỏ hàng thành chi tiết hóa đơn (dùng giá hiện tại, chỉ với SP đang bán)
            foreach (var cthd in gioHang.ChiTietGioHangs)
            {
                var spct = cthd.SanPhamChiTiet;

                if (spct.SoLuong < cthd.SoLuong)
                    return BadRequest(new { message = $"Sản phẩm {spct.MaSPChiTiet} không đủ tồn kho" });

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

                // ⭐ TRỪ TỒN KHO
                spct.SoLuong -= cthd.SoLuong;
            }

            // Áp dụng mã giảm giá nếu có
            if (!string.IsNullOrEmpty(dto.DiscountCode))
            {
                var discount = await _context.PhieuGiamGias.FirstOrDefaultAsync(x => x.MaCode == dto.DiscountCode && x.TrangThai);
                if (discount != null)
                {
                    // Kiểm tra xem khách hàng có phiếu này không và còn số lượng không
                    if (gioHang.IDKhachHang.HasValue)
                    {
                        var customerVoucher = await _context.KhachHangPhieuGiams
                            .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == discount.IDPhieuGiamGia &&
                                                     x.IDKhachHang == gioHang.IDKhachHang.Value &&
                                                     x.TrangThai &&
                                                     x.SoLuongDaSuDung < x.SoLuong);

                        if (customerVoucher != null)
                        {
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
                        else
                        {
                            return BadRequest(new { message = "Phiếu giảm giá không hợp lệ hoặc đã được sử dụng hết." });
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

            // Thêm phí vận chuyển nếu có
            if (dto.Shipping && dto.ShippingFee.HasValue && dto.ShippingFee.Value > 0)
            {
                hoaDon.PhiVanChuyen = dto.ShippingFee.Value;
                hoaDon.TongTien += dto.ShippingFee.Value;
            }

            // Xóa giỏ hàng và chi tiết giỏ hàng
            _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
            _context.GioHangs.Remove(gioHang);

            await _context.SaveChangesAsync();
            return Ok(new { hoaDon.IDHoaDon, hoaDon.MaHoaDon, message = "Chuyển giỏ hàng thành hóa đơn thành công" });
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
    }
}
