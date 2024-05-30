using System.Text.Json.Serialization;

namespace CiamDev.AF.DTOs 
{
    public class ResponseContent
    {
        [JsonPropertyName("data")]
        public ResponseData Data { get; set; } = new();
    }
}