# Copilot Studio Extensibility - Python Implementation

This Python implementation provides the same Copilot Studio extensibility features as the .NET version, built on Azure Durable Task SDKs.

## Features

- **Copilot Studio Integration**: Direct integration with Copilot Studio agents using REST APIs
- **Power Platform Connectivity**: Support for Power Automate flows and Dataverse connections
- **Hybrid Agent Orchestration**: Route conversations between Copilot Studio and Azure AI agents
- **Topic Management**: Handle Copilot Studio topics and conversation flows
- **PAC CLI Integration**: Use Power Platform CLI for agent management

## Prerequisites

- Python 3.8 or later
- Azure subscription with Durable Task Scheduler
- Power Platform environment with Copilot Studio access
- Power Platform CLI (PAC CLI) installed

## Getting Started

1. Install dependencies:
```bash
pip install -r requirements.txt
```

2. Configure environment variables:
```bash
export AZURE_TENANT_ID="your-tenant-id"
export AZURE_CLIENT_ID="your-client-id"  
export AZURE_CLIENT_SECRET="your-client-secret"
export POWER_PLATFORM_ENVIRONMENT_URL="https://yourenv.crm.dynamics.com"
export COPILOT_STUDIO_BOT_ID="your-copilot-studio-bot-id"
export TASKHUB="copilot-extensibility"
export ENDPOINT="your-dts-endpoint"
```

3. Run the worker:
```bash
python worker.py
```

4. In another terminal, run the client:
```bash
python client.py
```

## Usage Examples

### Start a Hybrid Agent Conversation
```python
from models import ConversationRequest, AgentRoutingPreference

request = ConversationRequest(
    user_id="user123",
    message="Help me with a complex business process",
    routing_preference=AgentRoutingPreference.AUTO
)

# The orchestrator will automatically route to the best agent
result = await client.start_conversation(request)
```

### Multi-Agent Collaboration
```python
from models import MultiAgentRequest, AgentCapability, AgentType

request = MultiAgentRequest(
    task_description="Create a comprehensive business plan",
    required_capabilities=[
        AgentCapability(
            name="analysis",
            description="Market analysis and research", 
            supported_by=[AgentType.AZURE_AI]
        ),
        AgentCapability(
            name="workflow",
            description="Business process design",
            supported_by=[AgentType.COPILOT_STUDIO]
        )
    ],
    user_id="user123"
)

result = await client.start_multi_agent_collaboration(request)
```

### Topic Management
```python
from models import TopicManagementRequest, TopicAction

request = TopicManagementRequest(
    bot_id="your-bot-id",
    topic_id="customer-onboarding",
    action=TopicAction.START
)

result = await client.manage_topic(request)
```