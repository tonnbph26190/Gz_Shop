using QuanApi.Dtos;

namespace QuanApi.Services
{
    public interface IShippingService
    {
        decimal CalculateShippingFee(string province, string district, decimal orderValue, decimal weight = 0);
        decimal ApplyShippingDiscount(decimal shippingFee, decimal orderValue);
        ShippingInfoDto GetShippingInfo(string province, string district, decimal orderValue, decimal weight = 0);
    }

    public class ShippingService : IShippingService
    {
        // Phí vận chuyển cơ bản theo vùng (từ Hà Nội)
        private readonly Dictionary<string, decimal> _regionBaseFees = new()
        {
           // Miền Bắc - Gần Hà Nội nên phí thấp
{ "Bắc Giang", 25000 },
{ "Bắc Kạn", 27000 },
{ "Bắc Ninh", 22000 },
{ "Cao Bằng", 30000 },
{ "Điện Biên", 30000 },
{ "Hà Giang", 30000 },
{ "Hà Nam", 25000 },
{ "Hà Nội", 20000 },
{ "Hải Phòng", 25000 },
{ "Hòa Bình", 25000 },
{ "Hưng Yên", 25000 },
{ "Lai Châu", 30000 },
{ "Lạng Sơn", 27000 },
{ "Lào Cai", 30000 },
{ "Nam Định", 30000 },
{ "Ninh Bình", 30000 },
{ "Phú Thọ", 25000 },
{ "Quảng Ninh", 30000 },
{ "Sơn La", 30000 },
{ "Thái Bình", 30000 },
{ "Thái Nguyên", 25000 },
{ "Tuyên Quang", 30000 },
{ "Vĩnh Phúc", 25000 },
{ "Yên Bái", 27000 },

// Miền Trung - Khoảng cách trung bình
{ "Thanh Hóa", 40000 },
{ "Nghệ An", 45000 },
{ "Hà Tĩnh", 45000 },
{ "Quảng Bình", 50000 },
{ "Quảng Trị", 50000 },
{ "Thừa Thiên Huế", 50000 },
{ "Đà Nẵng", 55000 },
{ "Quảng Nam", 55000 },
{ "Quảng Ngãi", 60000 },
{ "Bình Định", 60000 },
{ "Phú Yên", 65000 },
{ "Khánh Hòa", 65000 },
{ "Ninh Thuận", 70000 },
{ "Bình Thuận", 70000 },

// Miền Nam - Xa nhất nên phí cao nhất
{ "Hồ Chí Minh", 75000 },
{ "Bình Dương", 75000 },
{ "Đồng Nai", 80000 },
{ "Bà Rịa - Vũng Tàu", 85000 },
{ "Long An", 80000 },
{ "Tiền Giang", 85000 },
{ "Bến Tre", 90000 },
{ "Vĩnh Long", 90000 },
{ "Trà Vinh", 95000 },
{ "Cần Thơ", 90000 },
{ "Đồng Tháp", 90000 },
{ "An Giang", 95000 },
{ "Kiên Giang", 100000 },
{ "Cà Mau", 110000 },
{ "Bạc Liêu", 105000 },
{ "Sóc Trăng", 95000 },
{ "Hậu Giang", 95000 },
{ "Tây Ninh", 85000 },
{ "Bình Phước", 85000 },
{ "Đắk Lắk", 90000 },
{ "Đắk Nông", 95000 },
{ "Lâm Đồng", 85000 },
{ "Gia Lai", 95000 },
{ "Kon Tum", 100000 },
        };

        // Phí phụ trội theo quận/huyện đặc biệt
        private readonly Dictionary<string, decimal> _districtSurcharge = new()
        {
            // Các quận trung tâm giảm phí
            { "Quận 1", -5000 },
            { "Quận 3", -3000 },
            { "Quận Hai Bà Trưng", -3000 },
            { "Quận Hoàn Kiếm", -5000 },
            { "Quận Ba Đình", -3000 },
            
            // Các huyện xa tăng phí
            { "Huyện Cần Giờ", 15000 },
            { "Huyện Củ Chi", 10000 },
            { "Huyện Nhà Bè", 8000 },
            { "Huyện Bình Chánh", 8000 }
        };

