"""
Services for Power Platform and Copilot Studio integration
"""

import asyncio
import aiohttp
import json
import subprocess
import logging
from typing import Dict, List, Optional, Any
from azure.identity import DefaultAzureCredential
from models import *

logger = logging.getLogger(__name__)

class PowerPlatformGraphService:
    """Service for interacting with Power Platform via Microsoft Graph and REST APIs"""
    
    def __init__(self):
        self.environment_url = os.getenv("POWER_PLATFORM_ENVIRONMENT_URL")
        if not self.environment_url:
            raise ValueError("POWER_PLATFORM_ENVIRONMENT_URL environment variable is required")
        
        self.credential = DefaultAzureCredential()
        logger.info(f"Initialized PowerPlatformGraphService for environment: {self.environment_url}")
    
    async def send_message_to_copilot_studio(self, request: CopilotStudioRequest) -> CopilotStudioResponse:
        """Send a message to Copilot Studio bot"""
        logger.info(f"Sending message to Copilot Studio bot {request.bot_id}")
        
        try:
            endpoint = f"{self.environment_url}/api/botmanagement/v1/bots/{request.bot_id}/conversations"
            
            payload = {
                "message": request.message,
                "userId": request.user_id,
                "conversationId": request.conversation_id,
                "topic": request.topic,
                "variables": request.variables or {}
            }
            
            token = await self._get_power_platform_token()
            headers = {
                "Authorization": f"Bearer {token}",
                "Content-Type": "application/json"
            }
            
            async with aiohttp.ClientSession() as session:
                async with session.post(endpoint, json=payload, headers=headers) as response:
                    if response.status != 200:
                        error_text = await response.text()
                        logger.error(f"Failed to send message to Copilot Studio: {response.status} - {error_text}")
                        raise Exception(f"Copilot Studio API error: {response.status}")
                    
                    response_data = await response.json()
                    
                    return CopilotStudioResponse(
                        message=response_data.get("message", ""),
                        conversation_id=response_data.get("conversationId", request.conversation_id or ""),
                        topic=response_data.get("topic"),
                        variables=response_data.get("variables"),
                        topic_completed=response_data.get("topicCompleted", False),
                        next_topic=response_data.get("nextTopic")
                    )
        
        except Exception as ex:
            logger.error(f"Error sending message to Copilot Studio bot {request.bot_id}: {ex}")
            raise
    
    async def get_copilot_studio_topics(self, bot_id: str) -> List[TopicStatus]:
        """Get topics for a Copilot Studio bot"""
        logger.info(f"Retrieving topics for Copilot Studio bot {bot_id}")
        
        try:
            endpoint = f"{self.environment_url}/api/botmanagement/v1/bots/{bot_id}/topics"
            
            token = await self._get_power_platform_token()
            headers = {"Authorization": f"Bearer {token}"}
            
            async with aiohttp.ClientSession() as session:
                async with session.get(endpoint, headers=headers) as response:
                    if response.status != 200:
                        logger.error(f"Failed to retrieve topics: {response.status}")
                        return []
                    
                    response_data = await response.json()
                    topics = []
                    
                    for topic_data in response_data.get("topics", []):
                        topics.append(TopicStatus(
                            topic_id=topic_data.get("id", ""),
                            name=topic_data.get("name", ""),
                            status=topic_data.get("status", ""),
                            variables=topic_data.get("variables"),
                            last_user_input=topic_data.get("lastUserInput"),
                            last_updated=topic_data.get("lastUpdated")
                        ))
                    
                    return topics
        
        except Exception as ex:
            logger.error(f"Error retrieving topics for bot {bot_id}: {ex}")
            return []
    
    async def trigger_power_automate_flow(self, request: PowerAutomateFlowRequest) -> PowerAutomateFlowResponse:
        """Trigger a Power Automate flow"""
        logger.info(f"Triggering Power Automate flow {request.flow_id}")
        
        try:
            endpoint = f"{self.environment_url}/api/flows/{request.flow_id}/triggers/{request.trigger_name}/run"
            
            payload = {
                "inputs": request.input_data,
                "metadata": {
                    "triggeredBy": request.user_id or "system",
                    "triggeredAt": datetime.utcnow().isoformat()
                }
            }
            
            token = await self._get_power_platform_token()
            headers = {
                "Authorization": f"Bearer {token}",
                "Content-Type": "application/json"
            }
            
            async with aiohttp.ClientSession() as session:
                async with session.post(endpoint, json=payload, headers=headers) as response:
                    response_data = await response.json()
                    
                    if response.status != 200:
                        logger.error(f"Failed to trigger Power Automate flow: {response.status} - {response_data}")
                        return PowerAutomateFlowResponse(
                            flow_id=request.flow_id,
                            run_id="",
                            status="Failed",
                            error_message=f"HTTP {response.status}: {response_data}"
                        )
                    
                    return PowerAutomateFlowResponse(
                        flow_id=request.flow_id,
                        run_id=response_data.get("runId", ""),
                        status=response_data.get("status", "Unknown"),
                        output_data=response_data.get("outputs")
                    )
        
        except Exception as ex:
            logger.error(f"Error triggering Power Automate flow {request.flow_id}: {ex}")
            return PowerAutomateFlowResponse(
                flow_id=request.flow_id,
                run_id="",
                status="Error",
                error_message=str(ex)
            )
    
    async def get_environment_info(self) -> Dict[str, Any]:
        """Get Power Platform environment information"""
        logger.info("Retrieving Power Platform environment information")
        
        try:
            endpoint = f"{self.environment_url}/api/environments/current"
            
            token = await self._get_power_platform_token()
            headers = {"Authorization": f"Bearer {token}"}
            
            async with aiohttp.ClientSession() as session:
                async with session.get(endpoint, headers=headers) as response:
                    if response.status != 200:
                        logger.warning(f"Failed to retrieve environment info: {response.status}")
                        return {
                            "error": f"HTTP {response.status}",
                            "environmentUrl": self.environment_url
                        }
                    
                    return await response.json()
        
        except Exception as ex:
            logger.error(f"Error retrieving Power Platform environment information: {ex}")
            return {
                "error": str(ex),
                "environmentUrl": self.environment_url
            }
    
    async def _get_power_platform_token(self) -> str:
        """Get access token for Power Platform"""
        try:
            token = await self.credential.get_token("https://service.powerapps.com/.default")
            return token.token
        except Exception as ex:
            logger.error(f"Failed to obtain Power Platform access token: {ex}")
            raise


