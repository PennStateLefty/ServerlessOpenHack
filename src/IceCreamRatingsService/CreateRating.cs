using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using IceCreamRatingsService.Models;
using System.Net.Http;

namespace IceCreamRatingsService
{
    public static class CreateRating
    {
        [FunctionName("CreateRating")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "IceCreamRatings",
                collectionName: "Ratings",
                ConnectionStringSetting = "CosmosDBConnection")]IAsyncCollector<Rating> ratingItems,
            ILogger log)
        {
            CreateRatingRequest createRatingRequest;

            try
            {
                createRatingRequest = JsonConvert.DeserializeObject<CreateRatingRequest>(await req.ReadAsStringAsync());
            }
            catch 
            {
                return new BadRequestObjectResult("Invalid request");
            }

            if (createRatingRequest.UserId != null)
            {
                var client = new HttpClient();
                string userUrl = $"https://serverlessohapi.azurewebsites.net/api/GetUser?userId={createRatingRequest.UserId}";
                var response = await client.GetAsync(userUrl);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return new BadRequestObjectResult("UserId is not valid");
                }
            }
            else
            {
                return new BadRequestObjectResult("UserId not found");
            }

            if (createRatingRequest.ProductId != null)
            {
                var client = new HttpClient();
                string userUrl = $"https://serverlessohapi.azurewebsites.net/api/GetProduct?productId={createRatingRequest.ProductId}";
                var response = await client.GetAsync(userUrl);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return new BadRequestObjectResult("ProductId is not valid");
                }
            }
            else
            {
                return new BadRequestObjectResult("ProductId not found");
            }

            if (createRatingRequest.Rating < 0 || createRatingRequest.Rating > 5)
            {
                return new BadRequestObjectResult("Rating must be between 0 and 5");
            }

            var rating = new Rating
            {
                id = Guid.NewGuid(),
                Timestamp = DateTime.Now,
                UserId = createRatingRequest.UserId,
                ProductId = createRatingRequest.ProductId,
                LocationName = createRatingRequest.LocationName,
                RatingValue = createRatingRequest.Rating,
                UserNotes = createRatingRequest.UserNotes
            };

            await ratingItems.AddAsync(rating);

            return new OkObjectResult(rating);
        }
    }
}
