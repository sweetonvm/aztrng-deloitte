using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Deloitte.Training.Models;


namespace Deloitte.Training.Functions
{
    public class ServiceBusProcessor
    {
        private readonly ILogger<ServiceBusProcessor> _logger;

        public ServiceBusProcessor(ILogger<ServiceBusProcessor> logger)
        {
            _logger = logger;
        }

        [Function("ProcessTrainingMessage")]
        public async Task Run(
            [ServiceBusTrigger("trainingqueue", Connection = "ServiceBusConnectionString")] 
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            try
            {
                // Retrieve and log the message body
                string messageBody = message.Body.ToString();
                _logger.LogInformation($"ServiceBusProcessor received message: {messageBody}");

                // Deserialize the message
                var trainingData = JsonSerializer.Deserialize<TrainingData>(messageBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (trainingData == null)
                {
                    throw new JsonException("Deserialized object is null");
                }

                // Process the message
                await ProcessMessage(trainingData);

                // Complete the message processing
                await messageActions.CompleteMessageAsync(message);

                _logger.LogInformation($"Successfully processed message for {trainingData.Name}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing message");
                
                // Create a dictionary to store error details
                var errorDetails = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "ErrorType", "DeserializationError" },
                    { "ErrorMessage", ex.Message },
                    { "MessageBody", message.Body.ToString() }
                };

                // Dead-letter the message with error details
                await messageActions.DeadLetterMessageAsync(message, errorDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");

                // Create a dictionary to store error details
                var errorDetails = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "ErrorType", "ProcessingError" },
                    { "ErrorMessage", ex.Message },
                    { "StackTrace", ex.StackTrace ?? "No stack trace available" }
                };

                // Dead-letter the message with error details
                await messageActions.DeadLetterMessageAsync(message, errorDetails);
            }
        }

        private async Task ProcessMessage(TrainingData trainingData)
        {
            // Placeholder for business logic
            _logger.LogInformation($"Processing training data for: {trainingData.Name}");
            await Task.CompletedTask; // Remove or replace with actual async operations
        }
    }
}







// using System;
// using System.Text.Json;
// using System.Threading.Tasks;
// using Microsoft.Azure.Functions.Worker;
// using Microsoft.Extensions.Logging;
// using Azure.Messaging.ServiceBus;

// namespace Deloitte.Training.Functions
// {
//     public class ServiceBusProcessor
//     {
//         private readonly ILogger<ServiceBusProcessor> _logger;

//         public ServiceBusProcessor(ILogger<ServiceBusProcessor> logger)
//         {
//             _logger = logger;
//         }

//         [Function("ProcessTrainingMessage")]
//         public async Task Run(
//             [ServiceBusTrigger("training-queue", Connection = "ServiceBusConnectionString")] 
//             ServiceBusReceivedMessage message,
//             ServiceBusMessageActions messageActions)
//         {
//             try
//             {
//                 string messageBody = message.Body.ToString();
//                 _logger.LogInformation($"ServiceBusProcessor received message: {messageBody}");

//                 // Deserialize the message
//                 var trainingData = JsonSerializer.Deserialize<TrainingData>(messageBody);

//                 // Process the message
//                 await ProcessMessage(trainingData);

//                 // Complete the message
//                 await messageActions.CompleteMessageAsync(message);

//                 _logger.LogInformation($"Successfully processed message for {trainingData?.Name}");
//             }
//             catch (JsonException ex)
//             {
//                 _logger.LogError(ex, "Error deserializing message");
//                 // Dead-letter the message
//                 await messageActions.DeadLetterMessageAsync(message, ex.Message);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error processing message");
//                 // Dead-letter the message
//                 await messageActions.DeadLetterMessageAsync(message, ex.Message);
//             }
//         }

//         private async Task ProcessMessage(TrainingData trainingData)
//         {
//             // Add your business logic here
//             // For example:
//             // - Additional validation
//             // - Data transformation
//             // - Database operations
//             // - External API calls

//             await Task.CompletedTask; // Remove this when you add actual async operations
//         }
//     }
// }