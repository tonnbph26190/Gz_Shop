using QuanView.Areas.Admin.Models;

namespace QuanView.ViewModels
{
    public class SanPhamFilterViewModel
    {
        public List<SanPhamDto> SanPhams { get; set; } = new List<SanPhamDto>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        
        // Bộ lọc
        public string? Keyword { get; set; }
        public string? TrangThai { get; set; }
        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }
        public int? QtyFrom { get; set; }
        public int? QtyTo { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        
        // Dropdown data
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> TrangThaiOptions { get; set; } = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
        {
            new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "", Text = "Tất cả trạng thái" },
            new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "active", Text = "Hoạt động" },
            new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "inactive", Text = "Ngưng hoạt động" }
        };
    }
}
