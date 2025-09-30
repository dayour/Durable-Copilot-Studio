using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using CopilotStudioExtensibility.Models;
using CopilotStudioExtensibility.Services;

namespace CopilotStudioExtensibility.Functions;

/// <summary>
/// Main orchestrator for Copilot Studio extensibility scenarios
/// </summary>
public class CopilotStudioOrchestrator
{
    private readonly ILogger<CopilotStudioOrchestrator> _logger;

    public CopilotStudioOrchestrator(ILogger<CopilotStudioOrchestrator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Orchestrates hybrid agent conversations
    /// </summary>
    [Function(nameof(HybridAgentConversationOrchestrator))]
    public async Task<AgentResponse> HybridAgentConversationOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var request = context.GetInput<ConversationRequest>()
            ?? throw new ArgumentNullException(nameof(context), "Conversation request is required");

        var logger = context.CreateReplaySafeLogger<CopilotStudioOrchestrator>();
        logger.LogInformation("Starting hybrid agent conversation for user {UserId}", request.UserId);

        // Set initial status
        context.SetCustomStatus(new
        {
            step = "Analyzing",
            message = "Analyzing your request to determine the best agent...",
            progress = 10
        });

        try
        {
            // Step 1: Determine routing
            logger.LogInformation("Step 1: Determining agent routing");
            context.SetCustomStatus(new
            {
                step = "Routing",
                message = "Selecting the most appropriate agent for your request...",
                progress = 30
            });

            var routingDecision = await context.CallActivityAsync<AgentRoutingDecision>(
                nameof(CopilotStudioActivities.DetermineAgentRouting), request);

            logger.LogInformation("Routing decision: {Agent} (confidence: {Confidence})",
                routingDecision.SelectedAgent, routingDecision.Confidence);

            // Step 2: Execute with chosen agent
            context.SetCustomStatus(new
            {
                step = "Processing",
                message = $"Processing your request with {routingDecision.SelectedAgent}...",
                progress = 60,
                agent = routingDecision.SelectedAgent.ToString(),
                confidence = routingDecision.Confidence
            });

            var response = await context.CallActivityAsync<AgentResponse>(
                nameof(CopilotStudioActivities.ExecuteAgentRequest), 
                new { Request = request, Routing = routingDecision });

            // Step 3: Check if follow-up or escalation is needed
            if (response.RequiresFollowUp && routingDecision.SelectedAgent == AgentType.CopilotStudio)
            {
                logger.LogInformation("Step 3: Handling follow-up or escalation");
                context.SetCustomStatus(new
                {
                    step = "FollowUp",
                    message = "Processing follow-up actions...",
                    progress = 80
                });

                var followUpResponse = await HandleFollowUpAsync(context, request, response);
                if (followUpResponse != null)
                {
                    response = followUpResponse;
                }
            }

            // Step 4: Final processing
            context.SetCustomStatus(new
            {
                step = "Completed",
                message = "Request completed successfully",
                progress = 100,
                agent = response.AgentType
            });

            logger.LogInformation("Hybrid agent conversation completed for user {UserId}", request.UserId);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in hybrid agent conversation for user {UserId}", request.UserId);
            
            // Return error response
            return new AgentResponse(
                Message: "I apologize, but I encountered an error processing your request. Please try again.",
                AgentType: "Error",
                AgentId: "system",
                Context: new Dictionary<string, object> { ["error"] = ex.Message }
            );
        }
    }

    /// <summary>
    /// Orchestrates multi-agent collaboration scenarios
    /// </summary>
    [Function(nameof(MultiAgentCollaborationOrchestrator))]
    public async Task<List<AgentResponse>> MultiAgentCollaborationOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var request = context.GetInput<MultiAgentRequest>()
            ?? throw new ArgumentNullException(nameof(context), "Multi-agent request is required");

        var logger = context.CreateReplaySafeLogger<CopilotStudioOrchestrator>();
        logger.LogInformation("Starting multi-agent collaboration for task: {Task}", request.TaskDescription);

        var responses = new List<AgentResponse>();

