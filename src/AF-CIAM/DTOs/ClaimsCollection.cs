using System.Text.Json.Serialization;

namespace CiamDev.AF.DTOs 
{
    public class ClaimsCollection
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CorrelationId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DateOfBirth { get; set; }
        public string? ApiVersion { get; set; }
        public List<string> CustomRoles { get; set; } = [];
    }
}