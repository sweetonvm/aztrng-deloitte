using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
 
namespace ServiceBusToLogicAppFunction
{
    public class ServiceBusToLogicAppFunction
    {
        private readonly ILogger<ServiceBusToLogicAppFunction> _logger;
 
        public ServiceBusToLogicAppFunction(ILogger<ServiceBusToLogicAppFunction> logger)
        {
            _logger = logger;
        }
 
        [Function(nameof(ServiceBusToLogicAppFunction))]
        public async Task Run(
            [ServiceBusTrigger("sbqueue", Connection = "Endpoint=sb://aztrng1212.servicebus.windows.net/;SharedAccessKeyName=aztrng1212;SharedAccessKey=jqJlmoEwq9zb3Bowv7kDjw5x3lPTXLFFQ+ASbP+BCyU=;EntityPath=sbqueue")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            // Log message details
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);
 
            try
            {
                // Convert the message body to a string (assuming it's in JSON format)
                string messageBody = message.Body.ToString();
 
                // Convert the message body to XML format (you can modify this to match your XML structure)
                string xmlMessage = ConvertToXml(messageBody);
 
                // Log the generated XML message
                _logger.LogInformation("Converted XML Message: {xmlMessage}", xmlMessage);
 
                // Send the XML message to the Service Bus queue (this is where it will be picked up by Logic App)
                // The message is already in the Service Bus queue, so no need to send it again here
 
                // Complete the message to remove it from the queue
                await messageActions.CompleteMessageAsync(message);
                _logger.LogInformation("Message successfully completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error processing the message: {error}", ex.Message);
                // Optionally, handle message dead-lettering or other error actions
            }
        }
 
        // Helper method to convert a message to XML format
        private string ConvertToXml(string messageBody)
        {
            // Simple conversion to XML format. You can modify this to suit your specific XML structure.
            return $@"
<Message>
<Content>{messageBody}</Content>
</Message>";
        }
    }
}

// using Microsoft.Azure.Functions.Worker;
// using Microsoft.Extensions.Logging;
// using System.Net.Http;
// using System.Text;

// public class ServiceBusToLogicAppFunction
// {
//     private readonly HttpClient _httpClient;
//     private readonly ILogger<ServiceBusToLogicAppFunction> _logger;

//     public ServiceBusToLogicAppFunction(ILogger<ServiceBusToLogicAppFunction> logger, IHttpClientFactory httpClientFactory)
//     {
//         _logger = logger;
//         _httpClient = httpClientFactory.CreateClient();
//     }

//     [Function("ServiceBusToLogicApp")]
//     public async Task RunAsync(
//         [ServiceBusTrigger("sbqueue", Connection = "ServiceBusListenConnection")] string message)
//     {
//         _logger.LogInformation($"Received message from Service Bus: {message}");

//         var logicAppUrl = Environment.GetEnvironmentVariable("LogicAppUrl");

//         if (string.IsNullOrWhiteSpace(logicAppUrl))
//         {
//             _logger.LogError("Logic App URL is not configured.");
//             return;
//         }

//         try
//         {
//             var content = new StringContent(message, Encoding.UTF8, "application/xml");
//             var response = await _httpClient.PostAsync(logicAppUrl, content);

//             if (response.IsSuccessStatusCode)
//             {
//                 _logger.LogInformation("Message successfully sent to Logic App.");
//             }
//             else
//             {
//                 _logger.LogError($"Logic App returned an error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError($"Error sending message to Logic App: {ex.Message}");
//         }
//     }
// }
