using Microsoft.AspNetCore.Mvc;
using QuanApi.Dtos;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingController : ControllerBase
    {
        private readonly IShippingService _shippingService;

        public ShippingController(IShippingService shippingService)
        {
            _shippingService = shippingService;
        }

        [HttpPost("calculate")]
        public ActionResult<ShippingInfoDto> CalculateShipping([FromBody] CalculateShippingRequest request)
        {
            try
            {
                // Log request để debug
                Console.WriteLine($"Shipping calculation request: Province={request?.Province}, District={request?.District}, OrderValue={request?.OrderValue}");

                if (request == null)
                {
                    return BadRequest("Request không được null");
                }

                if (string.IsNullOrEmpty(request.Province))
                {
                    return BadRequest(new { 
                        error = "Tỉnh/thành phố không được để trống",
                        originalFee = 50000,
                        finalFee = 50000,
                        discountAmount = 0,
                        discountMessage = ""
                    });
                }

                if (request.OrderValue < 0)
                {
                    return BadRequest("Giá trị đơn hàng không được âm");
                }

                var shippingInfo = _shippingService.GetShippingInfo(
                    request.Province, 
                    request.District ?? "", 
                    request.OrderValue
                );

                Console.WriteLine($"Shipping calculation result: OriginalFee={shippingInfo.OriginalFee}, FinalFee={shippingInfo.FinalFee}");

                return Ok(shippingInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Shipping calculation error: {ex}");
                return StatusCode(500, $"Lỗi tính phí vận chuyển: {ex.Message}");
            }
        }


        [HttpGet("test")]
        public ActionResult<object> TestShipping()
        {
            try
            {
                var testCases = new[]
                {
                    new { Province = "Hà Nội", District = "Ba Đình", OrderValue = 100000m },
                    new { Province = "TP Hồ Chí Minh", District = "Quận 1", OrderValue = 250000m },
                    new { Province = "Thành phố Đà Nẵng", District = "Hải Châu", OrderValue = 400000m },
                    new { Province = "Tỉnh Cần Thơ", District = "Ninh Kiều", OrderValue = 600000m },
                    new { Province = "Quảng Ninh", District = "", OrderValue = 150000m }
                };

                var results = testCases.Select(test => new
                {
                    Input = test,
                    Result = _shippingService.GetShippingInfo(test.Province, test.District, test.OrderValue)
                }).ToArray();

                return Ok(new { 
                    message = "Shipping service test completed", 
                    testResults = results 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi test shipping service: {ex.Message}");
            }
        }

        [HttpGet("fee")]
        public ActionResult<decimal> GetShippingFee(
            [FromQuery] string province, 
            [FromQuery] string? district = null, 
            [FromQuery] decimal orderValue = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(province))
                {
                    return BadRequest("Tỉnh/thành phố không được để trống");
                }

                var shippingInfo = _shippingService.GetShippingInfo(province, district ?? "", orderValue);
                return Ok(shippingInfo.FinalFee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi tính phí vận chuyển: {ex.Message}");
            }
        }

        [HttpGet("provinces")]
        public ActionResult<IEnumerable<string>> GetSupportedProvinces()
        {
            var provinces = new[]
            {
                // Miền Bắc
                "Hà Nội", "Hải Phòng", "Quảng Ninh", "Thái Nguyên", "Vĩnh Phúc", 
                "Bắc Ninh", "Hưng Yên", "Hà Nam", "Nam Định", "Ninh Bình",
                
                // Miền Trung
                "Thanh Hóa", "Nghệ An", "Hà Tĩnh", "Quảng Bình", "Quảng Trị", 
                "Thừa Thiên Huế", "Đà Nẵng", "Quảng Nam", "Quảng Ngãi", "Bình Định", 
                "Phú Yên", "Khánh Hòa", "Ninh Thuận", "Bình Thuận",
                
                // Miền Nam
                "Hồ Chí Minh", "Bình Dương", "Đồng Nai", "Bà Rịa - Vũng Tàu", 
                "Long An", "Tiền Giang", "Bến Tre", "Vĩnh Long", "Trà Vinh", 
                "Cần Thơ", "Đồng Tháp", "An Giang", "Kiên Giang", "Cà Mau", 
                "Bạc Liêu", "Sóc Trăng", "Hậu Giang", "Tây Ninh", "Bình Phước", 
                "Đắk Lắk", "Đắk Nông", "Lâm Đồng", "Gia Lai", "Kon Tum"
            };

            return Ok(provinces.OrderBy(p => p));
        }

        [HttpGet("districts")]
        public ActionResult<IEnumerable<string>> GetDistricts([FromQuery] string province)
        {
            if (string.IsNullOrEmpty(province))
            {
                return BadRequest("Tỉnh/thành phố không được để trống");
            }

            var districts = GetDistrictsByProvince(province);
            return Ok(districts.OrderBy(d => d));
        }

        [HttpGet("wards")]
        public ActionResult<IEnumerable<string>> GetWards([FromQuery] string province, [FromQuery] string district)
        {
            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(district))
            {
                return BadRequest("Tỉnh/thành phố và quận/huyện không được để trống");
            }

            var wards = GetWardsByDistrict(province, district);
            return Ok(wards.OrderBy(w => w));
        }

        private IEnumerable<string> GetDistrictsByProvince(string province)
        {
            return province switch
            {
                "Hà Nội" => new[] { "Ba Đình", "Hoàn Kiếm", "Tây Hồ", "Long Biên", "Cầu Giấy", "Đống Đa", "Hai Bà Trưng", "Hoàng Mai", "Thanh Xuân", "Sóc Sơn", "Đông Anh", "Gia Lâm", "Nam Từ Liêm", "Bắc Từ Liêm", "Mê Linh", "Hà Đông", "Sơn Tây", "Ba Vì", "Phúc Thọ", "Đan Phượng", "Hoài Đức", "Quốc Oai", "Thạch Thất", "Chương Mỹ", "Thanh Oai", "Thường Tín", "Phú Xuyên", "Ứng Hòa", "Mỹ Đức" },
                "Hồ Chí Minh" => new[] { "Quận 1", "Quận 2", "Quận 3", "Quận 4", "Quận 5", "Quận 6", "Quận 7", "Quận 8", "Quận 9", "Quận 10", "Quận 11", "Quận 12", "Bình Thạnh", "Gò Vấp", "Phú Nhuận", "Tân Bình", "Tân Phú", "Thủ Đức", "Bình Tân", "Hóc Môn", "Củ Chi", "Nhà Bè", "Cần Giờ" },
                "Đà Nẵng" => new[] { "Hải Châu", "Thanh Khê", "Sơn Trà", "Ngũ Hành Sơn", "Liên Chiểu", "Cẩm Lệ", "Hòa Vang", "Hoàng Sa" },
                "Hải Phòng" => new[] { "Hồng Bàng", "Ngô Quyền", "Lê Chân", "Hải An", "Kiến An", "Đồ Sơn", "Dương Kinh", "Thuỷ Nguyên", "An Dương", "An Lão", "Kiến Thuỵ", "Tiên Lãng", "Vĩnh Bảo", "Cát Hải", "Bạch Long Vĩ" },
                "Cần Thơ" => new[] { "Ninh Kiều", "Ô Môn", "Bình Thuỷ", "Cái Răng", "Thốt Nốt", "Vĩnh Thạnh", "Cờ Đỏ", "Phong Điền", "Thới Lai" },
                _ => new[] { "Trung tâm", "Ngoại thành" }
            };
        }

        private IEnumerable<string> GetWardsByDistrict(string province, string district)
        {
            // Simplified ward data - in real application, this would come from a comprehensive database
            return district switch
            {
                "Ba Đình" => new[] { "Phúc Xá", "Trúc Bạch", "Vĩnh Phúc", "Cống Vị", "Liễu Giai", "Nguyễn Trung Trực", "Quán Thánh", "Ngọc Hà", "Điện Biên", "Đội Cấn", "Ngọc Khánh", "Kim Mã", "Giảng Võ", "Thành Công" },
                "Hoàn Kiếm" => new[] { "Phúc Tấn", "Đồng Xuân", "Hàng Mã", "Hàng Buồm", "Hàng Đào", "Hàng Bồ", "Cửa Đông", "Lý Thái Tổ", "Hàng Bạc", "Hàng Gai", "Chương Dương Độ", "Hàng Trống", "Cửa Nam", "Hàng Bông", "Tràng Tiền", "Trần Hưng Đạo", "Phan Chu Trinh" },
                "Quận 1" => new[] { "Tân Định", "Đa Kao", "Bến Nghé", "Bến Thành", "Nguyễn Thái Bình", "Phạm Ngũ Lão", "Cầu Ông Lãnh", "Cô Giang", "Nguyễn Cư Trinh", "Cầu Kho" },
                "Quận 2" => new[] { "Thảo Điền", "An Phú", "Bình An", "Bình Trưng Đông", "Bình Trưng Tây", "Bình Khánh", "An Lợi Đông", "Thạnh Mỹ Lợi", "Cát Lái", "Thủ Thiêm" },
                _ => new[] { "Phường 1", "Phường 2", "Phường 3", "Phường 4", "Phường 5" }
            };
        }

        [HttpGet("discount-info")]
        public ActionResult<object> GetDiscountInfo()
        {
            var discountTiers = new[]
            {
                new { MinOrderValue = 500000, DiscountPercent = 100, Description = "Miễn phí vận chuyển cho đơn hàng từ 500.000đ" },
                new { MinOrderValue = 300000, DiscountPercent = 50, Description = "Giảm 50% phí vận chuyển cho đơn hàng từ 300.000đ" },
                new { MinOrderValue = 200000, DiscountPercent = 20, Description = "Giảm 20% phí vận chuyển cho đơn hàng từ 200.000đ" }
            };

            return Ok(discountTiers);
        }
    }
}
