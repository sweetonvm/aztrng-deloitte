using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace CsvSearchFunction.Functions {
    public static class SearchById
{
    [Function("SearchById")]
    public static async Task<IResult> Run(
        HttpRequest req,
        ILogger log)
    {
        log.LogInformation("SearchById function triggered.");

        try
        {
            // Parse request body for ID
            var requestBody = await JsonSerializer.DeserializeAsync<RequestPayload>(req.Body);
            if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Id))
            {
                log.LogWarning("Invalid input: ID is required.");
                return Results.BadRequest("Invalid input: ID is required.");
            }

            log.LogInformation($"Searching for record with ID: {requestBody.Id}");

            // Blob Storage details
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string containerName = "results";
            string blobName = "testdata.csv";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if the blob exists
            if (!await blobClient.ExistsAsync())
            {
                log.LogError($"File {blobName} does not exist in container {containerName}.");
                return Results.NotFound("CSV file not found.");
            }

            // Read the CSV file
            using var blobStream = await blobClient.OpenReadAsync();
            using var reader = new StreamReader(blobStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

            var records = csv.GetRecords<dynamic>().ToList();
            var record = records.FirstOrDefault(r => r.id == requestBody.Id);

            if (record == null)
            {
                log.LogInformation($"No record found with ID: {requestBody.Id}");
                return Results.NotFound($"Record with ID {requestBody.Id} not found.");
            }

            log.LogInformation($"Record found: {JsonSerializer.Serialize(record)}");
            return Results.Ok(record);
        }
        catch (Exception ex)
        {
            log.LogError($"An error occurred: {ex.Message}", ex);
            return Results.Problem(detail: "Internal Server Error.");
        }
    }

    private record RequestPayload(string Id);
}
    public class RequestModel
    {
        public required string Id { get; set; }
    }

    public class RecordModel
    {
        public required string Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
    }
}