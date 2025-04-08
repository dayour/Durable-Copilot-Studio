# Fan-Out Fan-In Pattern

## Description of the Sample

This sample demonstrates the Fan-Out Fan-In pattern with the Azure Durable Task Scheduler using the .NET SDK. The Fan-Out Fan-In pattern represents a way to execute multiple operations in parallel and then aggregate the results, making it ideal for parallelized data processing scenarios.

In this sample:
1. The orchestrator takes a list of work items as input
2. It fans out by creating a separate task for each work item using `ProcessWorkItemActivity` 
3. All these tasks execute in parallel
4. It waits for all tasks to complete using `Task.WhenAll`
5. It fans in by aggregating all individual results using `AggregateResultsActivity`
6. The final aggregated result is returned to the client

This pattern is useful for:
- Processing large datasets in parallel for improved throughput
- Batch processing operations that can be executed independently
- Distributing computational workload across multiple workers
- Aggregating results from multiple sources or computations
=======
## Architecture

Below is an architecture diagram illustrating the Fan-Out Fan-In pattern as implemented in this sample:

```
                                  +-------------------+
                                  |                   |
                                  |     HTTP Client   |
                                  |                   |
                                  +--------+----------+
                                           |
                                           | HTTP Request
                                           v
+------------------+              +--------+----------+
|                  |              |                   |
|  Azure Durable   |<------------>|   ClientService   |
|  Task Scheduler  |              |                   |
|                  |              +-------------------+
+--------+---------+                       ^
         |                                 |
         |                                 | Orchestration
         |                                 | Results
         |                                 |
         |                                 |
         |                                 |
         |                                 |
         |                                 |
         |                                 |
         |                                 |
         v                                 |
+--------+-----------------------------+   |
|                                      |   |
|          WorkerService               |   |
|   +----------------------------+     |   |
|   |                            |     |   |
|   |       HelloWorld           |     |   |
|   |      Orchestration         +-----+---+
|   |                            |     |
|   +-----+--------+--------+----+     |
|         |        |        |          |
|    Fan-Out       |        |          |
|         |        |        |          |
|         v        v        v          |
|    +----+--+ +---+--+ +---+--+       |
|    |       | |      | |      |       |
|    | Say   | | Say  | | Say  |       |
|    | Hello | | Hello| | Hello|       |
|    |       | |      | |      |       |
|    +-------+ +------+ +------+       |
|        ^                             |
|        |         Fan-In              |
|        +-----------------------------+
|                                      |
+--------------------------------------+

+------------------------------------------|
|                                          |
|  Legend:                                 |
|  -------                                 |
|  ➜ Request/Response Flow                 |
|  ↔ Service Communication                 |
|                                          |
+------------------------------------------+
```

The sample is structured as follows:

- **ClientService**: ASP.NET Core Web API that exposes endpoints to start and manage orchestrations
- **WorkerService**: ASP.NET Core service that processes the activities and implements the orchestration logic
- **Durable Task Scheduler**: The Azure backend service that manages the orchestration state and messaging

The Fan-Out Fan-In pattern flow:
1. Client makes HTTP request to the ClientService
2. ClientService creates one or more HelloWorld orchestrations
3. Each orchestration fans out to multiple parallel SayHello activities
4. The WorkerService processes each activity independently
5. Results from all activities are fanned back in to the orchestration
6. The aggregated results are stored in the Durable Task Scheduler
7. Client can query the orchestration status and results

Both services use the Durable Task SDK with the Azure Managed backend for orchestration management:

```csharp
// In ClientService/Program.cs
builder.Services.AddDurableTaskClient(clientBuilder =>
{
    clientBuilder.UseDurableTaskScheduler(connectionString);
});

// In WorkerService/Program.cs
builder.Services.AddDurableTaskWorker(workerBuilder =>
{
    workerBuilder.UseDurableTaskScheduler(connectionString);
    // Register orchestrations and activities...
});
```

<<<<<<< HEAD
1. **OrchestrationService**: Accepts HTTP requests and converts them into orchestrations
2. **WorkerService**: Implements the activity logic for the orchestrations
>>>>>>> f7d0e77 (Update readme to have architecture diagram)

