using CopilotStudioExtensibility.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;

namespace CopilotStudioExtensibility.Services;

/// <summary>
/// Service for routing conversations to the appropriate agent
/// </summary>
public interface IAgentRoutingService
{
    Task<AgentRoutingDecision> DetermineRoutingAsync(ConversationRequest request);
    Task<AgentResponse> RouteAndExecuteAsync(ConversationRequest request);
    Task<bool> CanHandleRequestAsync(AgentType agentType, string request);
}

/// <summary>
/// Implementation of agent routing service using AI-powered decision making
/// </summary>
public class AgentRoutingService : IAgentRoutingService
{
    private readonly ILogger<AgentRoutingService> _logger;
    private readonly IPowerPlatformGraphService _powerPlatformService;
    private readonly AgentsClient? _azureAIClient;
    private readonly string _azureAIAgentId;

    public AgentRoutingService(
        ILogger<AgentRoutingService> logger,
        IPowerPlatformGraphService powerPlatformService)
    {
        _logger = logger;
        _powerPlatformService = powerPlatformService;

        // Initialize Azure AI client if configured
        var azureAIEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_ENDPOINT");
        var azureAIKey = Environment.GetEnvironmentVariable("AZURE_AI_KEY");
        _azureAIAgentId = Environment.GetEnvironmentVariable("AZURE_AI_AGENT_ID") ?? "";

        if (!string.IsNullOrEmpty(azureAIEndpoint))
        {
            try
            {
                _azureAIClient = new AgentsClient(azureAIEndpoint, new DefaultAzureCredential());
                _logger.LogInformation("Initialized Azure AI client for routing decisions");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Azure AI client, using rule-based routing");
            }
        }
    }

