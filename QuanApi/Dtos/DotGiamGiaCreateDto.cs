using QuanApi.Data;

namespace QuanApi.Dtos
{
    public class DotGiamGiaCreateDto
    {
        public DotGiamGia Dot { get; set; }
        public List<Guid> ChiTietIds { get; set; }
    }
}
