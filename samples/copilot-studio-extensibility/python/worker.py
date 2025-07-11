"""
Worker for Copilot Studio extensibility with Azure Durable Task SDKs
"""

import asyncio
import logging
import os
import json
from datetime import datetime
from azure.identity import DefaultAzureCredential, ManagedIdentityCredential
from durabletask.azuremanaged.worker import DurableTaskSchedulerWorker
from models import *
from services import PowerPlatformGraphService, PacCliService, AgentRoutingService

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize services
power_platform_service = PowerPlatformGraphService()
pac_service = PacCliService()
routing_service = AgentRoutingService(power_platform_service)

# Activity functions
def determine_agent_routing(ctx, request_json: str) -> str:
    """Determine which agent should handle a conversation request"""
    logger.info("Determining agent routing")
    
    try:
        request = from_dict(ConversationRequest, json.loads(request_json))
        
        # Run the async function in a new event loop
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        try:
            decision = loop.run_until_complete(routing_service.determine_routing(request))
            return json.dumps(to_dict(decision))
        finally:
            loop.close()
    
    except Exception as ex:
        logger.error(f"Error determining agent routing: {ex}")
        decision = AgentRoutingDecision(
            selected_agent=AgentType.COPILOT_STUDIO,
            reason="Error in routing, defaulting to Copilot Studio",
            confidence=0.5
        )
        return json.dumps(to_dict(decision))

def execute_agent_request(ctx, input_json: str) -> str:
    """Execute a request with the specified agent"""
    logger.info("Executing agent request")
    
    try:
        input_data = json.loads(input_json)
        request = from_dict(ConversationRequest, input_data["request"])
        
        # Run the async function in a new event loop
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        try:
            response = loop.run_until_complete(routing_service.route_and_execute(request))
            return json.dumps(to_dict(response))
        finally:
            loop.close()
    
    except Exception as ex:
        logger.error(f"Error executing agent request: {ex}")
        response = AgentResponse(
            message="I apologize, but I encountered an error processing your request. Please try again.",
            agent_type="Error",
            agent_id="system",
            context={"error": str(ex)}
        )
        return json.dumps(to_dict(response))

def plan_agent_collaboration(ctx, request_json: str) -> str:
    """Plan multi-agent collaboration steps"""
    logger.info("Planning agent collaboration")
    
    try:
        request = from_dict(MultiAgentRequest, json.loads(request_json))
        
        steps = []
        
        # Analyze required capabilities and create collaboration plan
        for capability in request.required_capabilities:
            selected_agent = _select_best_agent_for_capability(capability)
            
            step = AgentCollaborationStep(
                agent_type=selected_agent,
                description=f"Handle {capability.name}: {capability.description}",
                prompt=_create_prompt_for_capability(capability, request.task_description),
                input_context=request.context
            )
            
            steps.append(step)
        
        # If no specific capabilities were provided, create a general plan
        if not steps:
            steps = _create_general_collaboration_plan(request)
        
        logger.info(f"Created collaboration plan with {len(steps)} steps")
        return json.dumps([to_dict(step) for step in steps])
    
    except Exception as ex:
        logger.error(f"Error planning agent collaboration: {ex}")
        
        # Return a default plan with error handling
        default_step = AgentCollaborationStep(
            agent_type=AgentType.COPILOT_STUDIO,
            description="Handle request with error recovery",
            prompt=f"Process this request with error handling: {json.loads(request_json).get('task_description', 'Unknown task')}"
        )
        return json.dumps([to_dict(default_step)])

def manage_topic(ctx, request_json: str) -> str:
    """Manage Copilot Studio topics"""
    logger.info("Managing Copilot Studio topic")
    
    try:
        request = from_dict(TopicManagementRequest, json.loads(request_json))
        
        # Run the async function in a new event loop
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        try:
            response = loop.run_until_complete(_manage_topic_async(request))
            return json.dumps(to_dict(response))
        finally:
            loop.close()
    
    except Exception as ex:
        logger.error(f"Error managing topic: {ex}")
        response = AgentResponse(
            message=f"Error managing topic: {ex}",
            agent_type="Error",
            agent_id="topic-manager",
            context={"error": str(ex)}
        )
        return json.dumps(to_dict(response))

