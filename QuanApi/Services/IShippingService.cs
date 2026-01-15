namespace QuanApi.Services
{
    public interface IShippingService
    {
        object CalculateShipping(string province, string district, decimal orderValue);

        IEnumerable<string> GetProvinces();
        IEnumerable<string> GetDistricts(string province);
        IEnumerable<string> GetWards(string province, string district);
        object GetDiscountInfo();
    }
    
        public class ShippingService : IShippingService
        {
            public object CalculateShipping(string province, string district, decimal orderValue)
            {
                decimal baseFee = 50000;
                decimal discountPercent = 0;

                if (orderValue >= 500000) discountPercent = 100;
                else if (orderValue >= 300000) discountPercent = 50;
                else if (orderValue >= 200000) discountPercent = 20;

                decimal discountAmount = baseFee * discountPercent / 100;
                decimal finalFee = baseFee - discountAmount;

                return new
                {
                    Province = province,
                    District = district,
                    OriginalFee = baseFee,
                    DiscountPercent = discountPercent,
                    DiscountAmount = discountAmount,
                    FinalFee = finalFee
                };
            }

            public IEnumerable<string> GetProvinces()
            {
                return new[]
                {
                "Hà Nội", "Hồ Chí Minh", "Đà Nẵng", "Hải Phòng", "Cần Thơ"
            };
            }

            public IEnumerable<string> GetDistricts(string province)
            {
                return province switch
                {
                    "Hà Nội" => new[] { "Ba Đình", "Cầu Giấy", "Đống Đa" },
                    "Hồ Chí Minh" => new[] { "Quận 1", "Quận 7", "Thủ Đức" },
                    _ => new[] { "Trung tâm", "Ngoại thành" }
                };
            }

            public IEnumerable<string> GetWards(string province, string district)
            {
                return new[] { "Phường 1", "Phường 2", "Phường 3" };
            }

            public object GetDiscountInfo()
            {
                return new[]
                {
                new { MinOrderValue = 500000, DiscountPercent = 100 },
                new { MinOrderValue = 300000, DiscountPercent = 50 },
                new { MinOrderValue = 200000, DiscountPercent = 20 }
            };
            }
        }
    }


