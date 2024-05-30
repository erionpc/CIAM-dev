using System;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using CiamDev.AF.DTOs;

namespace CiamDev.AF
{
    public class EnrichToken
    {
        private readonly ILogger<EnrichToken> _logger;

        private const string _FunctionName = "EnrichToken";

        public EnrichToken(ILogger<EnrichToken> logger)
        {
            _logger = logger;
        }
        
        [Function(_FunctionName)]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("{function} function processed a request.", _FunctionName);

            try 
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic deserialisedRequest = JsonSerializer.Deserialize<ExpandoObject>(requestBody)!;                
                
                JsonElement data = deserialisedRequest?.data;
                data.TryGetProperty("authenticationContext", out JsonElement authenticationContext);
                authenticationContext.TryGetProperty("correlationId", out JsonElement correlationId);

                ResponseContent response = new();
                response.Data.Actions[0].Claims.CorrelationId = !string.IsNullOrEmpty(correlationId.ToString()) ? correlationId.ToString() : string.Empty;
                response.Data.Actions[0].Claims.ApiVersion = "1.0.0";
                response.Data.Actions[0].Claims.DateOfBirth = "01/01/2000";
                response.Data.Actions[0].Claims.CustomRoles.Add("Writer");
                response.Data.Actions[0].Claims.CustomRoles.Add("Editor");

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the {function} function.", _FunctionName);
                return new BadRequestObjectResult("An error occurred. Please try again later.");
            }
        }
    }
}
