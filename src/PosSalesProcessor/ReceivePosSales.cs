using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PosSalesProcessor.Models;

namespace PosSalesProcessor
{
    public static class ReceivePosSales
    {
        [FunctionName("ReceivePosSales")]
        public static async Task Run(
            [EventHubTrigger("sales", Connection = "EventHubConnectionString")] EventData[] events,
            [CosmosDB(
                databaseName: "Orders",
                collectionName: "sales",
                ConnectionStringSetting = "CosmosDBConnection")]IAsyncCollector<Order> orders,
            ILogger log)
        {

            string topicName = "receipts";

            // the client that owns the connection and can be used to create senders and receivers
            ServiceBusClient client;

            // the sender used to publish messages to the topic
            ServiceBusSender sender;

            client = new ServiceBusClient(Environment.GetEnvironmentVariable("SBconnectionString"));
            
            sender = client.CreateSender(topicName);



            var exceptions = new List<Exception>();

            log.LogInformation($"Received {events.Length} sales events");

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    var order = JsonConvert.DeserializeObject<Order>(messageBody);

                    await orders.AddAsync(order);

                    if (!string.IsNullOrEmpty(order.header.receiptUrl))
                    {

                        var sbmessage = new ReceiptData()
                        {
                            totalItems = order.details.Length,
                            totalCost = order.header.totalCost,
                            salesNumber = order.header.salesNumber,
                            salesDate = order.header.dateTime,
                            storeLocation = order.header.locationId,
                            receiptUrl = order.header.receiptUrl
                        };
                        var message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sbmessage));
                        await sender.SendMessageAsync(new ServiceBusMessage(message));

                    }
                    
                    await Task.Yield();



                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }



    }
}
