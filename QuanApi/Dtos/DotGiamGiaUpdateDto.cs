namespace QuanApi.Dtos
{
    public class DotGiamGiaUpdateDto
    {
        public Guid IDDotGiamGia { get; set; }
        public string MaDot { get; set; }
        public string TenDot { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public int PhanTramGiam { get; set; }
        public List<Guid> SanPhamChiTietIds { get; set; }
    }

}
