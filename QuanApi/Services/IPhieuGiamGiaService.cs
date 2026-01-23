using AutoMapper;
using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;
using System;
using static QuanApi.Services.PhieuGiamGiaService;

namespace QuanApi.Services
{
    public interface IPhieuGiamGiaService
    {
        Task<List<PhieuGiamGia>> GetAllAsync();
        Task<PhieuGiamGia?> GetByIdAsync(Guid id);
        Task<PhieuGiamGia> CreateAsync(CreatePhieuGiamGiaDto dto, string? nguoiTao);
        Task<bool> UpdateAsync(Guid id, UpdatePayload payload, string? nguoiCapNhat);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> RemoveCustomerAsync(Guid voucherId, Guid customerId);
        Task<(bool success, string message)> AddCustomerAsync(Guid voucherId, Guid customerId, string? nguoiTao);
        Task<object?> GetVoucherCustomersAsync(Guid voucherId);
        Task<(bool success, string message, object? data)> UseVoucherAsync(
              Guid voucherId,
              Guid customerId,
              string? nguoiCapNhat);

        Task<bool> ResetVoucherUsageAsync(
            Guid voucherId,
            Guid customerId,
            string? nguoiCapNhat);

        Task<object> CheckVoucherCodeAsync(string code);
    }

    public class PhieuGiamGiaService : IPhieuGiamGiaService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PhieuGiamGiaService> _logger;
        public PhieuGiamGiaService(
            BanQuanAu1DbContext context,
            IMapper mapper,
            ILogger<PhieuGiamGiaService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<PhieuGiamGia>> GetAllAsync()
        {
            _logger.LogInformation("Lấy danh sách phiếu giảm giá");

            return await _context.PhieuGiamGias
                .AsNoTracking()
                .OrderByDescending(p => p.NgayTao)
                .ToListAsync();
        }

        public async Task<PhieuGiamGia?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Lấy chi tiết phiếu giảm giá ID: {Id}", id);

            return await _context.PhieuGiamGias.FindAsync(id);
        }