    public async Task<AgentRoutingDecision> DetermineRoutingAsync(ConversationRequest request)
    {
        _logger.LogInformation("Determining routing for user {UserId} with message: {Message}", 
            request.UserId, request.Message);

        try
        {
            // Handle explicit routing preferences
            if (request.RoutingPreference != AgentRoutingPreference.Auto)
            {
                return HandleExplicitRouting(request);
            }

            // Use AI-powered routing if Azure AI client is available
            if (_azureAIClient != null && !string.IsNullOrEmpty(_azureAIAgentId))
            {
                return await DetermineRoutingWithAIAsync(request);
            }

            // Fall back to rule-based routing
            return DetermineRoutingWithRules(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining routing for request");
            
            // Default to Copilot Studio for error cases
            return new AgentRoutingDecision(
                SelectedAgent: AgentType.CopilotStudio,
                Reason: "Error in routing logic, defaulting to Copilot Studio",
                Confidence: 0.5f,
                RoutingContext: new Dictionary<string, object> { ["error"] = ex.Message }
            );
        }
    }

    public async Task<AgentResponse> RouteAndExecuteAsync(ConversationRequest request)
    {
        _logger.LogInformation("Routing and executing request for user {UserId}", request.UserId);

        var routingDecision = await DetermineRoutingAsync(request);
        
        _logger.LogInformation("Routing decision: {Agent} (confidence: {Confidence}) - {Reason}",
            routingDecision.SelectedAgent, routingDecision.Confidence, routingDecision.Reason);

        return routingDecision.SelectedAgent switch
        {
            AgentType.CopilotStudio => await ExecuteCopilotStudioRequestAsync(request, routingDecision),
            AgentType.AzureAI => await ExecuteAzureAIRequestAsync(request, routingDecision),
            AgentType.Hybrid => await ExecuteHybridRequestAsync(request, routingDecision),
            _ => throw new NotSupportedException($"Agent type {routingDecision.SelectedAgent} not supported")
        };
    }

    public async Task<bool> CanHandleRequestAsync(AgentType agentType, string request)
    {
        try
        {
            return agentType switch
            {
                AgentType.CopilotStudio => await CanCopilotStudioHandleAsync(request),
                AgentType.AzureAI => await CanAzureAIHandleAsync(request),
                AgentType.PowerAutomate => await CanPowerAutomateHandleAsync(request),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if agent {AgentType} can handle request", agentType);
            return false;
        }
    }

    private AgentRoutingDecision HandleExplicitRouting(ConversationRequest request)
    {
        return request.RoutingPreference switch
        {
            AgentRoutingPreference.PreferCopilotStudio => new AgentRoutingDecision(
                AgentType.CopilotStudio, "User preference for Copilot Studio", 1.0f),
            
            AgentRoutingPreference.PreferAzureAI => new AgentRoutingDecision(
                AgentType.AzureAI, "User preference for Azure AI", 1.0f),
            
            AgentRoutingPreference.CopilotStudioOnly => new AgentRoutingDecision(
                AgentType.CopilotStudio, "User explicitly requested Copilot Studio only", 1.0f),
            
            AgentRoutingPreference.AzureAIOnly => new AgentRoutingDecision(
                AgentType.AzureAI, "User explicitly requested Azure AI only", 1.0f),
            
            _ => DetermineRoutingWithRules(request)
        };
    }

    private async Task<AgentRoutingDecision> DetermineRoutingWithAIAsync(ConversationRequest request)
    {
        try
        {
            var prompt = $@"
Analyze this user message and determine the best agent to handle it:
Message: ""{request.Message}""
User ID: {request.UserId}
Context: {JsonSerializer.Serialize(request.Context ?? new Dictionary<string, object>())}

Available agents:
1. CopilotStudio - Best for: conversational flows, structured dialogs, low-code scenarios, business processes
2. AzureAI - Best for: complex reasoning, content generation, technical queries, open-ended conversations
3. Hybrid - Best for: requests requiring both structured flows and advanced AI capabilities

Respond with a JSON object containing:
{{
    ""selectedAgent"": ""CopilotStudio"" | ""AzureAI"" | ""Hybrid"",
    ""reason"": ""detailed explanation of why this agent was chosen"",
    ""confidence"": 0.0-1.0,
    ""requiresFollowUp"": true/false
}}";

            var response = await GetAIResponseAsync(prompt);
            var decision = JsonSerializer.Deserialize<AIRoutingResponse>(response);

            if (decision != null && Enum.TryParse<AgentType>(decision.SelectedAgent, out var agentType))
            {
                return new AgentRoutingDecision(
                    SelectedAgent: agentType,
                    Reason: decision.Reason,
                    Confidence: decision.Confidence,
                    RoutingContext: new Dictionary<string, object>
                    {
                        ["aiDecision"] = true,
                        ["requiresFollowUp"] = decision.RequiresFollowUp
                    }
                );
            }
            
            // Fall back to rule-based if AI response parsing fails
            _logger.LogWarning("Failed to parse AI routing response, falling back to rules");
            return DetermineRoutingWithRules(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using AI for routing decision");
            return DetermineRoutingWithRules(request);
        }
    }

    private AgentRoutingDecision DetermineRoutingWithRules(ConversationRequest request)
    {
        var message = request.Message.ToLowerInvariant();
        
        // Keywords that suggest Copilot Studio is appropriate
        var copilotStudioKeywords = new[] { "workflow", "process", "step", "form", "approval", "business", "policy", "procedure" };
        
        // Keywords that suggest Azure AI is appropriate
        var azureAIKeywords = new[] { "explain", "analyze", "creative", "generate", "complex", "reasoning", "technical", "code" };
        
        var copilotStudioScore = copilotStudioKeywords.Count(keyword => message.Contains(keyword));
        var azureAIScore = azureAIKeywords.Count(keyword => message.Contains(keyword));
        
        if (copilotStudioScore > azureAIScore)
        {
            return new AgentRoutingDecision(
                AgentType.CopilotStudio,
                $"Message contains {copilotStudioScore} Copilot Studio keywords indicating structured process",
                Math.Min(0.6f + (copilotStudioScore * 0.1f), 1.0f)
            );
        }
        else if (azureAIScore > copilotStudioScore)
        {
            return new AgentRoutingDecision(
                AgentType.AzureAI,
                $"Message contains {azureAIScore} Azure AI keywords indicating complex reasoning needed",
                Math.Min(0.6f + (azureAIScore * 0.1f), 1.0f)
            );
        }
        else
        {
            // Default to Copilot Studio for balanced or unclear cases
            return new AgentRoutingDecision(
                AgentType.CopilotStudio,
                "No clear indicators, defaulting to Copilot Studio for structured handling",
                0.5f
            );
        }
    }

    private async Task<AgentResponse> ExecuteCopilotStudioRequestAsync(ConversationRequest request, AgentRoutingDecision decision)
    {
        var copilotRequest = new CopilotStudioRequest(
            BotId: Environment.GetEnvironmentVariable("COPILOT_STUDIO_BOT_ID") ?? "",
            UserId: request.UserId,
            Message: request.Message,
            ConversationId: request.ConversationId,
            Variables: request.Context
        );

        var response = await _powerPlatformService.SendMessageToCopilotStudioAsync(copilotRequest);
        
        return new AgentResponse(
            Message: response.Message,
            AgentType: "CopilotStudio",
            AgentId: copilotRequest.BotId,
            ConversationId: response.ConversationId,
            Context: response.Variables,
            RequiresFollowUp: !response.TopicCompleted,
            NextAction: response.NextTopic
        );
    }

    private async Task<AgentResponse> ExecuteAzureAIRequestAsync(ConversationRequest request, AgentRoutingDecision decision)
    {
        if (_azureAIClient == null)
        {
            throw new InvalidOperationException("Azure AI client not initialized");
        }

        // Create a thread and send the message
        var threadResponse = await _azureAIClient.CreateThreadAsync();
        var thread = threadResponse.Value;

        await _azureAIClient.CreateMessageAsync(thread.Id, Azure.AI.Projects.MessageRole.User, request.Message);
        
        var runResponse = await _azureAIClient.CreateRunAsync(thread.Id, _azureAIAgentId);
        var run = runResponse.Value;

        // Poll for completion
        while (run.Status == Azure.AI.Projects.RunStatus.Queued || run.Status == Azure.AI.Projects.RunStatus.InProgress)
        {
            await Task.Delay(1000);
            runResponse = await _azureAIClient.GetRunAsync(thread.Id, run.Id);
            run = runResponse.Value;
        }

        // Get the response
        var messages = await _azureAIClient.GetMessagesAsync(thread.Id);
        var lastMessage = messages.Value.FirstOrDefault(m => m.Role == Azure.AI.Projects.MessageRole.Agent);
        
        var responseText = "";
        if (lastMessage?.ContentItems.FirstOrDefault() is Azure.AI.Projects.MessageTextContent textContent)
        {
            responseText = textContent.Text;
        }

        return new AgentResponse(
            Message: responseText,
            AgentType: "AzureAI",
            AgentId: _azureAIAgentId,
            ConversationId: thread.Id,
            Context: request.Context,
            RequiresFollowUp: false
        );
    }

    private async Task<AgentResponse> ExecuteHybridRequestAsync(ConversationRequest request, AgentRoutingDecision decision)
    {
        // For hybrid scenarios, we'll typically start with Copilot Studio and escalate to Azure AI if needed
        var copilotResponse = await ExecuteCopilotStudioRequestAsync(request, decision);
        
        // Check if escalation to Azure AI is needed
        if (copilotResponse.RequiresFollowUp && copilotResponse.NextAction == "escalate")
        {
            var azureAIResponse = await ExecuteAzureAIRequestAsync(request, decision);
            
            return new AgentResponse(
                Message: $"{copilotResponse.Message}\n\n[Escalated to Azure AI]\n{azureAIResponse.Message}",
                AgentType: "Hybrid",
                AgentId: "hybrid-routing",
                ConversationId: copilotResponse.ConversationId,
                Context: MergeContexts(copilotResponse.Context, azureAIResponse.Context),
                RequiresFollowUp: azureAIResponse.RequiresFollowUp
            );
        }
        
        return copilotResponse;
    }

    private async Task<string> GetAIResponseAsync(string prompt)
    {
        if (_azureAIClient == null) throw new InvalidOperationException("Azure AI client not available");

        var threadResponse = await _azureAIClient.CreateThreadAsync();
        var thread = threadResponse.Value;

        await _azureAIClient.CreateMessageAsync(thread.Id, Azure.AI.Projects.MessageRole.User, prompt);
        
        var runResponse = await _azureAIClient.CreateRunAsync(thread.Id, _azureAIAgentId);
        var run = runResponse.Value;

        while (run.Status == Azure.AI.Projects.RunStatus.Queued || run.Status == Azure.AI.Projects.RunStatus.InProgress)
        {
            await Task.Delay(1000);
            runResponse = await _azureAIClient.GetRunAsync(thread.Id, run.Id);
            run = runResponse.Value;
        }

        var messages = await _azureAIClient.GetMessagesAsync(thread.Id);
        var lastMessage = messages.Value.FirstOrDefault(m => m.Role == Azure.AI.Projects.MessageRole.Agent);
        
        if (lastMessage?.ContentItems.FirstOrDefault() is Azure.AI.Projects.MessageTextContent textContent)
        {
            return textContent.Text;
        }
        
        return "{}";
    }

    private Task<bool> CanCopilotStudioHandleAsync(string request)
    {
        // Simple heuristics - in a real implementation, this might check available topics
        var businessKeywords = new[] { "process", "workflow", "approval", "form", "business", "policy" };
        return Task.FromResult(businessKeywords.Any(keyword => request.ToLowerInvariant().Contains(keyword)));
    }

    private Task<bool> CanAzureAIHandleAsync(string request)
    {
        // Azure AI can handle most requests, but we'll check for complexity indicators
        var complexKeywords = new[] { "explain", "analyze", "creative", "generate", "complex", "technical" };
        return Task.FromResult(complexKeywords.Any(keyword => request.ToLowerInvariant().Contains(keyword)));
    }

    private Task<bool> CanPowerAutomateHandleAsync(string request)
    {
        var automationKeywords = new[] { "automate", "trigger", "flow", "schedule", "integration" };
        return Task.FromResult(automationKeywords.Any(keyword => request.ToLowerInvariant().Contains(keyword)));
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

    private record AIRoutingResponse(string SelectedAgent, string Reason, float Confidence, bool RequiresFollowUp);
}