## Prerequisites

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
2. [Docker](https://www.docker.com/products/docker-desktop/) (for running the emulator)
3. [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (if using a deployed Durable Task Scheduler)
=======
## Prerequisites

- .NET 8 SDK
- Docker (for containerized deployment)
- Azure Durable Task Scheduler instance (or local development storage)
>>>>>>> 3765c9c (Update README)

## Configuring Durable Task Scheduler

There are two ways to run this sample:

### Using the Emulator (Recommended)

The emulator simulates a scheduler and taskhub in a Docker container, making it ideal for development and learning.

1. Install Docker if it's not already installed.

2. Pull the Docker Image for the Emulator:
```bash
docker pull mcr.microsoft.com/dts/dts-emulator:v0.0.6
```

3. Run the Emulator:
```bash
docker run --name dtsemulator -d -p 8080:8080 -p 8082:8082 mcr.microsoft.com/dts/dts-emulator:v0.0.6
```
Wait a few seconds for the container to be ready.

Note: The example code automatically uses the default emulator settings (endpoint: http://localhost:8080, taskhub: default). You don't need to set any environment variables.

### Using a Deployed Scheduler and Taskhub in Azure

For production scenarios or when you're ready to deploy to Azure:

1. Create a Scheduler using the Azure CLI:
```bash
az durabletask scheduler create --resource-group <testrg> --name <testscheduler> --location <eastus> --ip-allowlist "[0.0.0.0/0]" --sku-capacity 1 --sku-name "Dedicated" --tags "{'myattribute':'myvalue'}"
```

2. Create Your Taskhub:
```bash
az durabletask taskhub create --resource-group <testrg> --scheduler-name <testscheduler> --name <testtaskhub>
```

3. Retrieve the Endpoint for the Scheduler from the Azure portal.

4. Set the Environment Variables:

   Bash:
   ```bash
   export TASKHUB=<taskhubname>
   export ENDPOINT=<taskhubEndpoint>
   ```

   PowerShell:
   ```powershell
   $env:TASKHUB = "<taskhubname>"
   $env:ENDPOINT = "<taskhubEndpoint>"
   ```

## Authentication

The sample includes smart detection of the environment and configures authentication automatically:

- For local development with the emulator (when endpoint is http://localhost:8080), no authentication is required.
- For Azure deployments, DefaultAzureCredential is used, which tries multiple authentication methods:
  - Managed Identity
  - Environment variables (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET)
  - Azure CLI login
  - Visual Studio login
  - and more

The connection string is constructed dynamically based on the environment:
```csharp
// For local emulator
connectionString = $"Endpoint={hostAddress};TaskHub={taskHubName};Authentication=None";

// For Azure
connectionString = $"Endpoint={hostAddress};TaskHub={taskHubName};Authentication=DefaultAzureCredential";
```

## How to Run the Sample

Once you have set up either the emulator or deployed scheduler, follow these steps to run the sample:

1. First, build the solution:
```bash
cd FanOutFanIn
dotnet build
```

2. Start the worker in a terminal:
```bash
cd Worker
dotnet run
```
You should see output indicating the worker has started and registered the orchestration and activities.

3. In a new terminal, run the client:
```bash
cd Client
dotnet run
```

## Understanding the Code Structure

### Worker Project

The Worker project contains:

- **ParallelProcessingOrchestration.cs**: Defines the orchestrator and activity functions in a single file
- **Program.cs**: Sets up the worker host with proper connection string handling

#### Orchestration Implementation

The orchestration uses the fan-out fan-in pattern by creating parallel activity tasks and waiting for all to complete:

```csharp
public override async Task<Dictionary<string, int>> RunAsync(TaskOrchestrationContext context, List<string> workItems)
{
    // Step 1: Fan-out by creating a task for each work item in parallel
    List<Task<Dictionary<string, int>>> processingTasks = new List<Task<Dictionary<string, int>>>();
    
    foreach (string workItem in workItems)
    {
        // Create a task for each work item (fan-out)
        Task<Dictionary<string, int>> task = context.CallActivityAsync<Dictionary<string, int>>(
            nameof(ProcessWorkItemActivity), workItem);
        processingTasks.Add(task);
    }
    
    // Step 2: Wait for all parallel tasks to complete
    Dictionary<string, int>[] results = await Task.WhenAll(processingTasks);
    
    // Step 3: Fan-in by aggregating all results
    Dictionary<string, int> aggregatedResults = await context.CallActivityAsync<Dictionary<string, int>>(
        nameof(AggregateResultsActivity), results);
    
    return aggregatedResults;
}
```

<<<<<<< HEAD
Each activity is implemented as a separate class decorated with the `[DurableTask]` attribute:

```csharp
[DurableTask]
public class ProcessWorkItemActivity : TaskActivity<string, Dictionary<string, int>>
{
    // Implementation processes a single work item
}

[DurableTask]
public class AggregateResultsActivity : TaskActivity<Dictionary<string, int>[], Dictionary<string, int>>
{
    // Implementation aggregates individual results
}
```

The worker uses Microsoft.Extensions.Hosting for proper lifecycle management:
```csharp
builder.Services.AddDurableTaskWorker()
    .AddTasks(registry =>
    {
        registry.AddOrchestrator<ParallelProcessingOrchestration>();
        registry.AddActivity<ProcessWorkItemActivity>();
        registry.AddActivity<AggregateResultsActivity>();
    })
    .UseDurableTaskScheduler(connectionString);
```

### Client Project

The Client project:

- Uses the same connection string logic as the worker
- Creates a list of work items to be processed in parallel
- Schedules an orchestration instance with the list as input
- Waits for the orchestration to complete and displays the aggregated results
- Uses WaitForInstanceCompletionAsync for efficient polling

```csharp
List<string> workItems = new List<string>
{
    "Task1",
    "Task2",
    "Task3",
    "LongerTask4",
    "VeryLongTask5"
};

// Schedule the orchestration with the work items
string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
    "ParallelProcessingOrchestration", 
    workItems);

// Wait for completion
var instance = await client.WaitForInstanceCompletionAsync(
    instanceId,
    getInputsAndOutputs: true,
    cts.Token);
```

## Understanding the Output

When you run the client, you should see:
1. The client starting an orchestration with a list of work items
2. The worker processing each work item in parallel
3. The worker aggregating all results
4. The client displaying the final aggregated results from the completed orchestration

Example output:
```
Starting Fan-Out Fan-In Pattern - Parallel Processing Client
Using local emulator with no authentication
Starting parallel processing orchestration with 5 work items
Work items: ["Task1","Task2","Task3","LongerTask4","VeryLongTask5"]
Started orchestration with ID: 7f8e9a6b-1c2d-3e4f-5a6b-7c8d9e0f1a2b
Waiting for orchestration to complete...
Orchestration completed with status: Completed
Processing results:
Work item: Task1, Result: 5
Work item: Task2, Result: 5
Work item: Task3, Result: 5
Work item: LongerTask4, Result: 11
Work item: VeryLongTask5, Result: 13
Total items processed: 5
```

When you run the sample, you'll see output from both the worker and client processes:

### Worker Output
The worker shows:
- Registration of the orchestrator and activities
- Log entries when each activity is called
- Parallel processing of multiple work items
- Final aggregation of results

### Client Output
The client shows:
- Starting the orchestration with a list of work items
- The unique orchestration instance ID
- The final aggregated results, showing each work item and its corresponding result
- Total count of processed items

This demonstrates the power of the Fan-Out Fan-In pattern for parallel processing and result aggregation.

## Reviewing the Orchestration in the Durable Task Scheduler Dashboard

To access the Durable Task Scheduler Dashboard and review your orchestration:

### Using the Emulator
1. Navigate to http://localhost:8082 in your web browser
2. Click on the "default" task hub
3. You'll see the orchestration instance in the list
4. Click on the instance ID to view the execution details, which will show:
   - The parallel execution of multiple activity tasks
   - The fan-in aggregation step
   - The input and output at each step
   - The time taken for each step

### Using a Deployed Scheduler
1. Navigate to the Scheduler resource in the Azure portal
2. Go to the Task Hub subresource that you're using
3. Click on the dashboard URL in the top right corner
4. Search for your orchestration instance ID
5. Review the execution details

The dashboard visualizes the Fan-Out Fan-In pattern, making it easy to see how tasks are distributed in parallel and then aggregated back together.
=======
## Running the Sample

### Using Docker

The simplest way to run the sample is using Docker:

```bash
# Build and run the Docker containers
./build-docker.sh
```

Alternatively, you can use Azure Developer CLI to deploy to Azure:

```bash
azd up
```

### Running Locally

#### Start the Worker Service

```bash
cd WorkerService
dotnet run
```

The Worker Service will start and wait for orchestration tasks.

#### Start the Client Service

```bash
cd ClientService
dotnet run
```

The Client Service will start and expose HTTP endpoints to create and manage orchestrations.

## Using the Sample

### Create a Fan-Out Fan-In Orchestration

Send a POST request to the Client Service to start a fan-out fan-in pattern test:

```bash
curl -X POST -H "Content-Type: application/json" \
  -d '{"iterations":10,"parallelActivities":5,"parallelOrchestrations":1}' \
  http://localhost:8080/api/orchestrations
```

This will create a new orchestration that:
- Runs 10 iterations
- In each iteration, fans out to 5 parallel activities (SayHello)
- Fans in the results from all activities
- You can also create multiple parallel orchestrations

### Check Orchestration Status

You can check the status of your orchestration using the returned instance ID:

```bash
curl http://localhost:8080/api/orchestrations/{instanceId}
```

You can also check multiple orchestrations at once:

```bash
curl -X POST -H "Content-Type: application/json" \
  -d '["instance1-id", "instance2-id", "instance3-id"]' \
  http://localhost:8080/api/orchestrations/status
```

### Check Service Status

You can verify both services are running with:

```bash
# Client Service status
curl http://localhost:8080/status

# Worker Service status
curl http://localhost:8080/status
```

## Fan-Out Fan-In Parameters

The fan-out fan-in test accepts the following parameters:

- **iterations**: The number of sequential iterations to run
- **parallelActivities**: The number of parallel activities to fan out to in each iteration
- **parallelOrchestrations**: The number of parallel orchestrations to create (defaults to 1)
<<<<<<< HEAD
>>>>>>> f7d0e77 (Update readme to have architecture diagram)
=======

## Key Components in Detail

### 1. Client Service (HTTP API Layer)

The ClientService (`ClientService/Program.cs`) is responsible for handling HTTP requests and scheduling orchestrations. Key components include:

#### API Endpoints

```csharp
// Start new orchestration(s)
app.MapPost("/api/orchestrations", async ([FromServices] DurableTaskClient client, [FromBody] FanOutFanInRequest request) =>
{
    // The code schedules orchestrations based on request parameters
    var tasks = new List<Task<string>>();
    
    for (int i = 0; i < request.ParallelOrchestrations; i++)
    {
        // Schedule orchestration with the HelloWorld function
        tasks.Add(client.ScheduleNewOrchestrationInstanceAsync(
            "HelloWorld", 
            new FanOutFanInOrchestrationInput
            {
                Iterations = request.Iterations,
                ParallelActivities = request.ParallelActivities
            }));
    }
    
    // Return accepted response with orchestration IDs
    instanceIds = (await Task.WhenAll(tasks)).ToList();
    return Results.Accepted($"/api/orchestrations/status", new { 
        count = instanceIds.Count,
        instanceIds = instanceIds 
    });
});

// Check status of an orchestration
app.MapGet("/api/orchestrations/{instanceId}", async ([FromServices] DurableTaskClient client, string instanceId) =>
{
    var instance = await client.GetInstanceAsync(instanceId);
    // Return orchestration status details
});

// Additional endpoints for checking multiple statuses and service health...
```

#### Input and Result Models

The ClientService defines models for API requests and responses in `ClientService/Models/`:

```csharp
// ClientInputModels.cs
public class FanOutFanInRequest
{
    public int Iterations { get; set; } = 10;
    public int ParallelActivities { get; set; } = 5;
    public int ParallelOrchestrations { get; set; } = 1;
}

// ClientResultModels.cs
public class OrchestrationStatusResponse
{
    public string InstanceId { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Output { get; set; }
    // Additional properties...
}
```

### 2. Worker Service (Orchestration & Activity Logic)

The WorkerService (`WorkerService/Program.cs`) handles the execution of orchestrations and activities. Key components include:

#### Task Registration

The worker service uses a fluent API to register orchestrations and activities:

```csharp
builder.Services.AddDurableTaskWorker(workerBuilder =>
{
    // Configure the worker to use the Durable Task Scheduler backend
    workerBuilder.UseDurableTaskScheduler(connectionString);
    
    // Register all tasks (orchestrations and activities)
    workerBuilder.AddTasks(registry =>
    {
        // Register the HelloWorld orchestration
        registry.AddOrchestratorFunc<FanOutFanInOrchestrationInput, FanOutFanInTestResult>(
            "HelloWorld", 
            async (ctx, input) => {
                var orchestration = new HelloWorld(logger);
                return await orchestration.RunAsync(ctx, input);
            });

        // Register the SayHello activity
        registry.AddActivityFunc<ActivityInput, ActivityResult>(
            "SayHello",
            async (ctx, input) => {
                var activity = new SayHello(logger);
                return await activity.RunAsync(ctx, input);
            });
    });
});
```

#### HelloWorld Orchestration Implementation

The orchestration implementation (`WorkerService/Orchestrations/FanOutFanInOrchestration.cs`) is the heart of the Fan-Out Fan-In pattern:

```csharp
public async Task<FanOutFanInTestResult> RunAsync(TaskOrchestrationContext context, FanOutFanInOrchestrationInput input)
{
    var stopwatch = Stopwatch.StartNew();
    var results = new List<ActivityResult>();
    
    // Run multiple iterations of parallel activities
    for (int i = 0; i < input.Iterations; i++)
    {
        var tasks = new List<Task<ActivityResult>>();
        
        // Fan-Out: Create multiple parallel activities
        for (int j = 0; j < input.ParallelActivities; j++)
        {
            var task = context.CallActivityAsync<ActivityResult>(
                "SayHello",
                new ActivityInput { 
                    IterationNumber = i, 
                    ActivityNumber = j 
                });
            tasks.Add(task);
        }
        
        // Wait for all parallel activities to complete
        await Task.WhenAll(tasks);
        
        // Fan-In: Collect results
        results.AddRange(tasks.Select(t => t.Result));
    }
    
    stopwatch.Stop();
    
    // Return aggregated results
    return new FanOutFanInTestResult
    {
        TotalActivities = input.Iterations * input.ParallelActivities,
        ElapsedTimeMs = stopwatch.ElapsedMilliseconds,
        AverageActivityTimeMs = results.Average(r => r.ProcessingTimeMs),
        Results = results
    };
}
```

Key aspects of this implementation:
1. **Iterations Loop**: Runs multiple iterations sequentially
2. **Fan-Out**: Creates multiple parallel activities in each iteration
3. **Task.WhenAll**: Waits for all parallel activities to complete (synchronization point)
4. **Fan-In**: Collects results from all parallel activities
5. **Performance Tracking**: Records execution time and calculates statistics

#### SayHello Activity Implementation

The activity implementation (`WorkerService/Activities/FanOutFanInActivity.cs`) represents the work performed in parallel:

```csharp
public Task<ActivityResult> RunAsync(TaskActivityContext context, ActivityInput input)
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        // Simulate some work
        string output = "Hello World";
        
        stopwatch.Stop();
        
        // Return activity result with timing information
        return Task.FromResult(new ActivityResult
        {
            IterationNumber = input.IterationNumber,
            ActivityNumber = input.ActivityNumber,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
            Output = output
        });
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger?.LogError(ex, "Activity failed");
        throw; // Re-throw to let the Durable Task Framework handle it
    }
}
```

#### Model Classes

The Worker Service defines models for orchestration inputs and outputs:

```csharp
// WorkerService/Models/OrchestrationModels.cs
public class FanOutFanInOrchestrationInput
{
    public int Iterations { get; set; }
    public int ParallelActivities { get; set; }
}

public class FanOutFanInTestResult
{
    public int TotalActivities { get; set; }
    public long ElapsedTimeMs { get; set; }
    public double AverageActivityTimeMs { get; set; }
    public List<ActivityResult> Results { get; set; } = new();
}

// WorkerService/Models/ActivityModels.cs
public class ActivityInput
{
    public int IterationNumber { get; set; }
    public int ActivityNumber { get; set; }
}

public class ActivityResult
{
    public int IterationNumber { get; set; }
    public int ActivityNumber { get; set; }
    public long ProcessingTimeMs { get; set; }
    public string Output { get; set; } = string.Empty;
}
```

### 3. Azure Durable Task Scheduler Integration

Both services connect to the Azure Durable Task Scheduler service, which handles:
- Persisting orchestration state
- Delivering messages between services
- Managing activity executions
- Handling retries and error handling
- Providing monitoring and diagnostics

## Containerization

Both services are containerized using Docker. The Dockerfiles demonstrate how to package .NET applications that use the Durable Task SDK:

- `ClientService/Dockerfile`
- `WorkerService/Dockerfile`

When deployed to Azure, the `azure.yaml` file configures how these containers are deployed to Azure Container Apps.

## Advanced Features and Best Practices

- **Robust Logging**: Both services use structured logging with correlation IDs to track orchestrations and activities
- **Error Handling**: Activities use try-catch blocks to properly handle and report errors
- **Performance Tracking**: Execution times are tracked and reported
- **Containerization**: Services are designed to run in containers for easy deployment
- **Configurability**: Connection strings and other settings are externalized
- **Health Endpoints**: Both services provide status endpoints for monitoring

## Conclusion

This sample demonstrates how to implement the Fan-Out Fan-In pattern using the Durable Task SDK with Azure Durable Task Scheduler. It showcases parallel processing, orchestration coordination, and proper error handling in a distributed system architecture.