def trigger_power_automate_flow(ctx, request_json: str) -> str:
    """Trigger a Power Automate flow"""
    logger.info("Triggering Power Automate flow")
    
    try:
        request = from_dict(PowerAutomateFlowRequest, json.loads(request_json))
        
        # Run the async function in a new event loop
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        try:
            response = loop.run_until_complete(power_platform_service.trigger_power_automate_flow(request))
            return json.dumps(to_dict(response))
        finally:
            loop.close()
    
    except Exception as ex:
        logger.error(f"Error triggering Power Automate flow: {ex}")
        response = PowerAutomateFlowResponse(
            flow_id=json.loads(request_json).get("flow_id", ""),
            run_id="",
            status="Error",
            error_message=str(ex)
        )
        return json.dumps(to_dict(response))

def get_environment_info(ctx, input_data: str) -> str:
    """Get Power Platform environment information"""
    logger.info("Getting Power Platform environment information")
    
    try:
        # Run the async function in a new event loop
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        try:
            environment_info = loop.run_until_complete(power_platform_service.get_environment_info())
            
            # Enhance with PAC CLI information
            pac_info = loop.run_until_complete(pac_service.get_environment_info())
            if pac_info:
                environment_info["pacCliInfo"] = pac_info
            
            return json.dumps(environment_info)
        finally:
            loop.close()
    
    except Exception as ex:
        logger.error(f"Error getting environment information: {ex}")
        return json.dumps({"error": str(ex)})

def list_copilot_studio_bots(ctx, input_data: str) -> str:
    """List available Copilot Studio bots"""
    logger.info("Listing Copilot Studio bots")
    
    try:
        # Run the async function in a new event loop
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        try:
            bots = loop.run_until_complete(pac_service.list_copilot_studio_bots())
            logger.info(f"Found {len(bots)} Copilot Studio bots")
            return json.dumps(bots)
        finally:
            loop.close()
    
    except Exception as ex:
        logger.error(f"Error listing Copilot Studio bots: {ex}")
        return json.dumps([])

# Orchestrator functions
def hybrid_agent_conversation_orchestrator(ctx, request_json: str) -> str:
    """Orchestrate hybrid agent conversations"""
    logger.info("Starting hybrid agent conversation orchestration")
    
    request = from_dict(ConversationRequest, json.loads(request_json))
    logger.info(f"Starting hybrid agent conversation for user {request.user_id}")
    
    try:
        # Step 1: Determine routing
        logger.info("Step 1: Determining agent routing")
        routing_decision_json = yield ctx.call_activity('determine_agent_routing', input=json.dumps(to_dict(request)))
        routing_decision = from_dict(AgentRoutingDecision, json.loads(routing_decision_json))
        
        logger.info(f"Routing decision: {routing_decision.selected_agent.value} (confidence: {routing_decision.confidence})")
        
        # Step 2: Execute with chosen agent
        input_data = {
            "request": to_dict(request),
            "routing": to_dict(routing_decision)
        }
        response_json = yield ctx.call_activity('execute_agent_request', input=json.dumps(input_data))
        response = from_dict(AgentResponse, json.loads(response_json))
        
        # Step 3: Check if follow-up or escalation is needed
        if response.requires_follow_up and routing_decision.selected_agent == AgentType.COPILOT_STUDIO:
            logger.info("Step 3: Handling follow-up or escalation")
            
            if response.next_action == "escalate":
                # Escalate to Azure AI
                escalation_request = ConversationRequest(
                    user_id=request.user_id,
                    message=f"Continue conversation: {response.message}. Original request: {request.message}",
                    conversation_id=response.conversation_id,
                    context=response.context,
                    routing_preference=AgentRoutingPreference.AZURE_AI_ONLY
                )
                
                escalation_input = {
                    "request": to_dict(escalation_request),
                    "routing": to_dict(AgentRoutingDecision(AgentType.AZURE_AI, "Follow-up escalation", 1.0))
                }
                
                escalation_response_json = yield ctx.call_activity('execute_agent_request', input=json.dumps(escalation_input))
                escalation_response = from_dict(AgentResponse, json.loads(escalation_response_json))
                
                # Combine responses
                response = AgentResponse(
                    message=f"{response.message}\n\n[Escalated to Azure AI]\n{escalation_response.message}",
                    agent_type="Hybrid",
                    agent_id="hybrid-routing",
                    conversation_id=response.conversation_id,
                    context={**(response.context or {}), **(escalation_response.context or {})},
                    requires_follow_up=escalation_response.requires_follow_up
                )
        
        logger.info(f"Hybrid agent conversation completed for user {request.user_id}")
        return json.dumps(to_dict(response))
    
    except Exception as ex:
        logger.error(f"Error in hybrid agent conversation for user {request.user_id}: {ex}")
        
        # Return error response
        error_response = AgentResponse(
            message="I apologize, but I encountered an error processing your request. Please try again.",
            agent_type="Error",
            agent_id="system",
            context={"error": str(ex)}
        )
        return json.dumps(to_dict(error_response))

