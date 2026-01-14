using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

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
        private readonly BanQuanAu1DbContext _context;

        public ChatLieuService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatLieu>> GetAllAsync(string? keyword)
        {
            var query = _context.ChatLieus.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenChatLieu.Contains(keyword) || x.MaChatLieu.Contains(keyword));

            return await query.ToListAsync();
        }

        public async Task<ChatLieu?> GetByIdAsync(Guid id) => await _context.ChatLieus.FindAsync(id);

        public async Task<bool> CreateAsync(ChatLieu cl)
        {
            cl.IDChatLieu = Guid.NewGuid();
            cl.NgayTao = DateTime.Now;
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
            entity.LanCapNhatCuoi = DateTime.Now;
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
            cl.LanCapNhatCuoi = DateTime.Now;
            cl.NguoiCapNhat = "auto-toggle";

            var saved = await _context.SaveChangesAsync() > 0;
            return (saved, cl.TrangThai);
        }

        public async Task<(int Total, List<ChatLieu> Data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai)
        {
            var query = _context.ChatLieus.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenChatLieu.Contains(keyword) || x.MaChatLieu.Contains(keyword));

            if (trangThai == "active") query = query.Where(x => x.TrangThai == true);
            else if (trangThai == "inactive") query = query.Where(x => x.TrangThai == false);

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (total, data);
        }
    }
}
