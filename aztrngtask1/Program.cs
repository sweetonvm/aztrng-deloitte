using Azure;
using Azure.Core;
using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddFunctionsWorkerCore();
        services.AddLogging();

        // Add Cosmos DB Client
        services.AddSingleton(sp => new CosmosClient(
            Environment.GetEnvironmentVariable("CosmosDBConnectionString")));

        // Add Service Bus Client
        services.AddSingleton(sp => new ServiceBusClient(
            Environment.GetEnvironmentVariable("ServiceBusConnectionString")));

        // Add Event Grid Client
        services.AddSingleton(sp => new EventGridPublisherClient(
            new Uri(Environment.GetEnvironmentVariable("EventGridTopicEndpoint")),
            new AzureKeyCredential(Environment.GetEnvironmentVariable("EventGridAccessKey"))));
    })
    .Build();

host.Run();