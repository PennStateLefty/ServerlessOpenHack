using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace OrderBatchService
{
    public static class MonitorOrderBatch
    {
        [FunctionName("HttpMonitorOrderBatch")]
        public static async Task RunMonitorBatch(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            string batchId = context.GetInput<string>();
            log.LogInformation("Received event for batch: " + batchId);

            var gate1 = context.WaitForExternalEvent("OrderHeaderDetails.csv");
            var gate2 = context.WaitForExternalEvent("OrderLineItems.csv");
            var gate3 = context.WaitForExternalEvent("ProductInformation.csv");

            // All three files must be created before the batch can be processed
            await Task.WhenAll(gate1, gate2, gate3);

            log.LogInformation("Recieved all files for batch: " + batchId);
            await context.CallActivityAsync("ProcessBatch", batchId);
        }

        [FunctionName("HttpProcessBatch")]
        public static void RunProcessBatch(
            [ActivityTrigger] string batchId,
            ILogger log)
        {
            log.LogInformation($"Processing batchId: {batchId}.");
        }

        [FunctionName("HttpMonitorOrderBatchStart")]
        public static async Task<HttpResponseMessage> RunHttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "order/{filename}")] HttpRequestMessage req, string filename,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Use the file name as the batchId and gate for processing
            // The prefix is the batchId
            // The files are the gates
            string[] fileNameTokens = filename.Split("-");
            string batchId = fileNameTokens[0];
            string file = fileNameTokens[1];

            // Check if an instance with the specified ID already exists or an existing one stopped running(completed/failed/terminated).
            var existingInstance = await starter.GetStatusAsync(batchId);
            if (existingInstance == null
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Failed
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
            {
                // An instance with the specified ID doesn't exist or an existing one stopped running, create one.
                await starter.StartNewAsync("HttpMonitorOrderBatch", batchId, batchId);
                log.LogInformation($"Started orchestration with ID = '{batchId}'.");
            }

            log.LogInformation("Raising Orchestration Event with batchId=" + batchId + ", fileName=" + filename + ", file=" + file);
            await starter.RaiseEventAsync(batchId, file, batchId);

            // Return status response
            return starter.CreateCheckStatusResponse(req, batchId);
        }
    }
}