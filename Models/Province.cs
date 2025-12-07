using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class Province
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // Mã tỉnh/thành phố

        [JsonIgnore]
        public ICollection<District> Districts { get; set; } = new List<District>();
    }
}
















