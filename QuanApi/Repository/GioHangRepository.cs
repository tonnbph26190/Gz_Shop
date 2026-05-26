using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;
using QuanApi.Repository.IRepository;
using System.Linq;

namespace QuanApi.Repository
{
    public class GioHangRepository : GioHangIRepository
    {
        BanQuanAu1DbContext _db;
        public GioHangRepository(BanQuanAu1DbContext db)
        {
            _db = db;
        }
        public List<SanPhamKhachHangViewModel> ListSPCT(int pageNumber, int pageSize)
        {
            var all = _db.SanPhamChiTiets
                .Include(spct => spct.SanPham)
                    .ThenInclude(sp => sp.DanhMuc)
                .Include(spct => spct.KichCo)
                .Include(spct => spct.MauSac)
                .Include(spct => spct.DotGiamGia)
                .Include(spct => spct.AnhSanPhams)
			  .Where(spct =>
	spct.TrangThai &&
	spct.SanPham != null &&
	spct.SanPham.TrangThai
)
				.ToList();

            var grouped = all
                .GroupBy(spct => spct.IDSanPham)
                .OrderBy(g => g.Key)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new SanPhamKhachHangViewModel
                {
                    TenSanPham = g.First().SanPham.TenSanPham,
                    DanhMuc = g.First().SanPham.DanhMuc.TenDanhMuc,
                    UrlAnh = g.First().AnhSanPhams.Where(a => a.TrangThai && a.LaAnhChinh).Select(a => a.UrlAnh).FirstOrDefault()
                             ?? g.First().AnhSanPhams.Where(a => a.TrangThai).OrderBy(a => a.NgayTao).Select(a => a.UrlAnh).FirstOrDefault()
                             ?? "/img/default-product.jpg",
                    BienThes = g.Select(spct => new BienTheSanPhamViewModel
                    {
                        IDSanPhamChiTiet = spct.IDSanPhamChiTiet,
                        Size = spct.KichCo.TenKichCo,
                        Mau = spct.MauSac.TenMauSac,
                        GiaGoc = spct.GiaBan,
                        GiaSauGiam = spct.DotGiamGia != null && spct.DotGiamGia.NgayBatDau <= DateTime.UtcNow && spct.DotGiamGia.NgayKetThuc >= DateTime.UtcNow
                            ? spct.GiaBan * (1 - spct.DotGiamGia.PhanTramGiam / 100m)
                            : spct.GiaBan,
                        SoLuong = Math.Max(0, spct.SoLuong - spct.SoLuongDatCho)
                    }).ToList()
                })
                .ToList();

            return grouped;
        }


		public SanPhamChiTietDto? detailSpct(Guid id)
		{
            var spct = _db.SanPhamChiTiets
                .Include(ct => ct.SanPham)
                    .ThenInclude(sp => sp.DanhMuc)
                .Include(ct => ct.KichCo)
                .Include(ct => ct.MauSac)
                .Include(ct => ct.AnhSanPhams)
                .Include(ct => ct.DotGiamGia)
			   .FirstOrDefault(ct =>
	ct.IDSanPhamChiTiet == id &&
	ct.TrangThai &&
	ct.SanPham != null &&
	ct.SanPham.TrangThai
);

			if (spct == null)
				return null;

			return new SanPhamChiTietDto
            {
                IdSanPhamChiTiet = spct.IDSanPhamChiTiet,
                IdSanPham = spct.IDSanPham,
                TenSanPham = spct.SanPham?.TenSanPham ?? "",
                TenDanhMuc = spct.SanPham?.DanhMuc?.TenDanhMuc ?? "",
                AnhDaiDien = spct.AnhSanPhams?.Where(a => a.LaAnhChinh).Select(a => a.UrlAnh).FirstOrDefault() ?? "",
                TenKichCo = spct.KichCo?.TenKichCo ?? "",
                TenMauSac = spct.MauSac?.TenMauSac ?? "",
                GiaBan = spct.GiaBan,
                price = (spct.DotGiamGia != null && spct.DotGiamGia.NgayBatDau <= DateTime.UtcNow && spct.DotGiamGia.NgayKetThuc >= DateTime.UtcNow)
                    ? spct.GiaBan * (1 - spct.DotGiamGia.PhanTramGiam / 100m)
                    : spct.GiaBan,
                SoLuong = Math.Max(0, spct.SoLuong - spct.SoLuongDatCho),
                SoLuongVatLy = spct.SoLuong,
                SoLuongDatCho = spct.SoLuongDatCho,
                SoLuongKhaDung = Math.Max(0, spct.SoLuong - spct.SoLuongDatCho),
                TrangThai = spct.TrangThai
            };
        }

