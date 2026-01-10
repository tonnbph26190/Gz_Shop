using QuanView.Models;

namespace QuanView.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        // Overload cho phép chỉ định ReturnUrl riêng (dùng cho POS)
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context, string returnUrl);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