class PacCliService:
    """Service for executing Power Platform CLI (PAC CLI) commands"""
    
    def __init__(self):
        self.environment_url = os.getenv("POWER_PLATFORM_ENVIRONMENT_URL")
        if not self.environment_url:
            raise ValueError("POWER_PLATFORM_ENVIRONMENT_URL environment variable is required")
    
    async def is_authenticated(self) -> bool:
        """Check if PAC CLI is authenticated"""
        try:
            result = await self._execute_pac_command("auth list")
            return result["success"] and result["output"]
        except Exception as ex:
            logger.warning(f"Error checking PAC CLI authentication status: {ex}")
            return False
    
    async def authenticate(self, tenant_id: str) -> bool:
        """Authenticate with PAC CLI"""
        try:
            logger.info(f"Authenticating with PAC CLI for tenant {tenant_id}")
            
            result = await self._execute_pac_command(f"auth create --url {self.environment_url} --tenant {tenant_id}")
            
            if result["success"]:
                logger.info("Successfully authenticated with PAC CLI")
                return True
            else:
                logger.error(f"Failed to authenticate with PAC CLI: {result['error']}")
                return False
        
        except Exception as ex:
            logger.error(f"Error during PAC CLI authentication: {ex}")
            return False
    
    async def list_copilot_studio_bots(self) -> List[Dict[str, Any]]:
        """List Copilot Studio bots via PAC CLI"""
        try:
            logger.info("Listing Copilot Studio bots via PAC CLI")
            
            result = await self._execute_pac_command("chatbot list --json")
            
            if not result["success"]:
                logger.error(f"Failed to list Copilot Studio bots: {result['error']}")
                return []
            
            bots = self._parse_json_array_output(result["output"])
            logger.info(f"Found {len(bots)} Copilot Studio bots")
            
            return bots
        
        except Exception as ex:
            logger.error(f"Error listing Copilot Studio bots: {ex}")
            return []
    
    async def get_bot_details(self, bot_id: str) -> Dict[str, Any]:
        """Get details for a Copilot Studio bot"""
        try:
            logger.info(f"Getting details for Copilot Studio bot {bot_id}")
            
            result = await self._execute_pac_command(f"chatbot show --chatbot-id {bot_id} --json")
            
            if not result["success"]:
                logger.error(f"Failed to get bot details: {result['error']}")
                return {}
            
            return self._parse_json_object_output(result["output"])
        
        except Exception as ex:
            logger.error(f"Error getting bot details for {bot_id}: {ex}")
            return {}
    
    async def _execute_pac_command(self, command: str) -> Dict[str, Any]:
        """Execute a PAC CLI command"""
        try:
            process = await asyncio.create_subprocess_exec(
                "pac", *command.split(),
                stdout=asyncio.subprocess.PIPE,
                stderr=asyncio.subprocess.PIPE
            )
            
            stdout, stderr = await asyncio.wait_for(process.communicate(), timeout=30.0)
            
            output = stdout.decode().strip()
            error = stderr.decode().strip()
            success = process.returncode == 0
            
            logger.debug(f"PAC CLI command executed: pac {command} - Exit Code: {process.returncode}")
            
            return {
                "success": success,
                "output": output,
                "error": error
            }
        
        except asyncio.TimeoutError:
            logger.error(f"PAC CLI command timed out: pac {command}")
            return {
                "success": False,
                "output": "",
                "error": "Command timed out"
            }
        except Exception as ex:
            logger.error(f"Error executing PAC CLI command: pac {command} - {ex}")
            return {
                "success": False,
                "output": "",
                "error": str(ex)
            }
    
    def _parse_json_array_output(self, output: str) -> List[Dict[str, Any]]:
        """Parse JSON array output"""
        try:
            if not output.strip():
                return []
            
            return json.loads(output)
        
        except Exception as ex:
            logger.error(f"Error parsing JSON array output: {output} - {ex}")
            return []
    
    def _parse_json_object_output(self, output: str) -> Dict[str, Any]:
        """Parse JSON object output"""
        try:
            if not output.strip():
                return {}
            
            return json.loads(output)
        
        except Exception as ex:
            logger.error(f"Error parsing JSON object output: {output} - {ex}")
            return {}


