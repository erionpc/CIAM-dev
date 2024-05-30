using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CIAMAzureFunctions
{
    public class CiamAttributeCollectionStartFunction
    {
        public static async Task<object> Run(HttpRequest req, ILogger log)
        {
            log.LogInformation("CiamAttributeCollectionStart function was triggered");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic request = JsonConvert.DeserializeObject(requestBody);

            string requestString = request.ToString();
            log.LogDebug("CiamAttributeCollectionStart request:{newline}{request}", Environment.NewLine, requestString);

            var actions = new List<ICIAMAction>();

            if (ShouldActionBeBlocked(request, log))
            {
                actions.Add(new BlockedAction());
            }
            else
            {
                actions.Add(new PrefillValuesAction());
            }

            var dataObject = new Data
            {
                Actions = actions
            };

            dynamic response = new ResponseObject
            {
                Data = dataObject
            };

            string responseString = JsonConvert.SerializeObject(response);
            log.LogDebug("CiamAttributeCollectionStart response:{newline}{request}", Environment.NewLine, responseString);

            return response;
        }

        private static bool ShouldActionBeBlocked(dynamic request, ILogger log)
        {
            string emailAddress = request?.data?.userSignUpInfo?.identities[0]?.issuerAssignedId;
            log.LogDebug("CiamAttributeCollectionStart request emailAddress: {emailAddress}", emailAddress);
            bool shouldBlock = false;
            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                string domain = emailAddress.Split('@')[1].ToLowerInvariant();
                log.LogDebug("CiamAttributeCollectionStart request emailAddress domain: {domain}", domain);

                string domainBlacklistConfig = Environment.GetEnvironmentVariable("DomainBlacklist", EnvironmentVariableTarget.Process);
                log.LogDebug("CiamAttributeCollectionStart DomainBlacklist config: {config}", domainBlacklistConfig);
                string[] domainBlacklist = domainBlacklistConfig.Split(',');

                if (domainBlacklist.Contains(domain))
                {
                    shouldBlock = true;
                    log.LogDebug("CiamAttributeCollectionStart request emailAddress domain is in blacklist: {domain}. Sign-up will be blocked.", domain);
                }
                else
                {
                    log.LogDebug("CiamAttributeCollectionStart request emailAddress domain is not in blacklist: {domain}. Sign-up can continue.", domain);
                }
            }

            return shouldBlock;
        }

        public class ResponseObject
        {
            [JsonProperty("data")]
            public Data Data { get; set; }
        }

        [JsonObject]
        public class Data
        {
            [JsonProperty("@odata.type")]
            public string Type { get; set; } = "microsoft.graph.onAttributeCollectionStartResponseData";
            [JsonProperty("actions")]
            public List<ICIAMAction> Actions { get; set; }
        }

        public interface ICIAMAction
        {
            public string Type { get; set; }
        }

        [JsonObject]
        public class ContinueWithDefaultBehaviorAction : ICIAMAction
        {
            [JsonProperty("@odata.type")]
            public string Type { get; set; } = "microsoft.graph.attributeCollectionStart.continueWithDefaultBehavior";
        }

        [JsonObject]
        public class PrefillValuesAction : ICIAMAction
        {
            [JsonProperty("@odata.type")]
            public string Type { get; set; } = "microsoft.graph.attributeCollectionStart.setPrefillValues";
            [JsonProperty("inputs")]
            public Dictionary<string, object> Inputs { get; set; } = new Dictionary<string, object>() 
            {
                { "extension_389b6ccf3e334181a4b44dc9bc8ea36d_Dateofbirth", DateTime.UtcNow.ToString("dd/MM/yyyy") }
            };
        }

        [JsonObject]
        public class BlockedAction : ICIAMAction
        {
            [JsonProperty("@odata.type")]
            public string Type { get; set; } = "microsoft.graph.attributeCollectionStart.showBlockPage";
            [JsonProperty("message")]
            public string Message { get; set; } = "Sorry, your access request has been blocked. Try reaching an admin at admin@kochoepciam.onmicrosoft.com.";
        }
    }
}