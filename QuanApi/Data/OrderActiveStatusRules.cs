using System;
using System.Collections.Generic;

namespace QuanApi.Data
{
    /// <summary>
    /// Trạng thái đơn hàng đang xử lý (chưa kết thúc, chưa hủy).
    /// Dùng khi đếm đơn liên quan tới sản phẩm ngưng bán hoặc hiển thị reservation.
    /// </summary>
    public static class OrderActiveStatusRules
    {
        public static readonly HashSet<string> ActiveOrderStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Chờ xác nhận",
            "Đã thanh toán chờ xác nhận",
            "DaThanhToan",
            "Đã thanh toán",
            "Đã xác nhận",
            "Chờ lấy hàng",
            "Đang giao",
            "Đã giao",
            "Đã lấy hàng",
            "Chờ giao hàng",
            "Đang giao hàng",
            "Giao hàng thành công"
        };

        public static bool IsActiveOrderStatus(string? status)
        {
            return !string.IsNullOrWhiteSpace(status) && ActiveOrderStatuses.Contains(status.Trim());
        }
    }
}
