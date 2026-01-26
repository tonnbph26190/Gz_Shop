using QuanApi.Data;
using QuanView.ViewModels;
using System.Text.Json;
using System.Text;

namespace QuanView.Services
{
    public interface IKieuDangService
    {
        Task<PagedResult<KieuDang>> GetPagedAsync(string? keyword, string? trangThai, int page, int pageSize);
        Task<bool> CreateAsync(KieuDang model);
        Task<KieuDang?> GetByIdAsync(Guid id);
        Task<bool> UpdateAsync(KieuDang model);
        Task<bool> DeleteAsync(Guid id);
        Task<(bool success, bool? trangThai)> ToggleStatusAsync(Guid id);
    }
    public class KieuDangService : IKieuDangService
    {
        private readonly HttpClient _httpClient;

        public KieuDangService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("MyApi");
        }

        public async Task<PagedResult<KieuDang>> GetPagedAsync(
            string? keyword, string? trangThai, int page, int pageSize)
        {
            var url = $"KieuDang/paged?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrEmpty(keyword))
                url += $"&keyword={Uri.EscapeDataString(keyword)}";

            if (!string.IsNullOrEmpty(trangThai))
                url += $"&trangThai={Uri.EscapeDataString(trangThai)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Không thể tải danh sách kiểu dáng.");

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            return JsonSerializer.Deserialize<PagedResult<KieuDang>>(json, options)!;
        }

        public async Task<bool> CreateAsync(KieuDang model)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("KieuDang/Create", content);
            return response.IsSuccessStatusCode;
        }
        public async Task<KieuDang?> GetByIdAsync(Guid id)
        {
            return await _httpClient.GetFromJsonAsync<KieuDang>($"KieuDang/{id}");
        }

        // ➕ UPDATE
        public async Task<bool> UpdateAsync(KieuDang model)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync($"KieuDang/{model.IDKieuDang}", content);
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"KieuDang/{id}");
            return response.IsSuccessStatusCode;
        }

        // ➕ TOGGLE STATUS
        public async Task<(bool success, bool? trangThai)> ToggleStatusAsync(Guid id)
        {
            var response = await _httpClient.PutAsync($"KieuDang/ToggleStatus/{id}", null);
            if (!response.IsSuccessStatusCode)
                return (false, null);

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            bool? trangThai = null;

            if (data != null && data.TryGetValue("trangThai", out var value))
            {
                // value có thể là JsonElement
                if (value is JsonElement je &&
                    (je.ValueKind == JsonValueKind.True || je.ValueKind == JsonValueKind.False))
                {
                    trangThai = je.GetBoolean();
                }
            }

            return (true, trangThai);
        }
    }
}