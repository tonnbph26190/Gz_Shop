using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;
using QuanApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace QuanApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class HoaDonsController : ControllerBase
	{
		private readonly BanQuanAu1DbContext _context;
		private readonly ILogger<HoaDonsController> _logger;
		private readonly IEmailService _emailService;
		private readonly IOrderHistoryService _orderHistoryService;
		private readonly ILoyaltyService _loyaltyService;
		private readonly IShippingPolicyService _shippingPolicyService;
		private readonly IInventoryReservationService _inventoryReservationService;

		public HoaDonsController(BanQuanAu1DbContext context, ILogger<HoaDonsController> logger,
			IEmailService emailService, IOrderHistoryService orderHistoryService,
			ILoyaltyService loyaltyService, IShippingPolicyService shippingPolicyService,
			IInventoryReservationService inventoryReservationService)
		{
			_context = context;
			_logger = logger;
			_emailService = emailService;
			_orderHistoryService = orderHistoryService;
			_loyaltyService = loyaltyService;
			_shippingPolicyService = shippingPolicyService;
			_inventoryReservationService = inventoryReservationService;
		}

		// GET: api/HoaDons - Cho admin (hiển thị tất cả đơn hàng)
		[HttpGet]
		public async Task<ActionResult<IEnumerable<object>>> GetHoaDons(
			int page = 1,
			int pageSize = 10,
			string? trangThai = null,
			string? tuNgay = null,
			string? denNgay = null,
			string? loaiDonHang = null,
			string? khachHang = null,
			string? maDonHang = null)
		{
			try
			{
				var query = _context.HoaDons
					.Include(h => h.KhachHang)
					.Include(h => h.NhanVien)
					.Include(h => h.PhieuGiamGia)
					.Include(h => h.PhuongThucThanhToan)
					.Include(h => h.ChiTietHoaDons) // Thêm include này để tránh lỗi
					.AsQueryable();

				// Áp dụng các bộ lọc
				if (!string.IsNullOrEmpty(trangThai))
				{
					if (string.Equals(trangThai, "Chờ xác nhận", StringComparison.OrdinalIgnoreCase))
					{
						query = query.Where(h =>
							h.TrangThai == "Chờ xác nhận" ||
							(
								(h.TrangThai == "DaThanhToan" || h.TrangThai == "Đã thanh toán")
								&& (h.TrangThaiThanhToan == "Đã thanh toán"
									|| h.TrangThai == "DaThanhToan"
									|| h.TrangThai == "Đã thanh toán")
								&& h.PhuongThucThanhToan != null
								&& (
									(h.PhuongThucThanhToan.MaPhuongThuc != null && (
										h.PhuongThucThanhToan.MaPhuongThuc.ToLower().Contains("bank")
										|| h.PhuongThucThanhToan.MaPhuongThuc.ToLower().Contains("transfer")
										|| h.PhuongThucThanhToan.MaPhuongThuc.ToLower().Contains("vnpay")
										|| h.PhuongThucThanhToan.MaPhuongThuc.ToLower().Contains("qr")
									))
									|| (h.PhuongThucThanhToan.TenPhuongThuc != null && (
										h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("chuyển khoản")
										|| h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("chuyen khoan")
										|| h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("bank")
										|| h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("transfer")
										|| h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("vnpay")
										|| h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("qr")
									))
								)
							));
					}
					else
					{
						query = query.Where(h => h.TrangThai == trangThai);
					}
				}

				if (!string.IsNullOrEmpty(tuNgay) && DateTime.TryParse(tuNgay, out var tuNgayDate))
				{
					// So sánh theo khoảng UTC để tránh lỗi Date/Kind khi map timestamp với PostgreSQL.
					var startDateUtc = DateTime.SpecifyKind(tuNgayDate.Date, DateTimeKind.Utc);
					query = query.Where(h => h.NgayTao >= startDateUtc);
				}

				if (!string.IsNullOrEmpty(denNgay) && DateTime.TryParse(denNgay, out var denNgayDate))
				{
					// Lấy hết dữ liệu trong ngày denNgay (inclusive) bằng upper-bound độc quyền.
					var endExclusiveUtc = DateTime.SpecifyKind(denNgayDate.Date.AddDays(1), DateTimeKind.Utc);
					query = query.Where(h => h.NgayTao < endExclusiveUtc);
				}

				if (!string.IsNullOrEmpty(loaiDonHang))
				{
					if (loaiDonHang == "online")
					{
						query = query.Where(h => !string.IsNullOrEmpty(h.DiaChiGiaoHang));
					}
					else if (loaiDonHang == "taiquay")
					{
						query = query.Where(h => string.IsNullOrEmpty(h.DiaChiGiaoHang));
					}
				}

				if (!string.IsNullOrEmpty(khachHang))
				{
					var khachHangLower = khachHang.ToLower();
					query = query.Where(h =>
						(h.KhachHang != null && h.KhachHang.TenKhachHang.ToLower().Contains(khachHangLower)) ||
						(h.TenNguoiNhan != null && h.TenNguoiNhan.ToLower().Contains(khachHangLower)) ||
						(h.SoDienThoaiNguoiNhan != null && h.SoDienThoaiNguoiNhan.Contains(khachHangLower))
					);
				}

				if (!string.IsNullOrEmpty(maDonHang))
				{
					var maDonHangLower = maDonHang.ToLower();
					query = query.Where(h => h.MaHoaDon.ToLower().Contains(maDonHangLower));
				}

				// Tính tổng số bản ghi sau khi lọc
				var totalCount = await query.CountAsync();
				var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

				// Thống kê theo cùng bộ lọc (khớp Admin/QuanLyDonHang — online/tại quầy/chờ xác nhận)
				var totalOnlineCount = await query.CountAsync(h => !string.IsNullOrEmpty(h.DiaChiGiaoHang));
				var totalTaiQuayCount = await query.CountAsync(h => string.IsNullOrEmpty(h.DiaChiGiaoHang));
				var totalPendingCount = await query.CountAsync(h => h.TrangThai == "Chờ xác nhận");

				// Ưu tiên đơn đã chuyển khoản + đã thanh toán nhưng còn chờ xác nhận lên đầu danh sách.
				var prioritizedQuery = OrderByTransferPendingPriority(query);

				// Áp dụng phân trang sau khi đã ưu tiên.
				var hoaDonsRaw = await prioritizedQuery
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.Select(h => new
					{
						IDHoaDon = h.IDHoaDon,
						MaHoaDon = h.MaHoaDon,
						TongTien = h.TongTien,
						TienGiam = h.TienGiam,
						TrangThai = h.TrangThai,
						NgayTao = h.NgayTao,
						TenNguoiNhan = h.TenNguoiNhan,
						SoDienThoaiNguoiNhan = h.SoDienThoaiNguoiNhan,
						DiaChiGiaoHang = h.DiaChiGiaoHang,
						KhachHang = h.KhachHang != null ? new
						{
							IDKhachHang = h.KhachHang.IDKhachHang,
							TenKhachHang = h.KhachHang.TenKhachHang,
							SoDienThoai = h.KhachHang.SoDienThoai
						} : null,
						NhanVien = h.NhanVien != null ? new
						{
							IDNhanVien = h.NhanVien.IDNhanVien,
							TenNhanVien = h.NhanVien.TenNhanVien
						} : null,
						SoLuongSanPham = h.ChiTietHoaDons.Count,
						TrangThaiThanhToan = h.TrangThaiThanhToan,
						PhuongThucThanhToan = h.PhuongThucThanhToan != null
							? new
							{
								MaPhuongThuc = h.PhuongThucThanhToan.MaPhuongThuc,
								TenPhuongThuc = h.PhuongThucThanhToan.TenPhuongThuc
							}
							: null
					})
					.ToListAsync();

				var hoaDons = hoaDonsRaw
					.Select(h =>
					{
						var canHighlight = IsPaidTransferPendingConfirmation(
							h.TrangThai,
							h.TrangThaiThanhToan,
							h.PhuongThucThanhToan?.MaPhuongThuc,
							h.PhuongThucThanhToan?.TenPhuongThuc);
						var displayStatus = canHighlight
							? "Đã thanh toán chờ xác nhận"
							: h.TrangThai;

						return new
						{
							h.IDHoaDon,
							h.MaHoaDon,
							h.TongTien,
							h.TienGiam,
							TrangThai = displayStatus,
							TrangThaiGoc = h.TrangThai,
							h.NgayTao,
							h.TenNguoiNhan,
							h.SoDienThoaiNguoiNhan,
							h.DiaChiGiaoHang,
							h.KhachHang,
							h.NhanVien,
							h.SoLuongSanPham,
							h.TrangThaiThanhToan,
							h.PhuongThucThanhToan,
							CanHighlightChuyenKhoanChoXacNhan = canHighlight,
							GhiChuChuyenKhoanChoXacNhan = canHighlight
								? "Đơn đã thanh toán chuyển khoản, đang chờ quản lý xác nhận."
								: null
						};
					})
					.ToList();

				// Thêm thông tin phân trang vào response headers
				Response.Headers.Append("X-Total-Count", totalCount.ToString());
				Response.Headers.Append("X-Total-Pages", totalPages.ToString());
				Response.Headers.Append("X-Current-Page", page.ToString());
				Response.Headers.Append("X-Page-Size", pageSize.ToString());

				return Ok(new
				{
					Data = hoaDons,
					Pagination = new
					{
						TotalCount = totalCount,
						TotalPages = totalPages,
						CurrentPage = page,
						PageSize = pageSize,
						HasPreviousPage = page > 1,
						HasNextPage = page < totalPages
					},
					Statistics = new
					{
						TotalOnlineCount = totalOnlineCount,
						TotalTaiQuayCount = totalTaiQuayCount,
						TotalPendingCount = totalPendingCount
					}
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi lấy danh sách hóa đơn: {Message}", ex.Message);
				return StatusCode(500, "Lỗi server khi lấy danh sách hóa đơn");
			}
		}

		// GET: api/HoaDons/5
		[HttpGet("{id}")]
		public async Task<ActionResult<object>> GetHoaDon(Guid id)
		{
			try
			{
				var hoaDon = await _context.HoaDons
					.Include(h => h.KhachHang)
					.Include(h => h.NhanVien)
					.Include(h => h.PhieuGiamGia)
					.Include(h => h.PhuongThucThanhToan)
					.Include(h => h.ChiTietHoaDons)
						.ThenInclude(ct => ct.SanPhamChiTiet)
							.ThenInclude(spct => spct.KichCo)
					.Include(h => h.ChiTietHoaDons)
						.ThenInclude(ct => ct.SanPhamChiTiet)
							.ThenInclude(spct => spct.MauSac)
					.Include(h => h.ChiTietHoaDons)
						.ThenInclude(ct => ct.SanPhamChiTiet)
							.ThenInclude(spct => spct.HoaTiet) // Thêm include cho HoaTiet
					.Include(h => h.ChiTietHoaDons)
						.ThenInclude(ct => ct.SanPhamChiTiet)
							.ThenInclude(spct => spct.SanPham) // Thêm include này để tránh lỗi
						.Where(h => h.IDHoaDon == id)
						.Select(h => new
						{
							IDHoaDon = h.IDHoaDon,
							MaHoaDon = h.MaHoaDon,
							BanTaiQuay = h.BanTaiQuay,
							TongTien = h.TongTien,
							TienGiam = h.TienGiam,
							SoTienGiamTuDiem = h.SoTienGiamTuDiem,
							DiemDaDung = h.DiemDaDung,
							DiemCong = h.DiemCong,
							TyLeQuyDoiDiem = h.TyLeQuyDoiDiem,
							PhiVanChuyen = h.PhiVanChuyen,
							PhiVanChuyenGoc = h.PhiVanChuyenGoc,
							SoTienGiamPhiVanChuyen = h.SoTienGiamPhiVanChuyen,
							TrangThai = h.TrangThai,
							NgayTao = h.NgayTao,
							TenNguoiNhan = h.TenNguoiNhan,
							SoDienThoaiNguoiNhan = h.SoDienThoaiNguoiNhan,
							DiaChiGiaoHang = h.DiaChiGiaoHang,
							LyDoHuyDon = h.LyDoHuyDon,
							KhachHang = h.KhachHang != null ? new
							{
								IDKhachHang = h.KhachHang.IDKhachHang,
								TenKhachHang = h.KhachHang.TenKhachHang,
								SoDienThoai = h.KhachHang.SoDienThoai,
								Email = h.KhachHang.Email
							} : null,
							NhanVien = h.NhanVien != null ? new
							{
								IDNhanVien = h.NhanVien.IDNhanVien,
								TenNhanVien = h.NhanVien.TenNhanVien,
								SoDienThoai = h.NhanVien.SoDienThoai
							} : null,
							PhieuGiamGia = h.PhieuGiamGia != null ? new
							{
								IDPhieuGiamGia = h.PhieuGiamGia.IDPhieuGiamGia,
								MaPhieu = h.PhieuGiamGia.MaCode,
								TenPhieu = h.PhieuGiamGia.TenPhieu
							} : null,
							PhuongThucThanhToan = h.PhuongThucThanhToan != null ? new
							{
								IDPhuongThucThanhToan = h.PhuongThucThanhToan.IDPhuongThucThanhToan,
								TenPhuongThuc = h.PhuongThucThanhToan.TenPhuongThuc
							} : null,
							ChiTietHoaDons = h.ChiTietHoaDons
								.Where(ct => ct.SanPhamChiTiet != null)
								.Select(ct => new
								{
									IDChiTietHoaDon = ct.IDChiTietHoaDon,
									MaChiTietHoaDon = ct.MaChiTietHoaDon,
									SoLuong = ct.SoLuong,
									DonGia = ct.DonGia,
									ThanhTien = ct.ThanhTien,
									SanPhamChiTiet = new
									{
										IDSanPhamChiTiet = ct.SanPhamChiTiet!.IDSanPhamChiTiet,
										MaSPChiTiet = ct.SanPhamChiTiet.MaSPChiTiet,
										GiaBan = ct.SanPhamChiTiet.GiaBan,
										SoLuongTonHienTai = ct.SanPhamChiTiet.SoLuong - ct.SanPhamChiTiet.SoLuongDatCho,
										SoLuongDatMua = ct.SoLuong,
										SoLuongTonTruocXacNhan = h.DaTruTonKho
											? (ct.SanPhamChiTiet.SoLuong - ct.SanPhamChiTiet.SoLuongDatCho) + ct.SoLuong
											: (ct.SanPhamChiTiet.SoLuong - ct.SanPhamChiTiet.SoLuongDatCho),
										SoLuongTonDuKienSauHuy = (h.TrangThai == "Đã hủy" || h.TrangThai == "Đã hoàn tiền")
											? (ct.SanPhamChiTiet.SoLuong - ct.SanPhamChiTiet.SoLuongDatCho)
											: (ct.SanPhamChiTiet.SoLuong - ct.SanPhamChiTiet.SoLuongDatCho) + ct.SoLuong,
										KichCo = ct.SanPhamChiTiet.KichCo != null ? new { TenKichCo = ct.SanPhamChiTiet.KichCo.TenKichCo } : null,
										MauSac = ct.SanPhamChiTiet.MauSac != null ? new { TenMauSac = ct.SanPhamChiTiet.MauSac.TenMauSac } : null,
										HoaTiet = ct.SanPhamChiTiet.HoaTiet != null ? new { TenHoaTiet = ct.SanPhamChiTiet.HoaTiet.TenHoaTiet } : null,
										SanPham = ct.SanPhamChiTiet.SanPham != null ? new
										{
											IDSanPham = ct.SanPhamChiTiet.SanPham.IDSanPham,
											TenSanPham = ct.SanPhamChiTiet.SanPham.TenSanPham,
											MaSanPham = ct.SanPhamChiTiet.SanPham.MaSanPham
										} : null
									}
								}).ToList()
						})
						.FirstOrDefaultAsync();

				if (hoaDon == null)
				{
					return NotFound();
				}

				return Ok(hoaDon);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi lấy chi tiết hóa đơn: {Message}", ex.Message);
				return StatusCode(500, "Lỗi khi tải chi tiết đơn hàng");
			}
		}

		// POST: api/HoaDons
		[HttpPost]
		public async Task<ActionResult<HoaDon>> CreateHoaDon([FromBody] CreateHoaDonDto dto)
		{
			var reservationLines = new List<InventoryLine>();
			var shouldAutoConfirmAfterPayment = dto.XacNhanNgaySauThanhToan;
			try
			{
				await using var tx = await _context.Database.BeginTransactionAsync();
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				// Kiểm tra dữ liệu đầu vào
				if (dto.ChiTietHoaDons == null || !dto.ChiTietHoaDons.Any())
				{
					return BadRequest("Không có sản phẩm nào trong đơn hàng");
				}

				var merchandiseSubtotal = Math.Max(dto.ChiTietHoaDons.Sum(x => x.ThanhTien), 0);
				if (merchandiseSubtotal <= 0)
				{
					return BadRequest("Tổng tiền hàng phải lớn hơn 0");
				}

				PhieuGiamGia? voucher = null;
				if (dto.PhieuGiamGiaId.HasValue)
				{
					var now = DateTime.UtcNow;
					voucher = await _context.PhieuGiamGias
						.AsNoTracking()
						.FirstOrDefaultAsync(x => x.IDPhieuGiamGia == dto.PhieuGiamGiaId.Value);

					if (voucher == null || !voucher.TrangThai || voucher.NgayBatDau > now || voucher.NgayKetThuc < now)
					{
						return BadRequest("Phiếu giảm giá không hợp lệ hoặc đã hết hiệu lực.");
					}
					if (voucher.DonToiThieu.HasValue && merchandiseSubtotal < voucher.DonToiThieu.Value)
					{
						return BadRequest($"Đơn hàng chưa đạt giá trị tối thiểu {voucher.DonToiThieu.Value:n0} để áp dụng phiếu giảm giá.");
					}

					if (dto.KhachHangId.HasValue)
					{
						var usedByCustomer = await HasCustomerUsedVoucherAsync(dto.PhieuGiamGiaId.Value, dto.KhachHangId.Value);
						if (usedByCustomer)
						{
							return BadRequest("Khách hàng này đã sử dụng phiếu giảm giá này trước đó.");
						}
					}
					else
					{
						var normalizedPhone = NormalizePhoneForVoucherLimit(dto.SoDienThoaiNguoiNhan);
						var normalizedEmail = NormalizeEmailForVoucherLimit(dto.EmailNguoiNhan);
						if (string.IsNullOrWhiteSpace(normalizedPhone) && string.IsNullOrWhiteSpace(normalizedEmail))
						{
							return BadRequest("Vui lòng nhập số điện thoại hoặc email để sử dụng phiếu giảm giá.");
						}

						var usedByGuest = await HasGuestUsedVoucherAsync(dto.PhieuGiamGiaId.Value, normalizedPhone, normalizedEmail);
						if (usedByGuest)
						{
							return BadRequest("Số điện thoại hoặc email này đã sử dụng phiếu giảm giá này trước đó.");
						}
					}
				}

				// Kiểm tra tồn kho trước khi trừ (tránh trừ một phần rồi mới báo lỗi)
				foreach (var chiTiet in dto.ChiTietHoaDons)
				{
					var spct = await _context.SanPhamChiTiets.FindAsync(chiTiet.IDSanPhamChiTiet);
					if (spct == null)
						return BadRequest($"Không tìm thấy sản phẩm chi tiết {chiTiet.IDSanPhamChiTiet}");
					if (chiTiet.SoLuong <= 0)
						return BadRequest("Số lượng sản phẩm phải lớn hơn 0");
					var soLuongKhaDung = spct.SoLuong - spct.SoLuongDatCho;
					if (soLuongKhaDung < chiTiet.SoLuong)
						return BadRequest($"Sản phẩm {spct.MaSPChiTiet} không đủ số lượng khả dụng. Khả dụng: {soLuongKhaDung}");
				}

				// Tạo hóa đơn mới
				var hoaDon = new HoaDon
				{
					IDHoaDon = Guid.NewGuid(),
					MaHoaDon = $"HD_{DateTime.UtcNow:yyyyMMddHHmmss}",
					IDKhachHang = dto.KhachHangId,
					IDNhanVien = dto.NhanVienId,
					IDPhieuGiamGia = dto.PhieuGiamGiaId,
					IDPhuongThucThanhToan = dto.PhuongThucThanhToanId,
					TongTien = merchandiseSubtotal,
					TienGiam = 0,
					PhiVanChuyen = 0,
					BanTaiQuay = dto.BanTaiQuay,
					TrangThai = "Chờ xác nhận",
					TrangThaiThanhToan = shouldAutoConfirmAfterPayment ? "Đã thanh toán" : "Chưa thanh toán",
					DaDatChoTonKho = true,
					DaTruTonKho = false,
					TenNguoiNhan = dto.TenNguoiNhan ?? "",
					SoDienThoaiNguoiNhan = dto.SoDienThoaiNguoiNhan ?? "",
					DiaChiGiaoHang = dto.DiaChiGiaoHang ?? "",
					NgayTao = DateTime.UtcNow,
					NguoiTao = "Customer",
					TrangThaiHoaDon = true
				};

				_context.HoaDons.Add(hoaDon);

				// Tạo chi tiết hóa đơn từ dữ liệu được gửi lên
				foreach (var chiTiet in dto.ChiTietHoaDons)
				{
					var chiTietHoaDon = new ChiTietHoaDon
					{
						IDChiTietHoaDon = Guid.NewGuid(),
						MaChiTietHoaDon = $"CTHD_{DateTime.UtcNow:yyyyMMddHHmmss}_{chiTiet.IDSanPhamChiTiet.ToString().Substring(0, 8)}",
						IDHoaDon = hoaDon.IDHoaDon,
						IDSanPhamChiTiet = chiTiet.IDSanPhamChiTiet,
						SoLuong = chiTiet.SoLuong,
						DonGia = chiTiet.DonGia,
						ThanhTien = chiTiet.ThanhTien,
						NgayTao = DateTime.UtcNow,
						NguoiTao = "Customer",
						TrangThai = true
					};

					_context.ChiTietHoaDons.Add(chiTietHoaDon);
					reservationLines.Add(new InventoryLine(chiTiet.IDSanPhamChiTiet, chiTiet.SoLuong));

					//// Cập nhật số lượng sản phẩm
					//var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(chiTiet.IDSanPhamChiTiet);
					//if (sanPhamChiTiet != null)
					//{
					//    sanPhamChiTiet.SoLuong -= chiTiet.SoLuong;
					//    if (sanPhamChiTiet.SoLuong < 0)
					//    {
					//        return BadRequest($"Sản phẩm {sanPhamChiTiet.MaSPChiTiet} không đủ số lượng");
					//    }
					//}
				}

				if (voucher != null)
				{
					var voucherDiscount = hoaDon.TongTien * (voucher.GiaTriGiam / 100m);
					if (voucher.GiaTriGiamToiDa.HasValue)
					{
						voucherDiscount = Math.Min(voucherDiscount, voucher.GiaTriGiamToiDa.Value);
					}

					voucherDiscount = Math.Max(voucherDiscount, 0);
					hoaDon.TienGiam = voucherDiscount;
					hoaDon.TongTien = Math.Max(hoaDon.TongTien - voucherDiscount, 0);
				}

				var loyaltyResult = await _loyaltyService.BuildCheckoutResultAsync(
					dto.KhachHangId,
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

				var shippingFeeFromRequest = dto.PhiVanChuyen ?? 0;
				if (shippingFeeFromRequest > 0)
				{
					if (dto.PhiVanChuyenDaGiam)
					{
						var originalShippingFee = dto.PhiVanChuyenGoc.HasValue && dto.PhiVanChuyenGoc.Value >= shippingFeeFromRequest
							? dto.PhiVanChuyenGoc.Value
							: shippingFeeFromRequest;
						hoaDon.PhiVanChuyenGoc = originalShippingFee;
						hoaDon.PhiVanChuyen = shippingFeeFromRequest;
						hoaDon.SoTienGiamPhiVanChuyen = dto.SoTienGiamPhiVanChuyen ?? Math.Max(originalShippingFee - shippingFeeFromRequest, 0);
						hoaDon.TongTien += shippingFeeFromRequest;
					}
					else
					{
						var shippingPolicy = await _shippingPolicyService.ResolveCustomerDiscountAsync(dto.KhachHangId);
						var finalShippingFee = shippingFeeFromRequest * (1 - shippingPolicy.Percent / 100m);
						finalShippingFee = Math.Max(finalShippingFee, 0);

						hoaDon.PhiVanChuyenGoc = shippingFeeFromRequest;
						hoaDon.PhiVanChuyen = finalShippingFee;
						hoaDon.SoTienGiamPhiVanChuyen = shippingFeeFromRequest - finalShippingFee;
						hoaDon.TongTien += finalShippingFee;
					}
				}

				if (dto.KhachHangId.HasValue)
				{
					await _loyaltyService.ApplyCheckoutPointChangesAsync(dto.KhachHangId.Value, hoaDon.IDHoaDon, loyaltyResult, "Online");
				}

				var reserveResult = await _inventoryReservationService.ReserveAsync(reservationLines, "Online");
				if (!reserveResult.Success)
				{
					return BadRequest(reserveResult.ErrorMessage ?? "Không thể giữ chỗ tồn kho cho đơn hàng.");
				}

				if (shouldAutoConfirmAfterPayment)
				{
					var commitResult = await _inventoryReservationService.CommitReservedAsync(reservationLines, "VNPay");
					if (!commitResult.Success)
					{
						return BadRequest(commitResult.ErrorMessage ?? "Không thể trừ tồn kho cho đơn đã thanh toán VNPay.");
					}

					hoaDon.TrangThai = "Đã xác nhận";
					hoaDon.DaDatChoTonKho = false;
					hoaDon.DaTruTonKho = true;
				}

				await _context.SaveChangesAsync();
				await tx.CommitAsync();

				return CreatedAtAction(nameof(GetHoaDon), new { id = hoaDon.IDHoaDon }, hoaDon);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi tạo hóa đơn: {Message}", ex.Message);
				if (ex.InnerException != null)
				{
					_logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException.Message);
				}
				return StatusCode(500, $"Lỗi server: {ex.Message}");
			}
		}

		// PUT: api/HoaDons/{id}/trangthai
		[HttpPut("{id}/trangthai")]
		public async Task<IActionResult> UpdateTrangThai(Guid id, [FromBody] UpdateTrangThaiDto dto)
		{
			try
			{
				await using var tx = await _context.Database.BeginTransactionAsync();

				var hoaDon = await _context.HoaDons
					.Include(h => h.ChiTietHoaDons)
					.Include(h => h.KhachHang)
					.FirstOrDefaultAsync(h => h.IDHoaDon == id);

				if (hoaDon == null)
				{
					return NotFound("Không tìm thấy hóa đơn");
				}

				var oldStatus = hoaDon.TrangThai;
				var lyDoHuy = dto.TrangThai == "Đã hủy"
					? (dto.LyDoHuyDon ?? hoaDon.LyDoHuyDon)
					: hoaDon.LyDoHuyDon;

				var inventoryLines = hoaDon.ChiTietHoaDons
					.Select(x => new InventoryLine(x.IDSanPhamChiTiet, x.SoLuong))
					.ToList();

				if (dto.TrangThai == "Đã xác nhận" && oldStatus == "Chờ xác nhận")
				{
					var commitResult = await _inventoryReservationService.CommitReservedAsync(inventoryLines, dto.NguoiCapNhat ?? "System");
					if (!commitResult.Success)
					{
						await tx.RollbackAsync();
						return BadRequest(commitResult.ErrorMessage ?? "Không thể xác nhận đơn vì tồn kho không hợp lệ.");
					}

					hoaDon.DaDatChoTonKho = false;
					hoaDon.DaTruTonKho = true;
				}
				else if (dto.TrangThai == "Đã hủy" && oldStatus != "Đã hủy")
				{
					if (hoaDon.DaDatChoTonKho)
					{
						var releaseResult = await _inventoryReservationService.ReleaseAsync(inventoryLines, dto.NguoiCapNhat ?? "System");
						if (!releaseResult.Success)
						{
							await tx.RollbackAsync();
							return BadRequest(releaseResult.ErrorMessage ?? "Không thể nhả giữ chỗ tồn kho khi hủy đơn.");
						}
					}

					if (hoaDon.DaTruTonKho)
					{
						var restockResult = await _inventoryReservationService.RestockAsync(inventoryLines, dto.NguoiCapNhat ?? "System");
						if (!restockResult.Success)
						{
							await tx.RollbackAsync();
							return BadRequest(restockResult.ErrorMessage ?? "Không thể hoàn kho khi hủy đơn.");
						}
					}

					hoaDon.DaDatChoTonKho = false;
					hoaDon.DaTruTonKho = false;
				}

				var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
					$@"UPDATE ""HoaDons""
					   SET ""TrangThai"" = {dto.TrangThai},
					       ""NguoiCapNhat"" = {dto.NguoiCapNhat},
					       ""LanCapNhatCuoi"" = {dto.LanCapNhatCuoi},
					       ""LyDoHuyDon"" = {lyDoHuy},
					       ""DaDatChoTonKho"" = {hoaDon.DaDatChoTonKho},
					       ""DaTruTonKho"" = {hoaDon.DaTruTonKho}
					   WHERE ""IDHoaDon"" = {id}
					     AND ""TrangThai"" = {oldStatus};");

				if (affected == 0)
				{
					await tx.RollbackAsync();
					return Conflict("Đơn hàng đã được cập nhật bởi thao tác khác, vui lòng tải lại.");
				}

				await _orderHistoryService.SaveOrderHistoryAsync(id, oldStatus, dto.TrangThai, dto.NguoiCapNhat, dto.LyDoHuyDon);
				await _context.SaveChangesAsync();
				await tx.CommitAsync();

				hoaDon.TrangThai = dto.TrangThai;
				hoaDon.LyDoHuyDon = lyDoHuy;

				_logger.LogInformation($"Updated order {id} status to {dto.TrangThai}");

				if (dto.TrangThai == "Đã hủy")
				{
					await _emailService.SendOrderCancellationEmailAsync(hoaDon, dto.LyDoHuyDon);
				}
				else
				{
					await _emailService.SendOrderStatusChangeEmailAsync(hoaDon, oldStatus, dto.TrangThai);
				}

				return Ok(new { message = "Cập nhật trạng thái thành công" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating order status: {Message}", ex.Message);
				return StatusCode(500, "Lỗi khi cập nhật trạng thái đơn hàng");
			}
		}

		// GET: api/HoaDons/{id}/rollback-options
		[HttpGet("{id}/rollback-options")]
		public async Task<IActionResult> GetRollbackOptions(Guid id)
		{
			try
			{
				var hoaDon = await _context.HoaDons.FindAsync(id);
				if (hoaDon == null)
				{
					return NotFound("Không tìm thấy hóa đơn");
				}

				var validStatuses = await _orderHistoryService.GetValidRollbackStatusesAsync(id, hoaDon.TrangThai);

				return Ok(new
				{
					currentStatus = hoaDon.TrangThai,
					validRollbackStatuses = validStatuses,
					canRollback = validStatuses.Any()
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting rollback options: {Message}", ex.Message);
				return StatusCode(500, "Lỗi khi lấy tùy chọn rollback");
			}
		}

		// POST: api/HoaDons/{id}/rollback
		[HttpPost("{id}/rollback")]
		public async Task<IActionResult> RollbackStatus(Guid id, [FromBody] RollbackStatusDto dto)
		{
			try
			{
				var hoaDon = await _context.HoaDons
					.Include(h => h.KhachHang)
					.FirstOrDefaultAsync(h => h.IDHoaDon == id);

				if (hoaDon == null)
				{
					return NotFound("Không tìm thấy hóa đơn");
				}

				var oldStatus = hoaDon.TrangThai;
				var success = await _orderHistoryService.RollbackOrderStatusAsync(id, dto.TargetStatus, dto.Reason, dto.UpdatedBy);

				if (!success)
				{
					return BadRequest("Không thể rollback đơn hàng về trạng thái này");
				}

				// Gửi email thông báo rollback
				await _emailService.SendOrderStatusChangeEmailAsync(hoaDon, oldStatus, dto.TargetStatus);

				return Ok(new { message = "Rollback trạng thái thành công" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error rolling back order status: {Message}", ex.Message);
				return StatusCode(500, "Lỗi khi rollback trạng thái đơn hàng");
			}
		}

		// GET: api/HoaDons/{id}/history
		[HttpGet("{id}/history")]
		public async Task<IActionResult> GetOrderHistory(Guid id)
		{
			try
			{
				var history = await _orderHistoryService.GetOrderHistoryAsync(id);

				var result = history.Select(h => new
				{
					id = h.IDLichSuHoaDon,
					trangThai = h.TrangThai,
					ghiChu = h.GhiChu,
					ngayTao = h.NgayTao,
					nguoiTao = h.NguoiTao
				}).ToList();

				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting order history: {Message}", ex.Message);
				return StatusCode(500, "Lỗi khi lấy lịch sử đơn hàng");
			}
		}

		// PUT: api/HoaDons/5
		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateHoaDon(Guid id, HoaDon hoaDon)
		{
			if (id != hoaDon.IDHoaDon)
			{
				return BadRequest();
			}

			_context.Entry(hoaDon).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!HoaDonExists(id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return NoContent();
		}

		// DELETE: api/HoaDons/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteHoaDon(Guid id)
		{
			try
			{
				var hoaDon = await _context.HoaDons
					.Include(h => h.ChiTietHoaDons)
					.ThenInclude(ct => ct.SanPhamChiTiet)
					.FirstOrDefaultAsync(h => h.IDHoaDon == id);

				if (hoaDon == null)
				{
					return NotFound();
				}

				var lines = hoaDon.ChiTietHoaDons
					.Select(x => new InventoryLine(x.IDSanPhamChiTiet, x.SoLuong))
					.ToList();

				if (hoaDon.DaDatChoTonKho)
				{
					var releaseResult = await _inventoryReservationService.ReleaseAsync(lines, "System");
					if (!releaseResult.Success)
					{
						return BadRequest(releaseResult.ErrorMessage ?? "Không thể nhả tồn đặt chỗ trước khi xóa đơn.");
					}
				}

				if (hoaDon.DaTruTonKho)
				{
					var restockResult = await _inventoryReservationService.RestockAsync(lines, "System");
					if (!restockResult.Success)
					{
						return BadRequest(restockResult.ErrorMessage ?? "Không thể hoàn kho trước khi xóa đơn.");
					}
				}

				_context.HoaDons.Remove(hoaDon);

				await _context.SaveChangesAsync();

				_logger.LogInformation($"Đã xóa đơn hàng {id} thành công");
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error deleting order {id}: {ex.Message}");
				return StatusCode(500, "Internal server error");
			}
		}

		private bool HoaDonExists(Guid id)
		{
			return _context.HoaDons.Any(e => e.IDHoaDon == id);
		}

		// GET: api/HoaDons/don-hang-can-xac-nhan
		[HttpGet("don-hang-can-xac-nhan")]
		public async Task<ActionResult<object>> GetDonHangCanXacNhan()
		{
			try
			{
				var donHangCanXacNhan = await _context.HoaDons
					.Where(h => h.TrangThai == "Chờ xác nhận" && !string.IsNullOrEmpty(h.DiaChiGiaoHang))
					.Include(h => h.KhachHang)
					.OrderByDescending(h => h.NgayTao)
					.Take(5)
					.Select(h => new
					{
						id = h.IDHoaDon,
						maHoaDon = h.MaHoaDon,
						tenKhachHang = h.KhachHang != null ? h.KhachHang.TenKhachHang : h.TenNguoiNhan,
						ngayTao = h.NgayTao,
						tongTien = h.TongTien
					})
					.ToListAsync();

				var tongSo = await _context.HoaDons
					.CountAsync(h => h.TrangThai == "Chờ xác nhận" && !string.IsNullOrEmpty(h.DiaChiGiaoHang));

				return Ok(new
				{
					tongSo = tongSo,
					danhSach = donHangCanXacNhan
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi lấy đơn hàng cần xác nhận: {Message}", ex.Message);
				return StatusCode(500, $"Lỗi server: {ex.Message}");
			}
		}

		// GET: api/HoaDons/guest 
		[HttpGet("guest")]
		public async Task<ActionResult<IEnumerable<HoaDon>>> GetHoaDonsByGuest(
			string? search = null, int page = 1, int pageSize = 10)
		{
			try
			{
				page = page < 1 ? 1 : page;
				pageSize = pageSize < 1 ? 10 : pageSize;

				if (string.IsNullOrEmpty(search))
				{
					return BadRequest("Mã đơn hàng là bắt buộc để tìm đơn hàng");
				}

				// Chỉ lấy các cột cần cho màn hình danh sách đơn hàng để giảm kích thước response
				var query = _context.HoaDons
					.AsNoTracking()
					.Where(h => h.TrangThaiHoaDon);

				query = query.Where(h => h.MaHoaDon.Contains(search));

				query = query.OrderByDescending(h => h.NgayTao);

				var totalCount = await query.CountAsync();
				var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);
				if (totalPages > 0 && page > totalPages)
				{
					page = totalPages;
				}
				var prioritizedQuery = OrderByTransferPendingPriority(query);
				var hoaDons = await prioritizedQuery
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.Select(h => new
					{
						IDHoaDon = h.IDHoaDon,
						MaHoaDon = h.MaHoaDon,
						TongTien = h.TongTien,
						TrangThai = h.TrangThai,
						TrangThaiThanhToan = h.TrangThaiThanhToan,
						PhuongThucThanhToanMa = h.PhuongThucThanhToan != null ? h.PhuongThucThanhToan.MaPhuongThuc : null,
						PhuongThucThanhToanTen = h.PhuongThucThanhToan != null ? h.PhuongThucThanhToan.TenPhuongThuc : null,
						DiaChiGiaoHang = h.DiaChiGiaoHang,
						NgayTao = h.NgayTao
					})
					.ToListAsync();

				var hoaDonsView = hoaDons
					.Select(h => new HoaDon
					{
						IDHoaDon = h.IDHoaDon,
						MaHoaDon = h.MaHoaDon,
						TongTien = h.TongTien,
						TrangThai = IsPaidTransferPendingConfirmation(
							h.TrangThai,
							h.TrangThaiThanhToan,
							h.PhuongThucThanhToanMa,
							h.PhuongThucThanhToanTen)
							? "Đã thanh toán chờ xác nhận"
							: h.TrangThai,
						DiaChiGiaoHang = h.DiaChiGiaoHang,
						NgayTao = h.NgayTao
					})
					.ToList();

				Response.Headers.Append("X-Total-Count", totalCount.ToString());
				Response.Headers.Append("X-Total-Pages", totalPages.ToString());
				Response.Headers.Append("X-Current-Page", page.ToString());
				Response.Headers.Append("X-Page-Size", pageSize.ToString());

				return Ok(hoaDonsView);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi tìm đơn hàng cho khách vãng lai với mã đơn hàng {Search}", search);
				return StatusCode(500, "Lỗi nội bộ server");
			}
		}

		// GET: api/HoaDons/customer/{customerId} - Cho người dùng đã đăng nhập
		[HttpGet("customer/{customerId}")]
		public async Task<ActionResult<IEnumerable<HoaDon>>> GetHoaDonsByCustomer(
			Guid customerId, string? search = null, string? fromDate = null, string? toDate = null,
			int page = 1, int pageSize = 10)
		{
			try
			{
				page = page < 1 ? 1 : page;
				pageSize = pageSize < 1 ? 10 : pageSize;

				// Trả dữ liệu rút gọn để tránh response quá nặng dẫn đến stream bị ngắt giữa chừng
				var query = _context.HoaDons
					.AsNoTracking()
					.Where(h => h.IDKhachHang == customerId && h.TrangThaiHoaDon)
					.AsQueryable();

				// Tìm kiếm theo mã đơn hàng
				if (!string.IsNullOrEmpty(search))
				{
					query = query.Where(h => h.MaHoaDon.Contains(search));
				}

				// Lọc theo ngày từ (UTC range compare để ổn định với PostgreSQL timestamp)
				if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var fromDateParsed))
				{
					var fromDateUtc = DateTime.SpecifyKind(fromDateParsed.Date, DateTimeKind.Utc);
					query = query.Where(h => h.NgayTao >= fromDateUtc);
				}

				// Lọc theo ngày đến (inclusive theo ngày, dùng upper-bound độc quyền)
				if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var toDateParsed))
				{
					var toDateExclusiveUtc = DateTime.SpecifyKind(toDateParsed.Date.AddDays(1), DateTimeKind.Utc);
					query = query.Where(h => h.NgayTao < toDateExclusiveUtc);
				}

				var totalCount = await query.CountAsync();
				var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);
				if (totalPages > 0 && page > totalPages)
				{
					page = totalPages;
				}

				var prioritizedQuery = OrderByTransferPendingPriority(query);
				var hoaDons = await prioritizedQuery
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.Select(h => new
					{
						IDHoaDon = h.IDHoaDon,
						MaHoaDon = h.MaHoaDon,
						TongTien = h.TongTien,
						TrangThai = h.TrangThai,
						TrangThaiThanhToan = h.TrangThaiThanhToan,
						PhuongThucThanhToanMa = h.PhuongThucThanhToan != null ? h.PhuongThucThanhToan.MaPhuongThuc : null,
						PhuongThucThanhToanTen = h.PhuongThucThanhToan != null ? h.PhuongThucThanhToan.TenPhuongThuc : null,
						DiaChiGiaoHang = h.DiaChiGiaoHang,
						NgayTao = h.NgayTao
					})
					.ToListAsync();

				var hoaDonsView = hoaDons
					.Select(h => new HoaDon
					{
						IDHoaDon = h.IDHoaDon,
						MaHoaDon = h.MaHoaDon,
						TongTien = h.TongTien,
						TrangThai = IsPaidTransferPendingConfirmation(
							h.TrangThai,
							h.TrangThaiThanhToan,
							h.PhuongThucThanhToanMa,
							h.PhuongThucThanhToanTen)
							? "Đã thanh toán chờ xác nhận"
							: h.TrangThai,
						DiaChiGiaoHang = h.DiaChiGiaoHang,
						NgayTao = h.NgayTao
					})
					.ToList();

				Response.Headers.Append("X-Total-Count", totalCount.ToString());
				Response.Headers.Append("X-Total-Pages", totalPages.ToString());
				Response.Headers.Append("X-Current-Page", page.ToString());
				Response.Headers.Append("X-Page-Size", pageSize.ToString());

				return Ok(hoaDonsView);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi lấy đơn hàng cho khách hàng {CustomerId}", customerId);
				return StatusCode(500, "Lỗi nội bộ server");
			}
		}

		private static string NormalizePhoneForVoucherLimit(string? phone)
		{
			if (string.IsNullOrWhiteSpace(phone))
				return string.Empty;

			var digits = new string(phone.Where(char.IsDigit).ToArray());
			if (digits.StartsWith("84") && digits.Length == 11)
				return "0" + digits.Substring(2);

			return digits;
		}

		private static string NormalizeEmailForVoucherLimit(string? email)
		{
			return string.IsNullOrWhiteSpace(email)
				? string.Empty
				: email.Trim().ToLowerInvariant();
		}

		private static bool IsPaidTransferPendingConfirmation(
			string? orderStatus,
			string? paymentStatus,
			string? paymentMethodCode,
			string? paymentMethodName)
		{
			var isPendingConfirmationStatus =
				string.Equals(orderStatus, "Chờ xác nhận", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(orderStatus, "DaThanhToan", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(orderStatus, "Đã thanh toán", StringComparison.OrdinalIgnoreCase);

			if (!isPendingConfirmationStatus)
			{
				return false;
			}

			var isPaidByPaymentStatus =
				string.Equals(paymentStatus, "Đã thanh toán", StringComparison.OrdinalIgnoreCase);
			var isPaidByOrderStatus =
				string.Equals(orderStatus, "DaThanhToan", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(orderStatus, "Đã thanh toán", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(orderStatus, "Đã thanh toán chờ xác nhận", StringComparison.OrdinalIgnoreCase);

			if (!isPaidByPaymentStatus && !isPaidByOrderStatus)
			{
				return false;
			}

			return IsTransferPaymentMethod(paymentMethodCode, paymentMethodName);
		}

		private static bool IsTransferPaymentMethod(string? paymentMethodCode, string? paymentMethodName)
		{
			var normalized = RemoveDiacritics($"{paymentMethodCode} {paymentMethodName}")
				.ToLowerInvariant();

			return normalized.Contains("chuyenkhoan")
				|| normalized.Contains("bank")
				|| normalized.Contains("transfer")
				|| normalized.Contains("vnpay")
				|| normalized.Contains("qr");
		}

		private static string RemoveDiacritics(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}

			var normalizedText = value.Normalize(NormalizationForm.FormD);
			var builder = new StringBuilder(normalizedText.Length);

			foreach (var ch in normalizedText)
			{
				if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
				{
					builder.Append(ch);
				}
			}

			return builder.ToString().Normalize(NormalizationForm.FormC);
		}

		private static IOrderedQueryable<HoaDon> OrderByTransferPendingPriority(IQueryable<HoaDon> source)
		{
			return source
				.OrderByDescending(h =>
					(h.TrangThai == "Chờ xác nhận"
						|| h.TrangThai == "DaThanhToan"
						|| h.TrangThai == "Đã thanh toán")
					&& (h.TrangThaiThanhToan == "Đã thanh toán"
						|| h.TrangThai == "DaThanhToan"
						|| h.TrangThai == "Đã thanh toán")
					&& h.PhuongThucThanhToan != null
					&& (
						(h.PhuongThucThanhToan.MaPhuongThuc != null && (
							h.PhuongThucThanhToan.MaPhuongThuc.ToLower().Contains("bank")
							|| h.PhuongThucThanhToan.MaPhuongThuc.ToLower().Contains("transfer")
							|| h.PhuongThucThanhToan.MaPhuongThuc.ToLower().Contains("vnpay")
							|| h.PhuongThucThanhToan.MaPhuongThuc.ToLower().Contains("qr")
						))
						|| (h.PhuongThucThanhToan.TenPhuongThuc != null && (
							h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("chuyển khoản")
							|| h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("chuyen khoan")
							|| h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("bank")
							|| h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("transfer")
							|| h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("vnpay")
							|| h.PhuongThucThanhToan.TenPhuongThuc.ToLower().Contains("qr")
						))
					))
				.ThenByDescending(h => h.NgayTao);
		}

		private async Task<bool> HasCustomerUsedVoucherAsync(Guid voucherId, Guid customerId)
		{
			return await _context.HoaDons
				.AsNoTracking()
				.AnyAsync(h =>
					h.IDPhieuGiamGia == voucherId &&
					h.IDKhachHang == customerId &&
					h.TrangThaiHoaDon &&
					h.TrangThai != "Đã hủy");
		}

		private async Task<bool> HasGuestUsedVoucherAsync(Guid voucherId, string normalizedPhone, string normalizedEmail)
		{
			if (string.IsNullOrWhiteSpace(normalizedPhone) && string.IsNullOrWhiteSpace(normalizedEmail))
				return false;

			if (!string.IsNullOrWhiteSpace(normalizedPhone))
			{
				var usedPhones = await _context.HoaDons
					.AsNoTracking()
					.Where(h =>
						h.IDPhieuGiamGia == voucherId &&
						h.TrangThaiHoaDon &&
						h.TrangThai != "Đã hủy")
					.Select(h => h.SoDienThoaiNguoiNhan)
					.ToListAsync();

				if (usedPhones.Any(phone => NormalizePhoneForVoucherLimit(phone) == normalizedPhone))
					return true;
			}

			if (!string.IsNullOrWhiteSpace(normalizedEmail))
			{
				var usedEmails = await _context.HoaDons
					.AsNoTracking()
					.Where(h =>
						h.IDPhieuGiamGia == voucherId &&
						h.IDKhachHang.HasValue &&
						h.TrangThaiHoaDon &&
						h.TrangThai != "Đã hủy")
					.Join(
						_context.KhachHang.AsNoTracking(),
						h => h.IDKhachHang!.Value,
						kh => kh.IDKhachHang,
						(_, kh) => kh.Email)
					.ToListAsync();

				if (usedEmails.Any(email => NormalizeEmailForVoucherLimit(email) == normalizedEmail))
					return true;
			}

			return false;
		}
	}

	public class CreateHoaDonDto
	{
		public Guid? KhachHangId { get; set; }
		public Guid? NhanVienId { get; set; }
		public Guid? PhieuGiamGiaId { get; set; }
		public Guid PhuongThucThanhToanId { get; set; }
		public decimal TongTien { get; set; }
		public decimal? TienGiam { get; set; }
		public decimal? PhiVanChuyen { get; set; }
		public bool PhiVanChuyenDaGiam { get; set; }
		public bool UsePoint { get; set; }
		public int? RequestedUsedPoints { get; set; }
		public decimal? PhiVanChuyenGoc { get; set; }
		public decimal? SoTienGiamPhiVanChuyen { get; set; }
		public bool XacNhanNgaySauThanhToan { get; set; } = false;
		public bool BanTaiQuay { get; set; } = false;
		public string TenNguoiNhan { get; set; }
		public string SoDienThoaiNguoiNhan { get; set; }
		public string? EmailNguoiNhan { get; set; }
		public string DiaChiGiaoHang { get; set; }
		public string GhiChu { get; set; }
		public List<ChiTietHoaDonDto> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDonDto>();
	}

	public class ChiTietHoaDonDto
	{
		public Guid IDSanPhamChiTiet { get; set; }
		public int SoLuong { get; set; }
		public decimal DonGia { get; set; }
		public decimal ThanhTien { get; set; }
	}

	public class UpdateTrangThaiDto
	{
		public string TrangThai { get; set; }
		public string NguoiCapNhat { get; set; }
		public DateTime LanCapNhatCuoi { get; set; }
		public string? LyDoHuyDon { get; set; }
	}

	public class RollbackStatusDto
	{
		public string TargetStatus { get; set; }
		public string Reason { get; set; }
		public string UpdatedBy { get; set; }
	}
}
