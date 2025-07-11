using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using CopilotStudioExtensibility.Models;
using CopilotStudioExtensibility.Services;
using System.Text.Json;

namespace CopilotStudioExtensibility.Functions;

/// <summary>
/// Helper record for ExecuteAgentRequest input
/// </summary>
public record ExecuteAgentInput(ConversationRequest Request, AgentRoutingDecision Routing);

/// <summary>
/// Activity functions for Copilot Studio extensibility
/// </summary>
public class CopilotStudioActivities
{
    private readonly ILogger<CopilotStudioActivities> _logger;
    private readonly IAgentRoutingService _routingService;
    private readonly IPowerPlatformGraphService _powerPlatformService;
    private readonly IPacCliService _pacService;

    public CopilotStudioActivities(
        ILogger<CopilotStudioActivities> logger,
        IAgentRoutingService routingService,
        IPowerPlatformGraphService powerPlatformService,
        IPacCliService pacService)
    {
        _logger = logger;
        _routingService = routingService;
        _powerPlatformService = powerPlatformService;
        _pacService = pacService;
    }

    /// <summary>
    /// Determines which agent should handle a conversation request
    /// </summary>
    [Function(nameof(DetermineAgentRouting))]
    public async Task<AgentRoutingDecision> DetermineAgentRouting(
        [ActivityTrigger] ConversationRequest request)
    {
        _logger.LogInformation("Determining agent routing for user {UserId}", request.UserId);
        
        try
        {
            var decision = await _routingService.DetermineRoutingAsync(request);
            _logger.LogInformation("Routing decision: {Agent} (confidence: {Confidence}) - {Reason}",
                decision.SelectedAgent, decision.Confidence, decision.Reason);
            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining agent routing");
            return new AgentRoutingDecision(
                AgentType.CopilotStudio,
                "Error in routing, defaulting to Copilot Studio",
                0.5f
            );
        }
    }

    /// <summary>
    /// Executes a request with the specified agent
    /// </summary>
    [Function(nameof(ExecuteAgentRequest))]
    public async Task<AgentResponse> ExecuteAgentRequest(
        [ActivityTrigger] string input)
    {
        var inputData = JsonSerializer.Deserialize<ExecuteAgentInput>(input);
        
        if (inputData?.Request == null || inputData.Routing == null)
        {
            throw new ArgumentException("Invalid input parameters");
        }

        Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, 
            "Executing request with {Agent} for user {UserId}",
            inputData.Routing.SelectedAgent, inputData.Request.UserId);

