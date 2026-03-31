using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public class OrderHistoryService : IOrderHistoryService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly ILogger<OrderHistoryService> _logger;

        // Định nghĩa luồng trạng thái hợp lệ
        private readonly Dictionary<string, List<string>> _statusFlow = new()
        {
            ["Chờ xác nhận"] = new List<string> { "Đã xác nhận", "Đã hủy" },
            ["Đã xác nhận"] = new List<string> { "Chờ lấy hàng", "Đã hủy" },
            ["Chờ lấy hàng"] = new List<string> { "Đã lấy hàng", "Đã hủy" },
            ["Đã lấy hàng"] = new List<string> { "Chờ giao hàng", "Đã hủy" },
            ["Chờ giao hàng"] = new List<string> { "Đang giao hàng", "Đã hủy" },
            ["Đang giao hàng"] = new List<string> { "Đã giao", "Đã hủy" },
            ["Đã giao"] = new List<string> { "Giao hàng thành công", "Đã hủy" },
            ["Giao hàng thành công"] = new List<string>(), // Không thể chuyển sang trạng thái khác
            ["Đã hủy"] = new List<string>() // Không thể chuyển sang trạng thái khác
        };

        // Định nghĩa các trạng thái có thể rollback
        private readonly Dictionary<string, List<string>> _rollbackFlow = new()
        {
            ["Đã xác nhận"] = new List<string> { "Chờ xác nhận" },
            ["Chờ lấy hàng"] = new List<string> { "Đã xác nhận" },
            ["Đã lấy hàng"] = new List<string> { "Chờ lấy hàng" },
            ["Chờ giao hàng"] = new List<string> { "Đã lấy hàng" },
            ["Đang giao hàng"] = new List<string> { "Chờ giao hàng" },
            ["Đã giao"] = new List<string> { "Đang giao hàng" },
            ["Giao hàng thành công"] = new List<string> { "Đã giao" },
            ["Đã hủy"] = new List<string>() // Không cho phép rollback từ trạng thái đã hủy
        };

        public OrderHistoryService(BanQuanAu1DbContext context, ILogger<OrderHistoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CanRollbackToStatusAsync(Guid orderId, string targetStatus)
        {
            try
            {
                var order = await _context.HoaDons.FindAsync(orderId);
                if (order == null) return false;

                return _rollbackFlow.ContainsKey(order.TrangThai) && 
                       _rollbackFlow[order.TrangThai].Contains(targetStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi kiểm tra khả năng rollback cho đơn hàng {orderId}");
                return false;
            }
        }

        public async Task<List<string>> GetValidRollbackStatusesAsync(Guid orderId, string currentStatus)
        {
            try
            {
                // Lấy danh sách trạng thái có thể rollback theo luồng
                return _rollbackFlow.ContainsKey(currentStatus) 
                    ? _rollbackFlow[currentStatus] 
                    : new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách trạng thái rollback hợp lệ cho đơn hàng {orderId}");
                return new List<string>();
            }
        }

        public async Task<bool> RollbackOrderStatusAsync(Guid orderId, string targetStatus, string reason, string updatedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.HoaDons
                    .Include(h => h.ChiTietHoaDons)
                        .ThenInclude(ct => ct.SanPhamChiTiet)
                    .FirstOrDefaultAsync(h => h.IDHoaDon == orderId);

                if (order == null)
                {
                    _logger.LogWarning($"Không tìm thấy đơn hàng {orderId} để rollback");
                    return false;
                }

                var oldStatus = order.TrangThai;

                // Kiểm tra khả năng rollback
                if (!await CanRollbackToStatusAsync(orderId, targetStatus))
                {
                    _logger.LogWarning($"Không thể rollback đơn hàng {orderId} từ '{oldStatus}' về '{targetStatus}'");
                    return false;
                }

                // Xử lý logic nghiệp vụ khi rollback
                await HandleRollbackBusinessLogicAsync(order, oldStatus, targetStatus);

                // Cập nhật trạng thái đơn hàng
                order.TrangThai = targetStatus;
                order.LanCapNhatCuoi = DateTime.UtcNow;
                order.NguoiCapNhat = updatedBy;

                // Lưu lịch sử rollback
                await SaveOrderHistoryAsync(orderId, oldStatus, targetStatus, updatedBy, $"Rollback: {reason}");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Đã rollback đơn hàng {orderId} từ '{oldStatus}' về '{targetStatus}' bởi {updatedBy}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Lỗi khi rollback đơn hàng {orderId}: {ex.Message}");
                return false;
            }
        }

        public async Task SaveOrderHistoryAsync(Guid orderId, string oldStatus, string newStatus, string updatedBy, string reason = null)
        {
            try
            {
                var history = new LichSuHoaDon
                {
                    IDLichSuHoaDon = Guid.NewGuid(),
                    MaLichSuHoaDon = $"LS{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    IDHoaDon = orderId,
                    TrangThai = newStatus,
                    GhiChu = string.IsNullOrEmpty(reason) 
                        ? $"Thay đổi từ '{oldStatus}' sang '{newStatus}'"
                        : $"Thay đổi từ '{oldStatus}' sang '{newStatus}'. Lý do: {reason}",
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = updatedBy,
                    TrangThaiLichSu = true
                };

                _context.LichSuHoaDons.Add(history);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã lưu lịch sử thay đổi trạng thái cho đơn hàng {orderId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lưu lịch sử đơn hàng {orderId}: {ex.Message}");
            }
        }

        public async Task<List<LichSuHoaDon>> GetOrderHistoryAsync(Guid orderId)
        {
            try
            {
                return await _context.LichSuHoaDons
                    .Where(h => h.IDHoaDon == orderId && h.TrangThaiLichSu)
                    .OrderByDescending(h => h.NgayTao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy lịch sử đơn hàng {orderId}: {ex.Message}");
                return new List<LichSuHoaDon>();
            }
        }

        private async Task HandleRollbackBusinessLogicAsync(HoaDon order, string oldStatus, string targetStatus)
        {
            // Xử lý logic nghiệp vụ khi rollback
            switch (oldStatus)
            {
                case "Đã hủy":
                    // Nếu rollback từ "Đã hủy", cần trừ lại số lượng sản phẩm
                    if (targetStatus != "Đã hủy")
                    {
                        foreach (var chiTiet in order.ChiTietHoaDons ?? new List<ChiTietHoaDon>())
                        {
                            if (chiTiet.SanPhamChiTiet != null)
                            {
                                chiTiet.SanPhamChiTiet.SoLuong -= chiTiet.SoLuong;
                                _logger.LogInformation($"Trừ lại {chiTiet.SoLuong} sản phẩm {chiTiet.SanPhamChiTiet.MaSPChiTiet} khi rollback từ 'Đã hủy'");
                            }
                        }
                    }
                    break;

                case "Đã xác nhận":
                    // Rollback từ "Đã xác nhận" về "Chờ xác nhận" => hoàn trả lại tồn kho
                    if (targetStatus == "Chờ xác nhận")
                    {
                        foreach (var chiTiet in order.ChiTietHoaDons ?? new List<ChiTietHoaDon>())
                        {
                            if (chiTiet.SanPhamChiTiet != null)
                            {
                                var soLuongCu = chiTiet.SanPhamChiTiet.SoLuong;
                                chiTiet.SanPhamChiTiet.SoLuong += chiTiet.SoLuong;
                                _logger.LogInformation(
                                    $"Hoàn trả {chiTiet.SoLuong} sản phẩm {chiTiet.SanPhamChiTiet.MaSPChiTiet} khi rollback từ 'Đã xác nhận' về 'Chờ xác nhận': {soLuongCu} -> {chiTiet.SanPhamChiTiet.SoLuong}");
                            }
                        }
                    }
                    break;

                case "Đã giao hàng":
                    // Rollback từ "Đã giao hàng" có thể cần xử lý hoàn tiền
                    if (targetStatus == "Đang giao hàng")
                    {
                        _logger.LogInformation($"Rollback đơn hàng {order.IDHoaDon} từ 'Đã giao hàng' về 'Đang giao hàng' - có thể cần xử lý hoàn tiền");
                    }
                    break;
            }

            // Xử lý khi rollback VỀ trạng thái "Đã hủy"
            if (targetStatus == "Đã hủy" && oldStatus != "Đã hủy")
            {
                // Hoàn trả số lượng sản phẩm về kho
                foreach (var chiTiet in order.ChiTietHoaDons ?? new List<ChiTietHoaDon>())
                {
                    if (chiTiet.SanPhamChiTiet != null)
                    {
                        chiTiet.SanPhamChiTiet.SoLuong += chiTiet.SoLuong;
                        _logger.LogInformation($"Hoàn trả {chiTiet.SoLuong} sản phẩm {chiTiet.SanPhamChiTiet.MaSPChiTiet} khi rollback về 'Đã hủy'");
                    }
                }
            }

            await Task.CompletedTask;
        }
    }
}
