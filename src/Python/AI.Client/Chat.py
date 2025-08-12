from typing import Any

class Chat:
    def SystemChatMessage(prompt: str) -> Any:
        return {
                    "role": "system",
                    "content": prompt
               }
        
    def UserChatMessage(prompt: str) -> Any:
        return {
                    "role": "user",
                    "content": prompt
               }
            
    def ToolChatMessage(tool_call_id: str, tool_name: str, content: Any) -> Any:
        return {
                    "role": "tool",
                    "name": tool_name,
                    "tool_call_id": tool_call_id,
                    "content": content
               }          
    
    def AssistantChatMessage(completion: Any) -> Any:
        for content in completion.choices:    
            match content.finish_reason:
                case 'stop':
                    return {
                                "role": "assistant",
                                "content": content.message.content if hasattr(content.message, 'content') else "",
                            }
                case 'tool_calls':
                    tool_call = content.message.tool_calls[0]
                    return {
                                "role": "assistant",
                                "tool_calls": [{
                                    "function": {
                                        "name": tool_call.function.name,
                                        "arguments": tool_call.function.arguments
                                    },
                                    "id": tool_call.id,
                                    "type": "function"
                                }]
                            }