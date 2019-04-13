using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DurableV18Sample
{
    public static class LongRunOrchestrator
    {
        [FunctionName("LongRunOrchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Wait for 5 minutes 
            DateTime startAgain = context.CurrentUtcDateTime.Add(TimeSpan.FromMinutes(5));
            await context.CreateTimer(startAgain, CancellationToken.None);

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("LongRunOrchestrator_Hello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("LongRunOrchestrator_Hello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("LongRunOrchestrator_Hello", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("LongRunOrchestrator_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("LongRunOrchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("LongRunOrchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        public class Status
        {
            public bool HasRunning { get; set; }
        }

        [FunctionName("StatusCheck")]
        public static async Task<IActionResult> StatusCheck(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")]
            HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient client,
            ILogger log)
        {
            var runtimeStatus = new List<OrchestrationRuntimeStatus>();
            runtimeStatus.Add(OrchestrationRuntimeStatus.Running);
            var status = await client.GetStatusAsync(new DateTime(2015,10,10), null, runtimeStatus);
            return (ActionResult) new OkObjectResult(new Status() {HasRunning = (status.Count != 0)});
        }

        [FunctionName("OrchestrationStatus")]
        public static async Task<IActionResult> OrchestrationStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]
            HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient client,
            ILogger log)
        {
            var runtimeStatus = new List<OrchestrationRuntimeStatus>();
            runtimeStatus.Add(OrchestrationRuntimeStatus.Running);
            var status = await client.GetStatusAsync(DateTime.MinValue, DateTime.MaxValue, runtimeStatus);       
            return (ActionResult)new OkObjectResult(status);
        }

        [FunctionName("TestFunctionKeys")]
        public static async Task<IActionResult> TestFunctionKeys(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var variables = Environment.GetEnvironmentVariables();
            return (ActionResult)new OkObjectResult(variables);
        }
    }
}