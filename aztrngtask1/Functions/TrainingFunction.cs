using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Azure.Messaging.EventGrid;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Deloitte.Training.Models;

namespace Deloitte.Training.Functions
{
    public class TrainingFunction
    {
        private readonly ILogger<TrainingFunction> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly EventGridPublisherClient _eventGridClient;

        public TrainingFunction(
            CosmosClient cosmosClient,
            ServiceBusClient serviceBusClient,
            EventGridPublisherClient eventGridClient,
            ILogger<TrainingFunction> logger)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _serviceBusClient = serviceBusClient;
            _eventGridClient = eventGridClient;
        }

        [Function("ProcessTrainingRequest")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ProcessTrainingRequest")] HttpRequestData req)
        {
            _logger.LogInformation("Processing training request...");

            try
            {
                // Read request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"Request Body: {requestBody}");

                var trainingData = JsonSerializer.Deserialize<TrainingData>(requestBody);

                _logger.LogInformation("Successfully deserialized training data.");

                // 1. Save to Cosmos DB
                var container = _cosmosClient.GetContainer("aztrngfinal", "testcontainer");
                await container.CreateItemAsync(new
                {
                    id = Guid.NewGuid().ToString(),
                    name = trainingData.Name,
                    company = trainingData.Company,
                    purpose = trainingData.Purpose,
                    timestamp = DateTime.UtcNow
                });

                // 2. Send to Service Bus
                var sender = _serviceBusClient.CreateSender("trainingqueue");
                var message = new ServiceBusMessage(requestBody)
                {
                    TimeToLive = TimeSpan.FromMinutes(1) // Message will move to DLQ after 1 hour
                };
                _logger.LogInformation("Sending message to Service Bus...");
                await sender.SendMessageAsync(message);
                _logger.LogInformation("Message successfully sent to Service Bus.");
                
                // 3. Publish to Event Grid
                var eventGridEvent = new EventGridEvent(
                    "TrainingRequest",
                    "Training.NewRequest",
                    "1.0",
                    requestBody);
                _logger.LogInformation("Publishing to Event Grid...");
                await _eventGridClient.SendEventAsync(eventGridEvent);
                _logger.LogInformation("Event successfully published to Event Grid.");
                
                // Create success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { 
                    message = "Request processed successfully",
                    requestId = Guid.NewGuid().ToString()
                });

                _logger.LogInformation("Function completed successfully.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing the request.");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteStringAsync($"Error processing request: {ex.Message}");
                return response;
            }
        }
    }
}
