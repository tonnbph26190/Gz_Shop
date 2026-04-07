using AutoMapper;
using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanApi.Services
{
    public interface IKhachHangService
    {
        Task<(IEnumerable<KhachHangDto> Data, int TotalCount)> GetKhachHangAsync(
           string? search,
           int pageNumber,
           int pageSize,
           string? sortBy,
           bool sortAscending
         );
        Task<KhachHang?> GetKhachHangByIdAsync(Guid id);
        Task<KhachHang> CreateKhachHangAsync(CreateKhachHangDto dto, string? currentUser);
        Task<bool> UpdateAsync(Guid id, UpdateKhachHangDto dto, string? currentUser);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<IEnumerable<DiaChi>> GetAddressesAsync(Guid id);
        Task<DiaChi?> GetDefaultAddressAsync(Guid customerId);
    }
    public class KhachHangService : IKhachHangService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly ILogger<KhachHangService> _logger;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;

        public KhachHangService(BanQuanAu1DbContext context, ILogger<KhachHangService> logger, IMapper mapper,
        IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _emailService = emailService;
        }

		public async Task<(IEnumerable<KhachHangDto> Data, int TotalCount)> GetKhachHangAsync(
	string? search,
	int pageNumber,
	int pageSize,
	string? sortBy,
	bool sortAscending)
		{
			_logger.LogInformation("Đang lấy danh sách khách hàng...");

			var query = _context.KhachHang
				.Include(kh => kh.DiaChis)
				.AsQueryable();

			// 🔍 Search
			if (!string.IsNullOrEmpty(search))
			{
				query = query.Where(kh =>
					kh.MaKhachHang.Contains(search) ||
					kh.TenKhachHang.Contains(search) ||
					(kh.Email != null && kh.Email.Contains(search)) ||
					kh.SoDienThoai.Contains(search));
			}

			// 🔽 Sort (🔥 mặc định = điểm cao → thấp)
			switch (sortBy?.ToLower())
			{
				case "makhachhang":
					query = sortAscending
						? query.OrderBy(kh => kh.MaKhachHang)
						: query.OrderByDescending(kh => kh.MaKhachHang);
					break;

				case "tenkhachhang":
					query = sortAscending
						? query.OrderBy(kh => kh.TenKhachHang)
						: query.OrderByDescending(kh => kh.TenKhachHang);
					break;

				case "ngaytao":
					query = sortAscending
						? query.OrderBy(kh => kh.NgayTao)
						: query.OrderByDescending(kh => kh.NgayTao);
					break;

				case "rank": // 🔥 thực chất là sort theo điểm
					query = sortAscending
						? query.OrderBy(kh => kh.TongDiemTichLuy)
						: query.OrderByDescending(kh => kh.TongDiemTichLuy);
					break;

				default:
					// 🔥 AUTO: không bấm vẫn xếp theo điểm
					query = query.OrderByDescending(kh => kh.TongDiemTichLuy);
					break;
			}

			var totalCount = await query.CountAsync();

			// 🚀 Data + Rank
			var data = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.Select(kh => new KhachHangDto
				{
					IDKhachHang = kh.IDKhachHang,
					MaKhachHang = kh.MaKhachHang,
					TenKhachHang = kh.TenKhachHang,
					Email = kh.Email,
					SoDienThoai = kh.SoDienThoai,
					SoDiemHienTai = kh.SoDiemHienTai,
					TongDiemTichLuy = kh.TongDiemTichLuy,
					NgayTao = kh.NgayTao,
					NguoiTao = kh.NguoiTao,
					TrangThai = kh.TrangThai,

					// 🔥 TÍNH RANK CHUẨN
					Rank = _context.HangKhachHangs
						.Where(h => h.TrangThai == true
							&& kh.TongDiemTichLuy >= h.DiemTu
							&& (h.DiemDen == null || kh.TongDiemTichLuy <= h.DiemDen))
						.Select(h => h.TenHang)
						.FirstOrDefault()
				})
				.ToListAsync();

			return (data, totalCount);
		}
		public async Task<KhachHang?> GetKhachHangByIdAsync(Guid id)
        {
            _logger.LogInformation("Đang lấy chi tiết khách hàng với ID: {CustomerId}", id);

            return await _context.KhachHang
                .Include(kh => kh.DiaChis)
                .FirstOrDefaultAsync(kh => kh.IDKhachHang == id);
        }
        public async Task<KhachHang> CreateKhachHangAsync(
       CreateKhachHangDto dto,
       string? currentUser)
        {
            _logger.LogInformation("Đang tạo khách hàng mới với Mã khách hàng: {MaKhachHang}", dto.MaKhachHang);

            // Validate trùng
            if (await _context.KhachHang.AnyAsync(kh => kh.MaKhachHang == dto.MaKhachHang))
                throw new InvalidOperationException("DUPLICATE_MAKHACHHANG");

            if (!string.IsNullOrEmpty(dto.Email) &&
                await _context.KhachHang.AnyAsync(kh => kh.Email == dto.Email))
                throw new InvalidOperationException("DUPLICATE_EMAIL");

            if (!string.IsNullOrEmpty(dto.SoDienThoai) &&
                await _context.KhachHang.AnyAsync(kh => kh.SoDienThoai == dto.SoDienThoai))
                throw new InvalidOperationException("DUPLICATE_PHONE");

            if (string.IsNullOrEmpty(dto.MatKhau))
                throw new InvalidOperationException("PASSWORD_REQUIRED");

            var khachHang = _mapper.Map<KhachHang>(dto);

            khachHang.IDKhachHang = Guid.NewGuid();
            khachHang.NgayTao = DateTime.UtcNow;
            khachHang.NguoiTao = currentUser ?? "System";
            khachHang.LanCapNhatCuoi = null;
            khachHang.NguoiCapNhat = null;
            khachHang.TrangThai = true;
            khachHang.MatKhau = dto.MatKhau;

            // ===== Xử lý địa chỉ =====
            if (dto.DiaChis != null && dto.DiaChis.Any())
            {
                var duplicateMaDiaChiInDto = dto.DiaChis
                    .GroupBy(d => d.MaDiaChi)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateMaDiaChiInDto.Any())
                    throw new InvalidOperationException("DUPLICATE_MADIACHI_IN_DTO");

                khachHang.DiaChis = new List<DiaChi>();

                foreach (var diaChiDto in dto.DiaChis)
                {
                    var diaChi = _mapper.Map<DiaChi>(diaChiDto);
                    diaChi.IDDiaChi = Guid.NewGuid();
                    diaChi.IDKhachHang = khachHang.IDKhachHang;
                    diaChi.NgayTao = DateTime.UtcNow;
                    diaChi.NguoiTao = currentUser ?? "System";
                    diaChi.LanCapNhatCuoi = null;
                    diaChi.NguoiCapNhat = null;
                    diaChi.TrangThai = true;

                    khachHang.DiaChis.Add(diaChi);
                }
            }

            _context.KhachHang.Add(khachHang);
            await _context.SaveChangesAsync();

            // ===== Gửi email =====
            if (!string.IsNullOrEmpty(khachHang.Email))
            {
                var subject = "Chào mừng bạn đến với Cửa hàng bán quần âu GZ!";
                var emailBody = new StringBuilder();

                emailBody.AppendLine($"<p>Xin chào <strong>{khachHang.TenKhachHang}</strong>,</p>");
                emailBody.AppendLine("<p>Bạn đã đăng ký tài khoản thành công.</p>");
                emailBody.AppendLine("<ul>");
                emailBody.AppendLine($"<li><strong>Mã khách hàng:</strong> {khachHang.MaKhachHang}</li>");
                emailBody.AppendLine($"<li><strong>Email:</strong> {khachHang.Email}</li>");
                emailBody.AppendLine($"<li><strong>Số điện thoại:</strong> {khachHang.SoDienThoai}</li>");
                emailBody.AppendLine("</ul>");

                try
                {
                    await _emailService.SendEmailAsync(khachHang.Email, subject, emailBody.ToString());
                    _logger.LogInformation("Đã gửi email cho khách hàng: {Email}", khachHang.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi gửi email cho {Email}", khachHang.Email);
                }
            }

            return khachHang;
        }
        public async Task<bool> UpdateAsync(Guid id, UpdateKhachHangDto dto, string? currentUser)
        {
            if (id != dto.IDKhachHang)
                throw new ArgumentException("ID trong URL không khớp với ID khách hàng trong dữ liệu.");

            var originalKhachHang = await _context.KhachHang
                .Include(kh => kh.DiaChis)
                .FirstOrDefaultAsync(kh => kh.IDKhachHang == id);

            if (originalKhachHang == null)
                throw new KeyNotFoundException("Không tìm thấy khách hàng để cập nhật.");

            if (await _context.KhachHang.AnyAsync(kh => kh.MaKhachHang == dto.MaKhachHang && kh.IDKhachHang != id))
                throw new InvalidOperationException("Mã khách hàng đã tồn tại.");

            if (!string.IsNullOrEmpty(dto.Email) &&
                await _context.KhachHang.AnyAsync(kh => kh.Email == dto.Email && kh.IDKhachHang != id))
                throw new InvalidOperationException("Email đã tồn tại.");

            if (!string.IsNullOrEmpty(dto.SoDienThoai) &&
                await _context.KhachHang.AnyAsync(kh => kh.SoDienThoai == dto.SoDienThoai && kh.IDKhachHang != id))
                throw new InvalidOperationException("Số điện thoại đã tồn tại.");

            // Map dữ liệu chính
            _mapper.Map(dto, originalKhachHang);
            originalKhachHang.LanCapNhatCuoi = DateTime.UtcNow;
            originalKhachHang.NguoiCapNhat = currentUser ?? "System";

            if (!string.IsNullOrEmpty(dto.MatKhau))
                originalKhachHang.MatKhau = dto.MatKhau;

            // ==== XỬ LÝ ĐỊA CHỈ ====
            var existingDiaChis = originalKhachHang.DiaChis.ToList();
            var updatedDiaChisDto = dto.DiaChis ?? new List<DiaChiDto>();

            var duplicateMa = updatedDiaChisDto
                .GroupBy(d => d.MaDiaChi)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateMa.Any())
                throw new InvalidOperationException($"Mã địa chỉ bị trùng: {string.Join(", ", duplicateMa)}");

            // Xóa địa chỉ bị remove
            foreach (var existing in existingDiaChis)
            {
                if (!updatedDiaChisDto.Any(d => d.IDDiaChi == existing.IDDiaChi))
                {
                    _context.DiaChis.Remove(existing);
                    _logger.LogInformation("Xóa địa chỉ ID: {DiaChiId}", existing.IDDiaChi);
                }
            }

            // Thêm / cập nhật
            foreach (var diaChiDto in updatedDiaChisDto)
            {
                var existing = existingDiaChis.FirstOrDefault(d => d.IDDiaChi == diaChiDto.IDDiaChi);

                if (existing == null)
                {
                    if (await _context.DiaChis.AnyAsync(d =>
                            d.IDKhachHang == originalKhachHang.IDKhachHang &&
                            d.MaDiaChi == diaChiDto.MaDiaChi))
                    {
                        throw new InvalidOperationException($"Mã địa chỉ '{diaChiDto.MaDiaChi}' đã tồn tại.");
                    }

                    var newDiaChi = _mapper.Map<DiaChi>(diaChiDto);
                    newDiaChi.IDDiaChi = Guid.NewGuid();
                    newDiaChi.IDKhachHang = originalKhachHang.IDKhachHang;
                    newDiaChi.NgayTao = DateTime.UtcNow;
                    newDiaChi.NguoiTao = currentUser ?? "System";
                    newDiaChi.TrangThai = true;

                    _context.DiaChis.Add(newDiaChi);
                    _logger.LogInformation("Thêm địa chỉ mới ID: {DiaChiId}", newDiaChi.IDDiaChi);
                }
                else
                {
                    if (await _context.DiaChis.AnyAsync(d =>
                            d.IDKhachHang == originalKhachHang.IDKhachHang &&
                            d.MaDiaChi == diaChiDto.MaDiaChi &&
                            d.IDDiaChi != diaChiDto.IDDiaChi))
                    {
                        throw new InvalidOperationException($"Mã địa chỉ '{diaChiDto.MaDiaChi}' đã tồn tại cho địa chỉ khác.");
                    }

                    _mapper.Map(diaChiDto, existing);
                    existing.LanCapNhatCuoi = DateTime.UtcNow;
                    existing.NguoiCapNhat = currentUser ?? "System";

                    _logger.LogInformation("Cập nhật địa chỉ ID: {DiaChiId}", existing.IDDiaChi);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Đang xóa khách hàng với ID: {CustomerId}", id);

            var khachHang = await _context.KhachHang
                                          .Include(kh => kh.DiaChis)
                                          .FirstOrDefaultAsync(kh => kh.IDKhachHang == id);

            if (khachHang == null)
            {
                _logger.LogWarning("Không tìm thấy khách hàng với ID: {CustomerId} để xóa.", id);
                throw new KeyNotFoundException("Không tìm thấy khách hàng để xóa.");
            }

            // Nếu có ràng buộc FK (ví dụ: hóa đơn), có thể soft-delete thay vì remove
            _context.KhachHang.Remove(khachHang);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã xóa khách hàng ID: {CustomerId} thành công.", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi cơ sở dữ liệu khi xóa khách hàng ID: {CustomerId}.", id);
                throw; // để controller bắt và trả 500
            }

            return true;
        }
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.KhachHang.AnyAsync(e => e.IDKhachHang == id);
        }

        public async Task<IEnumerable<DiaChi>> GetAddressesAsync(Guid id)
        {
            _logger.LogInformation("Đang lấy danh sách địa chỉ của khách hàng ID: {CustomerId}", id);

            var khachHang = await _context.KhachHang
                .Include(kh => kh.DiaChis)
                .FirstOrDefaultAsync(kh => kh.IDKhachHang == id);

            if (khachHang == null)
            {
                _logger.LogWarning("Không tìm thấy khách hàng với ID: {CustomerId}", id);
                throw new KeyNotFoundException("Không tìm thấy khách hàng.");
            }

            var addresses = khachHang.DiaChis?
                .Where(d => d.TrangThai)
                .ToList() ?? new List<DiaChi>();

            return addresses;
        }

        public async Task<DiaChi?> GetDefaultAddressAsync(Guid customerId)
        {
            _logger.LogInformation("Đang lấy địa chỉ mặc định của khách hàng ID: {CustomerId}", customerId);

            var khachHang = await _context.KhachHang
                .Include(kh => kh.DiaChis)
                .FirstOrDefaultAsync(kh => kh.IDKhachHang == customerId);

            if (khachHang == null)
            {
                _logger.LogWarning("Không tìm thấy khách hàng với ID: {CustomerId}", customerId);
                throw new KeyNotFoundException("Không tìm thấy khách hàng.");
            }

            var defaultAddress = khachHang.DiaChis?
                .FirstOrDefault(d => d.TrangThai && d.LaMacDinh);

            return defaultAddress; // có thể null nếu không có địa chỉ mặc định
        }

    }
}
