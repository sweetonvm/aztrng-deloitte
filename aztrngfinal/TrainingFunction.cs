using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.ServiceBus;
using System.Text;
using Microsoft.AspNetCore.Http;

public class Function1
{
    private readonly ILogger _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly QueueClient _queueClient; // Change to QueueClient

    public Function1(ILoggerFactory loggerFactory, CosmosClient cosmosClient, QueueClient queueClient)
    {
        _logger = loggerFactory.CreateLogger<Function1>();
        _cosmosClient = cosmosClient; // Use CosmosClient
        _queueClient = queueClient; // Use QueueClient
    }

    [Function("Function1")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) // Change HttpRequestData to HttpRequest
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var payload = JsonConvert.DeserializeObject<Payload>(requestBody);

        // Create document in Cosmos DB
        var container = _cosmosClient.GetContainer("databaseId", "containerId");
        await container.CreateItemAsync(payload);

        // Send message to Service Bus
        var message = new Message(Encoding.UTF8.GetBytes(requestBody))
        {
            ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddHours(1) // Hold for 1 hour
        };
        await _queueClient.SendAsync(message);

        return new OkObjectResult("Document created and message sent.");
    }
}

public class Payload
{
    public required string Name { get; set; }
    public required string Company { get; set; }
    public required string Purpose { get; set; }
}