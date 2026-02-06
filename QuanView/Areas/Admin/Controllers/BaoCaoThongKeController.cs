using Microsoft.AspNetCore.Mvc;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin")] // Route tùy chỉnh để truy cập qua /Admin/ThongKe
    public class BaoCaoThongKeController : Controller
    {
        [HttpGet("ThongKe")]
        public IActionResult ThongKe()
        {
            return View("~/Areas/Admin/Views/BaoCaoThongKe/ThongKe.cshtml");
        }
    }
}
