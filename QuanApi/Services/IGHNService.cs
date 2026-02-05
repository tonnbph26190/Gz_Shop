namespace QuanApi.Services
{
    public interface IGHNService
    {
        Task<bool> IsConfiguredAsync();
        Task<List<GHNProvinceDto>> GetProvincesAsync();
        Task<List<GHNDistrictDto>> GetDistrictsAsync(int provinceId);
        Task<List<GHNWardDto>> GetWardsAsync(int districtId);
        Task<GHNFeeResult?> GetFeeAsync(int toDistrictId, string toWardCode, int weightGrams, int? length = null, int? width = null, int? height = null, int? serviceId = null);
    }

    public class GHNProvinceDto
    {
        public int ProvinceID { get; set; }
        public string ProvinceName { get; set; } = "";
        public string? Code { get; set; }
    }

    public class GHNDistrictDto
    {
        public int DistrictID { get; set; }
        public string DistrictName { get; set; } = "";
        public int ProvinceID { get; set; }
        public string? Code { get; set; }
    }

    public class GHNWardDto
    {
        public string WardCode { get; set; } = "";
        public string WardName { get; set; } = "";
        public int DistrictID { get; set; }
    }

    public class GHNFeeResult
    {
        public decimal Total { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal InsuranceFee { get; set; }
        public decimal CodFee { get; set; }
    }
}
