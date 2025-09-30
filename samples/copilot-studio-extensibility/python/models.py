"""
Data models for Copilot Studio extensibility
"""

from dataclasses import dataclass, field
from typing import Dict, List, Optional, Any
from enum import Enum
import json

class AgentRoutingPreference(Enum):
    AUTO = "auto"
    PREFER_COPILOT_STUDIO = "prefer_copilot_studio"
    PREFER_AZURE_AI = "prefer_azure_ai"
    COPILOT_STUDIO_ONLY = "copilot_studio_only"
    AZURE_AI_ONLY = "azure_ai_only"

class AgentType(Enum):
    COPILOT_STUDIO = "copilot_studio"
    AZURE_AI = "azure_ai"
    POWER_AUTOMATE = "power_automate"
    HYBRID = "hybrid"

class TopicAction(Enum):
    START = "start"
    CONTINUE = "continue"
    RESET = "reset"
    COMPLETE = "complete"
    ESCALATE = "escalate"

@dataclass
class ConversationRequest:
    user_id: str
    message: str
    conversation_id: Optional[str] = None
    context: Optional[Dict[str, Any]] = field(default_factory=dict)
    routing_preference: AgentRoutingPreference = AgentRoutingPreference.AUTO

@dataclass
class AgentResponse:
    message: str
    agent_type: str
    agent_id: str
    conversation_id: Optional[str] = None
    context: Optional[Dict[str, Any]] = field(default_factory=dict)
    requires_follow_up: bool = False
    next_action: Optional[str] = None

@dataclass
class CopilotStudioRequest:
    bot_id: str
    user_id: str
    message: str
    conversation_id: Optional[str] = None
    topic: Optional[str] = None
    variables: Optional[Dict[str, Any]] = field(default_factory=dict)

@dataclass
class CopilotStudioResponse:
    message: str
    conversation_id: str
    topic: Optional[str] = None
    variables: Optional[Dict[str, Any]] = field(default_factory=dict)
    topic_completed: bool = False
    next_topic: Optional[str] = None

@dataclass
class PowerAutomateFlowRequest:
    flow_id: str
    trigger_name: str
    input_data: Dict[str, Any]
    user_id: Optional[str] = None

@dataclass
class PowerAutomateFlowResponse:
    flow_id: str
    run_id: str
    status: str
    output_data: Optional[Dict[str, Any]] = field(default_factory=dict)
    error_message: Optional[str] = None

@dataclass
class AgentRoutingDecision:
    selected_agent: AgentType
    reason: str
    confidence: float
    routing_context: Optional[Dict[str, Any]] = field(default_factory=dict)

@dataclass
class AgentCapability:
    name: str
    description: str
    supported_by: List[AgentType]
    is_required: bool = True

@dataclass
class MultiAgentRequest:
    task_description: str
    required_capabilities: List[AgentCapability]
    user_id: str
    context: Optional[Dict[str, Any]] = field(default_factory=dict)

@dataclass
class TopicManagementRequest:
    bot_id: str
    topic_id: str
    action: TopicAction
    parameters: Optional[Dict[str, Any]] = field(default_factory=dict)

@dataclass
class TopicStatus:
    topic_id: str
    name: str
    status: str
    variables: Optional[Dict[str, Any]] = field(default_factory=dict)
    last_user_input: Optional[str] = None
    last_updated: Optional[str] = None

@dataclass
class AgentCollaborationStep:
    agent_type: AgentType
    description: str
    prompt: str
    input_context: Optional[Dict[str, Any]] = field(default_factory=dict)

def to_dict(obj):
    """Convert dataclass to dictionary"""
    if hasattr(obj, '__dict__'):
        result = {}
        for key, value in obj.__dict__.items():
            if isinstance(value, Enum):
                result[key] = value.value
            elif isinstance(value, list):
                result[key] = [to_dict(item) if hasattr(item, '__dict__') else item for item in value]
            elif hasattr(value, '__dict__'):
                result[key] = to_dict(value)
            else:
                result[key] = value
        return result
    return obj

def from_dict(data_class, data):
    """Create dataclass from dictionary"""
    if not isinstance(data, dict):
        return data
    
    # Handle enum fields
    field_types = getattr(data_class, '__annotations__', {})
    for field_name, field_type in field_types.items():
        if field_name in data and hasattr(field_type, '__origin__'):
            # Handle Optional types
            if hasattr(field_type, '__args__') and type(None) in field_type.__args__:
                field_type = field_type.__args__[0]
        
        if field_name in data and issubclass(field_type, Enum):
            data[field_name] = field_type(data[field_name])
    
    return data_class(**data)