namespace GiaLaiOCOP.Api.Dtos
{
    public class DistrictDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int ProvinceId { get; set; }
    }
}

