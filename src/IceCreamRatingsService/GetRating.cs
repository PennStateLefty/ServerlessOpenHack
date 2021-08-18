using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

using IceCreamRatingsService.Models;

namespace IceCreamRatingsService
{
    public static class GetRating
    {
        [FunctionName("GetRating")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rating/{id}")] HttpRequest req, string id,
            [CosmosDB(
                databaseName: "IceCreamRatings",
                collectionName: "Ratings",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "SELECT * FROM Ratings r where  r.id = {id}")]
                IEnumerable<Rating> rating,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request for rating id: { id }");

            Guid ratingId;

            if (!Guid.TryParse(id, out ratingId))
			{
                return new BadRequestObjectResult("Invalid Rating ID format");
            }

            if (rating.Count() == 0)
            {
                return new NotFoundObjectResult("Records not found");
            }
            else
            {
                return new OkObjectResult(rating.First());
            }
        }
    }
}
