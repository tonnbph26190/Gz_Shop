using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanApi.Data
{
    public class BannerSanPham
    {
        [Key]
        public Guid IDBannerSanPham { get; set; } = Guid.NewGuid();

        [Required]
        public int BannerId { get; set; }

        [Required]
        public Guid IDSanPham { get; set; }

        [ForeignKey(nameof(BannerId))]
        public virtual Banner? Banner { get; set; }

        [ForeignKey(nameof(IDSanPham))]
        public virtual SanPham? SanPham { get; set; }
    }
}