        public void AddGioHang(Guid iduser, Guid idsp, int soluong)
        {
            if (soluong <= 0)
            {
                throw new ArgumentException("Sản phẩm hoặc số lượng không hợp lệ.");
            }

            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    AddGioHangInternal(iduser, idsp, soluong);
                    return;
                }
                catch (DbUpdateConcurrencyException) when (attempt == 0)
                {
                    // Reload trạng thái mới nhất rồi thử lại 1 lần khi có race condition.
                    _db.ChangeTracker.Clear();
                }
            }
        }

        private void AddGioHangInternal(Guid iduser, Guid idsp, int soluong)
        {
            var nguoidung = _db.GioHangs.FirstOrDefault(gt => gt.IDKhachHang == iduser);
            if (nguoidung == null)
            {
                nguoidung = new GioHang
                {
                    IDKhachHang = iduser,
                    MaGioHang = $"GH{Guid.NewGuid():N}",
                    NgayTao = DateTime.UtcNow,
                    TrangThai = true
                };

                _db.GioHangs.Add(nguoidung);
                _db.SaveChanges();
            }

            var sp = _db.SanPhamChiTiets
                .Include(s => s.DotGiamGia)
                .Include(s => s.SanPham)
                .FirstOrDefault(s => s.IDSanPhamChiTiet == idsp);
            if (sp == null)
            {
                throw new ArgumentException("Sản phẩm hoặc số lượng không hợp lệ.");
            }

            EnsureProductAvailableForSale(sp);

            var existingLine = _db.ChiTietGioHangs
                .FirstOrDefault(ghct => ghct.IDGioHang == nguoidung.IDGioHang && ghct.IDSanPhamChiTiet == idsp);

            var soLuongHienCo = (existingLine?.SoLuong ?? 0) + soluong;
            var soLuongKhaDung = Math.Max(0, sp.SoLuong - sp.SoLuongDatCho);
            if (soLuongHienCo > soLuongKhaDung)
            {
                throw new InvalidOperationException($"Số lượng vượt quá tồn kho khả dụng. Tồn khả dụng: {soLuongKhaDung}");
            }

            var giaSauGiam = sp.GiaBan;
            if (sp.DotGiamGia != null &&
                sp.DotGiamGia.TrangThai &&
                sp.DotGiamGia.NgayBatDau <= DateTime.UtcNow &&
                sp.DotGiamGia.NgayKetThuc >= DateTime.UtcNow)
            {
                giaSauGiam = sp.GiaBan * (1 - sp.DotGiamGia.PhanTramGiam / 100m);
            }

            if (existingLine != null)
            {
                existingLine.SoLuong += soluong;
                existingLine.GiaBan = giaSauGiam;
                existingLine.LanCapNhatCuoi = DateTime.UtcNow;
            }
            else
            {
                _db.ChiTietGioHangs.Add(new Data.ChiTietGioHang
                {
                    MaChiTietGioHang = $"CTGH{Guid.NewGuid():N}",
                    IDGioHang = nguoidung.IDGioHang,
                    IDSanPhamChiTiet = idsp,
                    SoLuong = soluong,
                    GiaBan = giaSauGiam
                });
            }

            _db.SaveChanges();
        }
        public GioHang GetByUserId(Guid userId)
        {
            return _db.GioHangs
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(sp => sp.AnhSanPhams.Where(a => a.TrangThai))
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(sp => sp.SanPham)
                .FirstOrDefault(gt => gt.IDKhachHang == userId);
        }

        public void XoaChiTietGioHang(Guid idgiohang)
        {
            var ghct = _db.ChiTietGioHangs.FirstOrDefault(ghct => ghct.IDChiTietGioHang == idgiohang);
            if (ghct == null)
            {
                throw new KeyNotFoundException("giỏ hàng không ton tai");
            }
            _db.ChiTietGioHangs.Remove(ghct);
            _db.SaveChanges();
        }
        private decimal TinhGiaSauGiam(SanPhamChiTiet spct)
        {
            if (spct.DotGiamGia != null &&
                spct.DotGiamGia.NgayBatDau <= DateTime.UtcNow &&
                spct.DotGiamGia.NgayKetThuc >= DateTime.UtcNow)
            {
                return spct.GiaBan * (1 - spct.DotGiamGia.PhanTramGiam / 100);
            }
            return spct.GiaBan;
        }

        private static void EnsureProductAvailableForSale(SanPhamChiTiet sp)
        {
            if (!sp.TrangThai)
                throw new InvalidOperationException("Sản phẩm đã ngưng bán, không thể thêm vào giỏ.");
            if (sp.SanPham == null || !sp.SanPham.TrangThai)
                throw new InvalidOperationException("Sản phẩm đã ngưng bán, không thể thêm vào giỏ.");
        }

        public void UpdateChiTietGioHang(Guid idghct, int soluong)
        {
            var ghct = _db.ChiTietGioHangs
                          .Include(ct => ct.SanPhamChiTiet)
                              .ThenInclude(sp => sp.SanPham)
                          .Include(ct => ct.SanPhamChiTiet)
                          .ThenInclude(sp => sp.DotGiamGia)
                          .FirstOrDefault(ct => ct.IDChiTietGioHang == idghct);

            if (ghct == null)
                throw new KeyNotFoundException("Không tìm thấy giỏ hàng");

            if (soluong <= 0)
                throw new ArgumentException("Số lượng không hợp lệ");
            if (ghct.SanPhamChiTiet != null)
            {
                EnsureProductAvailableForSale(ghct.SanPhamChiTiet);
                var soLuongKhaDung = Math.Max(0, ghct.SanPhamChiTiet.SoLuong - ghct.SanPhamChiTiet.SoLuongDatCho);
                if (soluong > soLuongKhaDung)
                    throw new InvalidOperationException($"Số lượng vượt quá tồn kho khả dụng. Tồn khả dụng: {soLuongKhaDung}");
            }

            ghct.SoLuong = soluong;
            ghct.GiaBan = TinhGiaSauGiam(ghct.SanPhamChiTiet);

            _db.SaveChanges();
        }

        public FilterOptionsDto GetFilterOptions()
        {
            var categories = _db.DanhMucs
                .Where(dm => dm.TrangThai)
                .Select(dm => dm.TenDanhMuc)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var sizes = _db.KichCos
                .Where(kc => kc.TrangThai)
                .Select(kc => kc.TenKichCo)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var colors = _db.MauSacs
                .Where(ms => ms.TrangThai)
                .Select(ms => ms.TenMauSac)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return new FilterOptionsDto
            {
                Categories = categories,
                Sizes = sizes,
                Colors = colors
            };
        }

    }
}
