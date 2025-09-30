# Copilot Studio Extensibility with Azure Durable Functions

This comprehensive sample demonstrates how to build a robust, extensible orchestration framework that seamlessly integrates **Copilot Studio agents** from the Power Platform with **Azure AI Foundry agents** using Azure Durable Functions as the orchestration engine.

## 🚀 Key Features

### **Power Platform Integration**
- **Copilot Studio Connectivity**: Direct REST API integration with Copilot Studio bots
- **Power Automate Integration**: Trigger and monitor Power Automate flows from orchestrations
- **PAC CLI Support**: Automated bot management and deployment using Power Platform CLI
- **Microsoft Graph Integration**: Enhanced connectivity through Microsoft Graph APIs

### **Intelligent Agent Orchestration**
- **Hybrid Routing**: Automatically route conversations between Copilot Studio and Azure AI agents
- **Multi-Agent Collaboration**: Coordinate multiple agents to complete complex tasks
- **Topic Management**: Handle Copilot Studio topics with escalation capabilities
- **Context Preservation**: Maintain conversation context across agent handoffs

### **Enterprise-Grade Reliability**
- **Durable Orchestrations**: Fault-tolerant execution with automatic retries
- **State Persistence**: Maintain state across failures and interruptions
- **Monitoring**: Built-in monitoring and status tracking
- **Scalability**: Horizontal scaling through Azure infrastructure

## 🏗️ Architecture

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

## 📁 Project Structure

```
samples/copilot-studio-extensibility/
├── dotnet/                          # .NET Implementation
│   ├── Models/                      # Data models and contracts
│   ├── Services/                    # Core services (Graph, PAC CLI, Routing)
│   ├── Functions/                   # Orchestrators and activities
│   ├── CopilotStudioExtensibility.csproj
│   ├── Program.cs                   # Dependency injection setup
│   ├── host.json                    # Function app configuration
│   └── local.settings.json          # Local development settings
├── python/                          # Python Implementation
│   ├── models.py                    # Data models and contracts
│   ├── services.py                  # Core services
│   ├── worker.py                    # Orchestrators and activities
│   ├── client.py                    # Test client
│   ├── requirements.txt             # Python dependencies
│   └── README.md                    # Python-specific documentation
└── README.md                        # This file
```

## 🔧 Prerequisites

### **Azure Resources**
- Azure subscription with Durable Task Scheduler deployed
- Azure AI Foundry workspace (optional, for AI agent integration)
- Application Insights (optional, for monitoring)

### **Power Platform**
- Power Platform environment with Copilot Studio enabled
- Power Automate premium license (for flow integration)
- Service principal with appropriate permissions

### **Development Environment**
- .NET 8.0 SDK (for .NET implementation)
- Python 3.8+ (for Python implementation)
- Power Platform CLI (PAC CLI)
- Azure CLI
- Visual Studio Code or Visual Studio (recommended)

## ⚙️ Configuration

### **Environment Variables**

Set the following environment variables for both .NET and Python implementations:

```bash
# Azure Configuration
AZURE_TENANT_ID="your-tenant-id"
AZURE_CLIENT_ID="your-client-id"
AZURE_CLIENT_SECRET="your-client-secret"

# Power Platform Configuration
POWER_PLATFORM_ENVIRONMENT_URL="https://yourenv.crm.dynamics.com"
COPILOT_STUDIO_BOT_ID="your-copilot-studio-bot-id"

# Azure AI Configuration (Optional)
AZURE_AI_ENDPOINT="https://your-ai-endpoint.cognitiveservices.azure.com"
AZURE_AI_KEY="your-ai-key"
AZURE_AI_AGENT_ID="your-ai-agent-id"

# Durable Task Scheduler
TASKHUB="copilot-extensibility"
ENDPOINT="your-dts-endpoint"
```

### **Required Permissions**

Your service principal needs the following permissions:

#### **Microsoft Graph API**
- `User.Read`
- `Application.ReadWrite.All` (for Power Platform integration)

#### **Power Platform**
- `Dataverse API` access
- `Power Apps Service` access
- Environment Maker role in target environment

#### **Azure AI Services**
- `Cognitive Services User` role
- Access to Azure AI Foundry workspace

## 🚀 Getting Started

### **.NET Implementation**

1. **Build the project:**
   ```bash
   cd samples/copilot-studio-extensibility/dotnet
   dotnet build
   ```

2. **Configure settings:**
   ```bash
   # Copy and edit local.settings.json
   cp local.settings.json.template local.settings.json
   # Edit with your configuration values
   ```

3. **Run locally:**
   ```bash
   func start
   ```

4. **Test the APIs:**
   ```bash
   # Start a conversation
   curl -X POST http://localhost:7071/api/conversation/start \
        -H "Content-Type: application/json" \
        -d '{"userId":"test","message":"Help me with a business process"}'
   ```

### **Python Implementation**

1. **Install dependencies:**
   ```bash
   cd samples/copilot-studio-extensibility/python
   pip install -r requirements.txt
   ```

2. **Configure environment:**
   ```bash
   # Set environment variables or use .env file
   export AZURE_TENANT_ID="your-tenant-id"
   export POWER_PLATFORM_ENVIRONMENT_URL="https://yourenv.crm.dynamics.com"
   # ... other variables
   ```

