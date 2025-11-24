using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class Ward
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // Mã phường/xã
        public int DistrictId { get; set; }

        [JsonIgnore]
        public District? District { get; set; }
    }
}


