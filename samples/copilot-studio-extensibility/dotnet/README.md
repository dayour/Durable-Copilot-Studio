# Copilot Studio Extensibility with Azure Durable Functions

This sample demonstrates how to build robust, extensible orchestrations that seamlessly integrate Copilot Studio agents from the Power Platform with Azure AI Foundry agents using Azure Durable Functions.

## Features

- **Copilot Studio Integration**: Direct integration with Copilot Studio agents using REST APIs and Microsoft Graph
- **Power Platform Connectivity**: Support for Power Automate flows, Dataverse connections, and low-code agents
- **Hybrid Agent Orchestration**: Route conversations between Copilot Studio and Azure AI agents intelligently
- **Topic Management**: Handle Copilot Studio topics and conversation flows
- **PAC CLI Integration**: Use Power Platform CLI for agent management and deployment
- **Microsoft Graph Integration**: Leverage Connect-MgGraph for enhanced Power Platform connectivity

## Architecture

```
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│   Copilot Studio    │    │   Azure Durable     │    │   Azure AI Foundry  │
│      Agents         │◄──►│     Functions       │◄──►│      Agents         │
│                     │    │   Orchestrator      │    │                     │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
         │                           │                           │
         ▼                           ▼                           ▼
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│   Power Automate    │    │   Microsoft Graph   │    │   Azure OpenAI      │
│      Flows          │    │     APIs            │    │     Service         │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
```

## Prerequisites

- .NET 8.0 or later
- Azure Functions Core Tools
- Power Platform CLI (PAC CLI)
- Azure subscription with Durable Task Scheduler
- Power Platform environment with Copilot Studio access
- Microsoft Graph API permissions

## Getting Started

1. Clone this repository
2. Navigate to this directory
3. Configure your environment variables (see Configuration section)
4. Build and run the sample

```bash
dotnet build
func start
```

## Configuration

Set the following environment variables:

```bash
# Azure Configuration
AZURE_TENANT_ID="your-tenant-id"
AZURE_CLIENT_ID="your-client-id"
AZURE_CLIENT_SECRET="your-client-secret"

# Power Platform Configuration
POWER_PLATFORM_ENVIRONMENT_URL="https://yourenv.crm.dynamics.com"
COPILOT_STUDIO_BOT_ID="your-copilot-studio-bot-id"

# Azure AI Configuration
AZURE_AI_ENDPOINT="https://your-ai-endpoint.cognitiveservices.azure.com"
AZURE_AI_KEY="your-ai-key"

# Durable Task Scheduler
TASKHUB="copilot-extensibility"
ENDPOINT="your-dts-endpoint"
```

## Sample Scenarios

This sample includes several orchestration patterns:

1. **Hybrid Agent Routing**: Route user queries to the most appropriate agent (Copilot Studio vs Azure AI)
2. **Topic-Based Conversations**: Handle Copilot Studio topics and escalate to Azure AI when needed
3. **Power Automate Integration**: Trigger flows from within orchestrations
4. **Multi-Agent Collaboration**: Coordinate multiple agents to complete complex tasks

## License

This project is licensed under the MIT License - see the LICENSE file for details.