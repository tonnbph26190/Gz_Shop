namespace QuanApi.Helpers
{
    /// <summary>
    /// Thời gian theo múi giờ Việt Nam (UTC+7).
    /// </summary>
    public static class VietnamTime
    {
        public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

        /// <summary>Thời điểm hiện tại theo giờ Việt Nam (UTC+7).</summary>
        public static DateTime Now() => DateTime.UtcNow.Add(Offset);

        /// <summary>Chuyển thời điểm UTC sang giờ Việt Nam để so sánh/hiển thị.</summary>
        public static DateTime FromUtc(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
            return utc.Add(Offset);
        }
    }
}
