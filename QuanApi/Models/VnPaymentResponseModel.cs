namespace QuanView.Areas.Admin.Models
{
    public class VnPaymentResponseModel
    {
        public bool Success { get; set; }
        public string PaymentMethod { get; set; }
        public string OrderDescription { get; set; }
        public string OrderId { get; set; }
        public string PaymentId { get; set; }
        public string TransactionId { get; set; }
        public string Token { get; set; }
        public string VnPayResponseCode { get; set; }
    }

    public class VnPaymentRequestModel
    {
        public int OrderId { get; set; }
        public string Fullname { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }  // Số tiền cần thanh toán
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

}