        public decimal CalculateShippingFee(string province, string district, decimal orderValue, decimal weight = 0)
        {
            // Chuẩn hóa tên tỉnh để tìm kiếm chính xác hơn
            string normalizedProvince = NormalizeProvinceName(province);

            // Lấy phí cơ bản theo tỉnh
            decimal baseFee = _regionBaseFees.GetValueOrDefault(normalizedProvince, 50000); // Mặc định 50k cho tỉnh không có trong danh sách

            // Log để debug
            Console.WriteLine($"[ShippingService] Original province: '{province}', Normalized: '{normalizedProvince}', Base fee: {baseFee}");

            // Áp dụng phụ phí theo quận/huyện
            decimal districtFee = _districtSurcharge.GetValueOrDefault(district, 0);

            // Phí theo trọng lượng (nếu có)
            decimal weightFee = 0;
            if (weight > 2) // Miễn phí cho đơn hàng dưới 2kg
            {
                weightFee = (weight - 2) * 5000; // 5k/kg cho mỗi kg vượt quá 2kg
            }

            decimal totalFee = baseFee + districtFee + weightFee;

            // Đảm bảo phí không âm
            return Math.Max(totalFee, 0);
        }

        public decimal ApplyShippingDiscount(decimal shippingFee, decimal orderValue)
        {
            // Miễn phí vận chuyển cho đơn hàng trên 500k
            if (orderValue >= 500000)
            {
                return 0;
            }

            // Giảm 50% phí vận chuyển cho đơn hàng từ 300k
            if (orderValue >= 300000)
            {
                return shippingFee * 0.5m;
            }

            // Giảm 20% phí vận chuyển cho đơn hàng từ 200k
            if (orderValue >= 200000)
            {
                return shippingFee * 0.8m;
            }

            return shippingFee;
        }

        public ShippingInfoDto GetShippingInfo(string province, string district, decimal orderValue, decimal weight = 0)
        {
            decimal originalFee = CalculateShippingFee(province, district, orderValue, weight);
            decimal finalFee = ApplyShippingDiscount(originalFee, orderValue);
            decimal discount = originalFee - finalFee;

            string discountMessage = "";
            if (orderValue >= 500000)
            {
                discountMessage = "Miễn phí vận chuyển cho đơn hàng từ 500.000đ";
            }
            else if (orderValue >= 300000)
            {
                discountMessage = "Giảm 50% phí vận chuyển cho đơn hàng từ 300.000đ";
            }
            else if (orderValue >= 200000)
            {
                discountMessage = "Giảm 20% phí vận chuyển cho đơn hàng từ 200.000đ";
            }

            return new ShippingInfoDto
            {
                Province = province,
                District = district,
                OriginalFee = originalFee,
                DiscountAmount = discount,
                FinalFee = finalFee,
                DiscountMessage = discountMessage,
                EstimatedDeliveryDays = GetEstimatedDeliveryDays(province)
            };
        }

        private int GetEstimatedDeliveryDays(string province)
        {
            // Thời gian giao hàng ước tính theo vùng
            var majorCities = new[] { "Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Cần Thơ", "Hải Phòng" };

            if (majorCities.Contains(province))
            {
                return 2; // 1-2 ngày cho thành phố lớn
            }

            var nearbyProvinces = new[] {
                "Bình Dương", "Đồng Nai", "Long An", "Bắc Ninh", "Hưng Yên",
                "Vĩnh Phúc", "Hà Nam", "Quảng Nam", "Bình Thuận"
            };

            if (nearbyProvinces.Contains(province))
            {
                return 3; // 2-3 ngày cho tỉnh lân cận
            }

            return 5; // 3-5 ngày cho các tỉnh xa
        }

