using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using CopilotStudioExtensibility.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Add HTTP client
        services.AddHttpClient();
        
        // Register our services
        services.AddScoped<IPowerPlatformGraphService, PowerPlatformGraphService>();
        services.AddScoped<IPacCliService, PacCliService>();
        services.AddScoped<IAgentRoutingService, AgentRoutingService>();
        
        // Add logging
        services.AddLogging();
    })
    .Build();

host.Run();