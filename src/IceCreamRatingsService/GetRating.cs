using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IceCreamRatingsService.Models;

namespace IceCreamRatingsService
{
    /// <summary>
    /// Represents an Azure Function that gets a rating by it's unique id using a ComsosDB sql query binding.
    /// </summary>
    public static class GetRatingWithSql
    {
        [FunctionName("GetRatingWithSql")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rating/{id}")] HttpRequest req, string id,
            [CosmosDB(
                databaseName: "IceCreamRatings",
                collectionName: "Ratings",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "SELECT * FROM Ratings r where r.id = {id}")]
                IEnumerable<Rating> rating,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request for rating id: { id }");

            if (!Guid.TryParse(id, out Guid ratingId))
            {
                return new BadRequestObjectResult("Invalid Rating ID format");
            }

            if (rating.Count() == 0)
            {
                // 404 if not found
                return new NotFoundObjectResult("Records not found");
            }
            else
            {
                // IEnumerable was required for this binding
                // Single rating since the id is unique
                return new OkObjectResult(rating.Single());
            }
        }
    }

    /// <summary>
    /// Represents and Azure function that gets a rating by it's unique id.
    /// Note the partition key setting since /id makes sense, but doesn't work.
    /// </summary>
    public static class GetRatingWithId
    {
        [FunctionName("GetRatingWithId")]
        public static IActionResult RunWithId(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rating/v2/{id}")] HttpRequest req, string id,
            [CosmosDB(
                databaseName: "IceCreamRatings",
                collectionName: "Ratings",
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}",
                PartitionKey = "{id}")]
                Rating rating,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request for rating v2 with id: { id }");

            if (rating == null)
            {
                // 404 if not found
                return new NotFoundObjectResult("Records not found");
            }
            else
            {
                // Individual rating
                return new OkObjectResult(rating);
            }
        }
    }

    /// <summary>
    /// Represents an Azure Function that gets a rating by it's id with CosmosDB DocumentClient.
    /// </summary>
    public static class GetRatingWithDocumentClient
    {
        [FunctionName("GetRatingWithDocumentClient")]
        public static async Task<IActionResult> RunWithQuery(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rating/v3/{id}")] HttpRequest req, string id,
            [CosmosDB(
                databaseName: "IceCreamRatings",
                collectionName: "Ratings",
                ConnectionStringSetting = "CosmosDBConnection")]
                DocumentClient client,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request for rating v3 with id: { id }");

            if (!Guid.TryParse(id, out Guid ratingId))
            {
                return new BadRequestObjectResult("Invalid Rating ID format");
            }

            // Not sure how to use the binding parameters, so duplicated here
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("IceCreamRatings", "Ratings");

            // Could use async call this way and do anything the CosmosDB SDK allows for
            // This makes more sense for collections vs individual items
            // SingleOrDefault and FirstOrDefault threw NotSupportedExceptions from the SDK, so the handling of single items a little awkward
            IDocumentQuery<Rating> query = client.CreateDocumentQuery<Rating>(collectionUri)
                .Where(r => r.id == ratingId)
                .AsDocumentQuery();

            IList<Rating> ratings = new List<Rating>();
            while (query.HasMoreResults)
            {
                foreach (Rating rating in await query.ExecuteNextAsync())
                {
                    ratings.Add(rating);
                }
            }

            if (ratings.Count() == 0)
            {
                // 404 if not found
                return new NotFoundObjectResult("Records not found");
            }
            else
            {
                // Individual rating
                return new OkObjectResult(ratings.Single());
            }
        }
    }
}
