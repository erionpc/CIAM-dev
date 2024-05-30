using System.Text.Json.Serialization;

namespace CiamDev.AF.DTOs 
{
    public class ResponseData
    {
        [JsonPropertyName("@odata.type")]
        public string Odatatype { get; set; } = "microsoft.graph.onTokenIssuanceStartResponseData";
        [JsonPropertyName("actions")]
        public List<Action> Actions { get; set; } = [new Action()];
    }
}