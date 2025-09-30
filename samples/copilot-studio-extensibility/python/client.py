"""
Client for testing Copilot Studio extensibility orchestrations
"""

import asyncio
import json
import logging
import os
from azure.identity import DefaultAzureCredential, ManagedIdentityCredential
from durabletask.azuremanaged.client import DurableTaskSchedulerClient
from models import *

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class CopilotStudioClient:
    """Client for interacting with Copilot Studio extensibility orchestrations"""
    
    def __init__(self):
        # Get environment variables
        self.taskhub_name = os.getenv("TASKHUB", "copilot-extensibility")
        self.endpoint = os.getenv("ENDPOINT", "http://localhost:8080")
        
        # Credential handling
        credential = None
        if self.endpoint != "http://localhost:8080":
            try:
                client_id = os.getenv("AZURE_MANAGED_IDENTITY_CLIENT_ID")
                if client_id:
                    logger.info(f"Using Managed Identity with client ID: {client_id}")
                    credential = ManagedIdentityCredential(client_id=client_id)
                else:
                    logger.info("Using DefaultAzureCredential")
                    credential = DefaultAzureCredential()
            except Exception as e:
                logger.error(f"Authentication error: {e}")
                credential = None
        
        self.client = DurableTaskSchedulerClient(
            host_address=self.endpoint,
            secure_channel=self.endpoint != "http://localhost:8080",
            taskhub=self.taskhub_name,
            token_credential=credential
        )
        
        logger.info(f"Initialized client for taskhub: {self.taskhub_name}, endpoint: {self.endpoint}")
    
    async def start_conversation(self, request: ConversationRequest) -> str:
        """Start a hybrid agent conversation"""
        logger.info(f"Starting conversation for user {request.user_id}")
        
        instance_id = await self.client.schedule_new_orchestration_instance(
            orchestration_name="hybrid_agent_conversation_orchestrator",
            input=json.dumps(to_dict(request))
        )
        
        logger.info(f"Started conversation with instance ID: {instance_id}")
        return instance_id
    
    async def start_multi_agent_collaboration(self, request: MultiAgentRequest) -> str:
        """Start a multi-agent collaboration"""
        logger.info(f"Starting multi-agent collaboration for task: {request.task_description}")
        
        instance_id = await self.client.schedule_new_orchestration_instance(
            orchestration_name="multi_agent_collaboration_orchestrator",
            input=json.dumps(to_dict(request))
        )
        
        logger.info(f"Started collaboration with instance ID: {instance_id}")
        return instance_id
    
    async def manage_topic(self, request: TopicManagementRequest) -> str:
        """Manage a Copilot Studio topic"""
        logger.info(f"Managing topic {request.topic_id} with action {request.action.value}")
        
        instance_id = await self.client.schedule_new_orchestration_instance(
            orchestration_name="topic_based_conversation_orchestrator",
            input=json.dumps(to_dict(request))
        )
        
        logger.info(f"Started topic management with instance ID: {instance_id}")
        return instance_id
    
    async def get_orchestration_status(self, instance_id: str):
        """Get the status of an orchestration"""
        status = await self.client.get_orchestration_state(instance_id)
        return status
    
    async def wait_for_completion(self, instance_id: str, timeout_seconds: int = 30):
        """Wait for an orchestration to complete"""
        logger.info(f"Waiting for orchestration {instance_id} to complete...")
        
        for _ in range(timeout_seconds):
            status = await self.get_orchestration_status(instance_id)
            
            if status and hasattr(status, 'orchestration_status'):
                if status.orchestration_status.name in ['COMPLETED', 'FAILED', 'TERMINATED']:
                    logger.info(f"Orchestration {instance_id} completed with status: {status.orchestration_status.name}")
                    return status
            
            await asyncio.sleep(1)
        
        logger.warning(f"Orchestration {instance_id} did not complete within {timeout_seconds} seconds")
        return await self.get_orchestration_status(instance_id)

async def demo_conversation():
    """Demo: Simple conversation routing"""
    client = CopilotStudioClient()
    
    print("\n=== Demo: Hybrid Agent Conversation ===")
    
    request = ConversationRequest(
        user_id="demo-user",
        message="Help me create a business process for customer onboarding",
        routing_preference=AgentRoutingPreference.AUTO
    )
    
    instance_id = await client.start_conversation(request)
    print(f"Started conversation: {instance_id}")
    
    # Wait for completion
    final_status = await client.wait_for_completion(instance_id)
    
    if final_status and hasattr(final_status, 'serialized_output'):
        print(f"Conversation result: {final_status.serialized_output}")
    else:
        print("No output available")

