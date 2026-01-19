using System.Collections.Generic;

namespace QuanApi.Dtos
{

    public class PagedResultGeneric<T>
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public List<T> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