def multi_agent_collaboration_orchestrator(ctx, request_json: str) -> str:
    """Orchestrate multi-agent collaboration scenarios"""
    logger.info("Starting multi-agent collaboration orchestration")
    
    request = from_dict(MultiAgentRequest, json.loads(request_json))
    logger.info(f"Starting multi-agent collaboration for task: {request.task_description}")
    
    responses = []
    
    try:
        # Step 1: Plan agent collaboration
        collaboration_plan_json = yield ctx.call_activity('plan_agent_collaboration', input=json.dumps(to_dict(request)))
        collaboration_plan = [from_dict(AgentCollaborationStep, step_data) for step_data in json.loads(collaboration_plan_json)]
        
        logger.info(f"Collaboration plan created with {len(collaboration_plan)} steps")
        
        # Step 2: Execute each step
        for i, step in enumerate(collaboration_plan):
            logger.info(f"Executing step {i + 1}: {step.description} with {step.agent_type.value}")
            
            step_request = ConversationRequest(
                user_id=request.user_id,
                message=step.prompt,
                context=step.input_context,
                routing_preference=_get_routing_preference(step.agent_type)
            )
            
            step_input = {
                "request": to_dict(step_request),
                "routing": to_dict(AgentRoutingDecision(step.agent_type, step.description, 1.0))
            }
            
            step_response_json = yield ctx.call_activity('execute_agent_request', input=json.dumps(step_input))
            step_response = from_dict(AgentResponse, json.loads(step_response_json))
            
            responses.append(step_response)
            
            # Pass context to next step
            if i < len(collaboration_plan) - 1:
                collaboration_plan[i + 1].input_context = {
                    **(collaboration_plan[i + 1].input_context or {}),
                    **(step_response.context or {})
                }
        
        logger.info(f"Multi-agent collaboration completed with {len(responses)} responses")
        return json.dumps([to_dict(response) for response in responses])
    
    except Exception as ex:
        logger.error(f"Error in multi-agent collaboration: {ex}")
        
        error_response = AgentResponse(
            message="I encountered an error during the multi-agent collaboration. Please try again.",
            agent_type="Error",
            agent_id="system",
            context={"error": str(ex)}
        )
        responses.append(error_response)
        
        return json.dumps([to_dict(response) for response in responses])

def topic_based_conversation_orchestrator(ctx, request_json: str) -> str:
    """Orchestrate topic-based conversations with Copilot Studio"""
    logger.info("Starting topic-based conversation orchestration")
    
    request = from_dict(TopicManagementRequest, json.loads(request_json))
    logger.info(f"Starting topic-based conversation for topic {request.topic_id}")
    
    try:
        # Execute topic action
        response_json = yield ctx.call_activity('manage_topic', input=json.dumps(to_dict(request)))
        response = from_dict(AgentResponse, json.loads(response_json))
        
        # Check if escalation is needed
        if response.next_action == "escalate":
            logger.info(f"Topic escalation requested for {request.topic_id}")
            
            escalation_request = ConversationRequest(
                user_id="system",
                message=f"Handle escalation from Copilot Studio topic {request.topic_id}: {response.message}",
                context=response.context,
                routing_preference=AgentRoutingPreference.AZURE_AI_ONLY
            )
            
            escalation_input = {
                "request": to_dict(escalation_request),
                "routing": to_dict(AgentRoutingDecision(AgentType.AZURE_AI, "Topic escalation", 1.0))
            }
            
            escalation_response_json = yield ctx.call_activity('execute_agent_request', input=json.dumps(escalation_input))
            escalation_response = from_dict(AgentResponse, json.loads(escalation_response_json))
            
            response = AgentResponse(
                message=f"{response.message}\n\n[Escalated Response]\n{escalation_response.message}",
                agent_type="Hybrid",
                agent_id="topic-escalation",
                conversation_id=response.conversation_id,
                context={**(response.context or {}), **(escalation_response.context or {})}
            )
        
        return json.dumps(to_dict(response))
    
    except Exception as ex:
        logger.error(f"Error in topic-based conversation for {request.topic_id}: {ex}")
        
        error_response = AgentResponse(
            message="I encountered an error managing the topic. Please try again.",
            agent_type="Error",
            agent_id="system",
            context={"error": str(ex), "topicId": request.topic_id}
        )
        return json.dumps(to_dict(error_response))

