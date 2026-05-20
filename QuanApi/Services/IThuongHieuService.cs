using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
	public interface IThuongHieuService
	{
		Task<List<ThuongHieu>> GetAllAsync(string? keyword);
		Task<ThuongHieu?> GetByIdAsync(Guid id);
		Task<bool> CreateAsync(ThuongHieu th);
		Task<bool> UpdateAsync(Guid id, ThuongHieu th);
		Task<bool> DeleteAsync(Guid id);
		Task<bool> ToggleStatusAsync(Guid id);
		Task<(int total, List<ThuongHieu> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
	}
	public class ThuongHieuService : IThuongHieuService
	{
		private readonly BanQuanAu1DbContext _context;

		public ThuongHieuService(BanQuanAu1DbContext context)
		{
			_context = context;
		}

		public async Task<List<ThuongHieu>> GetAllAsync(string? keyword)
		{
			var query = _context.ThuongHieus.AsQueryable();
			if (!string.IsNullOrEmpty(keyword))
			{
				var keywordLower = keyword.ToLower();
				query = query.Where(x => x.TenThuongHieu.ToLower().Contains(keywordLower) || x.MaThuongHieu.ToLower().Contains(keywordLower));
			}

			// Order newest first so newly added items appear at the top
			query = query.OrderByDescending(x => x.LanCapNhatCuoi ?? x.NgayTao);

			return await query.ToListAsync();
		}

		public async Task<ThuongHieu?> GetByIdAsync(Guid id)
		{
			return await _context.ThuongHieus.FindAsync(id);
		}

		public async Task<bool> CreateAsync(ThuongHieu th)
		{
			th.IDThuongHieu = Guid.NewGuid();
			th.NgayTao = DateTime.UtcNow;
			if (string.IsNullOrEmpty(th.NguoiTao)) th.NguoiTao = "unknown";

			_context.ThuongHieus.Add(th);
			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<bool> UpdateAsync(Guid id, ThuongHieu th)
		{
			var entity = await _context.ThuongHieus.FindAsync(id);
			if (entity == null) return false;

			entity.TenThuongHieu = th.TenThuongHieu;
			entity.MaThuongHieu = th.MaThuongHieu;
			entity.LanCapNhatCuoi = DateTime.UtcNow;
			entity.NguoiCapNhat = string.IsNullOrEmpty(th.NguoiCapNhat) ? "unknown" : th.NguoiCapNhat;
			entity.TrangThai = th.TrangThai;

			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var entity = await _context.ThuongHieus.FindAsync(id);
			if (entity == null) return false;

			_context.ThuongHieus.Remove(entity);
			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<bool> ToggleStatusAsync(Guid id)
		{
			var th = await _context.ThuongHieus.FindAsync(id);
			if (th == null) return false;

			th.TrangThai = !th.TrangThai;
			th.LanCapNhatCuoi = DateTime.UtcNow;
			th.NguoiCapNhat = "auto-toggle";

			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<(int total, List<ThuongHieu> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai)
		{
			var query = _context.ThuongHieus.AsQueryable();

			if (!string.IsNullOrEmpty(keyword))
			{
				var keywordLower = keyword.ToLower();
				query = query.Where(x => x.TenThuongHieu.ToLower().Contains(keywordLower) || x.MaThuongHieu.ToLower().Contains(keywordLower));
			}

			if (!string.IsNullOrEmpty(trangThai))
			{
				if (trangThai == "active") query = query.Where(x => x.TrangThai == true);
				else if (trangThai == "inactive") query = query.Where(x => x.TrangThai == false);
			}

			// Ensure newest items are returned first, then apply paging
			query = query.OrderByDescending(x => x.LanCapNhatCuoi ?? x.NgayTao);

			var total = await query.CountAsync();
			var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

			return (total, data);
		}
	}
}
