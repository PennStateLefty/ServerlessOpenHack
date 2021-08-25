using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OrderBatchService
{
	public static class EventGridEntity
	{
		[FunctionName("CorrelateFromEventGrid")]
		public static async Task Run(
			[EventGridTrigger] EventGridEvent e,
			[DurableClient] IDurableEntityClient client,
			ILogger log
			)
		{
			log.LogInformation($"Message received from {e.Topic}");
			var blobEvent = JsonConvert.DeserializeObject<BlobData>(e.Data.ToString());
			var id = Regex.Match(blobEvent.url, "[0-9]{14}").Value;

			var entityId = new EntityId("OrderFileCorrelation", id);
			await client.SignalEntityAsync(entityId, "AddUri", blobEvent.url);
		}

		#region 
		//[FunctionName("EventGridEntity")]
		//public static async Task<List<string>> RunOrchestrator(
		//    [OrchestrationTrigger] IDurableOrchestrationContext context)
		//{
		//    var outputs = new List<string>();

		//    // Replace "hello" with the name of your Durable Activity Function.
		//    outputs.Add(await context.CallActivityAsync<string>("EventGridEntity_Hello", "Tokyo"));
		//    outputs.Add(await context.CallActivityAsync<string>("EventGridEntity_Hello", "Seattle"));
		//    outputs.Add(await context.CallActivityAsync<string>("EventGridEntity_Hello", "London"));

		//    // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
		//    return outputs;
		//}

		//[FunctionName("EventGridEntity_Hello")]
		//public static string SayHello([ActivityTrigger] string name, ILogger log)
		//{
		//    log.LogInformation($"Saying hello to {name}.");
		//    return $"Hello {name}!";
		//}

		//[FunctionName("DurableEntity_EventGridStart")]
		//public static async Task<HttpResponseMessage> HttpStart(
		//    [EventGridTrigger] EventGridTrigger trigger,
		//    [DurableClient] IDurableOrchestrationClient starter,
		//    ILogger log)
		//{
		//    // Function input comes from the request content.
		//    string instanceId = await starter.StartNewAsync("EventGridEntity", null);

		//    log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

		//    return starter.CreateCheckStatusResponse(req, instanceId);
		//}
		#endregion
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class OrderFileCorrelation
	{
		[JsonProperty("orderHeaderUri")]
		private string OrderHeaderUri { get; set; }
		[JsonProperty("orderLineItemUri")]
		private string OrderLineItemUri { get; set; }
		[JsonProperty("productInformationUri")]
		private string ProductInformationUri { get; set; }

		public void AddUri(string uri)
		{
			var fileName = uri.Substring(uri.LastIndexOf('/') + 1).Split('-');

			if (fileName.Length > 0)
			{
				switch (fileName[1])
				{
					case "OrderHeaderDetails.csv":
						this.OrderHeaderUri = uri;
						break;
					case "OrderLineItems.csv":
						this.OrderLineItemUri = uri;
						break;
					case "ProductInformation.csv":
						this.ProductInformationUri = uri;
						break;
					default:
						break;
				}
			}

			if (AllFilesAvailable())
			{
				//Call API to bundle calls here
				
			}
		}

		public bool AllFilesAvailable()
		{
			return (!string.IsNullOrEmpty(OrderHeaderUri) && !string.IsNullOrEmpty(OrderLineItemUri) && !string.IsNullOrEmpty(ProductInformationUri));
		}

		public Task Reset()
		{
			this.OrderHeaderUri = string.Empty;
			this.OrderLineItemUri = string.Empty;
			this.ProductInformationUri = string.Empty;

			return Task.CompletedTask;
		}

		public Task<bool> Get()
		{
			return Task.FromResult(AllFilesAvailable());
		}
		public void Delete()
		{
			Entity.Current.DeleteState();
		}

		[FunctionName(nameof(OrderFileCorrelation))]
		public static Task Run([EntityTrigger] IDurableEntityContext ctx)
			=> ctx.DispatchAsync<OrderFileCorrelation>();
	}

	public class BlobData
	{
		public string api { get; set; }
		public string clientRequestId { get; set; }
		public string requestId { get; set; }
		public string eTag { get; set; }
		public string contentType { get; set; }
		public int contentLength { get; set; }
		public string blobType { get; set; }
		public string url { get; set; }
		public string sequencer { get; set; }
		public Storagediagnostics storageDiagnostics { get; set; }
	}

	public class Storagediagnostics
	{
		public string batchId;
	}
}