        try
        {
            var response = await _routingService.RouteAndExecuteAsync(inputData.Request);
            Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, 
                "Request executed successfully with {Agent}", inputData.Routing.SelectedAgent);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing agent request");
            return new AgentResponse(
                Message: "I apologize, but I encountered an error processing your request. Please try again.",
                AgentType: "Error",
                AgentId: "system",
                Context: new Dictionary<string, object> { ["error"] = ex.Message }
            );
        }
    }

    /// <summary>
    /// Plans multi-agent collaboration steps
    /// </summary>
    [Function(nameof(PlanAgentCollaboration))]
    public async Task<List<AgentCollaborationStep>> PlanAgentCollaboration(
        [ActivityTrigger] MultiAgentRequest request)
    {
        _logger.LogInformation("Planning agent collaboration for task: {Task}", request.TaskDescription);

        try
        {
            var steps = new List<AgentCollaborationStep>();

            // Analyze required capabilities and create collaboration plan
            foreach (var capability in request.RequiredCapabilities)
            {
                var preferredAgents = capability.SupportedBy;
                var selectedAgent = SelectBestAgentForCapability(capability, preferredAgents);

                var step = new AgentCollaborationStep(
                    AgentType: selectedAgent,
                    Description: $"Handle {capability.Name}: {capability.Description}",
                    Prompt: CreatePromptForCapability(capability, request.TaskDescription),
                    InputContext: request.Context
                );

                steps.Add(step);
            }

            // If no specific capabilities were provided, create a general plan
            if (steps.Count == 0)
            {
                steps.AddRange(CreateGeneralCollaborationPlan(request));
            }

            _logger.LogInformation("Created collaboration plan with {StepCount} steps", steps.Count);
            return steps;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error planning agent collaboration");
            
            // Return a default plan with error handling
            return new List<AgentCollaborationStep>
            {
                new(AgentType.CopilotStudio, "Handle request with error recovery", 
                    $"Process this request with error handling: {request.TaskDescription}")
            };
        }
    }

    /// <summary>
    /// Manages Copilot Studio topics
    /// </summary>
    [Function(nameof(ManageTopic))]
    public async Task<AgentResponse> ManageTopic(
        [ActivityTrigger] TopicManagementRequest request)
    {
        _logger.LogInformation("Managing topic {TopicId} with action {Action}", 
            request.TopicId, request.Action);

        try
        {
            switch (request.Action)
            {
                case TopicAction.Start:
                    return await StartTopicAsync(request);
                
                case TopicAction.Continue:
                    return await ContinueTopicAsync(request);
                
                case TopicAction.Reset:
                    return await ResetTopicAsync(request);
                
                case TopicAction.Complete:
                    return await CompleteTopicAsync(request);
                
                case TopicAction.Escalate:
                    return await EscalateTopicAsync(request);
                
                default:
                    throw new ArgumentException($"Unknown topic action: {request.Action}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error managing topic {TopicId}", request.TopicId);
            return new AgentResponse(
                Message: $"Error managing topic {request.TopicId}: {ex.Message}",
                AgentType: "Error",
                AgentId: "topic-manager",
                Context: new Dictionary<string, object> { ["error"] = ex.Message, ["topicId"] = request.TopicId }
            );
        }
    }

    /// <summary>
    /// Triggers a Power Automate flow
    /// </summary>
    [Function(nameof(TriggerPowerAutomateFlow))]
    public async Task<PowerAutomateFlowResponse> TriggerPowerAutomateFlow(
        [ActivityTrigger] PowerAutomateFlowRequest request)
    {
        _logger.LogInformation("Triggering Power Automate flow {FlowId}", request.FlowId);

        try
        {
            var response = await _powerPlatformService.TriggerPowerAutomateFlowAsync(request);
            _logger.LogInformation("Power Automate flow triggered: {FlowId} - Status: {Status}", 
                request.FlowId, response.Status);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering Power Automate flow {FlowId}", request.FlowId);
            return new PowerAutomateFlowResponse(
                FlowId: request.FlowId,
                RunId: "",
                Status: "Error",
                ErrorMessage: ex.Message
            );
        }
    }

    /// <summary>
    /// Gets Power Platform environment information
    /// </summary>
    [Function(nameof(GetEnvironmentInfo))]
    public async Task<Dictionary<string, object>> GetEnvironmentInfo([ActivityTrigger] string input)
    {
        _logger.LogInformation("Getting Power Platform environment information");

        try
        {
            var environmentInfo = await _powerPlatformService.GetPowerPlatformEnvironmentInfoAsync();
            
            // Enhance with PAC CLI information
            var pacInfo = await _pacService.GetEnvironmentInfoAsync();
            if (pacInfo.Count > 0)
            {
                environmentInfo["pacCliInfo"] = pacInfo;
            }

            return environmentInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting environment information");
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }

    /// <summary>
    /// Lists available Copilot Studio bots
    /// </summary>
    [Function(nameof(ListCopilotStudioBots))]
    public async Task<List<Dictionary<string, object>>> ListCopilotStudioBots([ActivityTrigger] string input)
    {
        _logger.LogInformation("Listing Copilot Studio bots");

        try
        {
            var bots = await _pacService.ListCopilotStudioBotsAsync();
            _logger.LogInformation("Found {BotCount} Copilot Studio bots", bots.Count);
            return bots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Copilot Studio bots");
            return new List<Dictionary<string, object>>();
        }
    }

    private AgentType SelectBestAgentForCapability(AgentCapability capability, AgentType[] supportedAgents)
    {
        // Simple selection logic - prefer Copilot Studio for structured tasks, Azure AI for complex reasoning
        if (supportedAgents.Contains(AgentType.CopilotStudio) && 
            (capability.Name.Contains("workflow") || capability.Name.Contains("process")))
        {
            return AgentType.CopilotStudio;
        }
        
        if (supportedAgents.Contains(AgentType.AzureAI) && 
            (capability.Name.Contains("analysis") || capability.Name.Contains("creative")))
        {
            return AgentType.AzureAI;
        }

        // Default to first supported agent
        return supportedAgents.FirstOrDefault();
    }

    private string CreatePromptForCapability(AgentCapability capability, string taskDescription)
    {
        return $"Task: {taskDescription}\n\n" +
               $"Your specific role: {capability.Description}\n\n" +
               $"Focus on the {capability.Name} aspect of this task. " +
               "Provide a clear, actionable response that can be used by other agents in this collaboration.";
    }

    private List<AgentCollaborationStep> CreateGeneralCollaborationPlan(MultiAgentRequest request)
    {
        var task = request.TaskDescription.ToLowerInvariant();
        
        var steps = new List<AgentCollaborationStep>();
        
        // Start with Copilot Studio for structured analysis
        steps.Add(new AgentCollaborationStep(
            AgentType.CopilotStudio,
            "Initial analysis and structure",
            $"Analyze this request and provide a structured breakdown: {request.TaskDescription}"
        ));
        
        // Add Azure AI for complex reasoning if needed
        if (task.Contains("complex") || task.Contains("analyze") || task.Contains("creative"))
        {
            steps.Add(new AgentCollaborationStep(
                AgentType.AzureAI,
                "Advanced analysis and recommendations",
                $"Provide detailed analysis and recommendations for: {request.TaskDescription}"
            ));
        }
        
        // Add Power Automate integration if automation is mentioned
        if (task.Contains("automate") || task.Contains("flow") || task.Contains("trigger"))
        {
            steps.Add(new AgentCollaborationStep(
                AgentType.PowerAutomate,
                "Automation workflow",
                $"Design automation workflow for: {request.TaskDescription}"
            ));
        }
        
        return steps;
    }

    private async Task<AgentResponse> StartTopicAsync(TopicManagementRequest request)
    {
        var copilotRequest = new CopilotStudioRequest(
            BotId: request.BotId,
            UserId: "system",
            Message: $"Start topic: {request.TopicId}",
            Topic: request.TopicId,
            Variables: request.Parameters
        );

        var response = await _powerPlatformService.SendMessageToCopilotStudioAsync(copilotRequest);
        
        return new AgentResponse(
            Message: response.Message,
            AgentType: "CopilotStudio",
            AgentId: request.BotId,
            ConversationId: response.ConversationId,
            Context: response.Variables,
            RequiresFollowUp: !response.TopicCompleted,
            NextAction: response.NextTopic
        );
    }

    private async Task<AgentResponse> ContinueTopicAsync(TopicManagementRequest request)
    {
        var copilotRequest = new CopilotStudioRequest(
            BotId: request.BotId,
            UserId: "system",
            Message: "Continue with current topic",
            Topic: request.TopicId,
            Variables: request.Parameters
        );

        var response = await _powerPlatformService.SendMessageToCopilotStudioAsync(copilotRequest);
        
        return new AgentResponse(
            Message: response.Message,
            AgentType: "CopilotStudio",
            AgentId: request.BotId,
            ConversationId: response.ConversationId,
            Context: response.Variables,
            RequiresFollowUp: !response.TopicCompleted,
            NextAction: response.NextTopic
        );
    }

    private Task<AgentResponse> ResetTopicAsync(TopicManagementRequest request)
    {
        // Reset topic by starting it fresh
        return StartTopicAsync(request);
    }

    private Task<AgentResponse> CompleteTopicAsync(TopicManagementRequest request)
    {
        return Task.FromResult(new AgentResponse(
            Message: $"Topic {request.TopicId} has been completed successfully.",
            AgentType: "CopilotStudio",
            AgentId: request.BotId,
            Context: request.Parameters,
            RequiresFollowUp: false
        ));
    }

    private Task<AgentResponse> EscalateTopicAsync(TopicManagementRequest request)
    {
        return Task.FromResult(new AgentResponse(
            Message: $"Topic {request.TopicId} requires escalation to Azure AI for advanced assistance.",
            AgentType: "CopilotStudio",
            AgentId: request.BotId,
            Context: request.Parameters,
            RequiresFollowUp: true,
            NextAction: "escalate"
        ));
    }
}