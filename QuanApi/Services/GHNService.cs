using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QuanApi.Models;

namespace QuanApi.Services
{
    public class GHNService : IGHNService
    {
        private readonly HttpClient _http;
        private readonly GHNSettings _settings;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public GHNService(HttpClient http, IOptions<GHNSettings> options)
        {
            _http = http;
            _settings = options?.Value ?? new GHNSettings();
            if (!string.IsNullOrEmpty(_settings.BaseUrl))
            {
                _http.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
                _http.DefaultRequestHeaders.TryAddWithoutValidation("Token", _settings.Token);
                _http.DefaultRequestHeaders.TryAddWithoutValidation("ShopId", _settings.ShopId.ToString());
            }
        }

        public Task<bool> IsConfiguredAsync()
        {
            var ok = !string.IsNullOrWhiteSpace(_settings.Token) && _settings.ShopId > 0;
            return Task.FromResult(ok);
        }

        public async Task<List<GHNProvinceDto>> GetProvincesAsync()
        {
            if (!await IsConfiguredAsync()) return new List<GHNProvinceDto>();
            try
            {
                var res = await _http.GetAsync("master-data/province");
                res.EnsureSuccessStatusCode();
                var json = await res.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<GHNApiResponse<List<GHNProvinceDto>>>(json, JsonOptions);
                return root?.Data ?? new List<GHNProvinceDto>();
            }
            catch
            {
                return new List<GHNProvinceDto>();
            }
        }

        public async Task<List<GHNDistrictDto>> GetDistrictsAsync(int provinceId)
        {
            if (!await IsConfiguredAsync()) return new List<GHNDistrictDto>();
            try
            {
                var res = await _http.GetAsync($"master-data/district?province_id={provinceId}");
                res.EnsureSuccessStatusCode();
                var json = await res.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<GHNApiResponse<List<GHNDistrictDto>>>(json, JsonOptions);
                return root?.Data ?? new List<GHNDistrictDto>();
            }
            catch
            {
                return new List<GHNDistrictDto>();
            }
        }

        public async Task<List<GHNWardDto>> GetWardsAsync(int districtId)
        {
            if (!await IsConfiguredAsync()) return new List<GHNWardDto>();
            try
            {
                var res = await _http.GetAsync($"master-data/ward?district_id={districtId}");
                res.EnsureSuccessStatusCode();
                var json = await res.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<GHNApiResponse<List<GHNWardDto>>>(json, JsonOptions);
                return root?.Data ?? new List<GHNWardDto>();
            }
            catch
            {
                return new List<GHNWardDto>();
            }
        }

        public async Task<GHNFeeResult?> GetFeeAsync(int toDistrictId, string toWardCode, int weightGrams, int? length = null, int? width = null, int? height = null, int? serviceId = null)
        {
            if (!await IsConfiguredAsync()) return null;
            try
            {
                var fromDistrictId = _settings.FromDistrictId ?? 0;
                var fromWardCode = _settings.FromWardCode ?? "";
                var payload = new
                {
                    from_district_id = fromDistrictId,
                    from_ward_code = fromWardCode,
                    to_district_id = toDistrictId,
                    to_ward_code = toWardCode,
                    weight = weightGrams,
                    length = length ?? _settings.DefaultLength,
                    width = width ?? _settings.DefaultWidth,
                    height = height ?? _settings.DefaultHeight,
                    service_id = serviceId,
                    insurance_value = 0,
                    cod_value = 0
                };
                var res = await _http.PostAsJsonAsync("v2/shipping-order/fee", payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                res.EnsureSuccessStatusCode();
                var json = await res.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<GHNApiResponse<GHNFeeData>>(json, JsonOptions);
                if (root?.Data == null) return null;
                return new GHNFeeResult
                {
                    Total = root.Data.Total,
                    ServiceFee = root.Data.ServiceFee,
                    InsuranceFee = root.Data.InsuranceFee,
                    CodFee = root.Data.CodFee
                };
            }
            catch
            {
                return null;
            }
        }

        private class GHNApiResponse<T>
        {
            public int Code { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }

        private class GHNFeeData
        {
            public decimal Total { get; set; }
            public decimal ServiceFee { get; set; }
            public decimal InsuranceFee { get; set; }
            public decimal CodFee { get; set; }
        }
    }
}
