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
    }
}
