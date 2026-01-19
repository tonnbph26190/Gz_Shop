namespace QuanApi.Dtos
{

    public class NhanVienFilterDto : PagedFilterDto
    {
        public string? SearchTerm { get; set; } 
        public Guid? IDVaiTro { get; set; } 
        public bool? TrangThai { get; set; } 
    }
}
