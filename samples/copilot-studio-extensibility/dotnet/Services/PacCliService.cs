using CopilotStudioExtensibility.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace CopilotStudioExtensibility.Services;

/// <summary>
/// Service for executing Power Platform CLI (PAC CLI) commands
/// </summary>
public interface IPacCliService
{
    Task<bool> IsAuthenticatedAsync();
    Task<bool> AuthenticateAsync(string tenantId);
    Task<List<Dictionary<string, object>>> ListCopilotStudioBotsAsync();
    Task<Dictionary<string, object>> GetBotDetailsAsync(string botId);
    Task<bool> PublishBotAsync(string botId);
    Task<List<Dictionary<string, object>>> ListPowerAutomateFlowsAsync();
    Task<Dictionary<string, object>> GetEnvironmentInfoAsync();
}

/// <summary>
/// Implementation of PAC CLI service
/// </summary>
public class PacCliService : IPacCliService
{
    private readonly ILogger<PacCliService> _logger;
    private readonly string _environmentUrl;

    public PacCliService(ILogger<PacCliService> logger)
    {
        _logger = logger;
        _environmentUrl = Environment.GetEnvironmentVariable("POWER_PLATFORM_ENVIRONMENT_URL") 
            ?? throw new InvalidOperationException("POWER_PLATFORM_ENVIRONMENT_URL is not configured");
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var result = await ExecutePacCommandAsync("auth list");
            return result.Success && !string.IsNullOrEmpty(result.Output);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking PAC CLI authentication status");
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync(string tenantId)
    {
        try
        {
            _logger.LogInformation("Authenticating with PAC CLI for tenant {TenantId}", tenantId);
            
            var result = await ExecutePacCommandAsync($"auth create --url {_environmentUrl} --tenant {tenantId}");
            
            if (result.Success)
            {
                _logger.LogInformation("Successfully authenticated with PAC CLI");
                return true;
            }
            else
            {
                _logger.LogError("Failed to authenticate with PAC CLI: {Error}", result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PAC CLI authentication");
            return false;
        }
    }

    public async Task<List<Dictionary<string, object>>> ListCopilotStudioBotsAsync()
    {
        try
        {
            _logger.LogInformation("Listing Copilot Studio bots via PAC CLI");
            
            var result = await ExecutePacCommandAsync("chatbot list --json");
            
            if (!result.Success)
            {
                _logger.LogError("Failed to list Copilot Studio bots: {Error}", result.Error);
                return new List<Dictionary<string, object>>();
            }

            var bots = ParseJsonArrayOutput(result.Output);
            _logger.LogInformation("Found {Count} Copilot Studio bots", bots.Count);
            
            return bots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Copilot Studio bots");
            return new List<Dictionary<string, object>>();
        }
    }

    public async Task<Dictionary<string, object>> GetBotDetailsAsync(string botId)
    {
        try
        {
            _logger.LogInformation("Getting details for Copilot Studio bot {BotId}", botId);
            
            var result = await ExecutePacCommandAsync($"chatbot show --chatbot-id {botId} --json");
            
            if (!result.Success)
            {
                _logger.LogError("Failed to get bot details: {Error}", result.Error);
                return new Dictionary<string, object>();
            }

            var botDetails = ParseJsonObjectOutput(result.Output);
            return botDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bot details for {BotId}", botId);
            return new Dictionary<string, object>();
        }
    }

    public async Task<bool> PublishBotAsync(string botId)
    {
        try
        {
            _logger.LogInformation("Publishing Copilot Studio bot {BotId}", botId);
            
            var result = await ExecutePacCommandAsync($"chatbot publish --chatbot-id {botId}");
            
            if (result.Success)
            {
                _logger.LogInformation("Successfully published bot {BotId}", botId);
                return true;
            }
            else
            {
                _logger.LogError("Failed to publish bot {BotId}: {Error}", botId, result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing bot {BotId}", botId);
            return false;
        }
    }

    public async Task<List<Dictionary<string, object>>> ListPowerAutomateFlowsAsync()
    {
        try
        {
            _logger.LogInformation("Listing Power Automate flows via PAC CLI");
            
            var result = await ExecutePacCommandAsync("flow list --json");
            
            if (!result.Success)
            {
                _logger.LogError("Failed to list Power Automate flows: {Error}", result.Error);
                return new List<Dictionary<string, object>>();
            }

            var flows = ParseJsonArrayOutput(result.Output);
            _logger.LogInformation("Found {Count} Power Automate flows", flows.Count);
            
            return flows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Power Automate flows");
            return new List<Dictionary<string, object>>();
        }
    }

    public async Task<Dictionary<string, object>> GetEnvironmentInfoAsync()
    {
        try
        {
            _logger.LogInformation("Getting Power Platform environment information via PAC CLI");
            
            var result = await ExecutePacCommandAsync("env list --json");
            
            if (!result.Success)
            {
                _logger.LogError("Failed to get environment info: {Error}", result.Error);
                return new Dictionary<string, object>();
            }

            var environments = ParseJsonArrayOutput(result.Output);
            
            // Find the current environment
            var currentEnv = environments.FirstOrDefault(env => 
                env.ContainsKey("url") && env["url"].ToString() == _environmentUrl);
            
            return currentEnv ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting environment information");
            return new Dictionary<string, object>();
        }
    }

    private async Task<(bool Success, string Output, string Error)> ExecutePacCommandAsync(string command)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "pac",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) => {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) => {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for the process to complete with a timeout
            bool exited = await Task.Run(() => process.WaitForExit(30000)); // 30 second timeout

            if (!exited)
            {
                process.Kill();
                return (false, "", "Command timed out");
            }

            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();
            var success = process.ExitCode == 0;

            _logger.LogDebug("PAC CLI command executed: pac {Command} - Exit Code: {ExitCode}", 
                command, process.ExitCode);

            return (success, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing PAC CLI command: pac {Command}", command);
            return (false, "", ex.Message);
        }
    }

    private List<Dictionary<string, object>> ParseJsonArrayOutput(string output)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(output))
                return new List<Dictionary<string, object>>();

            var jsonArray = JsonSerializer.Deserialize<JsonElement[]>(output);
            var result = new List<Dictionary<string, object>>();

            foreach (var element in jsonArray)
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                if (dict != null)
                    result.Add(dict);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON array output: {Output}", output);
            return new List<Dictionary<string, object>>();
        }
    }

    private Dictionary<string, object> ParseJsonObjectOutput(string output)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(output))
                return new Dictionary<string, object>();

            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(output);
            return dict ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON object output: {Output}", output);
            return new Dictionary<string, object>();
        }
    }
}