        public async Task<PhieuGiamGia> CreateAsync(CreatePhieuGiamGiaDto dto, string? nguoiTao)
        {
            _logger.LogInformation("Tạo phiếu giảm giá mới: {MaCode}", dto.MaCode);

            var model = _mapper.Map<PhieuGiamGia>(dto);
            model.IDPhieuGiamGia = Guid.NewGuid();
            model.NgayTao = DateTime.UtcNow;
            model.LaCongKhai = true;   // mặc định công khai
            model.SoLuong = 1;        // mỗi khách hàng 1 phiếu

            _context.PhieuGiamGias.Add(model);
            await _context.SaveChangesAsync();

            // Phân phối cho tất cả khách hàng đang hoạt động
            var allCustomers = await _context.KhachHang
                .Where(kh => kh.TrangThai)
                .ToListAsync();

            var khachHangPhieuGiamList = new List<KhachHangPhieuGiam>();

            foreach (var customer in allCustomers)
            {
                var khachHangPhieuGiam = new KhachHangPhieuGiam
                {
                    IDKhachHangPhieuGiam = Guid.NewGuid(),
                    MaKhachHangPhieuGiam =
                        $"KHPG_{DateTime.Now:yyyyMMddHHmmss}_{customer.IDKhachHang.ToString().Substring(0, 8)}",

                    IDKhachHang = customer.IDKhachHang,
                    IDPhieuGiamGia = model.IDPhieuGiamGia,
                    SoLuong = 1,
                    SoLuongDaSuDung = 0,
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = nguoiTao ?? "System",
                    LanCapNhatCuoi = null,
                    NguoiCapNhat = null,
                    TrangThai = true
                };

                khachHangPhieuGiamList.Add(khachHangPhieuGiam);
            }

            if (khachHangPhieuGiamList.Any())
            {
                _context.KhachHangPhieuGiams.AddRange(khachHangPhieuGiamList);
                await _context.SaveChangesAsync();
            }

            return model;
        }
        public class UpdatePayload
        {
            public PhieuGiamGia Phieu { get; set; }
            public Guid? KhachHangId { get; set; }
        }
        public async Task<bool> UpdateAsync(Guid id, UpdatePayload payload, string? nguoiCapNhat)
        {
            var model = payload.Phieu;

            if (id != model.IDPhieuGiamGia)
                throw new ArgumentException("ID không khớp.");

            // Luôn set là công khai và mỗi khách hàng 1 phiếu
            model.LaCongKhai = true;
            model.SoLuong = 1;

            _context.Entry(model).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // Lấy tất cả khách hàng đang hoạt động
                var allCustomers = await _context.KhachHang
                    .Where(kh => kh.TrangThai)
                    .ToListAsync();

                // Xóa toàn bộ liên kết hiện tại
                var existingLinks = await _context.KhachHangPhieuGiams
                    .Where(x => x.IDPhieuGiamGia == id)
                    .ToListAsync();

                if (existingLinks.Any())
                {
                    _context.KhachHangPhieuGiams.RemoveRange(existingLinks);
                    await _context.SaveChangesAsync();
                }

                // Tạo lại liên kết cho tất cả khách hàng
                var khachHangPhieuGiamList = new List<KhachHangPhieuGiam>();

                foreach (var customer in allCustomers)
                {
                    var khachHangPhieuGiam = new KhachHangPhieuGiam
                    {
                        IDKhachHangPhieuGiam = Guid.NewGuid(),
                        MaKhachHangPhieuGiam =
                            $"KHPG_{DateTime.Now:yyyyMMddHHmmss}_{customer.IDKhachHang.ToString().Substring(0, 8)}",

                        IDKhachHang = customer.IDKhachHang,
                        IDPhieuGiamGia = id,
                        SoLuong = 1,
                        SoLuongDaSuDung = 0,
                        NgayTao = DateTime.UtcNow,
                        NguoiTao = nguoiCapNhat ?? "System",
                        LanCapNhatCuoi = null,
                        NguoiCapNhat = null,
                        TrangThai = true
                    };

                    khachHangPhieuGiamList.Add(khachHangPhieuGiam);
                }

                if (khachHangPhieuGiamList.Any())
                {
                    _context.KhachHangPhieuGiams.AddRange(khachHangPhieuGiamList);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.PhieuGiamGias
                    .AnyAsync(x => x.IDPhieuGiamGia == id);

                if (!exists)
                    return false;

                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.PhieuGiamGias.FindAsync(id);
            if (entity == null)
                return false;

            // Xóa liên kết với khách hàng trước
            var customerLinks = await _context.KhachHangPhieuGiams
                .Where(x => x.IDPhieuGiamGia == id)
                .ToListAsync();

            if (customerLinks.Any())
                _context.KhachHangPhieuGiams.RemoveRange(customerLinks);

            // Xóa phiếu giảm giá
            _context.PhieuGiamGias.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> RemoveCustomerAsync(Guid voucherId, Guid customerId)
        {
            var link = await _context.KhachHangPhieuGiams
                .FirstOrDefaultAsync(x =>
                    x.IDPhieuGiamGia == voucherId &&
                    x.IDKhachHang == customerId);

            if (link == null)
                return false;

            _context.KhachHangPhieuGiams.Remove(link);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<(bool success, string message)> AddCustomerAsync(
            Guid voucherId,
            Guid customerId,
            string? nguoiTao)
        {
            // Kiểm tra phiếu giảm giá
            var voucher = await _context.PhieuGiamGias.FindAsync(voucherId);
            if (voucher == null)
                return (false, "Không tìm thấy phiếu giảm giá.");

            // Kiểm tra khách hàng
            var customer = await _context.KhachHang.FindAsync(customerId);
            if (customer == null)
                return (false, "Không tìm thấy khách hàng.");

            // Kiểm tra liên kết đã tồn tại
            var existingLink = await _context.KhachHangPhieuGiams
                .FirstOrDefaultAsync(x =>
                    x.IDPhieuGiamGia == voucherId &&
                    x.IDKhachHang == customerId);

            if (existingLink != null)
                return (false, "Khách hàng đã được thêm vào phiếu giảm giá này.");

            // Tạo liên kết mới
            var khachHangPhieuGiam = new KhachHangPhieuGiam
            {
                IDKhachHangPhieuGiam = Guid.NewGuid(),
                MaKhachHangPhieuGiam =
                    $"KHPG_{DateTime.Now:yyyyMMddHHmmss}_{customerId.ToString().Substring(0, 8)}",

                IDKhachHang = customerId,
                IDPhieuGiamGia = voucherId,
                SoLuong = voucher.SoLuong,
                SoLuongDaSuDung = 0,
                NgayTao = DateTime.UtcNow,
                NguoiTao = nguoiTao ?? "System",
                LanCapNhatCuoi = null,
                NguoiCapNhat = null,
                TrangThai = true
            };

            _context.KhachHangPhieuGiams.Add(khachHangPhieuGiam);
            await _context.SaveChangesAsync();

            return (true, "Đã thêm khách hàng vào phiếu giảm giá thành công.");
        }

        public async Task<object?> GetVoucherCustomersAsync(Guid voucherId)
        {
            var voucher = await _context.PhieuGiamGias.FindAsync(voucherId);
            if (voucher == null)
                return null;

            var customersQuery = _context.KhachHangPhieuGiams
                .Include(x => x.KhachHang)
                .Where(x => x.IDPhieuGiamGia == voucherId && x.TrangThai);

            var customers = await customersQuery
                .Select(x => new
                {
                    id = x.IDKhachHang,
                    maKhachHang = x.KhachHang.MaKhachHang,
                    tenKhachHang = x.KhachHang.TenKhachHang,
                    email = x.KhachHang.Email,
                    soDienThoai = x.KhachHang.SoDienThoai,
                    soLuong = x.SoLuong,
                    soLuongDaSuDung = x.SoLuongDaSuDung,
                    soLuongConLai = x.SoLuong - x.SoLuongDaSuDung,
                    ngayTao = x.NgayTao,
                    nguoiTao = x.NguoiTao,
                    trangThai = x.TrangThai
                })
                .ToListAsync();

            return new
            {
                isPublic = voucher.LaCongKhai,
                totalCustomers = customers.Count,
                customers = customers
            };
        }
        public async Task<(bool success, string message, object? data)> UseVoucherAsync(
    Guid voucherId,
    Guid customerId,
    string? nguoiCapNhat)
        {
            var customerVoucher = await _context.KhachHangPhieuGiams
                .Include(x => x.KhachHang)
                .Include(x => x.PhieuGiamGia)
                .FirstOrDefaultAsync(x =>
                    x.IDPhieuGiamGia == voucherId &&
                    x.IDKhachHang == customerId);

            if (customerVoucher == null)
                return (false, "Không tìm thấy phiếu giảm giá cho khách hàng này.", null);

            if (!customerVoucher.TrangThai)
                return (false, "Phiếu giảm giá này đã bị vô hiệu hóa.", null);

            if (customerVoucher.SoLuongDaSuDung >= customerVoucher.SoLuong)
                return (false, "Phiếu giảm giá này đã được sử dụng hết.", null);

            var now = DateTime.UtcNow;

            if (now < customerVoucher.PhieuGiamGia.NgayBatDau ||
                now > customerVoucher.PhieuGiamGia.NgayKetThuc)
            {
                return (false, "Phiếu giảm giá này không còn hiệu lực.", null);
            }

            customerVoucher.SoLuongDaSuDung++;
            customerVoucher.LanCapNhatCuoi = now;
            customerVoucher.NguoiCapNhat = nguoiCapNhat ?? "System";

            if (customerVoucher.SoLuongDaSuDung >= customerVoucher.SoLuong)
            {
                customerVoucher.TrangThai = false;
            }

            await _context.SaveChangesAsync();

            var resultData = new
            {
                soLuongConLai = customerVoucher.SoLuong - customerVoucher.SoLuongDaSuDung,
                daSuDungHet = customerVoucher.SoLuongDaSuDung >= customerVoucher.SoLuong,
                isPublic = customerVoucher.PhieuGiamGia.LaCongKhai
            };

            var message = customerVoucher.PhieuGiamGia.LaCongKhai
                ? "Sử dụng phiếu giảm giá công khai thành công."
                : "Sử dụng phiếu giảm giá thành công.";

            return (true, message, resultData);
        }

        public async Task<bool> ResetVoucherUsageAsync(
            Guid voucherId,
            Guid customerId,
            string? nguoiCapNhat)
        {
            var customerVoucher = await _context.KhachHangPhieuGiams
                .FirstOrDefaultAsync(x =>
                    x.IDPhieuGiamGia == voucherId &&
                    x.IDKhachHang == customerId);

            if (customerVoucher == null)
                return false;

            customerVoucher.SoLuongDaSuDung = 0;
            customerVoucher.TrangThai = true;
            customerVoucher.LanCapNhatCuoi = DateTime.UtcNow;
            customerVoucher.NguoiCapNhat = nguoiCapNhat ?? "System";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<object> CheckVoucherCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new
                {
                    success = false,
                    message = "Mã giảm giá không được để trống."
                };
            }

            var phieuGiamGia = await _context.PhieuGiamGias
                .FirstOrDefaultAsync(pgg =>
                    pgg.MaCode == code &&
                    pgg.TrangThai);

            if (phieuGiamGia == null)
            {
                return new
                {
                    success = false,
                    message = "Mã giảm giá không tồn tại hoặc đã bị vô hiệu hóa."
                };
            }

            if (phieuGiamGia.NgayBatDau > DateTime.UtcNow)
            {
                return new
                {
                    success = false,
                    message = "Mã giảm giá chưa có hiệu lực."
                };
            }

            if (phieuGiamGia.NgayKetThuc < DateTime.UtcNow)
            {
                return new
                {
                    success = false,
                    message = "Mã giảm giá đã hết hạn."
                };
            }

            decimal tienGiam = phieuGiamGia.GiaTriGiam;

            if (phieuGiamGia.GiaTriGiamToiDa.HasValue &&
                tienGiam > phieuGiamGia.GiaTriGiamToiDa.Value)
            {
                tienGiam = phieuGiamGia.GiaTriGiamToiDa.Value;
            }

            return new
            {
                success = true,
                message = "Mã giảm giá hợp lệ.",
                tienGiam = tienGiam,
                phanTramGiam = phieuGiamGia.GiaTriGiam,
                giaTriToiDa = phieuGiamGia.GiaTriGiamToiDa,
                donToiThieu = phieuGiamGia.DonToiThieu
            };
        }

    }
}
