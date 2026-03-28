namespace QuanView.Models
{
    public class BannerLinkProductListVm
    {
        public int BannerId { get; set; }
        public string BannerTitle { get; set; } = string.Empty;
        public List<BannerLinkProductItemVm> Products { get; set; } = new();
    }

    public class BannerLinkProductItemVm
    {
        public Guid ProductId { get; set; }
        public Guid? DefaultVariantId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public decimal? MinPrice { get; set; }
    }
}
