using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CsvSearchFunction.Functions
{
    public class CsvSearchFunction
    {
        private readonly ILogger<CsvSearchFunction> _logger;

        public CsvSearchFunction(ILogger<CsvSearchFunction> logger)
        {
            _logger = logger;
        }

        [Function("CsvSearchFunction")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
