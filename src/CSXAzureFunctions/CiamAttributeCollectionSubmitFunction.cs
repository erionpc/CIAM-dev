using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace ThirdSpace.BT.AzureFunctions.CommunicationManagement
{
    public class CiamAttributeCollectionSubmitFunction
    {
        public static async Task<object> Run(HttpRequest req, ILogger log)
        {
            log.LogInformation("CiamAttributeCollectionSubmit function was triggered");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic request = JsonConvert.DeserializeObject(requestBody);

            string requestString = request.ToString();
            log.LogDebug("CiamAttributeCollectionSubmit request:{newline}{request}", Environment.NewLine, requestString);

            var actions = new List<ICIAMAction>();

            // validate request
            Dictionary<string, object> requestValidation = IsRequestValid(request, log, out DateTime? dateOfBirth);
            if (requestValidation.Count > 0)
            {
                actions.Add(new ValidationErrorAction(requestValidation));
            }
            // Modify attributes if validation passes
            else
            {
                Dictionary<string, object> modifiedAttributes = ModifyAttributes(dateOfBirth.Value, log);
                actions.Add(new ModifyAttributeValuesAction(modifiedAttributes));
            }

            dynamic response = new ResponseObject
            {
                Data = new Data()
                {
                    Actions = actions
                }
            };

            string responseString = JsonConvert.SerializeObject(response);
            log.LogDebug("CiamAttributeCollectionSubmit response:{newline}{response}", Environment.NewLine, responseString);

            return response;
        }

        private static Dictionary<string, object> IsRequestValid(dynamic request, ILogger log, out DateTime? dateOfBirth)
        {
            Dictionary<string, object> validationErrors = new Dictionary<string, object>();
            ValidateChallenge(request, log, validationErrors);
            dateOfBirth = ValidateDateOfBirth(request, log, validationErrors);

            if (validationErrors.Count == 0)
            {
                log.LogDebug("CiamAttributeCollectionSubmit request validation succeeded. Sign-up can continue.");
            }            

            return validationErrors;
        }

        private static void ValidateChallenge(dynamic request, ILogger log, Dictionary<string, object> validationErrors)
        {
            object challengeResponse = request?.data?.userSignUpInfo?.attributes?.extension_389b6ccf3e334181a4b44dc9bc8ea36d_Challenge?.value;
            log.LogDebug("CiamAttributeCollectionSubmit request challenge response: {challengeResponse}", challengeResponse.ToString());

            if (challengeResponse == null)
            {
                validationErrors.Add("extension_389b6ccf3e334181a4b44dc9bc8ea36d_Challenge", "No challenge response was provided");
                log.LogDebug("CiamAttributeCollectionSubmit request validation failed: no challenge response was provided");
                
                return;
            }

            bool challengeResponseParsed = int.TryParse(challengeResponse.ToString(), out int challengeResponseValue);
            if (!challengeResponseParsed)
            {
                validationErrors.Add("extension_389b6ccf3e334181a4b44dc9bc8ea36d_Challenge", "The challenge response is incorrect.");
                log.LogDebug("CiamAttributeCollectionSubmit request validation failed: the challenge response is incorrect. Expected integer value.");

                return;
            }

            _ = int.TryParse(Environment.GetEnvironmentVariable("ChallengeResponseExpected", EnvironmentVariableTarget.Process), out int challengeResponseExpected);

            log.LogDebug("CiamAttributeCollectionSubmit challenge response expected: {expected}", challengeResponseExpected);

            if (challengeResponseValue != challengeResponseExpected)
            {
                validationErrors.Add("extension_389b6ccf3e334181a4b44dc9bc8ea36d_Challenge", "Challenge response is incorrect");
                log.LogDebug("CiamAttributeCollectionSubmit request validation failed: expected challenge response {expected}, but got {actual}", challengeResponseExpected, challengeResponseValue);
            }
        }

        private static DateTime? ValidateDateOfBirth(dynamic request, ILogger log, Dictionary<string, object> validationErrors)
        {
            object dateOfBirthInput = request?.data?.userSignUpInfo?.attributes?.extension_389b6ccf3e334181a4b44dc9bc8ea36d_Dateofbirth?.value;
            log.LogDebug("CiamAttributeCollectionSubmit date of birth: {date}", dateOfBirthInput.ToString());

            bool dateParsed = DateTime.TryParseExact(dateOfBirthInput.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dateOfBirth);

            if (!dateParsed)
            {
                validationErrors.Add("extension_389b6ccf3e334181a4b44dc9bc8ea36d_Dateofbirth", "No valid date of birth was provided. Expected format: dd/MM/yyyy.");
                log.LogDebug("CiamAttributeCollectionSubmit request validation failed: no valid date of birth was provided");
                
                return null;
            }

            return dateOfBirth;
        }

        private static Dictionary<string, object> ModifyAttributes(DateTime dateOfBirth, ILogger log)
        {
            Dictionary<string, object> modifiedAttributes = new Dictionary<string, object>();
            int age = DateTime.UtcNow.Year - dateOfBirth.Year;
            
            modifiedAttributes.Add("extension_389b6ccf3e334181a4b44dc9bc8ea36d_Dateofbirth", age.ToString());
            log.LogDebug("CiamAttributeCollectionSubmit modified date of birth {date} to age in years {age}", dateOfBirth.ToString("dd/MM/yyyy"), age);

            return modifiedAttributes;
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
            public string Type { get; set; } = "microsoft.graph.onAttributeCollectionSubmitResponseData";
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
            public string Type { get; set; } = "microsoft.graph.attributeCollectionSubmit.continueWithDefaultBehavior";
        }

        [JsonObject]
        public class ValidationErrorAction : ICIAMAction
        {
            [JsonProperty("@odata.type")]
            public string Type { get; set; } = "microsoft.graph.attributeCollectionSubmit.ShowValidationError";
            [JsonProperty("inputs")]
            public Dictionary<string, object> Inputs { get; set; } = new Dictionary<string, object>();
            [JsonProperty("message")]
            public string Message { get; set; } = "Please fix below errors to proceed";

            public ValidationErrorAction()
            {
            }

            public ValidationErrorAction(Dictionary<string, object> errors)
            {
                this.Inputs = errors;
            }
        }

        [JsonObject]
        public class ModifyAttributeValuesAction : ICIAMAction
        {
            [JsonProperty("@odata.type")]
            public string Type { get; set; } = "microsoft.graph.attributeCollectionSubmit.modifyAttributeValues";
            [JsonProperty("attributes")]
            public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

            public ModifyAttributeValuesAction()
            {
            }

            public ModifyAttributeValuesAction(Dictionary<string, object> attributes)
            {
                this.Attributes = attributes;
            }
        }

        [JsonObject]
        public class BlockedAction : ICIAMAction
        {
            [JsonProperty("@odata.type")]
            public string Type { get; set; } = "microsoft.graph.attributeCollectionSubmit.showBlockPage";
            [JsonProperty("message")]
            public string Message { get; set; } = "Thank you for your response. Your access request is processing. You'll be notified when your request has been approved.";
        }
    }
}