3. **Start the worker:**
   ```bash
   python worker.py
   ```

4. **Run the client demos:**
   ```bash
   python client.py
   ```

## 📋 Usage Examples

### **1. Simple Conversation Routing**

Route a user message to the most appropriate agent:

**.NET (HTTP API)**
```bash
POST /api/conversation/start
{
  "userId": "user123",
  "message": "Help me automate customer onboarding",
  "routingPreference": "Auto"
}
```

**Python (Direct Call)**
```python
request = ConversationRequest(
    user_id="user123",
    message="Help me automate customer onboarding",
    routing_preference=AgentRoutingPreference.AUTO
)
instance_id = await client.start_conversation(request)
```

### **2. Multi-Agent Collaboration**

Coordinate multiple agents for complex tasks:

```python
request = MultiAgentRequest(
    task_description="Create a customer retention strategy",
    required_capabilities=[
        AgentCapability(
            name="data_analysis",
            description="Analyze customer churn patterns",
            supported_by=[AgentType.AZURE_AI]
        ),
        AgentCapability(
            name="process_design", 
            description="Design engagement workflows",
            supported_by=[AgentType.COPILOT_STUDIO]
        )
    ],
    user_id="user123"
)
```

### **3. Topic Management**

Manage Copilot Studio topics with escalation:

```python
request = TopicManagementRequest(
    bot_id="your-bot-id",
    topic_id="customer-support",
    action=TopicAction.START,
    parameters={"priority": "high"}
)
```

### **4. Power Automate Integration**

Trigger flows from orchestrations:

```python
flow_request = PowerAutomateFlowRequest(
    flow_id="your-flow-id",
    trigger_name="manual",
    input_data={"customer_id": "123", "priority": "high"}
)
```

## 🔄 Orchestration Patterns

### **1. Hybrid Agent Conversation**
- **Purpose**: Route single conversations to optimal agents
- **Use Case**: Customer support, information requests
- **Flow**: Request → Routing Decision → Agent Execution → Response

### **2. Multi-Agent Collaboration**
- **Purpose**: Coordinate multiple agents for complex tasks
- **Use Case**: Business analysis, comprehensive planning
- **Flow**: Task Analysis → Agent Planning → Sequential Execution → Aggregated Response

### **3. Topic-Based Management**
- **Purpose**: Handle structured Copilot Studio conversations
- **Use Case**: Guided workflows, form-based processes
- **Flow**: Topic Start → Conversation Flow → Escalation (if needed)

### **4. Escalation Handling**
- **Purpose**: Seamlessly hand off from low-code to AI agents
- **Use Case**: Complex technical queries, custom solutions
- **Flow**: Copilot Studio → Capability Check → Azure AI Escalation

## 🔍 Monitoring and Debugging

### **Orchestration Status**
```bash
GET /api/conversation/status/{instanceId}
```

### **Custom Status Updates**
The orchestrators provide real-time status updates:
- Step progress tracking
- Agent selection reasoning
- Error handling with context

### **Logging**
- Structured logging with correlation IDs
- Performance metrics
- Error tracking with stack traces

## 🧪 Testing

### **Unit Tests**
Run individual component tests:
```bash
# .NET
dotnet test

# Python  
pytest tests/
```

### **Integration Tests**
Test with actual services:
```bash
# Set test environment variables
export TEST_MODE=integration
python client.py
```

### **Load Testing**
Test orchestration performance:
```bash
# Multiple concurrent conversations
python load_test.py --concurrent=10 --duration=60
```

## 🚀 Deployment

### **Azure Functions (.NET)**
```bash
# Publish to Azure
func azure functionapp publish your-function-app
```

### **Container Deployment (Python)**
```bash
# Build container
docker build -t copilot-extensibility .

# Deploy to Azure Container Apps
az containerapp create \
  --name copilot-extensibility \
  --resource-group your-rg \
  --image copilot-extensibility
```

### **Azure DevOps Pipeline**
Use the included `azure-pipelines.yml` for CI/CD deployment.

## 🔒 Security Considerations

### **Authentication**
- Service principal authentication for production
- Managed Identity support for Azure-hosted scenarios
- Token caching and refresh handling

### **Authorization**
- Role-based access control (RBAC)
- Environment-specific permissions
- API key management

### **Data Protection**
- Encryption in transit and at rest
- PII data handling compliance
- Audit logging for sensitive operations

## 🎯 Best Practices

### **Performance**
- Async/await patterns throughout
- Connection pooling for HTTP clients
- Efficient state serialization

### **Reliability**
- Retry policies with exponential backoff
- Circuit breaker patterns
- Graceful degradation

### **Maintainability**
- Dependency injection for testability
- Configuration externalization
- Comprehensive error handling

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.

## 🆘 Support

- **Documentation**: [Azure Durable Functions Documentation](https://docs.microsoft.com/azure/azure-functions/durable/)
- **Power Platform**: [Power Platform Documentation](https://docs.microsoft.com/power-platform/)
- **Issues**: [GitHub Issues](https://github.com/Azure/Durable-Copilot-Studio/issues)
- **Community**: [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-durable-functions)

---

**Note**: This is a comprehensive sample demonstrating integration patterns. Adapt the code for your specific use cases and security requirements.