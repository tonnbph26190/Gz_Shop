using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public class OrderHistoryService : IOrderHistoryService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly ILogger<OrderHistoryService> _logger;
        private readonly IInventoryReservationService _inventoryReservationService;

        // Định nghĩa luồng trạng thái hợp lệ
        private readonly Dictionary<string, List<string>> _statusFlow = new()
        {
            ["Chờ xác nhận"] = new List<string> { "Đã xác nhận", "Đã hủy" },
            ["Đã xác nhận"] = new List<string> { "Chờ lấy hàng", "Đã hủy" },
            ["Chờ lấy hàng"] = new List<string> { "Đang giao", "Đã hủy" },
            ["Đang giao"] = new List<string> { "Đã giao", "Đã hủy" },
            ["Đã giao"] = new List<string> { "Đã hủy" },
            ["Đã lấy hàng"] = new List<string> { "Đang giao", "Đã hủy" },
            ["Chờ giao hàng"] = new List<string> { "Đang giao", "Đã hủy" },
            ["Đang giao hàng"] = new List<string> { "Đã giao", "Đã hủy" },
            ["Giao hàng thành công"] = new List<string>(), // Không thể chuyển sang trạng thái khác
            ["Đã hủy"] = new List<string>() // Không thể chuyển sang trạng thái khác
        };

        // Định nghĩa các trạng thái có thể rollback
        private readonly Dictionary<string, List<string>> _rollbackFlow = new()
        {
            ["Đã xác nhận"] = new List<string> { "Chờ xác nhận" },
            ["Chờ lấy hàng"] = new List<string> { "Đã xác nhận" },
            ["Đang giao"] = new List<string> { "Chờ lấy hàng" },
            ["Đã giao"] = new List<string> { "Đang giao" },
            ["Đã lấy hàng"] = new List<string> { "Chờ lấy hàng" },
            ["Chờ giao hàng"] = new List<string> { "Đã lấy hàng" },
            ["Đang giao hàng"] = new List<string> { "Chờ giao hàng" },
            ["Giao hàng thành công"] = new List<string> { "Đã giao" },
            ["Đã hủy"] = new List<string>() // Không cho phép rollback từ trạng thái đã hủy
        };

        public OrderHistoryService(
            BanQuanAu1DbContext context,
            ILogger<OrderHistoryService> logger,
            IInventoryReservationService inventoryReservationService)
        {
            _context = context;
            _logger = logger;
            _inventoryReservationService = inventoryReservationService;
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
            await Task.CompletedTask;
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
            var lines = (order.ChiTietHoaDons ?? new List<ChiTietHoaDon>())
                .Select(x => new InventoryLine(x.IDSanPhamChiTiet, x.SoLuong))
                .ToList();

            if (targetStatus == "Đã hủy" && oldStatus != "Đã hủy")
            {
                if (order.DaDatChoTonKho)
                {
                    var releaseResult = await _inventoryReservationService.ReleaseAsync(lines, "Rollback");
                    if (!releaseResult.Success)
                        throw new InvalidOperationException(releaseResult.ErrorMessage ?? "Không thể nhả giữ chỗ khi rollback.");
                }

                if (order.DaTruTonKho)
                {
                    var restockResult = await _inventoryReservationService.RestockAsync(lines, "Rollback");
                    if (!restockResult.Success)
                        throw new InvalidOperationException(restockResult.ErrorMessage ?? "Không thể hoàn kho khi rollback.");
                }

                order.DaDatChoTonKho = false;
                order.DaTruTonKho = false;
            }
            else if (oldStatus == "Đã xác nhận" && targetStatus == "Chờ xác nhận")
            {
                if (order.DaTruTonKho)
                {
                    var restockResult = await _inventoryReservationService.RestockAsync(lines, "Rollback");
                    if (!restockResult.Success)
                        throw new InvalidOperationException(restockResult.ErrorMessage ?? "Không thể hoàn kho khi rollback về chờ xác nhận.");
                }

                var reserveResult = await _inventoryReservationService.ReserveAsync(lines, "Rollback");
                if (!reserveResult.Success)
                    throw new InvalidOperationException(reserveResult.ErrorMessage ?? "Không thể đặt chỗ lại tồn kho khi rollback.");

                order.DaDatChoTonKho = true;
                order.DaTruTonKho = false;
            }

            await Task.CompletedTask;
        }
    }
}
