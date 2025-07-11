using System.Text.Json.Serialization;

namespace CopilotStudioExtensibility.Models;

/// <summary>
/// Represents a conversation request that can be routed to different agents
/// </summary>
public record ConversationRequest(
    string UserId,
    string Message,
    string? ConversationId = null,
    Dictionary<string, object>? Context = null,
    AgentRoutingPreference RoutingPreference = AgentRoutingPreference.Auto
);

/// <summary>
/// Response from any agent (Copilot Studio or Azure AI)
/// </summary>
public record AgentResponse(
    string Message,
    string AgentType,
    string AgentId,
    string? ConversationId = null,
    Dictionary<string, object>? Context = null,
    bool RequiresFollowUp = false,
    string? NextAction = null
);

/// <summary>
/// Copilot Studio specific request
/// </summary>
public record CopilotStudioRequest(
    string BotId,
    string UserId,
    string Message,
    string? ConversationId = null,
    string? Topic = null,
    Dictionary<string, object>? Variables = null
);

/// <summary>
/// Copilot Studio response
/// </summary>
public record CopilotStudioResponse(
    string Message,
    string ConversationId,
    string? Topic = null,
    Dictionary<string, object>? Variables = null,
    bool TopicCompleted = false,
    string? NextTopic = null
);

/// <summary>
/// Power Automate flow trigger request
/// </summary>
public record PowerAutomateFlowRequest(
    string FlowId,
    string TriggerName,
    Dictionary<string, object> InputData,
    string? UserId = null
);

/// <summary>
/// Power Automate flow response
/// </summary>
public record PowerAutomateFlowResponse(
    string FlowId,
    string RunId,
    string Status,
    Dictionary<string, object>? OutputData = null,
    string? ErrorMessage = null
);

/// <summary>
/// Agent routing decision
/// </summary>
public record AgentRoutingDecision(
    AgentType SelectedAgent,
    string Reason,
    float Confidence,
    Dictionary<string, object>? RoutingContext = null
);

/// <summary>
/// Multi-agent collaboration request
/// </summary>
public record MultiAgentRequest(
    string TaskDescription,
    List<AgentCapability> RequiredCapabilities,
    string UserId,
    Dictionary<string, object>? Context = null
);

/// <summary>
/// Agent capability definition
/// </summary>
public record AgentCapability(
    string Name,
    string Description,
    AgentType[] SupportedBy,
    bool IsRequired = true
);

/// <summary>
/// Agent routing preferences
/// </summary>
public enum AgentRoutingPreference
{
    Auto,
    PreferCopilotStudio,
    PreferAzureAI,
    CopilotStudioOnly,
    AzureAIOnly
}

/// <summary>
/// Agent types
/// </summary>
public enum AgentType
{
    CopilotStudio,
    AzureAI,
    PowerAutomate,
    Hybrid
}

/// <summary>
/// Topic management request
/// </summary>
public record TopicManagementRequest(
    string BotId,
    string TopicId,
    TopicAction Action,
    Dictionary<string, object>? Parameters = null
);

/// <summary>
/// Topic actions
/// </summary>
public enum TopicAction
{
    Start,
    Continue,
    Reset,
    Complete,
    Escalate
}

/// <summary>
/// Topic status
/// </summary>
public record TopicStatus(
    string TopicId,
    string Name,
    string Status,
    Dictionary<string, object>? Variables = null,
    string? LastUserInput = null,
    DateTime LastUpdated = default
);