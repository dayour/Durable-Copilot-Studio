using DurableTask.Core;
using Microsoft.DurableTask.AzureManagedBackend;
using Microsoft.Extensions.Logging;

// A connection string is used to connect to a Durable Task Scheduler instance.
// Note that there are no credentials in this connection string. Only identity-based credentials are supported.
// Expected format is "Endpoint=https://<host>;Authentication=<credentialType>;TaskHub=<hubName>"
// Valid credential types types include "DefaultAzure" and "ManagedIdentity"
// Example:
//   "Endpoint=https://my-scheduler-123.northcentralus.durabletask.io;Authentication=DefaultAzure;TaskHub=MyTaskHub"
string? connectionString = Environment.GetEnvironmentVariable("DTS_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("An environment variable named DTS_CONNECTION_STRING is required.");
    return;
}

// Configure the Durable Task worker to log to the console with a simple format
ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(
    options =>
    {
        options.SingleLine = true;
        options.UseUtcTimestamp = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    }));

// The AzureManagedOrchestrationService is the IOrchestrationService implementation that
// works with the Durable Task Scheduler backend.
AzureManagedOrchestrationService dtsExtension = new(
    AzureManagedOrchestrationServiceOptions.FromConnectionString(connectionString),
    loggerFactory);

// Create the worker and register the orchestrations/activities as normal
TaskHubWorker worker = new(dtsExtension, loggerFactory);
worker.AddTaskOrchestrations(typeof(HelloWorldOrchestration));
worker.AddTaskActivities(typeof(HelloActivity));

Console.WriteLine("Starting up task hub worker...");

await worker.StartAsync();

Console.WriteLine("Running the hello world orchestration...");

// Create the task hub client as normal and start the orchestration
TaskHubClient client = new(dtsExtension, null, loggerFactory);
OrchestrationInstance instance = await client.CreateOrchestrationInstanceAsync(
    orchestrationType: typeof(HelloWorldOrchestration),
    input: null);

Console.WriteLine($"Started orchestration with ID = '{instance.InstanceId}' successfully!");

// Block until the orchestration completes
OrchestrationState state = await client.WaitForOrchestrationAsync(instance, TimeSpan.FromMinutes(1));
Console.WriteLine($"Orchestration completed with status: {state.OrchestrationStatus} and output: {state.Output} ");
await worker.StopAsync();
dtsExtension.Dispose();

class HelloWorldOrchestration : TaskOrchestration<string[], string>
{
    public override async Task<string[]> RunTask(OrchestrationContext context, string _)
    {
        // Say hello to different cities around the world in time zone order
        string result1 = await context.ScheduleTask<string>(typeof(HelloActivity), "Tokyo");
        string result2 = await context.ScheduleTask<string>(typeof(HelloActivity), "Hyderabad");
        string result3 = await context.ScheduleTask<string>(typeof(HelloActivity), "London");
        string result4 = await context.ScheduleTask<string>(typeof(HelloActivity), "São Paulo");
        string result5 = await context.ScheduleTask<string>(typeof(HelloActivity), "Seattle");

        // Return greetings as an array
        return [result1, result2, result3, result4, result5];
    }
}

class HelloActivity : TaskActivity<string, string>
{
    protected override string Execute(TaskContext context, string input)
    {
        return $"Hello, {input}!";
    }
}
