using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CauHinhBanHangController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly ILoyaltyService _loyaltyService;
        private readonly IShippingPolicyService _shippingPolicyService;
        private readonly IShippingService _shippingService;

        public CauHinhBanHangController(
            BanQuanAu1DbContext context,
            ILoyaltyService loyaltyService,
            IShippingPolicyService shippingPolicyService,
            IShippingService shippingService)
        {
            _context = context;
            _loyaltyService = loyaltyService;
            _shippingPolicyService = shippingPolicyService;
            _shippingService = shippingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetConfig()
        {
            try
            {
                var config = await _loyaltyService.GetActiveConfigAsync();
                var tiers = await _context.HangKhachHangs
                    .Where(x => x.TrangThai)
                    .OrderBy(x => x.DiemTu)
                    .ToListAsync();

                return Ok(new { config, tiers });
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                if (message.Contains("column", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("42703", StringComparison.OrdinalIgnoreCase))
                {
                    message = "Database chua duoc cap nhat schema phi ship moi. Hay chay: dotnet ef database update --project QuanApi --startup-project QuanApi";
                }

                return StatusCode(500, new { message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateConfig([FromBody] UpdateCauHinhBanHangDto dto)
        {
            var config = await _context.CauHinhBanHangs
                .OrderByDescending(x => x.NgayTao)
                .FirstOrDefaultAsync(x => x.TrangThai);

            if (config == null)
            {
                config = new CauHinhBanHang
                {
                    IDCauHinhBanHang = Guid.NewGuid(),
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = dto.NguoiCapNhat ?? "Admin",
                    TrangThai = true
                };
                _context.CauHinhBanHangs.Add(config);
            }

            config.PhiShipNoiThanh = Math.Max(dto.PhiShipNoiThanh, 0);
            config.PhiShipNgoaiThanh = Math.Max(dto.PhiShipNgoaiThanh, 0);
            config.PhiShipToanQuoc = Math.Max(dto.PhiShipToanQuoc, 0);
            config.PhiShipMacDinh = Math.Max(
                dto.PhiShipMacDinh > 0 ? dto.PhiShipMacDinh : dto.PhiShipToanQuoc,
                0);
            config.TinhApDungPhiShip = string.IsNullOrWhiteSpace(dto.TinhApDungPhiShip)
                ? "Hà Nội"
                : dto.TinhApDungPhiShip.Trim();
            config.DanhSachQuanHuyenNoiThanh = string.IsNullOrWhiteSpace(dto.DanhSachQuanHuyenNoiThanh)
                ? null
                : dto.DanhSachQuanHuyenNoiThanh.Trim();
            config.NguonTinhPhiShipMacDinh = ShippingFeeSources.Config;
            config.SoTienTrenMotDiemTich = Math.Max(dto.SoTienTrenMotDiemTich, 1);
            config.SoTienGiamTrenMotDiem = Math.Max(dto.SoTienGiamTrenMotDiem, 1);
            config.DiemToiDaSuDungMoiDon = Math.Max(dto.DiemToiDaSuDungMoiDon, 0);
            config.LanCapNhatCuoi = DateTime.UtcNow;
            config.NguoiCapNhat = dto.NguoiCapNhat ?? "Admin";

            await _context.SaveChangesAsync();
            return Ok(config);
        }

        [HttpPost("tiers")]
        public async Task<IActionResult> CreateTier([FromBody] HangKhachHang dto)
        {
            dto.IDHangKhachHang = Guid.NewGuid();
            dto.NgayTao = DateTime.UtcNow;
            dto.TrangThai = true;
            _context.HangKhachHangs.Add(dto);
            await _context.SaveChangesAsync();
            return Ok(dto);
        }

        [HttpPut("tiers/{id}")]
        public async Task<IActionResult> UpdateTier(Guid id, [FromBody] HangKhachHang dto)
        {
            var tier = await _context.HangKhachHangs.FirstOrDefaultAsync(x => x.IDHangKhachHang == id);
            if (tier == null)
            {
                return NotFound();
            }

            tier.MaHang = dto.MaHang;
            tier.TenHang = dto.TenHang;
            tier.DiemTu = Math.Max(dto.DiemTu, 0);
            tier.DiemDen = dto.DiemDen;
            tier.PhanTramGiamPhiShip = Math.Clamp(dto.PhanTramGiamPhiShip, 0, 100);
            tier.TrangThai = dto.TrangThai;
            tier.LanCapNhatCuoi = DateTime.UtcNow;
            tier.NguoiCapNhat = dto.NguoiCapNhat ?? "Admin";

            await _context.SaveChangesAsync();
            return Ok(tier);
        }

        [HttpDelete("tiers/{id}")]
        public async Task<IActionResult> DeleteTier(Guid id)
        {
            var tier = await _context.HangKhachHangs.FirstOrDefaultAsync(x => x.IDHangKhachHang == id);
            if (tier == null)
            {
                return NotFound();
            }

            tier.TrangThai = false;
            tier.LanCapNhatCuoi = DateTime.UtcNow;
            tier.NguoiCapNhat = "Admin";
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] PreviewTinhToanDto dto)
        {
            var loyalty = await _loyaltyService.BuildCheckoutResultAsync(dto.CustomerId, dto.TienHang, dto.UsePoint);
            var policy = await _shippingPolicyService.ResolveCustomerDiscountAsync(dto.CustomerId);

            var shippingOriginal = dto.PhiShip;
            if (shippingOriginal <= 0 && dto.UseDefaultShipping)
            {
                shippingOriginal = await _shippingPolicyService.ResolveDefaultShippingFeeAsync();
            }

            var shippingFinal = _shippingService.ApplyShippingDiscount(shippingOriginal, policy.Percent);

            return Ok(new
            {
                tienHang = dto.TienHang,
                diemHienTai = loyalty.AvailablePoints,
                diemSuDung = loyalty.UsedPoints,
                tienGiamTuDiem = loyalty.DiscountFromPoints,
                diemCong = loyalty.EarnedPoints,
                phiShipGoc = shippingOriginal,
                phanTramGiamShip = policy.Percent,
                phiShipSauGiam = shippingFinal,
                tongThanhToan = Math.Max(dto.TienHang - loyalty.DiscountFromPoints, 0) + shippingFinal,
                thongDiepGiamShip = policy.Message
            });
        }
    }

    public class UpdateCauHinhBanHangDto
    {
        public decimal PhiShipMacDinh { get; set; }
        public decimal PhiShipNoiThanh { get; set; }
        public decimal PhiShipNgoaiThanh { get; set; }
        public decimal PhiShipToanQuoc { get; set; }
        public string? TinhApDungPhiShip { get; set; }
        public string? DanhSachQuanHuyenNoiThanh { get; set; }
        public string? NguonTinhPhiShipMacDinh { get; set; }
        public decimal SoTienTrenMotDiemTich { get; set; }
        public decimal SoTienGiamTrenMotDiem { get; set; }
        public int DiemToiDaSuDungMoiDon { get; set; }
        public string? NguoiCapNhat { get; set; }
    }

    public class PreviewTinhToanDto
    {
        public Guid? CustomerId { get; set; }
        public decimal TienHang { get; set; }
        public decimal PhiShip { get; set; }
        public bool UsePoint { get; set; }
        public bool UseDefaultShipping { get; set; } = true;
    }
}
