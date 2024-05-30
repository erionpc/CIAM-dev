using System.Text.Json.Serialization;

namespace CiamDev.AF.DTOs 
{
    public class Action
    {
        [JsonPropertyName("@odata.type")]
        public string Odatatype { get; set; } = "microsoft.graph.tokenIssuanceStart.provideClaimsForToken";
        [JsonPropertyName("claims")]
        public ClaimsCollection Claims { get; set; } = new();
    }
}