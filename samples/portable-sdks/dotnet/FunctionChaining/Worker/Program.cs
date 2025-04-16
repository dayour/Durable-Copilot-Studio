using Azure.Identity;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Worker;
using Microsoft.DurableTask.Worker.AzureManaged;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FunctionChaining;

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

// Configure logging and start the host
var builder = Host.CreateApplicationBuilder();

// Configure logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Configure services
builder.Services.AddDurableTaskWorker()
    .AddTasks(registry =>
    {
        registry.AddAllGeneratedTasks(); // This will add the tasks decorated with [DurableTask]
    })
    .UseDurableTaskScheduler(connectionString);

// Build the host
var host = builder.Build();

Console.WriteLine("Starting Function Chaining Pattern - Greeting Worker");

// Start the host
await host.StartAsync();

Console.WriteLine("Worker started. Press any key to stop...");
Console.ReadKey();

// Stop the host
await host.StopAsync();
