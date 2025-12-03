using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class District
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // Mã quận/huyện
        public int ProvinceId { get; set; }

        [JsonIgnore]
        public Province? Province { get; set; }

        [JsonIgnore]
        public ICollection<Ward> Wards { get; set; } = new List<Ward>();
    }
}









