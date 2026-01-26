using QuanApi.Data;

using QuanView.ViewModels;
using System.Text.Json;
using System.Text;

namespace QuanView.Services
{
    public interface IKichCoService
    {
        Task<PagedResult<KichCo>?> GetPagedAsync(string? keyword, string? trangThai, int page, int pageSize);
        Task<bool> CreateAsync(KichCo model, string? nguoiTao);
        Task<KichCo?> GetByIdAsync(Guid id);
        Task<bool> UpdateAsync(KichCo model, string? nguoiCapNhat);
        Task<bool> DeleteAsync(Guid id);
        Task<(bool success, bool? trangThai)> ToggleStatusAsync(Guid id);
    }
    public class KichCoService : IKichCoService
    {
        private readonly HttpClient _httpClient;

        public KichCoService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("MyApi");
        }

        public async Task<PagedResult<KichCo>?> GetPagedAsync(string? keyword, string? trangThai, int page, int pageSize)
        {
            var url = $"KichCo/paged?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrEmpty(keyword))
                url += $"&keyword={Uri.EscapeDataString(keyword)}";

            if (!string.IsNullOrEmpty(trangThai))
                url += $"&trangThai={Uri.EscapeDataString(trangThai)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<PagedResult<KichCo>>(json, options);
        }

        public async Task<bool> CreateAsync(KichCo model, string? nguoiTao)
        {
            model.NgayTao = DateTime.Now;
            model.NguoiTao = nguoiTao;

            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("KichCo/Create", content);
            return response.IsSuccessStatusCode;
        }
        public async Task<KichCo?> GetByIdAsync(Guid id)
        {
            return await _httpClient.GetFromJsonAsync<KichCo>($"KichCo/{id}");
        }

        public async Task<bool> UpdateAsync(KichCo model, string? nguoiCapNhat)
        {
            model.LanCapNhatCuoi = DateTime.Now;
            model.NguoiCapNhat = nguoiCapNhat;

            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"KichCo/{model.IDKichCo}", content);

            return response.IsSuccessStatusCode;
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"KichCo/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<(bool success, bool? trangThai)> ToggleStatusAsync(Guid id)
        {
            var response = await _httpClient.PutAsync($"KichCo/ToggleStatus/{id}", null);

            if (!response.IsSuccessStatusCode)
                return (false, null);

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            bool? trangThai = null;
            if (data != null && data.ContainsKey("trangThai"))
            {
                // JSON element có thể là JsonElement
                if (data["trangThai"] is JsonElement je &&
                  (je.ValueKind == JsonValueKind.True || je.ValueKind == JsonValueKind.False))
                {
                    trangThai = je.GetBoolean();
                }

            }

            return (true, trangThai);
        }
    }
}
