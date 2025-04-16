using Azure.Identity;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.AzureManaged;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

// Configure logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("Starting Function Chaining Pattern - Greeting Client");

// Get environment variables for endpoint and taskhub with defaults
string endpoint = Environment.GetEnvironmentVariable("ENDPOINT") ?? "http://localhost:8080";
string taskHubName = Environment.GetEnvironmentVariable("TASKHUB") ?? "default";

// Split the endpoint if it contains authentication info
string hostAddress = endpoint;
if (endpoint.Contains(';'))
{
    hostAddress = endpoint.Split(';')[0];
}

// Determine if we're connecting to the local emulator
bool isLocalEmulator = endpoint == "http://localhost:8080";

// Construct a proper connection string with authentication
string connectionString;
if (isLocalEmulator)
{
    // For local emulator, no authentication needed
    connectionString = $"Endpoint={hostAddress};TaskHub={taskHubName};Authentication=None";
    Console.WriteLine("Using local emulator with no authentication");
}
else
{
    // For Azure, use DefaultAzureCredential
    connectionString = $"Endpoint={hostAddress};TaskHub={taskHubName};Authentication=DefaultAzureCredential";
    Console.WriteLine("Using Azure endpoint with DefaultAzureCredential");
}

Console.WriteLine($"Using endpoint: {endpoint}");
Console.WriteLine($"Using task hub: {taskHubName}");
Console.WriteLine($"Host address: {hostAddress}");
Console.WriteLine($"Connection string: {connectionString}");
Console.WriteLine("This worker implements a simple greeting workflow with 3 chained activities");

// Create the client using DI service provider
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());

// Register the client
services.AddDurableTaskClient(options =>
{
    options.UseDurableTaskScheduler(connectionString);
});

var serviceProvider = services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<DurableTaskClient>();

// Create a name input for the greeting orchestration
string name = "User";
logger.LogInformation("Starting greeting orchestration for name: {Name}", name);

// Schedule the orchestration
var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
    "GreetingOrchestration", 
    name);

logger.LogInformation("Started orchestration with ID: {InstanceId}", instanceId);

// Wait for orchestration to complete
logger.LogInformation("Waiting for orchestration to complete...");

// Create a cancellation token source with timeout
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

// Wait for the orchestration to complete using built-in method
OrchestrationMetadata instance = await client.WaitForInstanceCompletionAsync(
    instanceId,
    getInputsAndOutputs: true,
    cts.Token);

logger.LogInformation("Orchestration completed with status: {Status}", instance.RuntimeStatus);

if (instance.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
{
    var result = instance.ReadOutputAs<string>();
    logger.LogInformation("Greeting result: {Result}", result);
}
else if (instance.RuntimeStatus == OrchestrationRuntimeStatus.Failed)
{
    logger.LogError("Orchestration failed: {ErrorMessage}", instance.FailureDetails?.ErrorMessage);
}