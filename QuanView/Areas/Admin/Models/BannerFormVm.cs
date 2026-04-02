namespace QuanView.Areas.Admin.Models
{
    public class BannerFormVm
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? ImageUrl { get; set; }
        public string? ProductLink { get; set; }
        public List<Guid> SelectedProductIds { get; set; } = new();
        public List<BannerProductOptionVm> AvailableProducts { get; set; } = new();
    }

    public class BannerProductOptionVm
    {
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