        try
        {
            context.SetCustomStatus(new
            {
                step = "Planning",
                message = "Planning multi-agent collaboration...",
                progress = 10
            });

            // Step 1: Plan agent collaboration
            var collaborationPlan = await context.CallActivityAsync<List<AgentCollaborationStep>>(
                nameof(CopilotStudioActivities.PlanAgentCollaboration), request);

            logger.LogInformation("Collaboration plan created with {StepCount} steps", collaborationPlan.Count);

            // Step 2: Execute each step
            for (int i = 0; i < collaborationPlan.Count; i++)
            {
                var step = collaborationPlan[i];
                var progress = 20 + (60 * (i + 1) / collaborationPlan.Count);

                context.SetCustomStatus(new
                {
                    step = $"Executing Step {i + 1}",
                    message = step.Description,
                    progress = progress,
                    currentAgent = step.AgentType.ToString()
                });

                logger.LogInformation("Executing step {StepNumber}: {Description} with {Agent}",
                    i + 1, step.Description, step.AgentType);

                var stepRequest = new ConversationRequest(
                    UserId: request.UserId,
                    Message: step.Prompt,
                    Context: step.InputContext,
                    RoutingPreference: GetRoutingPreference(step.AgentType)
                );

                var stepResponse = await context.CallActivityAsync<AgentResponse>(
                    nameof(CopilotStudioActivities.ExecuteAgentRequest),
                    new { Request = stepRequest, Routing = new AgentRoutingDecision(step.AgentType, step.Description, 1.0f) });

                responses.Add(stepResponse);

                // Pass context to next step
                if (i < collaborationPlan.Count - 1)
                {
                    collaborationPlan[i + 1].InputContext = MergeContexts(
                        collaborationPlan[i + 1].InputContext,
                        stepResponse.Context
                    );
                }
            }

            context.SetCustomStatus(new
            {
                step = "Completed",
                message = "Multi-agent collaboration completed successfully",
                progress = 100,
                agentsUsed = responses.Select(r => r.AgentType).Distinct().ToList()
            });

            logger.LogInformation("Multi-agent collaboration completed with {ResponseCount} responses", responses.Count);
            return responses;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in multi-agent collaboration");
            
            responses.Add(new AgentResponse(
                Message: "I encountered an error during the multi-agent collaboration. Please try again.",
                AgentType: "Error",
                AgentId: "system",
                Context: new Dictionary<string, object> { ["error"] = ex.Message }
            ));

            return responses;
        }
    }

    /// <summary>
    /// Orchestrates topic-based conversations with Copilot Studio
    /// </summary>
    [Function(nameof(TopicBasedConversationOrchestrator))]
    public async Task<AgentResponse> TopicBasedConversationOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var request = context.GetInput<TopicManagementRequest>()
            ?? throw new ArgumentNullException(nameof(context), "Topic management request is required");

        var logger = context.CreateReplaySafeLogger<CopilotStudioOrchestrator>();
        logger.LogInformation("Starting topic-based conversation for topic {TopicId}", request.TopicId);

        try
        {
            context.SetCustomStatus(new
            {
                step = "Managing Topic",
                message = $"Managing topic: {request.TopicId}",
                progress = 20,
                topic = request.TopicId,
                action = request.Action.ToString()
            });

            // Execute topic action
            var response = await context.CallActivityAsync<AgentResponse>(
                nameof(CopilotStudioActivities.ManageTopic), request);

            // Check if escalation is needed
            if (response.NextAction == "escalate")
            {
                logger.LogInformation("Topic escalation requested for {TopicId}", request.TopicId);
                
                context.SetCustomStatus(new
                {
                    step = "Escalating",
                    message = "Escalating to Azure AI for advanced assistance...",
                    progress = 70
                });

                var escalationRequest = new ConversationRequest(
                    UserId: "system",
                    Message: $"Handle escalation from Copilot Studio topic {request.TopicId}: {response.Message}",
                    Context: response.Context,
                    RoutingPreference: AgentRoutingPreference.AzureAIOnly
                );

                var escalationResponse = await context.CallActivityAsync<AgentResponse>(
                    nameof(CopilotStudioActivities.ExecuteAgentRequest),
                    new { Request = escalationRequest, Routing = new AgentRoutingDecision(AgentType.AzureAI, "Topic escalation", 1.0f) });

                response = new AgentResponse(
                    Message: $"{response.Message}\n\n[Escalated Response]\n{escalationResponse.Message}",
                    AgentType: "Hybrid",
                    AgentId: "topic-escalation",
                    ConversationId: response.ConversationId,
                    Context: MergeContexts(response.Context, escalationResponse.Context)
                );
            }

            context.SetCustomStatus(new
            {
                step = "Completed",
                message = "Topic conversation completed",
                progress = 100,
                topic = request.TopicId
            });

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in topic-based conversation for {TopicId}", request.TopicId);
            
            return new AgentResponse(
                Message: "I encountered an error managing the topic. Please try again.",
                AgentType: "Error",
                AgentId: "system",
                Context: new Dictionary<string, object> { ["error"] = ex.Message, ["topicId"] = request.TopicId }
            );
        }
    }

    private async Task<AgentResponse?> HandleFollowUpAsync(
        TaskOrchestrationContext context, 
        ConversationRequest originalRequest, 
        AgentResponse response)
    {
        if (response.NextAction == "escalate")
        {
            // Escalate to Azure AI
            var escalationRequest = new ConversationRequest(
                UserId: originalRequest.UserId,
                Message: $"Continue conversation: {response.Message}. Original request: {originalRequest.Message}",
                ConversationId: response.ConversationId,
                Context: response.Context,
                RoutingPreference: AgentRoutingPreference.AzureAIOnly
            );

            return await context.CallActivityAsync<AgentResponse>(
                nameof(CopilotStudioActivities.ExecuteAgentRequest),
                new { Request = escalationRequest, Routing = new AgentRoutingDecision(AgentType.AzureAI, "Follow-up escalation", 1.0f) });
        }

        return null;
    }

    private AgentRoutingPreference GetRoutingPreference(AgentType agentType)
    {
        return agentType switch
        {
            AgentType.CopilotStudio => AgentRoutingPreference.CopilotStudioOnly,
            AgentType.AzureAI => AgentRoutingPreference.AzureAIOnly,
            _ => AgentRoutingPreference.Auto
        };
    }

    private Dictionary<string, object>? MergeContexts(Dictionary<string, object>? context1, Dictionary<string, object>? context2)
    {
        if (context1 == null) return context2;
        if (context2 == null) return context1;
        
        var merged = new Dictionary<string, object>(context1);
        foreach (var kvp in context2)
        {
            merged[kvp.Key] = kvp.Value;
        }
        return merged;
    }
}

/// <summary>
/// Represents a step in multi-agent collaboration
/// </summary>
public record AgentCollaborationStep(
    AgentType AgentType,
    string Description,
    string Prompt,
    Dictionary<string, object>? InputContext = null
)
{
    public Dictionary<string, object>? InputContext { get; set; } = InputContext;
}