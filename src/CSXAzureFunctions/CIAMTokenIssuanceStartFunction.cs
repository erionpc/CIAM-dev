using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ThirdSpace.BT.AzureFunctions.CommunicationManagement
{
    public class CIAMTokenIssuanceStartFunction
    {
        public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
        {
            log.LogInformation("CIAMTokenIssuanceStart function was triggered.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic request = JsonConvert.DeserializeObject(requestBody);

            string requestString = request.ToString();
            log.LogDebug("CIAMTokenIssuanceStart request:{newline}{request}", Environment.NewLine, requestString);

            // Read the correlation ID from the Microsoft Entra request    
            string correlationId = request?.data?.authenticationContext?.correlationId;

            // Claims to return to Microsoft Entra
            ResponseContent response = new ResponseContent();
            response.Data.Actions[0].Claims.CorrelationId = correlationId;
            response.Data.Actions[0].Claims.ApiVersion = "1.0.0";
            response.Data.Actions[0].Claims.CustomRoles.Add("Writer");
            response.Data.Actions[0].Claims.CustomRoles.Add("Editor");

            string responseString = JsonConvert.SerializeObject(response);
            log.LogDebug("CIAMTokenIssuanceStart response:{newline}{response}", Environment.NewLine, responseString);

            return new OkObjectResult(response);
        }

        public class ResponseContent
        {
            [JsonProperty("data")]
            public Data Data { get; set; } = new Data();
        }

        public class Data
        {
            [JsonProperty("@odata.type")]
            public string ODataType { get; set; } = "microsoft.graph.onTokenIssuanceStartResponseData";
            [JsonProperty("actions")]
            public List<Action> Actions { get; set; } = new List<Action>() { new Action() };            
        }

        public class Action
        {
            [JsonProperty("@odata.type")]
            public string ODataType { get; set; } = "microsoft.graph.tokenIssuanceStart.provideClaimsForToken";
            [JsonProperty("claims")]
            public Claims Claims { get; set; } = new Claims();            
        }

        public class Claims
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string CorrelationId { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string ApiVersion { get; set; }
            public List<string> CustomRoles { get; set; } = new List<string>();
        }
    }
}
