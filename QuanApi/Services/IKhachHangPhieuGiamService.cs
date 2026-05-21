using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using System;

namespace QuanApi.Services
{
    public interface IKhachHangPhieuGiamService
    {
        Task<List<KhachHangPhieuGiam>> GetAllAsync();
        Task<KhachHangPhieuGiam?> GetByIdAsync(Guid id);
        Task<Guid?> GetKhachHangByVoucherAsync(Guid idPhieu);
        Task<List<object>> GetPublicDiscountVouchersAsync(decimal tongTien, Guid? customerId = null, string? soDienThoai = null, string? email = null);
        Task<List<object>> GetCustomerDiscountVouchersAsync(Guid customerId);
        Task<KhachHangPhieuGiam> CreateAsync(KhachHangPhieuGiam model);
        Task<bool> UpdateAsync(Guid id, KhachHangPhieuGiam model);
        Task<bool> DeleteAsync(Guid id);
    }

    public class KhachHangPhieuGiamService : IKhachHangPhieuGiamService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly ILogger<KhachHangPhieuGiamService> _logger;

        public KhachHangPhieuGiamService(
            BanQuanAu1DbContext context,
            ILogger<KhachHangPhieuGiamService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<KhachHangPhieuGiam>> GetAllAsync()
        {
            _logger.LogInformation("Lấy toàn bộ KhachHangPhieuGiam");

            return await _context.KhachHangPhieuGiams
                .AsNoTracking()
                .Include(k => k.KhachHang)
                .Include(k => k.PhieuGiamGia)
                .ToListAsync();
        }

        public async Task<KhachHangPhieuGiam?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Lấy KhachHangPhieuGiam theo ID: {Id}", id);

            return await _context.KhachHangPhieuGiams
                .Include(k => k.KhachHang)
                .Include(k => k.PhieuGiamGia)
                .FirstOrDefaultAsync(x => x.IDKhachHangPhieuGiam == id);
        }
         public async Task<Guid?> GetKhachHangByVoucherAsync(Guid idPhieu)
    {
        _logger.LogInformation("Lấy khách hàng theo phiếu giảm giá: {IdPhieu}", idPhieu);

        var item = await _context.KhachHangPhieuGiams
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == idPhieu);

