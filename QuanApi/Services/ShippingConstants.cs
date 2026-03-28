namespace QuanApi.Services
{
    public static class ShippingFeeSources
    {
        public const string Config = "Config";
        public const string Ghn = "GHN";

        public static string Normalize(string? value)
        {
            // Business rule: disable external shipping API source and always use configured zone fees.
            return Config;
        }
    }

    public static class ShippingFeeZones
    {
        public const string NoiThanh = "NoiThanh";
        public const string NgoaiThanh = "NgoaiThanh";
        public const string ToanQuoc = "ToanQuoc";
        public const string Legacy = "Legacy";
    }
}
