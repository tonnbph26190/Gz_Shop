namespace QuanApi.Dtos
{
    public class DotGiamGiaFilterDto
    {
        public string? MaDot { get; set; }
        public string? TenDot { get; set; }
        public int? PhanTramGiam { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string? TrangThai { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
    }
}
