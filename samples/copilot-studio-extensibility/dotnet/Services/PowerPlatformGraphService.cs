using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Authentication;
using Azure.Identity;
using CopilotStudioExtensibility.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CopilotStudioExtensibility.Services;

/// <summary>
/// Service for interacting with Microsoft Graph and Power Platform APIs
/// </summary>
public interface IPowerPlatformGraphService
{
    Task<CopilotStudioResponse> SendMessageToCopilotStudioAsync(CopilotStudioRequest request);
    Task<List<TopicStatus>> GetCopilotStudioTopicsAsync(string botId);
    Task<PowerAutomateFlowResponse> TriggerPowerAutomateFlowAsync(PowerAutomateFlowRequest request);
    Task<Dictionary<string, object>> GetPowerPlatformEnvironmentInfoAsync();
}

/// <summary>
/// Implementation of Power Platform Graph service using Microsoft Graph SDK
/// </summary>
public class PowerPlatformGraphService : IPowerPlatformGraphService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<PowerPlatformGraphService> _logger;
    private readonly string _environmentUrl;
    private readonly HttpClient _httpClient;

    public PowerPlatformGraphService(ILogger<PowerPlatformGraphService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _environmentUrl = Environment.GetEnvironmentVariable("POWER_PLATFORM_ENVIRONMENT_URL") 
            ?? throw new InvalidOperationException("POWER_PLATFORM_ENVIRONMENT_URL is not configured");

        // Initialize Graph client with appropriate scopes
        var credential = new DefaultAzureCredential();
        _graphClient = new GraphServiceClient(credential, new[] { 
            "https://graph.microsoft.com/.default",
            "https://service.powerapps.com/.default"
        });

        _logger.LogInformation("Initialized PowerPlatformGraphService for environment: {Environment}", _environmentUrl);
    }

    public async Task<CopilotStudioResponse> SendMessageToCopilotStudioAsync(CopilotStudioRequest request)
    {
        _logger.LogInformation("Sending message to Copilot Studio bot {BotId}", request.BotId);

        try
        {
            // Construct the Copilot Studio API endpoint
            var endpoint = $"{_environmentUrl}/api/botmanagement/v1/bots/{request.BotId}/conversations";
            
            // Prepare the request payload
            var payload = new
            {
                message = request.Message,
                userId = request.UserId,
                conversationId = request.ConversationId,
                topic = request.Topic,
                variables = request.Variables ?? new Dictionary<string, object>()
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            // Add authentication header
            var token = await GetPowerPlatformAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Send the request
            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send message to Copilot Studio: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Copilot Studio API error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            return new CopilotStudioResponse(
                Message: responseData.GetProperty("message").GetString() ?? "",
                ConversationId: responseData.GetProperty("conversationId").GetString() ?? request.ConversationId ?? "",
                Topic: responseData.TryGetProperty("topic", out var topicProp) ? topicProp.GetString() : null,
                Variables: responseData.TryGetProperty("variables", out var varsProp) ? 
                    JsonSerializer.Deserialize<Dictionary<string, object>>(varsProp.GetRawText()) : null,
                TopicCompleted: responseData.TryGetProperty("topicCompleted", out var completedProp) && completedProp.GetBoolean(),
                NextTopic: responseData.TryGetProperty("nextTopic", out var nextTopicProp) ? nextTopicProp.GetString() : null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Copilot Studio bot {BotId}", request.BotId);
            throw;
        }
    }

    public async Task<List<TopicStatus>> GetCopilotStudioTopicsAsync(string botId)
    {
        _logger.LogInformation("Retrieving topics for Copilot Studio bot {BotId}", botId);

        try
        {
            var endpoint = $"{_environmentUrl}/api/botmanagement/v1/bots/{botId}/topics";
            
            var token = await GetPowerPlatformAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to retrieve topics: {StatusCode}", response.StatusCode);
                return new List<TopicStatus>();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var topics = new List<TopicStatus>();
            if (responseData.TryGetProperty("topics", out var topicsArray))
            {
                foreach (var topicElement in topicsArray.EnumerateArray())
                {
                    topics.Add(new TopicStatus(
                        TopicId: topicElement.GetProperty("id").GetString() ?? "",
                        Name: topicElement.GetProperty("name").GetString() ?? "",
                        Status: topicElement.GetProperty("status").GetString() ?? "",
                        Variables: topicElement.TryGetProperty("variables", out var varsElement) ?
                            JsonSerializer.Deserialize<Dictionary<string, object>>(varsElement.GetRawText()) : null,
                        LastUserInput: topicElement.TryGetProperty("lastUserInput", out var inputElement) ?
                            inputElement.GetString() : null,
                        LastUpdated: topicElement.TryGetProperty("lastUpdated", out var updatedElement) ?
                            DateTime.Parse(updatedElement.GetString() ?? DateTime.UtcNow.ToString()) : DateTime.UtcNow
                    ));
                }
            }

            return topics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving topics for bot {BotId}", botId);
            return new List<TopicStatus>();
        }
    }

    public async Task<PowerAutomateFlowResponse> TriggerPowerAutomateFlowAsync(PowerAutomateFlowRequest request)
    {
        _logger.LogInformation("Triggering Power Automate flow {FlowId}", request.FlowId);

        try
        {
            var endpoint = $"{_environmentUrl}/api/flows/{request.FlowId}/triggers/{request.TriggerName}/run";
            
            var payload = new
            {
                inputs = request.InputData,
                metadata = new
                {
                    triggeredBy = request.UserId ?? "system",
                    triggeredAt = DateTime.UtcNow
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            var token = await GetPowerPlatformAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync(endpoint, content);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to trigger Power Automate flow: {StatusCode} - {Error}", 
                    response.StatusCode, responseContent);
                
                return new PowerAutomateFlowResponse(
                    FlowId: request.FlowId,
                    RunId: "",
                    Status: "Failed",
                    ErrorMessage: $"HTTP {response.StatusCode}: {responseContent}"
                );
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            return new PowerAutomateFlowResponse(
                FlowId: request.FlowId,
                RunId: responseData.GetProperty("runId").GetString() ?? "",
                Status: responseData.GetProperty("status").GetString() ?? "Unknown",
                OutputData: responseData.TryGetProperty("outputs", out var outputsProp) ?
                    JsonSerializer.Deserialize<Dictionary<string, object>>(outputsProp.GetRawText()) : null
            );
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

    public async Task<Dictionary<string, object>> GetPowerPlatformEnvironmentInfoAsync()
    {
        _logger.LogInformation("Retrieving Power Platform environment information");

        try
        {
            var endpoint = $"{_environmentUrl}/api/environments/current";
            
            var token = await GetPowerPlatformAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve environment info: {StatusCode}", response.StatusCode);
                return new Dictionary<string, object>
                {
                    ["error"] = $"HTTP {response.StatusCode}",
                    ["environmentUrl"] = _environmentUrl
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var environmentInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            
            return environmentInfo ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Power Platform environment information");
            return new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["environmentUrl"] = _environmentUrl
            };
        }
    }

    private async Task<string> GetPowerPlatformAccessTokenAsync()
    {
        try
        {
            var credential = new DefaultAzureCredential();
            var tokenContext = new Azure.Core.TokenRequestContext(new[] { "https://service.powerapps.com/.default" });
            var token = await credential.GetTokenAsync(tokenContext);
            return token.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain Power Platform access token");
            throw;
        }
    }
}