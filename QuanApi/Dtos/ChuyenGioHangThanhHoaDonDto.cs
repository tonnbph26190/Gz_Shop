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
        public bool Shipping { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? CustomerPaid { get; set; }
        public decimal? ShippingFee { get; set; }
    }
}
