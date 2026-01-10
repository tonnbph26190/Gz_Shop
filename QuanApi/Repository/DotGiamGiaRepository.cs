using BanQuanAu1.Web.Data;
using QuanApi.Data;
using QuanApi.Dtos;
using QuanApi.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuanApi.Repository
{
    public class DotGiamGiaRepository : DotGiamGiaIRepository
    {
        private readonly BanQuanAu1DbContext _context;

        public DotGiamGiaRepository(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultGeneric<DotGiamGia>> GetDotGiamGia(string maDot, string tenDot, int? phanTramGiam,
            DateTime? tuNgay, DateTime? denNgay, string trangThai, int page, int pageSize)
        {
            var query = _context.DotGiamGias.AsQueryable();

            if (!string.IsNullOrEmpty(maDot))
                query = query.Where(x => x.MaDot.Contains(maDot));

            if (!string.IsNullOrEmpty(tenDot))
                query = query.Where(x => x.TenDot.Contains(tenDot));

            if (phanTramGiam.HasValue)
                query = query.Where(x => x.PhanTramGiam == phanTramGiam.Value);

            if (tuNgay.HasValue)
                query = query.Where(x => x.NgayBatDau >= tuNgay.Value);

            if (denNgay.HasValue)
                query = query.Where(x => x.NgayKetThuc <= denNgay.Value);

            if (!string.IsNullOrEmpty(trangThai))
            {
                bool tt = trangThai == "true";
                query = query.Where(x => x.TrangThai == tt);
            }

            var total = await query.CountAsync();
            var data = await query.OrderByDescending(x => x.NgayTao)
                                  .Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

            return new PagedResultGeneric<DotGiamGia>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<DotGiamGia> GetByIdAsync(Guid id) => await _context.DotGiamGias.FindAsync(id);

        public async Task<bool> CreateAsync(DotGiamGia dot, List<Guid> chiTietIds)
        {
            if (dot.NgayKetThuc < dot.NgayBatDau || dot.NgayBatDau < DateTime.Today)
                return false;

            // Kiểm tra xem các sản phẩm đã có đợt giảm giá đang hoạt động hay chưa
            if (chiTietIds?.Count > 0)
            {
                var existingActiveDiscounts = await _context.SanPhamDotGiams
                    .Include(spdg => spdg.DotGiamGia)
                    .Where(spdg => chiTietIds.Contains(spdg.IDSanPhamChiTiet) && 
                                   spdg.DotGiamGia.TrangThai == true &&
                                   spdg.DotGiamGia.NgayBatDau <= DateTime.Now &&
                                   spdg.DotGiamGia.NgayKetThuc >= DateTime.Now)
                    .ToListAsync();

                if (existingActiveDiscounts.Any())
                {
                    // Nếu có sản phẩm đã có đợt giảm giá đang hoạt động, cập nhật thay vì tạo mới
                    foreach (var existingDiscount in existingActiveDiscounts)
                    {
                        // Cập nhật thông tin đợt giảm giá hiện tại
                        existingDiscount.DotGiamGia.TenDot = dot.TenDot;
                        existingDiscount.DotGiamGia.PhanTramGiam = dot.PhanTramGiam;
                        existingDiscount.DotGiamGia.NgayBatDau = dot.NgayBatDau;
                        existingDiscount.DotGiamGia.NgayKetThuc = dot.NgayKetThuc;
                        existingDiscount.DotGiamGia.LanCapNhatCuoi = DateTime.Now;
                        existingDiscount.DotGiamGia.NguoiCapNhat = dot.NguoiTao;
                    }

                    // Thêm các sản phẩm mới vào đợt giảm giá hiện tại (nếu có)
                    var existingDotId = existingActiveDiscounts.First().IDDotGiamGia;
                    var existingProductIds = existingActiveDiscounts.Select(spdg => spdg.IDSanPhamChiTiet).ToList();
                    var newProductIds = chiTietIds.Except(existingProductIds).ToList();

                    if (newProductIds.Any())
                    {
                        var newChiTiets = await _context.SanPhamChiTiets
                            .Where(x => newProductIds.Contains(x.IDSanPhamChiTiet))
                            .ToListAsync();

                        foreach (var ct in newChiTiets)
                        {
                            _context.SanPhamDotGiams.Add(new SanPhamDotGiam
                            {
                                IDSanPhamDotGiam = Guid.NewGuid(),
                                IDDotGiamGia = existingDotId,
                                IDSanPhamChiTiet = ct.IDSanPhamChiTiet,
                                MaSanPhamDotGiam = "SPDG_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                                GiaGoc = ct.GiaBan,
                                NgayTao = DateTime.Now,
                                TrangThai = true
                            });
                            ct.IDDotGiamGia = existingDotId;
                        }
                    }

                    await _context.SaveChangesAsync();
                    return true;
                }
            }

            // Nếu không có đợt giảm giá đang hoạt động, tạo mới
            dot.IDDotGiamGia = Guid.NewGuid();
            dot.NgayTao = DateTime.Now;
            dot.TrangThai = true;

            _context.DotGiamGias.Add(dot);

            if (chiTietIds?.Count > 0)
            {
                var chiTiets = await _context.SanPhamChiTiets
                                             .Where(x => chiTietIds.Contains(x.IDSanPhamChiTiet))
                                             .ToListAsync();

                foreach (var ct in chiTiets)
                {
                    _context.SanPhamDotGiams.Add(new SanPhamDotGiam
                    {
                        IDSanPhamDotGiam = Guid.NewGuid(),
                        IDDotGiamGia = dot.IDDotGiamGia,
                        IDSanPhamChiTiet = ct.IDSanPhamChiTiet,
                        MaSanPhamDotGiam = "SPDG_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                        GiaGoc = ct.GiaBan,
                        NgayTao = DateTime.Now,
                        TrangThai = true
                    });
                    ct.IDDotGiamGia = dot.IDDotGiamGia;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(DotGiamGia dot, List<Guid> chiTietIds)
        {
            if (dot.NgayKetThuc < dot.NgayBatDau || dot.NgayBatDau < DateTime.Today)
                return false;

            dot.LanCapNhatCuoi = DateTime.Now;
            _context.DotGiamGias.Update(dot);

            var oldSpGiam = await _context.SanPhamDotGiams
                .Where(x => x.IDDotGiamGia == dot.IDDotGiamGia)
                .ToListAsync();

            var giaGocDict = oldSpGiam.ToDictionary(x => x.IDSanPhamChiTiet, x => x.GiaGoc);

            var oldChiTietIds = oldSpGiam.Select(x => x.IDSanPhamChiTiet).ToList();

            var allChiTiets = await _context.SanPhamChiTiets
                .Where(x => oldChiTietIds.Contains(x.IDSanPhamChiTiet) || chiTietIds.Contains(x.IDSanPhamChiTiet))
                .ToDictionaryAsync(x => x.IDSanPhamChiTiet);

            foreach (var sp in oldSpGiam)
            {
                if (!chiTietIds.Contains(sp.IDSanPhamChiTiet))
                {
                    if (allChiTiets.TryGetValue(sp.IDSanPhamChiTiet, out var ct))
                    {
                        ct.GiaBan = sp.GiaGoc;
                        ct.IDDotGiamGia = null;
                    }
                }
            }

            _context.SanPhamDotGiams.RemoveRange(oldSpGiam);

            foreach (var id in chiTietIds)
            {
                if (allChiTiets.TryGetValue(id, out var ct))
                {
                    decimal giaGoc = giaGocDict.ContainsKey(id) ? giaGocDict[id] : ct.GiaBan;

                    _context.SanPhamDotGiams.Add(new SanPhamDotGiam
                    {
                        IDSanPhamDotGiam = Guid.NewGuid(),
                        IDDotGiamGia = dot.IDDotGiamGia,
                        IDSanPhamChiTiet = ct.IDSanPhamChiTiet,
                        MaSanPhamDotGiam = "SPDG_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                        GiaGoc = giaGoc,
                        NgayTao = DateTime.Now,
                        TrangThai = true
                    });
                    ct.IDDotGiamGia = dot.IDDotGiamGia;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }




        public async Task<List<SelectListItem>> GetAllSanPhamChiTietWithSelected(Guid idDot)
        {
            var selectedIds = await _context.SanPhamDotGiams
                .Where(x => x.IDDotGiamGia == idDot)
                .Select(x => x.IDSanPhamChiTiet)
                .ToListAsync();

            var allProducts = await _context.SanPhamChiTiets
                .Include(x => x.SanPham)
                .Include(x => x.KichCo)
                .Include(x => x.MauSac)
                .Include(x => x.HoaTiet)
                .Select(x => new SelectListItem
                {
                    Value = x.IDSanPhamChiTiet.ToString(),
                    Text = $"{x.MaSPChiTiet} - {x.SanPham.TenSanPham} | Size: {x.KichCo.TenKichCo}, Màu: {x.MauSac.TenMauSac}, Họa tiết: {x.HoaTiet.TenHoaTiet}",
                    Selected = selectedIds.Contains(x.IDSanPhamChiTiet)
                })
                .ToListAsync();

            // Sort selected products first, then unselected products
            return allProducts
                .OrderByDescending(x => x.Selected)
                .ThenBy(x => x.Text)
                .ToList();
        }






        public async Task<bool> DeleteAsync(Guid id)
        {
            var dot = await _context.DotGiamGias.FindAsync(id);
            if (dot == null) return false;

            var spGiams = await _context.SanPhamDotGiams
                                        .Where(x => x.IDDotGiamGia == id)
                                        .ToListAsync();

            var chiTietIds = spGiams.Select(x => x.IDSanPhamChiTiet).ToList();
            var chiTiets = await _context.SanPhamChiTiets
                                         .Where(x => chiTietIds.Contains(x.IDSanPhamChiTiet))
                                         .ToDictionaryAsync(x => x.IDSanPhamChiTiet);

            foreach (var sp in spGiams)
            {
                if (chiTiets.TryGetValue(sp.IDSanPhamChiTiet, out var ct))
                {
                    ct.GiaBan = sp.GiaGoc;
                    ct.IDDotGiamGia = null;
                }
            }

            _context.SanPhamDotGiams.RemoveRange(spGiams);
            _context.DotGiamGias.Remove(dot);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTrangThaiAsync(Guid id, bool trangThai)
        {
            var dot = await _context.DotGiamGias.FindAsync(id);
            if (dot == null) return false;

            dot.TrangThai = trangThai;
            dot.LanCapNhatCuoi = DateTime.Now;

            var spGiams = await _context.SanPhamDotGiams
                                        .Where(x => x.IDDotGiamGia == id)
                                        .ToListAsync();

            var chiTietIds = spGiams.Select(x => x.IDSanPhamChiTiet).ToList();
            var chiTiets = await _context.SanPhamChiTiets
                                         .Where(x => chiTietIds.Contains(x.IDSanPhamChiTiet))
                                         .ToDictionaryAsync(x => x.IDSanPhamChiTiet);

            foreach (var sp in spGiams)
            {
                if (chiTiets.TryGetValue(sp.IDSanPhamChiTiet, out var ct))
                {
                    if (trangThai)
                    {
                        ct.IDDotGiamGia = id;
                    }
                    else
                    {
                        ct.GiaBan = sp.GiaGoc;
                        ct.IDDotGiamGia = null;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Guid>> GetProductsWithActiveDiscounts(List<Guid> productIds)
        {
            if (productIds == null || !productIds.Any())
                return new List<Guid>();

            return await _context.SanPhamDotGiams
                .Include(spdg => spdg.DotGiamGia)
                .Where(spdg => productIds.Contains(spdg.IDSanPhamChiTiet) && 
                               spdg.DotGiamGia.TrangThai == true &&
                               spdg.DotGiamGia.NgayBatDau <= DateTime.Now &&
                               spdg.DotGiamGia.NgayKetThuc >= DateTime.Now)
                .Select(spdg => spdg.IDSanPhamChiTiet)
                .Distinct()
                .ToListAsync();
        }

    }
}