class AgentRoutingService:
    """Service for routing conversations to the appropriate agent"""
    
    def __init__(self, power_platform_service: PowerPlatformGraphService):
        self.power_platform_service = power_platform_service
        
        # Initialize Azure AI client if configured
        self.azure_ai_endpoint = os.getenv("AZURE_AI_ENDPOINT")
        self.azure_ai_key = os.getenv("AZURE_AI_KEY")
        self.azure_ai_agent_id = os.getenv("AZURE_AI_AGENT_ID", "")
        
        if self.azure_ai_endpoint:
            logger.info("Azure AI endpoint configured for routing decisions")
    
    async def determine_routing(self, request: ConversationRequest) -> AgentRoutingDecision:
        """Determine which agent should handle the conversation"""
        logger.info(f"Determining routing for user {request.user_id} with message: {request.message}")
        
        try:
            # Handle explicit routing preferences
            if request.routing_preference != AgentRoutingPreference.AUTO:
                return self._handle_explicit_routing(request)
            
            # Use rule-based routing (AI-powered routing would require additional AI client setup)
            return self._determine_routing_with_rules(request)
        
        except Exception as ex:
            logger.error(f"Error determining routing for request: {ex}")
            
            # Default to Copilot Studio for error cases
            return AgentRoutingDecision(
                selected_agent=AgentType.COPILOT_STUDIO,
                reason="Error in routing logic, defaulting to Copilot Studio",
                confidence=0.5,
                routing_context={"error": str(ex)}
            )
    
    async def route_and_execute(self, request: ConversationRequest) -> AgentResponse:
        """Route and execute a conversation request"""
        logger.info(f"Routing and executing request for user {request.user_id}")
        
        routing_decision = await self.determine_routing(request)
        
        logger.info(f"Routing decision: {routing_decision.selected_agent.value} "
                   f"(confidence: {routing_decision.confidence}) - {routing_decision.reason}")
        
        if routing_decision.selected_agent == AgentType.COPILOT_STUDIO:
            return await self._execute_copilot_studio_request(request, routing_decision)
        elif routing_decision.selected_agent == AgentType.AZURE_AI:
            return await self._execute_azure_ai_request(request, routing_decision)
        elif routing_decision.selected_agent == AgentType.HYBRID:
            return await self._execute_hybrid_request(request, routing_decision)
        else:
            raise ValueError(f"Agent type {routing_decision.selected_agent} not supported")
    
    def _handle_explicit_routing(self, request: ConversationRequest) -> AgentRoutingDecision:
        """Handle explicit routing preferences"""
        if request.routing_preference == AgentRoutingPreference.PREFER_COPILOT_STUDIO:
            return AgentRoutingDecision(
                AgentType.COPILOT_STUDIO, "User preference for Copilot Studio", 1.0)
        elif request.routing_preference == AgentRoutingPreference.PREFER_AZURE_AI:
            return AgentRoutingDecision(
                AgentType.AZURE_AI, "User preference for Azure AI", 1.0)
        elif request.routing_preference == AgentRoutingPreference.COPILOT_STUDIO_ONLY:
            return AgentRoutingDecision(
                AgentType.COPILOT_STUDIO, "User explicitly requested Copilot Studio only", 1.0)
        elif request.routing_preference == AgentRoutingPreference.AZURE_AI_ONLY:
            return AgentRoutingDecision(
                AgentType.AZURE_AI, "User explicitly requested Azure AI only", 1.0)
        else:
            return self._determine_routing_with_rules(request)
    
    def _determine_routing_with_rules(self, request: ConversationRequest) -> AgentRoutingDecision:
        """Determine routing using rule-based logic"""
        message = request.message.lower()
        
        # Keywords that suggest Copilot Studio is appropriate
        copilot_studio_keywords = ["workflow", "process", "step", "form", "approval", "business", "policy", "procedure"]
        
        # Keywords that suggest Azure AI is appropriate
        azure_ai_keywords = ["explain", "analyze", "creative", "generate", "complex", "reasoning", "technical", "code"]
        
        copilot_studio_score = sum(1 for keyword in copilot_studio_keywords if keyword in message)
        azure_ai_score = sum(1 for keyword in azure_ai_keywords if keyword in message)
        
        if copilot_studio_score > azure_ai_score:
            return AgentRoutingDecision(
                AgentType.COPILOT_STUDIO,
                f"Message contains {copilot_studio_score} Copilot Studio keywords indicating structured process",
                min(0.6 + (copilot_studio_score * 0.1), 1.0)
            )
        elif azure_ai_score > copilot_studio_score:
            return AgentRoutingDecision(
                AgentType.AZURE_AI,
                f"Message contains {azure_ai_score} Azure AI keywords indicating complex reasoning needed",
                min(0.6 + (azure_ai_score * 0.1), 1.0)
            )
        else:
            # Default to Copilot Studio for balanced or unclear cases
            return AgentRoutingDecision(
                AgentType.COPILOT_STUDIO,
                "No clear indicators, defaulting to Copilot Studio for structured handling",
                0.5
            )
    
    async def _execute_copilot_studio_request(self, request: ConversationRequest, decision: AgentRoutingDecision) -> AgentResponse:
        """Execute request with Copilot Studio"""
        copilot_request = CopilotStudioRequest(
            bot_id=os.getenv("COPILOT_STUDIO_BOT_ID", ""),
            user_id=request.user_id,
            message=request.message,
            conversation_id=request.conversation_id,
            variables=request.context
        )
        
        response = await self.power_platform_service.send_message_to_copilot_studio(copilot_request)
        
        return AgentResponse(
            message=response.message,
            agent_type="CopilotStudio",
            agent_id=copilot_request.bot_id,
            conversation_id=response.conversation_id,
            context=response.variables,
            requires_follow_up=not response.topic_completed,
            next_action=response.next_topic
        )
    
    async def _execute_azure_ai_request(self, request: ConversationRequest, decision: AgentRoutingDecision) -> AgentResponse:
        """Execute request with Azure AI (placeholder implementation)"""
        # This would require Azure AI client implementation
        # For now, return a placeholder response
        return AgentResponse(
            message=f"Azure AI response to: {request.message}",
            agent_type="AzureAI",
            agent_id=self.azure_ai_agent_id,
            conversation_id=request.conversation_id,
            context=request.context,
            requires_follow_up=False
        )
    
    async def _execute_hybrid_request(self, request: ConversationRequest, decision: AgentRoutingDecision) -> AgentResponse:
        """Execute hybrid request (Copilot Studio + Azure AI)"""
        # Start with Copilot Studio
        copilot_response = await self._execute_copilot_studio_request(request, decision)
        
        # Check if escalation to Azure AI is needed
        if copilot_response.requires_follow_up and copilot_response.next_action == "escalate":
            azure_ai_response = await self._execute_azure_ai_request(request, decision)
            
            return AgentResponse(
                message=f"{copilot_response.message}\n\n[Escalated to Azure AI]\n{azure_ai_response.message}",
                agent_type="Hybrid",
                agent_id="hybrid-routing",
                conversation_id=copilot_response.conversation_id,
                context={**(copilot_response.context or {}), **(azure_ai_response.context or {})},
                requires_follow_up=azure_ai_response.requires_follow_up
            )
        
        return copilot_response