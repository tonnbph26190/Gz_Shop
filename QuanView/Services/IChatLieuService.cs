using QuanApi.Data;
using QuanView.ViewModels;
using System.Text;
using System.Text.Json;

namespace QuanView.Services
{
    public interface IChatLieuService
    {
        Task<PagedResult<ChatLieu>> GetPagedAsync(
            string? keyword,
            string? trangThai,
            int page,
            int pageSize);
        Task<ChatLieu?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(ChatLieu model, string? nguoiTao);
        Task<bool> UpdateAsync(ChatLieu model, string? nguoiCapNhat);
        Task<bool> DeleteAsync(Guid id);
        Task<(bool success, bool trangThai)> ToggleStatusAsync(Guid id);
    }
    public class ChatLieuService : IChatLieuService
    {
        private readonly HttpClient _httpClient;

        public ChatLieuService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("MyApi");
        }

        public async Task<PagedResult<ChatLieu>> GetPagedAsync(
            string? keyword,
            string? trangThai,
            int page,
            int pageSize)
        {
            var url = $"ChatLieu/paged?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrEmpty(keyword))
                url += $"&keyword={Uri.EscapeDataString(keyword)}";

            if (!string.IsNullOrEmpty(trangThai))
                url += $"&trangThai={Uri.EscapeDataString(trangThai)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Không thể tải danh sách chất liệu.");

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var result = JsonSerializer.Deserialize<PagedResult<ChatLieu>>(json, options);

            return result!;
        }
        public async Task<bool> CreateAsync(ChatLieu model, string? nguoiTao)
        {
            model.NgayTao = DateTime.Now;
            model.NguoiTao = nguoiTao;

            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("ChatLieu/Create", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<ChatLieu?> GetByIdAsync(Guid id)
        {
            return await _httpClient.GetFromJsonAsync<ChatLieu>($"ChatLieu/{id}");
        }
        public async Task<bool> UpdateAsync(ChatLieu model, string? nguoiCapNhat)
        {
            model.LanCapNhatCuoi = DateTime.Now;
            model.NguoiCapNhat = nguoiCapNhat;

            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PutAsync($"ChatLieu/{model.IDChatLieu}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"ChatLieu/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<(bool success, bool trangThai)> ToggleStatusAsync(Guid id)
        {
            var response = await _httpClient.PutAsync($"ChatLieu/ToggleStatus/{id}", null);

            if (!response.IsSuccessStatusCode)
                return (false, false);

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            bool trangThai = false;
            if (data != null && data.ContainsKey("trangThai"))
            {
                trangThai = Convert.ToBoolean(data["trangThai"]);
            }

            return (true, trangThai);
        }
    }

}
