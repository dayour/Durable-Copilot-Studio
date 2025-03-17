using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Company.Function.Models;
using Company.Function.Activities;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.DurableTask.Entities;
using System.Reflection.Metadata.Ecma335;


namespace Company.Function
{
    public static partial class OrderProcessingOrchestration
    {
        [Function("OrderProcessingOrchestration_ParallelTest")]
        public static async Task<HttpResponseData> ParallelTest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "run/{count}")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            int count,
            FunctionContext executionContext,
            CancellationToken cancellationToken)
        {
            // this tests starts and runs multiple order ochestrations in parallel, logging progress over time.
            // note that it is not well suited for very large counts, especially when deployed, since both the http call and
            // the function will time out at some point.

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                ConcurrentBag<Task<string>> startTasks = [];

                ILogger logger = executionContext.GetLogger("OrderProcessingOrchestration_ParallelTest");

                // we are using warnings for test progress so we can filter them easily
                logger.LogWarning($"starting {count} orchestrations");

                Task bgtask = Task.Run(() => Parallel.For(
                    0,
                    count,
                    (int i) => startTasks.Add(
                        client.ScheduleNewOrchestrationInstanceAsync(
                            nameof(OrderProcessingOrchestration),
                            new OrderPayload("milk", TotalCost: 5, Quantity: 1)))));

                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    int startedCount = startTasks.Count(t => t.IsCompleted);
                    int faultedCount = startTasks.Count(t => t.IsFaulted);
                    if (startedCount == count || faultedCount > 0)
                    {
                        break;
                    }
                    logger.LogWarning($"{stopwatch.Elapsed} started {startedCount}/{count} orchestrations so far");
                }

                // await the tasks to make sure we observe any exceptions
                await Task.WhenAll(startTasks);

                logger.LogWarning($"{stopwatch.Elapsed} started all {count} orchestrations");

                ConcurrentBag<Task<OrchestrationMetadata>> completionTasks = [];

                Task bgtask2 = Task.Run(() => Parallel.ForEach(
                    startTasks.Select(t => t.Result),
                    (string instanceId) => completionTasks.Add(WaitForInstanceWithoutTimeout(instanceId))));

                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    int completedCount = completionTasks.Count(t => t.IsCompleted);
                    int faultedCount = completionTasks.Count(t => t.IsFaulted);
                    if (completedCount == count || faultedCount > 0)
                    {
                        break;
                    }
                    logger.LogWarning($"{stopwatch.Elapsed} completed {completedCount}/{count} orchestrations so far");
                }

                // await the tasks to make sure we observe any exceptions
                await Task.WhenAll(completionTasks);

                logger.LogWarning($"{stopwatch.Elapsed} completed all {count} orchestrations");

                var httpResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await httpResponse.WriteStringAsync($"completed all {count} orchestrations in approximately {stopwatch.Elapsed}\n");
                return httpResponse;
            }
            catch(Exception e)
            {
                var httpResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await httpResponse.WriteStringAsync($"encountered exception after approximately {stopwatch.Elapsed}:\n {e}\n");
                return httpResponse;
            }


            async Task<OrchestrationMetadata> WaitForInstanceWithoutTimeout(string instanceId)
            {
                while (true)
                {
                    try
                    {
                        return await client.WaitForInstanceCompletionAsync(instanceId, CancellationToken.None);
                    }
                    catch (Grpc.Core.RpcException grpcException) when (grpcException.StatusCode == Grpc.Core.StatusCode.Unavailable)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5)); // retry after a delay
                    }
                    catch (OperationCanceledException exception) when (IsTimeoutException(exception))
                    {
                        // retry immediately
                    }
                }
            }

            // To tell whether an exception is a timeout we search through the inner exceptions to see if any of them is a TimeoutException
            static bool IsTimeoutException(Exception? e)
            {
                while (e is not null)
                {
                    if (e is TimeoutException)
                    {
                        return true;
                    }
                    e = e.InnerException;
                }
                return false;
            }
        }
    }
}
