using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using System.Xml.Linq;

namespace HttptoServiceBus
{
    public class HttptoServiceBus
    {
        private readonly ILogger<HttptoServiceBus> _logger;
        private readonly string _serviceBusConnectionString = "Endpoint=sb://aztrng1212.servicebus.windows.net/;SharedAccessKeyName=aztrng1212;SharedAccessKey=jqJlmoEwq9zb3Bowv7kDjw5x3lPTXLFFQ+ASbP+BCyU=;EntityPath=sbqueue";  // Replace with your connection string
        private readonly string _queueName = "sbqueue";  // Replace with your queue name

        public HttptoServiceBus(ILogger<HttptoServiceBus> logger)
        {
            _logger = logger;
        }
 
        [Function("HttptoServiceBus")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
 
            try
            {
                // Get the request body (either JSON or plain text)
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
 
                // Convert the request body to XML format
                XDocument xmlMessage = ConvertToXml(requestBody);
 
                // Log the generated XML message
                _logger.LogInformation($"Generated XML Message: {xmlMessage}");
 
                // Send the XML message to Azure Service Bus
                await SendMessageToServiceBus(xmlMessage.ToString());
 
                // Return a successful response
                return new OkObjectResult($"Message processed and sent to Service Bus: {xmlMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing the request: {ex.Message}");
                return new StatusCodeResult(500); // Internal server error
            }
        }
 
        // Method to convert the message to XML format
        private XDocument ConvertToXml(string messageBody)
        {
            // Convert the request body into XML format
            return new XDocument(
                new XElement("Message",
                    new XElement("Content", messageBody)
                )
            );
        }
 
        // Method to send the XML message to Service Bus
        private async Task SendMessageToServiceBus(string xmlMessage)
        {
            try
            {
                // Initialize the Service Bus client
                await using var client = new ServiceBusClient(_serviceBusConnectionString);
 
                // Create a sender for the queue
                var sender = client.CreateSender(_queueName);
 
                // Create a Service Bus message with the XML content
                var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(xmlMessage))
                {
                    ContentType = "application/xml"
                };
 
                // Send the message to the Service Bus Queue
                await sender.SendMessageAsync(message);
 
                // Log the success of the operation
                _logger.LogInformation("Message successfully sent to Service Bus.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message to Service Bus: {ex.Message}");
                throw;
            }
        }
    }
}

// using Azure.Messaging.ServiceBus;
// using Microsoft.Azure.Functions.Worker;
// using Microsoft.Azure.Functions.Worker.Http;
// using Microsoft.Extensions.Logging;
// using System.Text;
// using System.Text.Json;
// using System.Threading.Tasks;
// using System.Xml.Linq;

// public class HttptoServiceBus
// {
//     private readonly ILogger<HttptoServiceBus> _logger;
//     private readonly string _serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusSendConnection");
//     private readonly string _queueName = "sbqueue"; // Replace with your queue name

//     public HttptoServiceBus(ILogger<HttptoServiceBus> logger)
//     {
//         _logger = logger;
//     }

//     [Function("HttpToSB")]
//     public async Task RunAsync(
//         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
//     {
//         _logger.LogInformation("Received an HTTP request.");

//         try
//         {
//             // Read the incoming HTTP request body
//             var requestBody = await req.ReadAsStringAsync();
//             if (string.IsNullOrWhiteSpace(requestBody))
//             {
//                 var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
//                 await errorResponse.WriteStringAsync("Request body cannot be empty.");
//                 return;
//             }

//             // Convert JSON to XML (basic conversion)
//             var jsonData = JsonDocument.Parse(requestBody);
//             var root = new XElement("Root");
//             foreach (var property in jsonData.RootElement.EnumerateObject())
//             {
//                 root.Add(new XElement(property.Name, property.Value.ToString()));
//             }

//             var xmlString = root.ToString();
//             _logger.LogInformation($"Converted JSON to XML: {xmlString}");

//             // Send the XML message directly to Service Bus
//             await SendMessageToServiceBusAsync(xmlString);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError($"Error processing request: {ex.Message}");
//             throw; // Let Azure Functions handle returning an appropriate error response
//         }
//     }

//     private async Task SendMessageToServiceBusAsync(string message)
//     {
//         try
//         {
//             // Create a Service Bus client
//             var client = new ServiceBusClient(_serviceBusConnectionString);
//             var sender = client.CreateSender(_queueName);

//             // Create a message to send
//             var serviceBusMessage = new ServiceBusMessage(message);

//             // Send the message to the Service Bus queue
//             await sender.SendMessageAsync(serviceBusMessage);
//             _logger.LogInformation("Message sent to Service Bus.");
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError($"Failed to send message to Service Bus: {ex.Message}");
//         }
//     }
// }


// using Microsoft.AspNetCore.Http;
// using Microsoft.Azure.Functions.Worker;
// using Microsoft.Azure.Functions.Worker.Http;
// using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus;
// using Microsoft.Extensions.Logging;
// using System.Text;
// using System.Text.Json;
// using System.Xml.Linq;

// public class HttptoServiceBus
// {
//     private readonly ILogger<HttptoServiceBus> _logger;

//     public HttptoServiceBus(ILogger<HttptoServiceBus> logger)
//     {
//         _logger = logger;
//     }

//     [Function("HttpToSB")]
//     [ServiceBusOutput("sbqueue", Connection = "ServiceBusSendConnection")]
//     public async Task<string> RunAsync(
//         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
//     {
//         _logger.LogInformation("Received an HTTP request.");

//         try
//         {
//             var requestBody = await req.ReadAsStringAsync();

//             if (string.IsNullOrWhiteSpace(requestBody))
//             {
//                 var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
//                 await errorResponse.WriteStringAsync("Request body cannot be empty.");
//                 return null;  // Ensure no further processing
//             }

//             _logger.LogInformation($"Received body: {requestBody}");

//             // Parse the incoming JSON body
//             var jsonData = JsonDocument.Parse(requestBody);
//             var root = new XElement("Root");

//             foreach (var property in jsonData.RootElement.EnumerateObject())
//             {
//                 root.Add(new XElement(property.Name, property.Value.ToString()));
//             }

//             // Convert the XElement to an XML string
//             var xmlString = root.ToString();
//             _logger.LogInformation($"Converted JSON to XML: {xmlString}");

//             // Return the XML string to be sent to the Service Bus
//             return xmlString;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError($"Error processing request: {ex.Message}");
//             throw; // Let Azure Functions handle returning an appropriate error response
//         }
//     }
// }
