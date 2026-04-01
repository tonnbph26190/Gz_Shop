using System;

namespace QuanApi.Dtos
{
    public class ChuyenGioHangThanhHoaDonDto
    {
        public Guid IDGioHang { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? Address { get; set; }
        public string? DiscountCode { get; set; }
        public bool UsePoint { get; set; }
        public int? RequestedUsedPoints { get; set; }
        public bool Shipping { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? CustomerPaid { get; set; }
        public decimal? ShippingFee { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public int? ToDistrictId { get; set; }
        public string? ToWardCode { get; set; }
        public int? Weight { get; set; }
        public string? ShippingFeeSource { get; set; }
    }
}