# Helper functions
def _select_best_agent_for_capability(capability: AgentCapability) -> AgentType:
    """Select the best agent for a given capability"""
    if (AgentType.COPILOT_STUDIO in capability.supported_by and 
        ("workflow" in capability.name or "process" in capability.name)):
        return AgentType.COPILOT_STUDIO
    
    if (AgentType.AZURE_AI in capability.supported_by and 
        ("analysis" in capability.name or "creative" in capability.name)):
        return AgentType.AZURE_AI
    
    # Default to first supported agent
    return capability.supported_by[0] if capability.supported_by else AgentType.COPILOT_STUDIO

def _create_prompt_for_capability(capability: AgentCapability, task_description: str) -> str:
    """Create a prompt for a specific capability"""
    return (f"Task: {task_description}\n\n"
            f"Your specific role: {capability.description}\n\n"
            f"Focus on the {capability.name} aspect of this task. "
            "Provide a clear, actionable response that can be used by other agents in this collaboration.")

def _create_general_collaboration_plan(request: MultiAgentRequest) -> List[AgentCollaborationStep]:
    """Create a general collaboration plan when no specific capabilities are provided"""
    task = request.task_description.lower()
    steps = []
    
    # Start with Copilot Studio for structured analysis
    steps.append(AgentCollaborationStep(
        agent_type=AgentType.COPILOT_STUDIO,
        description="Initial analysis and structure",
        prompt=f"Analyze this request and provide a structured breakdown: {request.task_description}"
    ))
    
    # Add Azure AI for complex reasoning if needed
    if any(keyword in task for keyword in ["complex", "analyze", "creative"]):
        steps.append(AgentCollaborationStep(
            agent_type=AgentType.AZURE_AI,
            description="Advanced analysis and recommendations",
            prompt=f"Provide detailed analysis and recommendations for: {request.task_description}"
        ))
    
    # Add Power Automate integration if automation is mentioned
    if any(keyword in task for keyword in ["automate", "flow", "trigger"]):
        steps.append(AgentCollaborationStep(
            agent_type=AgentType.POWER_AUTOMATE,
            description="Automation workflow",
            prompt=f"Design automation workflow for: {request.task_description}"
        ))
    
    return steps

def _get_routing_preference(agent_type: AgentType) -> AgentRoutingPreference:
    """Get routing preference for agent type"""
    if agent_type == AgentType.COPILOT_STUDIO:
        return AgentRoutingPreference.COPILOT_STUDIO_ONLY
    elif agent_type == AgentType.AZURE_AI:
        return AgentRoutingPreference.AZURE_AI_ONLY
    else:
        return AgentRoutingPreference.AUTO

async def _manage_topic_async(request: TopicManagementRequest) -> AgentResponse:
    """Manage Copilot Studio topics (async implementation)"""
    if request.action == TopicAction.START:
        return await _start_topic(request)
    elif request.action == TopicAction.CONTINUE:
        return await _continue_topic(request)
    elif request.action == TopicAction.RESET:
        return await _start_topic(request)  # Reset by starting fresh
    elif request.action == TopicAction.COMPLETE:
        return AgentResponse(
            message=f"Topic {request.topic_id} has been completed successfully.",
            agent_type="CopilotStudio",
            agent_id=request.bot_id,
            context=request.parameters,
            requires_follow_up=False
        )
    elif request.action == TopicAction.ESCALATE:
        return AgentResponse(
            message=f"Topic {request.topic_id} requires escalation to Azure AI for advanced assistance.",
            agent_type="CopilotStudio",
            agent_id=request.bot_id,
            context=request.parameters,
            requires_follow_up=True,
            next_action="escalate"
        )
    else:
        raise ValueError(f"Unknown topic action: {request.action}")

