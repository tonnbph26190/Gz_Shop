using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuanApi.Models;

namespace QuanApi.Services
{
    public class GHNService : IGHNService
    {
        private readonly HttpClient _http;
        private readonly GHNSettings _settings;
        private readonly ILogger<GHNService> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public GHNService(HttpClient http, IOptions<GHNSettings> options, ILogger<GHNService> logger)
        {
            _http = http;
            _settings = options?.Value ?? new GHNSettings();
            _logger = logger;
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
                var fromWardCode = (_settings.FromWardCode ?? string.Empty).Trim();
                if (fromDistrictId <= 0 || string.IsNullOrWhiteSpace(fromWardCode))
                {
                    _logger.LogWarning(
                        "GHN fee skipped: missing FromDistrictId/FromWardCode configuration. FromDistrictId={FromDistrictId}, FromWardCode='{FromWardCode}'",
                        fromDistrictId,
                        fromWardCode);
                    return null;
                }

                var resolvedServiceId = serviceId ?? await ResolveServiceIdAsync(fromDistrictId, toDistrictId);
                var payload = new Dictionary<string, object?>
                {
                    ["from_district_id"] = fromDistrictId,
                    ["from_ward_code"] = fromWardCode,
                    ["to_district_id"] = toDistrictId,
                    ["to_ward_code"] = toWardCode?.Trim(),
                    ["weight"] = weightGrams > 0 ? weightGrams : _settings.DefaultWeight,
                    ["length"] = length ?? _settings.DefaultLength,
                    ["width"] = width ?? _settings.DefaultWidth,
                    ["height"] = height ?? _settings.DefaultHeight,
                    ["insurance_value"] = 0,
                    ["cod_value"] = 0
                };

                if (resolvedServiceId.HasValue && resolvedServiceId.Value > 0)
                {
                    payload["service_id"] = resolvedServiceId.Value;
                }
                else
                {
                    payload["service_type_id"] = 2;
                }

                var res = await _http.PostAsJsonAsync(
                    "v2/shipping-order/fee",
                    payload,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
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
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "GHN fee request failed. ToDistrictId={ToDistrictId}, ToWardCode={ToWardCode}", toDistrictId, toWardCode);
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<int?> ResolveServiceIdAsync(int fromDistrictId, int toDistrictId)
        {
            try
            {
                var payload = new
                {
                    shop_id = _settings.ShopId,
                    from_district = fromDistrictId,
                    to_district = toDistrictId
                };

                var res = await _http.PostAsJsonAsync(
                    "v2/shipping-order/available-services",
                    payload,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                if (!res.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await res.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<GHNApiResponse<List<GHNAvailableServiceData>>>(json, JsonOptions);
                return root?.Data?
                    .Where(x => x.ServiceId > 0)
                    .OrderBy(x => x.ServiceTypeId == 2 ? 0 : 1)
                    .Select(x => (int?)x.ServiceId)
                    .FirstOrDefault();
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

        private class GHNAvailableServiceData
        {
            public int ServiceId { get; set; }
            public int ServiceTypeId { get; set; }
        }
    }
}
