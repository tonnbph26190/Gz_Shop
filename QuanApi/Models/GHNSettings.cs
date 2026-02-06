namespace QuanApi.Models
{
    public class GHNSettings
    {
        public const string SectionName = "GHN";
        public string Token { get; set; } = "";
        public int ShopId { get; set; }
        public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api";
        public int? FromDistrictId { get; set; }
        public string? FromWardCode { get; set; }
        public int DefaultWeight { get; set; } = 500;
        public int DefaultLength { get; set; } = 20;
        public int DefaultWidth { get; set; } = 20;
        public int DefaultHeight { get; set; } = 10;
    }
}