        return item?.IDKhachHang;
    }

		public async Task<List<object>> GetPublicDiscountVouchersAsync(decimal tongTien, Guid? customerId = null, string? soDienThoai = null, string? email = null)
		{
			_logger.LogInformation("Lấy danh sách phiếu giảm giá công khai");

			var now = DateTime.UtcNow;
            HashSet<Guid>? usedVoucherSet = null;

            if (customerId.HasValue)
            {
                var usedVoucherIds = await _context.HoaDons
                    .AsNoTracking()
                    .Where(h =>
                        h.IDKhachHang == customerId.Value &&
                        h.IDPhieuGiamGia.HasValue &&
                        h.TrangThaiHoaDon &&
                        h.TrangThai != "Đã hủy")
                    .Select(h => h.IDPhieuGiamGia!.Value)
                    .Distinct()
                    .ToListAsync();

                usedVoucherSet = usedVoucherIds.ToHashSet();
            }
            else
            {
                var normalizedPhone = NormalizePhoneForVoucherLimit(soDienThoai);
                var normalizedEmail = NormalizeEmailForVoucherLimit(email);
                if (!string.IsNullOrWhiteSpace(normalizedPhone) || !string.IsNullOrWhiteSpace(normalizedEmail))
                {
                    usedVoucherSet = new HashSet<Guid>();

                    if (!string.IsNullOrWhiteSpace(normalizedPhone))
                    {
                        var usedByPhone = await _context.HoaDons
                            .AsNoTracking()
                            .Where(h =>
                                h.IDPhieuGiamGia.HasValue &&
                                h.TrangThaiHoaDon &&
                                h.TrangThai != "Đã hủy")
                            .Select(h => new
                            {
                                VoucherId = h.IDPhieuGiamGia!.Value,
                                Phone = h.SoDienThoaiNguoiNhan
                            })
                            .ToListAsync();

                        foreach (var item in usedByPhone)
                        {
                            if (NormalizePhoneForVoucherLimit(item.Phone) == normalizedPhone)
                            {
                                usedVoucherSet.Add(item.VoucherId);
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(normalizedEmail))
                    {
                        var usedByEmail = await _context.HoaDons
                            .AsNoTracking()
                            .Where(h =>
                                h.IDPhieuGiamGia.HasValue &&
                                h.IDKhachHang.HasValue &&
                                h.TrangThaiHoaDon &&
                                h.TrangThai != "Đã hủy")
                            .Join(
                                _context.KhachHang.AsNoTracking(),
                                h => h.IDKhachHang!.Value,
                                kh => kh.IDKhachHang,
                                (h, kh) => new
                                {
                                    VoucherId = h.IDPhieuGiamGia!.Value,
                                    Email = kh.Email
                                })
                            .ToListAsync();

                        foreach (var item in usedByEmail)
                        {
                            if (NormalizeEmailForVoucherLimit(item.Email) == normalizedEmail)
                            {
                                usedVoucherSet.Add(item.VoucherId);
                            }
                        }
                    }
                }
            }

			var vouchers = await _context.PhieuGiamGias
				.AsNoTracking()
				.Where(p => p.LaCongKhai &&
							p.TrangThai &&
							p.SoLuong > 0 &&
							p.NgayBatDau <= now &&
							p.NgayKetThuc >= now &&
							p.DonToiThieu <= tongTien) // đủ điều kiện đơn tối thiểu
													   // ưu tiên mã có đơn tối thiểu gần với tổng tiền nhất
				.OrderBy(p => tongTien - p.DonToiThieu)

				// sau đó ưu tiên mã giảm nhiều hơn
				.ThenByDescending(p => p.GiaTriGiam)

				.Select(p => new
				{
					id = p.IDPhieuGiamGia,
					maCode = p.MaCode,
					tenPhieu = p.TenPhieu,
					giaTriGiam = p.GiaTriGiam,
					giaTriGiamToiDa = p.GiaTriGiamToiDa,
					donToiThieu = p.DonToiThieu,
					ngayBatDau = p.NgayBatDau,
					ngayKetThuc = p.NgayKetThuc,
					soLuong = p.SoLuong,
					loaiPhieu = p.LaCongKhai ? "Công khai" : "Riêng tư",
					trangThai = p.TrangThai
				})
				.ToListAsync();

            if (usedVoucherSet is { Count: > 0 })
            {
                vouchers = vouchers
                    .Where(v => !usedVoucherSet.Contains(v.id))
                    .ToList();
            }

            return vouchers
                .Select(v => (object)v)
                .ToList();
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
		public async Task<List<object>> GetCustomerDiscountVouchersAsync(Guid customerId)
        {
            _logger.LogInformation("Lấy phiếu giảm giá của khách hàng: {CustomerId}", customerId);

            var now = DateTime.UtcNow;
            var usedVoucherIds = await _context.HoaDons
                .AsNoTracking()
                .Where(h =>
                    h.IDKhachHang == customerId &&
                    h.IDPhieuGiamGia.HasValue &&
                    h.TrangThaiHoaDon &&
                    h.TrangThai != "Đã hủy")
                .Select(h => h.IDPhieuGiamGia!.Value)
                .Distinct()
                .ToListAsync();

            var usedVoucherSet = usedVoucherIds.ToHashSet();

            var vouchers = await _context.PhieuGiamGias
                .AsNoTracking()
                .Where(p => p.TrangThai &&
                            p.NgayBatDau <= now &&
                            p.NgayKetThuc >= now)
                .GroupJoin(
                    _context.KhachHangPhieuGiams.AsNoTracking()
                        .Where(x => x.IDKhachHang == customerId),
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
                        : x.Voucher.SoLuong,
                    loaiPhieu = x.Voucher.LaCongKhai ? "Công khai" : "Riêng tư"
                })
                .ToListAsync();

            return vouchers
                .Where(x => !usedVoucherSet.Contains(x.id))
                .Select(x => (object)x)
                .ToList();
        }

        public async Task<KhachHangPhieuGiam> CreateAsync(KhachHangPhieuGiam model)
        {
            _logger.LogInformation("Tạo KhachHangPhieuGiam mới");

            _context.KhachHangPhieuGiams.Add(model);
            await _context.SaveChangesAsync();

            return model;
        }
        public async Task<bool> UpdateAsync(Guid id, KhachHangPhieuGiam model)
        {
            _logger.LogInformation("Cập nhật KhachHangPhieuGiam ID: {Id}", id);

            var exists = await _context.KhachHangPhieuGiams
                .AnyAsync(e => e.IDKhachHangPhieuGiam == id);

            if (!exists)
                return false;

            _context.Entry(model).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency khi cập nhật KhachHangPhieuGiam ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Xóa KhachHangPhieuGiam ID: {Id}", id);

            var entity = await _context.KhachHangPhieuGiams.FindAsync(id);
            if (entity == null)
                return false;

            _context.KhachHangPhieuGiams.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

    }
}
