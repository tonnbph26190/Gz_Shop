using Microsoft.AspNetCore.Mvc;
using QuanApi.Services;

[ApiController]
[Route("api/[controller]")]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _service;

    public ShippingController(IShippingService service)
    {
        _service = service;
    }

    [HttpGet("calculate")]
    public IActionResult Calculate(
        string province,
        string district,
        decimal orderValue)
    {
        if (string.IsNullOrEmpty(province))
            return BadRequest("Province không được rỗng");

        return Ok(_service.CalculateShipping(province, district, orderValue));
    }

    [HttpGet("provinces")]
    public IActionResult Provinces()
        => Ok(_service.GetProvinces());

    [HttpGet("districts")]
    public IActionResult Districts(string province)
        => Ok(_service.GetDistricts(province));

    [HttpGet("wards")]
    public IActionResult Wards(string province, string district)
        => Ok(_service.GetWards(province, district));

    [HttpGet("discount-info")]
    public IActionResult DiscountInfo()
        => Ok(_service.GetDiscountInfo());
}