async def demo_multi_agent_collaboration():
    """Demo: Multi-agent collaboration"""
    client = CopilotStudioClient()
    
    print("\n=== Demo: Multi-Agent Collaboration ===")
    
    request = MultiAgentRequest(
        task_description="Create a comprehensive customer retention strategy",
        required_capabilities=[
            AgentCapability(
                name="data_analysis",
                description="Analyze customer data and identify churn patterns",
                supported_by=[AgentType.AZURE_AI]
            ),
            AgentCapability(
                name="process_design",
                description="Design customer engagement workflows",
                supported_by=[AgentType.COPILOT_STUDIO]
            ),
            AgentCapability(
                name="automation",
                description="Create automated follow-up processes",
                supported_by=[AgentType.POWER_AUTOMATE]
            )
        ],
        user_id="demo-user"
    )
    
    instance_id = await client.start_multi_agent_collaboration(request)
    print(f"Started collaboration: {instance_id}")
    
    # Wait for completion
    final_status = await client.wait_for_completion(instance_id, timeout_seconds=60)
    
    if final_status and hasattr(final_status, 'serialized_output'):
        print(f"Collaboration result: {final_status.serialized_output}")
    else:
        print("No output available")

async def demo_topic_management():
    """Demo: Topic management"""
    client = CopilotStudioClient()
    
    print("\n=== Demo: Topic Management ===")
    
    bot_id = os.getenv("COPILOT_STUDIO_BOT_ID", "demo-bot")
    
    request = TopicManagementRequest(
        bot_id=bot_id,
        topic_id="customer-support",
        action=TopicAction.START,
        parameters={"priority": "high", "category": "technical"}
    )
    
    instance_id = await client.manage_topic(request)
    print(f"Started topic management: {instance_id}")
    
    # Wait for completion
    final_status = await client.wait_for_completion(instance_id)
    
    if final_status and hasattr(final_status, 'serialized_output'):
        print(f"Topic management result: {final_status.serialized_output}")
    else:
        print("No output available")

async def demo_escalation_scenario():
    """Demo: Escalation from Copilot Studio to Azure AI"""
    client = CopilotStudioClient()
    
    print("\n=== Demo: Escalation Scenario ===")
    
    request = ConversationRequest(
        user_id="demo-user",
        message="I need help with a complex technical integration that requires custom coding and architecture design",
        routing_preference=AgentRoutingPreference.PREFER_COPILOT_STUDIO  # Start with Copilot Studio but allow escalation
    )
    
    instance_id = await client.start_conversation(request)
    print(f"Started escalation scenario: {instance_id}")
    
    # Wait for completion
    final_status = await client.wait_for_completion(instance_id)
    
    if final_status and hasattr(final_status, 'serialized_output'):
        print(f"Escalation result: {final_status.serialized_output}")
    else:
        print("No output available")

async def interactive_demo():
    """Interactive demo where user can input messages"""
    client = CopilotStudioClient()
    
    print("\n=== Interactive Demo ===")
    print("Enter messages to send to the hybrid agent system.")
    print("Type 'quit' to exit, 'collab' for collaboration demo, 'topic' for topic demo")
    
    while True:
        user_input = input("\nYour message: ").strip()
        
        if user_input.lower() in ['quit', 'exit', 'q']:
            break
        elif user_input.lower() == 'collab':
            await demo_multi_agent_collaboration()
            continue
        elif user_input.lower() == 'topic':
            await demo_topic_management()
            continue
        elif not user_input:
            continue
        
        request = ConversationRequest(
            user_id="interactive-user",
            message=user_input,
            routing_preference=AgentRoutingPreference.AUTO
        )
        
        instance_id = await client.start_conversation(request)
        print(f"Processing... (Instance: {instance_id})")
        
        # Wait for completion
        final_status = await client.wait_for_completion(instance_id)
        
        if final_status and hasattr(final_status, 'serialized_output'):
            try:
                output_data = json.loads(final_status.serialized_output)
                print(f"\nAgent Response:")
                print(f"Type: {output_data.get('agent_type', 'Unknown')}")
                print(f"Message: {output_data.get('message', 'No message')}")
                if output_data.get('requires_follow_up'):
                    print(f"Follow-up needed: {output_data.get('next_action', 'None specified')}")
            except json.JSONDecodeError:
                print(f"Response: {final_status.serialized_output}")
        else:
            print("No response available")

async def main():
    """Main function to run demos"""
    print("Copilot Studio Extensibility Demo Client")
    print("=" * 50)
    
    try:
        # Run predefined demos
        await demo_conversation()
        await demo_multi_agent_collaboration()
        await demo_topic_management()
        await demo_escalation_scenario()
        
        # Interactive demo
        await interactive_demo()
    
    except Exception as ex:
        logger.error(f"Error in demo: {ex}")
        print(f"Demo error: {ex}")

if __name__ == "__main__":
    asyncio.run(main())