        private string NormalizeProvinceName(string province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return string.Empty;

            // Loại bỏ khoảng trắng thừa và chuẩn hóa
            string normalized = province.Trim();

            // Mapping các tên tỉnh có thể khác nhau
            var provinceMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Thành phố trực thuộc trung ương
                { "TP Hồ Chí Minh", "Hồ Chí Minh" },
                { "Thành phố Hồ Chí Minh", "Hồ Chí Minh" },
                { "TP.HCM", "Hồ Chí Minh" },
                { "TPHCM", "Hồ Chí Minh" },
                { "Sài Gòn", "Hồ Chí Minh" },
                { "TP Hà Nội", "Hà Nội" },
                { "Thành phố Hà Nội", "Hà Nội" },
                { "TP Đà Nẵng", "Đà Nẵng" },
                { "Thành phố Đà Nẵng", "Đà Nẵng" },
                { "TP Hải Phòng", "Hải Phòng" },
                { "Thành phố Hải Phòng", "Hải Phòng" },
                { "TP Cần Thơ", "Cần Thơ" },
                { "Thành phố Cần Thơ", "Cần Thơ" },

                // Các tỉnh có thể có tiền tố
                { "Tỉnh An Giang", "An Giang" },
                { "Tỉnh Bà Rịa - Vũng Tàu", "Bà Rịa - Vũng Tàu" },
                { "Tỉnh Bạc Liêu", "Bạc Liêu" },
                { "Tỉnh Bắc Ninh", "Bắc Ninh" },
                { "Tỉnh Bến Tre", "Bến Tre" },
                { "Tỉnh Bình Định", "Bình Định" },
                { "Tỉnh Bình Dương", "Bình Dương" },
                { "Tỉnh Bình Phước", "Bình Phước" },
                { "Tỉnh Bình Thuận", "Bình Thuận" },
                { "Tỉnh Cà Mau", "Cà Mau" },
                { "Tỉnh Đắk Lắk", "Đắk Lắk" },
                { "Tỉnh Đắk Nông", "Đắk Nông" },
                { "Tỉnh Đồng Nai", "Đồng Nai" },
                { "Tỉnh Đồng Tháp", "Đồng Tháp" },
                { "Tỉnh Gia Lai", "Gia Lai" },
                { "Tỉnh Hà Nam", "Hà Nam" },
                { "Tỉnh Hà Tĩnh", "Hà Tĩnh" },
                { "Tỉnh Hậu Giang", "Hậu Giang" },
                { "Tỉnh Hưng Yên", "Hưng Yên" },
                { "Tỉnh Khánh Hòa", "Khánh Hòa" },
                { "Tỉnh Kiên Giang", "Kiên Giang" },
                { "Tỉnh Kon Tum", "Kon Tum" },
                { "Tỉnh Lâm Đồng", "Lâm Đồng" },
                { "Tỉnh Long An", "Long An" },
                { "Tỉnh Nam Định", "Nam Định" },
                { "Tỉnh Nghệ An", "Nghệ An" },
                { "Tỉnh Ninh Bình", "Ninh Bình" },
                { "Tỉnh Ninh Thuận", "Ninh Thuận" },
                { "Tỉnh Phú Yên", "Phú Yên" },
                { "Tỉnh Quảng Bình", "Quảng Bình" },
                { "Tỉnh Quảng Nam", "Quảng Nam" },
                { "Tỉnh Quảng Ngãi", "Quảng Ngãi" },
                { "Tỉnh Quảng Ninh", "Quảng Ninh" },
                { "Tỉnh Quảng Trị", "Quảng Trị" },
                { "Tỉnh Sóc Trăng", "Sóc Trăng" },
                { "Tỉnh Tây Ninh", "Tây Ninh" },
                { "Tỉnh Thái Nguyên", "Thái Nguyên" },
                { "Tỉnh Thanh Hóa", "Thanh Hóa" },
                { "Tỉnh Thừa Thiên Huế", "Thừa Thiên Huế" },
                { "Tỉnh Tiền Giang", "Tiền Giang" },
                { "Tỉnh Trà Vinh", "Trà Vinh" },
                { "Tỉnh Vĩnh Long", "Vĩnh Long" },
                { "Tỉnh Vĩnh Phúc", "Vĩnh Phúc" }
            };

            // Kiểm tra mapping trước
            if (provinceMapping.ContainsKey(normalized))
            {
                return provinceMapping[normalized];
            }

            // Nếu không có trong mapping, thử tìm kiếm gần đúng
            foreach (var key in _regionBaseFees.Keys)
            {
                if (string.Equals(key, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return key;
                }
            }

            // Thử loại bỏ các tiền tố phổ biến
            var prefixes = new[] { "Tỉnh ", "Thành phố ", "TP ", "TP." };
            foreach (var prefix in prefixes)
            {
                if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string withoutPrefix = normalized.Substring(prefix.Length).Trim();
                    foreach (var key in _regionBaseFees.Keys)
                    {
                        if (string.Equals(key, withoutPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            return key;
                        }
                    }
                }
            }

            // Trả về tên gốc nếu không tìm thấy
            return normalized;
        }
    }
}