async def _start_topic(request: TopicManagementRequest) -> AgentResponse:
    """Start a Copilot Studio topic"""
    copilot_request = CopilotStudioRequest(
        bot_id=request.bot_id,
        user_id="system",
        message=f"Start topic: {request.topic_id}",
        topic=request.topic_id,
        variables=request.parameters
    )
    
    response = await power_platform_service.send_message_to_copilot_studio(copilot_request)
    
    return AgentResponse(
        message=response.message,
        agent_type="CopilotStudio",
        agent_id=request.bot_id,
        conversation_id=response.conversation_id,
        context=response.variables,
        requires_follow_up=not response.topic_completed,
        next_action=response.next_topic
    )

async def _continue_topic(request: TopicManagementRequest) -> AgentResponse:
    """Continue a Copilot Studio topic"""
    copilot_request = CopilotStudioRequest(
        bot_id=request.bot_id,
        user_id="system",
        message="Continue with current topic",
        topic=request.topic_id,
        variables=request.parameters
    )
    
    response = await power_platform_service.send_message_to_copilot_studio(copilot_request)
    
    return AgentResponse(
        message=response.message,
        agent_type="CopilotStudio",
        agent_id=request.bot_id,
        conversation_id=response.conversation_id,
        context=response.variables,
        requires_follow_up=not response.topic_completed,
        next_action=response.next_topic
    )

async def main():
    """Main entry point for the worker process"""
    logger.info("Starting Copilot Studio Extensibility worker...")
    
    # Get environment variables
    taskhub_name = os.getenv("TASKHUB", "copilot-extensibility")
    endpoint = os.getenv("ENDPOINT", "http://localhost:8080")
    
    print(f"Using taskhub: {taskhub_name}")
    print(f"Using endpoint: {endpoint}")
    
    # Credential handling
    credential = None
    if endpoint != "http://localhost:8080":
        try:
            client_id = os.getenv("AZURE_MANAGED_IDENTITY_CLIENT_ID")
            if client_id:
                logger.info(f"Using Managed Identity with client ID: {client_id}")
                credential = ManagedIdentityCredential(client_id=client_id)
                credential.get_token("https://management.azure.com/.default")
                logger.info("Successfully authenticated with Managed Identity")
            else:
                logger.info("No client ID found, falling back to DefaultAzureCredential")
                credential = DefaultAzureCredential()
        except Exception as e:
            logger.error(f"Authentication error: {e}")
            logger.warning("Continuing without authentication - this may only work with local emulator")
            credential = None
    
    with DurableTaskSchedulerWorker(
        host_address=endpoint,
        secure_channel=endpoint != "http://localhost:8080",
        taskhub=taskhub_name,
        token_credential=credential
    ) as worker:
        
        # Register activities
        worker.add_activity(determine_agent_routing)
        worker.add_activity(execute_agent_request)
        worker.add_activity(plan_agent_collaboration)
        worker.add_activity(manage_topic)
        worker.add_activity(trigger_power_automate_flow)
        worker.add_activity(get_environment_info)
        worker.add_activity(list_copilot_studio_bots)
        
        # Register orchestrators
        worker.add_orchestrator(hybrid_agent_conversation_orchestrator)
        worker.add_orchestrator(multi_agent_collaboration_orchestrator)
        worker.add_orchestrator(topic_based_conversation_orchestrator)
        
        # Start the worker
        worker.start()
        
        try:
            # Keep the worker running
            while True:
                await asyncio.sleep(1)
        except KeyboardInterrupt:
            logger.info("Worker shutdown initiated")
    
    logger.info("Worker stopped")

if __name__ == "__main__":
    # Add missing imports
    import os
    from datetime import datetime
    asyncio.run(main())