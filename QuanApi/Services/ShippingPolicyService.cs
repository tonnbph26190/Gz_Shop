using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public class ShippingDiscountResult
    {
        public decimal Percent { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public interface IShippingPolicyService
    {
        Task<ShippingDiscountResult> ResolveCustomerDiscountAsync(Guid? customerId);
        Task<decimal> ResolveDefaultShippingFeeAsync();
        Task<decimal> ResolveGhnFallbackShippingFeeAsync(CauHinhBanHang? config = null);
        Task<CauHinhBanHang?> GetActiveShippingConfigAsync();
        string ResolveShippingFeeSourceFromConfig(CauHinhBanHang? config = null);
        string ResolveShippingFeeSource(string? requestedSource, CauHinhBanHang? config = null);
    }

    public class ShippingPolicyService : IShippingPolicyService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly ILoyaltyService _loyaltyService;

        public ShippingPolicyService(BanQuanAu1DbContext context, ILoyaltyService loyaltyService)
        {
            _context = context;
            _loyaltyService = loyaltyService;
        }

        public async Task<ShippingDiscountResult> ResolveCustomerDiscountAsync(Guid? customerId)
        {
            if (!customerId.HasValue)
            {
                return new ShippingDiscountResult();
            }

            await _loyaltyService.ResolveAndPersistTierAsync(customerId.Value);

            var customer = await _context.KhachHang
                .Include(x => x.HangKhachHang)
                .FirstOrDefaultAsync(x => x.IDKhachHang == customerId.Value && x.TrangThai);

            if (customer?.HangKhachHang == null || !customer.HangKhachHang.TrangThai)
            {
                return new ShippingDiscountResult();
            }

            var percent = Math.Clamp(customer.HangKhachHang.PhanTramGiamPhiShip, 0, 100);
            if (percent <= 0)
            {
                return new ShippingDiscountResult();
            }

            return new ShippingDiscountResult
            {
                Percent = percent,
                Message = $"Hạng {customer.HangKhachHang.TenHang}: giảm {percent:0.#}% phí vận chuyển"
            };
        }

        public async Task<decimal> ResolveDefaultShippingFeeAsync()
        {
            var config = await GetActiveShippingConfigAsync();

            return config?.PhiShipToanQuoc
                ?? config?.PhiShipMacDinh
                ?? 50000;
        }

        public async Task<decimal> ResolveGhnFallbackShippingFeeAsync(CauHinhBanHang? config = null)
        {
            config ??= await GetActiveShippingConfigAsync();
            return Math.Max(config?.PhiShipMacDinh ?? 50000, 0);
        }

        public async Task<CauHinhBanHang?> GetActiveShippingConfigAsync()
        {
            return await _context.CauHinhBanHangs
                .OrderByDescending(x => x.NgayTao)
                .FirstOrDefaultAsync(x => x.TrangThai);
        }

        public string ResolveShippingFeeSource(string? requestedSource, CauHinhBanHang? config = null)
        {
            if (!string.IsNullOrWhiteSpace(requestedSource))
            {
                return ShippingFeeSources.Normalize(requestedSource);
            }

            return ResolveShippingFeeSourceFromConfig(config);
        }

        public string ResolveShippingFeeSourceFromConfig(CauHinhBanHang? config = null)
        {
            return ShippingFeeSources.Normalize(config?.NguonTinhPhiShipMacDinh);
        }
    }
}
