using System;
using System.Collections.Generic;
using System.Linq;


namespace QuanApi.Dtos
{
 
    public class PagedFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "asc"; 


        public string ToQueryString()
        {
            var properties = GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var queryParams = new List<string>();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(this, null);
                if (value != null)
                {
                    if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                    {
                        queryParams.Add($"{prop.Name}={((DateTime)value):yyyy-MM-dd}");
                    }
                    else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                    {
                        queryParams.Add($"{prop.Name}={value.ToString()!.ToLower()}");
                    }
                    else if (prop.PropertyType == typeof(Guid) || prop.PropertyType == typeof(Guid?))
                    {
                        queryParams.Add($"{prop.Name}={value.ToString()}");
                    }
                    else
                    {
                        queryParams.Add($"{prop.Name}={Uri.EscapeDataString(value.ToString()!)}");
                    }
                }
            }
            return string.Join("&", queryParams);
        }
    }
}