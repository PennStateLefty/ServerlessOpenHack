// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
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
            [EventGridTrigger]EventGridEvent eventGridEvent,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log)
        {
            log.LogInformation("Received Event Grid Blob Created event: " + eventGridEvent.Data.ToString());

            if (eventGridEvent.Data is StorageBlobCreatedEventData)
            {
                try
                {
                    string url = ((StorageBlobCreatedEventData)eventGridEvent.Data).Url;
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
                catch (Exception ex)
                {
                    log.LogError(ex, "Error parsing event grid event");
                }
            }
            else
            {
                log.LogWarning("Ignoring event type: " + eventGridEvent.EventType.ToString());
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

            // all three departments must grant approval before a permit can be issued
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
