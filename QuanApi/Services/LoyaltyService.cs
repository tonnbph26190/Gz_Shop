using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public class LoyaltyCheckoutResult
    {
        public int AvailablePoints { get; set; }
        public int UsedPoints { get; set; }
        public decimal DiscountFromPoints { get; set; }
        public int EarnedPoints { get; set; }
        public decimal NetAmountForEarning { get; set; }
    }

    public interface ILoyaltyService
    {
        Task<CauHinhBanHang> GetActiveConfigAsync();
        Task<LoyaltyCheckoutResult> BuildCheckoutResultAsync(Guid? customerId, decimal merchandiseAmount, bool usePoint, int? requestedUsedPoints = null);
        Task<int> ApplyCheckoutPointChangesAsync(Guid customerId, Guid? orderId, LoyaltyCheckoutResult result, string actor);
        Task<int> ResolveAndPersistTierAsync(Guid customerId);
    }

    public class LoyaltyService : ILoyaltyService
    {
        private readonly BanQuanAu1DbContext _context;

        public LoyaltyService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<CauHinhBanHang> GetActiveConfigAsync()
        {
            var config = await _context.CauHinhBanHangs
                .OrderByDescending(x => x.NgayTao)
                .FirstOrDefaultAsync(x => x.TrangThai);

            return config ?? new CauHinhBanHang();
        }

        public async Task<LoyaltyCheckoutResult> BuildCheckoutResultAsync(Guid? customerId, decimal merchandiseAmount, bool usePoint, int? requestedUsedPoints = null)
        {
            var config = await GetActiveConfigAsync();
            var result = new LoyaltyCheckoutResult();

            if (!customerId.HasValue)
            {
                return result;
            }

            var customer = await _context.KhachHang.FirstOrDefaultAsync(x => x.IDKhachHang == customerId.Value && x.TrangThai);
            if (customer == null)
            {
                return result;
            }

            result.AvailablePoints = Math.Max(customer.SoDiemHienTai, 0);

            if (usePoint && result.AvailablePoints > 0)
            {
                var maxRedeemByConfig = config.DiemToiDaSuDungMoiDon > 0
                    ? config.DiemToiDaSuDungMoiDon
                    : result.AvailablePoints;

                var maxRedeemByAmount = (int)Math.Floor(merchandiseAmount / config.SoTienGiamTrenMotDiem);
                var maxAllowedPoints = Math.Min(result.AvailablePoints, Math.Min(maxRedeemByConfig, maxRedeemByAmount));
                var usedPoints = requestedUsedPoints.HasValue
                    ? Math.Min(Math.Max(requestedUsedPoints.Value, 0), maxAllowedPoints)
                    : maxAllowedPoints;

                result.UsedPoints = Math.Max(usedPoints, 0);
                result.DiscountFromPoints = result.UsedPoints * config.SoTienGiamTrenMotDiem;
            }

            result.NetAmountForEarning = Math.Max(merchandiseAmount - result.DiscountFromPoints, 0);
            result.EarnedPoints = (int)Math.Floor(result.NetAmountForEarning / config.SoTienTrenMotDiemTich);

            return result;
        }

        public async Task<int> ApplyCheckoutPointChangesAsync(Guid customerId, Guid? orderId, LoyaltyCheckoutResult result, string actor)
        {
            var customer = await _context.KhachHang.FirstOrDefaultAsync(x => x.IDKhachHang == customerId);
            if (customer == null)
            {
                return 0;
            }

            if (result.UsedPoints > 0)
            {
                var before = customer.SoDiemHienTai;
                customer.SoDiemHienTai = Math.Max(customer.SoDiemHienTai - result.UsedPoints, 0);

                _context.LichSuDiemKhachHangs.Add(new LichSuDiemKhachHang
                {
                    IDKhachHang = customerId,
                    IDHoaDon = orderId,
                    LoaiBienDong = "Tru",
                    SoDiemBienDong = -result.UsedPoints,
                    SoDiemTruoc = before,
                    SoDiemSau = customer.SoDiemHienTai,
                    MoTa = "Trừ điểm khi thanh toán hóa đơn",
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = actor,
                    TrangThai = true
                });
            }

            if (result.EarnedPoints > 0)
            {
                var before = customer.SoDiemHienTai;
                customer.SoDiemHienTai += result.EarnedPoints;
                customer.TongDiemTichLuy += result.EarnedPoints;

                _context.LichSuDiemKhachHangs.Add(new LichSuDiemKhachHang
                {
                    IDKhachHang = customerId,
                    IDHoaDon = orderId,
                    LoaiBienDong = "Cong",
                    SoDiemBienDong = result.EarnedPoints,
                    SoDiemTruoc = before,
                    SoDiemSau = customer.SoDiemHienTai,
                    MoTa = "Cộng điểm sau khi thanh toán hóa đơn",
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = actor,
                    TrangThai = true
                });
            }

            await ResolveAndPersistTierAsync(customerId);
            return customer.SoDiemHienTai;
        }

        public async Task<int> ResolveAndPersistTierAsync(Guid customerId)
        {
            var customer = await _context.KhachHang.FirstOrDefaultAsync(x => x.IDKhachHang == customerId);
            if (customer == null)
            {
                return 0;
            }

            var point = Math.Max(customer.SoDiemHienTai, 0);
            var tier = await _context.HangKhachHangs
                .Where(x => x.TrangThai && x.DiemTu <= point && (!x.DiemDen.HasValue || x.DiemDen.Value >= point))
                .OrderByDescending(x => x.DiemTu)
                .FirstOrDefaultAsync();

            customer.IDHangKhachHang = tier?.IDHangKhachHang;
            return point;
        }
    }
}
