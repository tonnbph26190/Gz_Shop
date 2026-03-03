namespace QuanView.Areas.Admin.Models
{
    /// <summary>Kết quả import CSV: từng dòng thành công hoặc lỗi kèm lý do.</summary>
    public class ImportCsvResultViewModel
    {
        /// <summary>Dòng đã nhập (để hiển thị lại trong textarea nếu cần).</summary>
        public string? CsvContent { get; set; }

        /// <summary>Các dòng xử lý thành công: (số dòng, mô tả).</summary>
        public List<ImportSuccessItem> SuccessList { get; set; } = new();

        /// <summary>Các dòng lỗi: (số dòng, lý do lỗi).</summary>
        public List<ImportErrorItem> ErrorList { get; set; } = new();

        /// <summary>Số dòng bỏ qua (trống hoặc không đủ cột).</summary>
        public int SkippedCount { get; set; }

        public bool HasResult => SuccessList.Count > 0 || ErrorList.Count > 0 || SkippedCount > 0;
    }

    public class ImportSuccessItem
    {
        public int LineNumber { get; set; }
        public string Message { get; set; } = "";
    }

    public class ImportErrorItem
    {
        public int LineNumber { get; set; }
        public string Reason { get; set; } = "";
        public string? RowPreview { get; set; }
    }
}
