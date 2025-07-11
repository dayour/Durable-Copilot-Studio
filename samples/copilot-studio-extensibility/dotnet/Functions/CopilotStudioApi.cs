using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using CopilotStudioExtensibility.Models;
using System.Net;
using System.Text.Json;

namespace CopilotStudioExtensibility.Functions;

/// <summary>
/// HTTP API functions for Copilot Studio extensibility
/// </summary>
public class CopilotStudioApi
{
    private readonly ILogger<CopilotStudioApi> _logger;

    public CopilotStudioApi(ILogger<CopilotStudioApi> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts a hybrid agent conversation
    /// </summary>
    [Function(nameof(StartConversation))]
    public async Task<HttpResponseData> StartConversation(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "conversation/start")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Starting hybrid agent conversation");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var conversationRequest = JsonSerializer.Deserialize<ConversationRequest>(requestBody ?? "{}");

            if (conversationRequest == null || string.IsNullOrEmpty(conversationRequest.Message))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid conversation request. Message is required.");
                return badRequestResponse;
            }

            // Start the orchestration
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(CopilotStudioOrchestrator.HybridAgentConversationOrchestrator),
                conversationRequest);

            _logger.LogInformation("Started conversation orchestration with instance ID: {InstanceId}", instanceId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                instanceId = instanceId,
                statusQueryGetUri = $"{req.Url.Scheme}://{req.Url.Authority}/api/conversation/status/{instanceId}",
                message = "Conversation started successfully"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting conversation");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    /// <summary>
    /// Gets the status of a conversation
    /// </summary>
    [Function(nameof(GetConversationStatus))]
    public async Task<HttpResponseData> GetConversationStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "conversation/status/{instanceId}")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        string instanceId)
    {
        _logger.LogInformation("Getting conversation status for instance: {InstanceId}", instanceId);

        try
        {
            var status = await client.GetInstanceAsync(instanceId);
            
            if (status == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Conversation not found");
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                instanceId = status.InstanceId,
                runtimeStatus = status.RuntimeStatus.ToString(),
                createdAt = status.CreatedAt,
                lastUpdatedAt = status.LastUpdatedAt,
                customStatus = status.SerializedCustomStatus,
                output = status.SerializedOutput
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation status");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    /// <summary>
    /// Starts a multi-agent collaboration
    /// </summary>
    [Function(nameof(StartMultiAgentCollaboration))]
    public async Task<HttpResponseData> StartMultiAgentCollaboration(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "collaboration/start")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Starting multi-agent collaboration");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var collaborationRequest = JsonSerializer.Deserialize<MultiAgentRequest>(requestBody ?? "{}");

            if (collaborationRequest == null || string.IsNullOrEmpty(collaborationRequest.TaskDescription))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid collaboration request. TaskDescription is required.");
                return badRequestResponse;
            }

            // Start the orchestration
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(CopilotStudioOrchestrator.MultiAgentCollaborationOrchestrator),
                collaborationRequest);

            _logger.LogInformation("Started collaboration orchestration with instance ID: {InstanceId}", instanceId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                instanceId = instanceId,
                statusQueryGetUri = $"{req.Url.Scheme}://{req.Url.Authority}/api/collaboration/status/{instanceId}",
                message = "Multi-agent collaboration started successfully"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting multi-agent collaboration");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    /// <summary>
    /// Manages Copilot Studio topics
    /// </summary>
    [Function(nameof(ManageTopic))]
    public async Task<HttpResponseData> ManageTopic(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "topic/manage")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Managing Copilot Studio topic");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var topicRequest = JsonSerializer.Deserialize<TopicManagementRequest>(requestBody ?? "{}");

            if (topicRequest == null || string.IsNullOrEmpty(topicRequest.TopicId))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid topic request. TopicId is required.");
                return badRequestResponse;
            }

            // Start the orchestration
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(CopilotStudioOrchestrator.TopicBasedConversationOrchestrator),
                topicRequest);

            _logger.LogInformation("Started topic management orchestration with instance ID: {InstanceId}", instanceId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                instanceId = instanceId,
                statusQueryGetUri = $"{req.Url.Scheme}://{req.Url.Authority}/api/topic/status/{instanceId}",
                message = "Topic management started successfully"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error managing topic");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    /// <summary>
    /// Lists available Copilot Studio bots
    /// </summary>
    [Function(nameof(ListBots))]
    public async Task<HttpResponseData> ListBots(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "bots")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Listing Copilot Studio bots");

        try
        {
            // Start an activity to list bots
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                "ListBotsOrchestrator", "list");

            // For immediate response, we could call the activity directly,
            // but for consistency with the async pattern, we'll return the orchestration
            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                instanceId = instanceId,
                statusQueryGetUri = $"{req.Url.Scheme}://{req.Url.Authority}/api/conversation/status/{instanceId}",
                message = "Bot listing started"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing bots");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    /// <summary>
    /// Gets Power Platform environment information
    /// </summary>
    [Function(nameof(GetEnvironmentInfo))]
    public async Task<HttpResponseData> GetEnvironmentInfo(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "environment")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Getting Power Platform environment information");

        try
        {
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                "EnvironmentInfoOrchestrator", "info");

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                instanceId = instanceId,
                statusQueryGetUri = $"{req.Url.Scheme}://{req.Url.Authority}/api/conversation/status/{instanceId}",
                message = "Environment info retrieval started"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting environment info");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    /// <summary>
    /// Triggers a Power Automate flow
    /// </summary>
    [Function(nameof(TriggerFlow))]
    public async Task<HttpResponseData> TriggerFlow(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "flow/trigger")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Triggering Power Automate flow");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var flowRequest = JsonSerializer.Deserialize<PowerAutomateFlowRequest>(requestBody ?? "{}");

            if (flowRequest == null || string.IsNullOrEmpty(flowRequest.FlowId))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid flow request. FlowId is required.");
                return badRequestResponse;
            }

            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                "TriggerFlowOrchestrator", flowRequest);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                instanceId = instanceId,
                statusQueryGetUri = $"{req.Url.Scheme}://{req.Url.Authority}/api/conversation/status/{instanceId}",
                message = "Flow trigger started"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering flow");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [Function(nameof(HealthCheck))]
    public async Task<HttpResponseData> HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Health check requested");

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            service = "Copilot Studio Extensibility"
        });

        return response;
    }
}