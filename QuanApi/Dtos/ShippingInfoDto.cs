namespace QuanApi.Dtos
{
    public class ShippingInfoDto
    {
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public decimal OriginalFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalFee { get; set; }
        public string DiscountMessage { get; set; } = string.Empty;
        public int EstimatedDeliveryDays { get; set; }
    }

    public class CalculateShippingRequest
    {
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public decimal OrderValue { get; set; }
        /// <summary>GHN: ID quận/huyện đích. Nếu gửi kèm ToWardCode sẽ gọi API GHN tính phí.</summary>
        public int? ToDistrictId { get; set; }
        /// <summary>GHN: Mã phường/xã đích.</summary>
        public string? ToWardCode { get; set; }
        /// <summary>Trọng lượng (gram). Mặc định 500 nếu không gửi.</summary>
        public int? Weight { get; set; }
    }
}
