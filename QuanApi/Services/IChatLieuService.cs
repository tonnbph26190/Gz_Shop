using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using System.Linq;

namespace QuanApi.Services
{
	public interface IChatLieuService
	{
		Task<List<ChatLieu>> GetAllAsync(string? keyword);
		Task<ChatLieu?> GetByIdAsync(Guid id);
		Task<bool> CreateAsync(ChatLieu cl);
		Task<bool> UpdateAsync(Guid id, ChatLieu cl);
		Task<bool> DeleteAsync(Guid id);
		Task<(bool Success, bool NewStatus)> ToggleStatusAsync(Guid id);
		Task<(int Total, List<ChatLieu> Data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
	}
	public class ChatLieuService : IChatLieuService
	{
		// Pseudocode / Plan:
		// 1. Keep CreateAsync as is (sets NgayTao = UtcNow).
		// 2. Ensure GetAllAsync returns items ordered by NgayTao descending so newly created items appear first.
		// 3. Ensure GetPagedAsync applies same ordering (OrderByDescending NgayTao) before Skip/Take so newest items are on top of paged results.
		// 4. Preserve existing filters (keyword, trangThai) and other methods unchanged.

		private readonly BanQuanAu1DbContext _context;

		public ChatLieuService(BanQuanAu1DbContext context)
		{
			_context = context;
		}

		public async Task<List<ChatLieu>> GetAllAsync(string? keyword)
		{
			var query = _context.ChatLieus.AsQueryable();
			if (!string.IsNullOrEmpty(keyword))
			{
				var keywordLower = keyword.ToLower();
				query = query.Where(x => x.TenChatLieu.ToLower().Contains(keywordLower) || x.MaChatLieu.ToLower().Contains(keywordLower));
			}

			// Order by creation date descending so newly added items appear first
			query = query.OrderByDescending(x => x.LanCapNhatCuoi ?? x.NgayTao);

			return await query.ToListAsync();
		}

		public async Task<ChatLieu?> GetByIdAsync(Guid id) => await _context.ChatLieus.FindAsync(id);

		public async Task<bool> CreateAsync(ChatLieu cl)
		{
			cl.IDChatLieu = Guid.NewGuid();
			cl.NgayTao = DateTime.UtcNow;
			if (string.IsNullOrEmpty(cl.NguoiTao)) cl.NguoiTao = "unknown";

			_context.ChatLieus.Add(cl);
			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<bool> UpdateAsync(Guid id, ChatLieu cl)
		{
			var entity = await _context.ChatLieus.FindAsync(id);
			if (entity == null) return false;

			entity.TenChatLieu = cl.TenChatLieu;
			entity.MaChatLieu = cl.MaChatLieu;
			entity.LanCapNhatCuoi = DateTime.UtcNow;
			entity.NguoiCapNhat = string.IsNullOrEmpty(cl.NguoiCapNhat) ? "unknown" : cl.NguoiCapNhat;
			entity.TrangThai = cl.TrangThai;

			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var entity = await _context.ChatLieus.FindAsync(id);
			if (entity == null) return false;

			_context.ChatLieus.Remove(entity);
			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<(bool Success, bool NewStatus)> ToggleStatusAsync(Guid id)
		{
			var cl = await _context.ChatLieus.FindAsync(id);
			if (cl == null) return (false, false);

			cl.TrangThai = !cl.TrangThai;
			cl.LanCapNhatCuoi = DateTime.UtcNow;
			cl.NguoiCapNhat = "auto-toggle";

			var saved = await _context.SaveChangesAsync() > 0;
			return (saved, cl.TrangThai);
		}

		public async Task<(int Total, List<ChatLieu> Data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai)
		{
			var query = _context.ChatLieus.AsQueryable();

			if (!string.IsNullOrEmpty(keyword))
			{
				var keywordLower = keyword.ToLower();
				query = query.Where(x => x.TenChatLieu.ToLower().Contains(keywordLower) || x.MaChatLieu.ToLower().Contains(keywordLower));
			}

			if (trangThai == "active") query = query.Where(x => x.TrangThai == true);
			else if (trangThai == "inactive") query = query.Where(x => x.TrangThai == false);

			// Ensure newest items appear first in paged results
			query = query.OrderByDescending(x =>x.LanCapNhatCuoi?? x.NgayTao);

			var total = await query.CountAsync();
			var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

			return (total, data);
		}
	}
}
