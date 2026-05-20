namespace QuanApi.Data
{
    public class Banner
    {
        public int Id { get; set; }
        public string? ImageUrl { get; set; }
        public string? Title { get; set; }
        public string? Link { get; set; }
        public virtual ICollection<BannerSanPham>? BannerSanPhams { get; set; }
    }


}
