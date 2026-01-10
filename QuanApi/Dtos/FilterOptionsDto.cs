namespace QuanApi.Dtos
{
    public class FilterOptionsDto
    {
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> Sizes { get; set; } = new List<string>();
        public List<string> Colors { get; set; } = new List<string>();
    }
}
