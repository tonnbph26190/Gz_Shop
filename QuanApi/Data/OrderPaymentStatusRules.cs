using System;

namespace QuanApi.Data
{
    public static class OrderPaymentStatusRules
    {
        public const string PendingRefundStatus = "Chờ hoàn tiền";
        public const string RefundedStatus = "Đã hoàn tiền";
        public const string CanceledStatus = "Đã hủy";
        public const string PaidStatusNoAccent = "DaThanhToan";
        public const string PaidStatus = "Đã thanh toán";
        public const string PaidPendingConfirmStatus = "Đã thanh toán chờ xác nhận";
        public const string PaymentPaidStatus = "Đã thanh toán";

        public static bool IsPaidOrder(string? orderStatus, string? paymentStatus)
        {
            var isPaidByPaymentStatus =
                string.Equals(paymentStatus, PaymentPaidStatus, StringComparison.OrdinalIgnoreCase);
            var isPaidByOrderStatus =
                string.Equals(orderStatus, PaidStatusNoAccent, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(orderStatus, PaidStatus, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(orderStatus, PaidPendingConfirmStatus, StringComparison.OrdinalIgnoreCase);

            return isPaidByPaymentStatus || isPaidByOrderStatus;
        }

        public static bool IsRefundRequested(string? status)
        {
            return string.Equals(status, PendingRefundStatus, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRefunded(string? status)
        {
            return string.Equals(status, RefundedStatus, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCanceled(string? status)
        {
            return string.Equals(status, CanceledStatus, StringComparison.OrdinalIgnoreCase);
        }
    }
}
