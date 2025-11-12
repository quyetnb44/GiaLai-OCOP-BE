namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO trả về các options cho filter
    /// </summary>
    public class FilterOptionsDto
    {
        public List<string> Districts { get; set; } = new List<string>();
        public List<string> Provinces { get; set; } = new List<string>();
        public List<string> BusinessFields { get; set; } = new List<string>();
        public List<int> OCOPRatings { get; set; } = new List<int> { 3, 4, 5 };
    }
}

