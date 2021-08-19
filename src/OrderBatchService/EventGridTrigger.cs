// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace OrderBatchService
{
    /// <summary>
    /// !JPH - TODO: Not really sure how to test this, so deploying and monitoring logs
    /// Represents an event grid trigger that subscribes to Blob Created events
    /// </summary>
    public static class EventGridTrigger
    {
        [FunctionName("EventGridTrigger")]
        public static async Task RunEventGridTrigger(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log)
        {
            log.LogInformation("Received Event Grid Blob Created event: " + eventGridEvent.Data.ToString());

            try
            {
                JObject json = JObject.Parse(eventGridEvent.Data.ToString());
                if (json.TryGetValue("url", out JToken urlToken))
                {
                    string url = urlToken.ToString();
                    log.LogInformation($"New file created at url = '{url}'.");

                    // Use the file name as the batchId and gate for processing
                    // The prefix is the batchId
                    // The files are the gates
                    string fileName = url.Substring(url.LastIndexOf("/") + 1);
                    string[] fileNameTokens = fileName.Split("-");
                    string batchId = fileNameTokens[0];
                    string file = fileNameTokens[1];

                    log.LogInformation($"Starting orchestration with ID = '{batchId}'.");
                    string instanceId = await client.StartNewAsync("MonitorOrderEvents", batchId);
                    log.LogInformation($"Started orchestration with ID = '{batchId}'.");

                    log.LogInformation("Raising Orchestration Event with batchId=" + batchId + ", fileName=" + fileName + ", file=" + file);
                    await client.RaiseEventAsync(batchId, fileName, file);
                }
                else
                {
                    log.LogWarning("No url found in the event grid payload");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error parsing event grid event");
            }
        }

        [FunctionName("MonitorOrderEvents")]
        public static async Task RunMonitorBatch(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            string batchId = context.GetInput<string>();

            log.LogInformation("Recieved event for batch: " + batchId);

            var gate1 = context.WaitForExternalEvent("OrderHeaderDetails.csv");
            var gate2 = context.WaitForExternalEvent("OrderLineItems.csv");
            var gate3 = context.WaitForExternalEvent("ProductInformation.csv");

            // All three files must be created before the batch can be processed
            await Task.WhenAll(gate1, gate2, gate3);

            log.LogInformation("Recieved all files for batch: " + batchId);
            await context.CallActivityAsync("ProcessBatch", batchId);
        }

        [FunctionName("ProcessBatch")]
        public static void RunProcessBatch(
            [ActivityTrigger] string batchId,
            ILogger log)
        {
            log.LogInformation($"Processing batchId: {batchId}.");
        }
    }
}
