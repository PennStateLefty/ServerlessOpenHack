using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace OrderBatchService
{
    public static class MonitorOrderBatch
    {
        [FunctionName("MonitorOrderBatch")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            outputs.Add(await context.CallActivityAsync<string>("MonitorOrderBatch_Hello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("MonitorOrderBatch_Hello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("MonitorOrderBatch_Hello", "London"));

            string applicationId = context.GetInput<string>();

            //var gate1 = context.WaitForExternalEvent("CityPlanningApproval");
            //var gate2 = context.WaitForExternalEvent("FireDeptApproval");
            //var gate3 = context.WaitForExternalEvent("BuildingDeptApproval");

            //// all three departments must grant approval before a permit can be issued
            //await Task.WhenAll(gate1, gate2, gate3);

            //await context.CallActivityAsync("IssueBuildingPermit", applicationId);

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("MonitorOrderBatch_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("MonitorOrderBatch_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("MonitorOrderBatch", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}