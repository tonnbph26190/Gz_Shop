using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
	public interface IDanhMucService
	{
		Task<List<DanhMuc>> GetAllAsync(string? keyword);
		Task<DanhMuc?> GetByIdAsync(Guid id);
		Task<bool> CreateAsync(DanhMuc dm);
		Task<bool> UpdateAsync(Guid id, DanhMuc dm);
		Task<bool> DeleteAsync(Guid id);
		Task<bool> ToggleStatusAsync(Guid id);
		Task<(int total, List<DanhMuc> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
	}

	public class DanhMucService : IDanhMucService
	{
		private readonly BanQuanAu1DbContext _context;

		public DanhMucService(BanQuanAu1DbContext context)
		{
			_context = context;
		}

		public async Task<List<DanhMuc>> GetAllAsync(string? keyword)
		{
			var query = _context.DanhMucs.AsQueryable();

			if (!string.IsNullOrEmpty(keyword))
			{
				var keywordLower = keyword.ToLower();
				query = query.Where(x => x.TenDanhMuc.ToLower().Contains(keywordLower) ||
										 x.MaDanhMuc.ToLower().Contains(keywordLower));
			}

			// Order newest first so newly added categories appear at the top
			return await query
				.OrderByDescending(x => x.NgayTao)
				.ToListAsync();
		}

		public async Task<DanhMuc?> GetByIdAsync(Guid id)
		{
			return await _context.DanhMucs.FindAsync(id);
		}

		/* 
		Pseudocode / Plan for CreateAsync:
		- Accept a DanhMuc DTO/entity 'dm' from caller.
		- Assign a new Guid to 'IDDanhMuc'.
		- Set 'NgayTao' to current UTC time.
		- Ensure 'NguoiTao' has a fallback value if not provided (keep as "unknown" when missing).
		- Ensure 'TrangThai' defaults to true for new items.
		- Explicitly leave the "last updated" audit fields empty/unspecified:
		  - Set 'LanCapNhatCuoi' to null so it is clear no update has occurred yet.
		  - Set 'NguoiCapNhat' to null so updater is unspecified.
		- Add the entity to the DbContext and save changes.
		- Return true if save affected rows, otherwise false.
		*/
		public async Task<bool> CreateAsync(DanhMuc dm)
		{
			dm.IDDanhMuc = Guid.NewGuid();
			dm.NgayTao = DateTime.UtcNow;
			dm.NguoiTao ??= "unknown";
			dm.TrangThai = true;

			// Leave "last updated" fields unset/clear when creating a new record
			dm.LanCapNhatCuoi = null;
			dm.NguoiCapNhat = null;

			_context.DanhMucs.Add(dm);
			return await _context.SaveChangesAsync() > 0;
		}

		/* 
		Plan (pseudocode):
		- Load the entity by id from _context.DanhMucs.
		- If entity is null, return false.
		- Update editable fields: TenDanhMuc, MaDanhMuc, TrangThai.
		- To "bring the item to the top when edited", update NgayTao to DateTime.UtcNow so ordering by NgayTao places it first.
		- Update audit fields: LanCapNhatCuoi and NguoiCapNhat (fallback to "unknown").
		- Save changes and return whether any rows were affected.
		*/
		public async Task<bool> UpdateAsync(Guid id, DanhMuc dm)
		{
			var entity = await _context.DanhMucs.FindAsync(id);
			if (entity == null) return false;

			entity.TenDanhMuc = dm.TenDanhMuc;
			entity.MaDanhMuc = dm.MaDanhMuc;
			entity.TrangThai = dm.TrangThai;

			// Move updated item to the top by updating creation timestamp used for ordering
			entity.NgayTao = DateTime.UtcNow;

			entity.LanCapNhatCuoi = DateTime.UtcNow;
			entity.NguoiCapNhat = string.IsNullOrEmpty(dm.NguoiCapNhat) ? "unknown" : dm.NguoiCapNhat;

			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var entity = await _context.DanhMucs.FindAsync(id);
			if (entity == null) return false;

			_context.DanhMucs.Remove(entity);
			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<bool> ToggleStatusAsync(Guid id)
		{
			var dm = await _context.DanhMucs.FindAsync(id);
			if (dm == null) return false;

			dm.TrangThai = !dm.TrangThai;
			dm.LanCapNhatCuoi = DateTime.UtcNow;
			dm.NguoiCapNhat = "auto-toggle";

			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<(int total, List<DanhMuc> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai)
		{
			var query = _context.DanhMucs.AsQueryable();

			if (!string.IsNullOrEmpty(keyword))
			{
				var keywordLower = keyword.ToLower();
				query = query.Where(x =>
					x.TenDanhMuc.ToLower().Contains(keywordLower) ||
					x.MaDanhMuc.ToLower().Contains(keywordLower));
			}

			if (!string.IsNullOrEmpty(trangThai))
			{
				if (trangThai == "active") query = query.Where(x => x.TrangThai == true);
				if (trangThai == "inactive") query = query.Where(x => x.TrangThai == false);
			}

			// Ensure newest items come first before paging
			query = query.OrderByDescending(x => x.NgayTao);

			var total = await query.CountAsync();
			var data = await query
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (total, data);
		}
	}
}
