namespace QuanView.Areas.Admin.Models
{
    public class BannerSanPhamVm
    {
        public int BannerId { get; set; }
        public string BannerTitle { get; set; } = string.Empty;
        public string ProductLink { get; set; } = string.Empty;
        public string? Keyword { get; set; }
        public List<BannerProductItemVm> AssignedProducts { get; set; } = new();
        public List<BannerProductItemVm> AvailableProducts { get; set; } = new();
    }

    public class BannerProductItemVm
    {
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal? MinPrice { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}
