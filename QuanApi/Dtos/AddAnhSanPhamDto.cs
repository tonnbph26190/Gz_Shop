using System.ComponentModel.DataAnnotations;

namespace QuanApi.Dtos
{
    public class AddAnhSanPhamDto
    {
        [Required]
        public string UrlAnh { get; set; }
        
        public bool LaAnhChinh { get; set; } = false;
    }
} 