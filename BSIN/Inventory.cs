using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BSIN
{
    public class Inventory
    {
        private readonly ILogger<Inventory> _logger;
        private static Dictionary<string, Book> booksStorage = new Dictionary<string, Book>();

        public Inventory(ILogger<Inventory> logger)
        {
            _logger = logger;
        }

        [Function("GetBooks")]
        public static async Task<HttpResponseData> GetBooks(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "books")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("GetBooks");
            log.LogInformation("Getting list of books.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(booksStorage.Values);

            return response;
        }

        [Function("CreateBook")]
        public static async Task<HttpResponseData> CreateBook(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "books")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("CreateBook");
            log.LogInformation("Creating a new book.");

            // Validate JWT Token
            if (!IsJwtValid(req))
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Invalid or missing JWT token.");
                return unauthorizedResponse;
            }

            var book = await req.ReadFromJsonAsync<Book>();
            var bookId = System.Guid.NewGuid().ToString();

            booksStorage[bookId] = book;

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteStringAsync("Book creation successful.");
            await response.WriteAsJsonAsync(book);

            return response;
        }

        [Function("CheckInventory")]
        public static async Task<HttpResponseData> CheckInventory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "books/{id}/inventory")] HttpRequestData req,
            string id,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("CheckInventory");
            log.LogInformation($"Checking inventory for book ID {id}.");

            if (!booksStorage.ContainsKey(id))
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { Message = "Book not found." });
                return notFoundResponse;
            }

            var book = booksStorage[id];

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { BookId = id, Inventory = book.Inventory });

            return response;
        }

        private static bool IsJwtValid(HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
            {
                return false;
            }

            var token = authHeaders.FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Validate issuer and audience
                var validIssuer = "https://sts.windows.net/f50e292a-1eea-496a-a943-a86c5b764d4d/";
                var validAudience = "api://31fa5f2f-cb62-429e-98c8-863dde0049b0";

                if (jwtToken.Issuer != validIssuer || !jwtToken.Audiences.Contains(validAudience))
                {
                    return false;
                }

                // Additional validations can be added here
                return true;
            }
            catch
            {
                return false;
            }
        }

        public class Book
        {
            public required string Title { get; set; }
            public required string Author { get; set; }
            public int Inventory { get; set; }
        }
    }
}
