import json
import os
from typing import Optional, Any
from dotenv import load_dotenv
from anthropic import Anthropic
from contextlib import AsyncExitStack
from openai import AzureOpenAI
from mcp import ClientSession
from mcp.client.streamable_http import streamablehttp_client
from Chat import Chat
class AIClient:
    def __init__(self):
        load_dotenv() 

        self.API_VERSION = "2023-07-01-preview"
        self.API_KEY = os.environ.get("OPENAI_API_KEY")
        self.OPENAI_ENDPOINT = os.environ.get("OPENAI_ENDPOINT")
        self.OPENAI_MODEL = os.environ.get("OPENAI_MODEL")
        self.OPENAI_Temperature = float(os.environ.get("OPENAI_Temperature"))
        self.OPENAI_MaxOutputTokenCount = int(os.environ.get("OPENAI_MaxOutputTokenCount"))
        self.OPENAI_TopP = float(os.environ.get("OPENAI_TopP"))
        self.OPENAI_FrequencyPenalty = float(os.environ.get("OPENAI_FrequencyPenalty"))
        self.OPENAI_PresencePenalty = float(os.environ.get("OPENAI_PresencePenalty"))
        self.MCP_URL = os.environ.get("MCP_URL")

        if not self.API_KEY :
            raise ValueError("Please set the OPENAI_API_KEY environment variable.")

        if not self.OPENAI_ENDPOINT :
            raise ValueError("Please set the OPENAI_ENDPOINT environment variable.")

        if not self.MCP_URL :
            raise ValueError("Please set the MCP_URL environment variable.")

        # Initialize session and client objects
        self.session: Optional[ClientSession] = None
        self.exit_stack = AsyncExitStack()
        self.anthropic = Anthropic()
        self.client = AzureOpenAI(
            api_key=self.API_KEY,
            azure_endpoint=self.OPENAI_ENDPOINT,
            api_version=self.API_VERSION
        )
        self.chatHistory = []
        self.available_tools = []
        self.defaultSystemPrompt = "You are a helpful assistant. Answer the user's questions to the best of your ability."        

    async def connect_to_mcp_server(self,  headers: Optional[dict] = None):
        """Connect to a HTTP Streamable MCP server"""        
        self._streams_context = streamablehttp_client(
            url=self.MCP_URL,
            headers=headers or {},
        )
        read_stream, write_stream, _ = await self._streams_context.__aenter__()  
        self._session_context = ClientSession(read_stream, write_stream)  
        self.session: ClientSession = await self._session_context.__aenter__()  

        await self.session.initialize()
        await self.get_server_tools()

    async def get_server_tools(self):
            if not self.session:
                print("No active session found.")
                return
            print("Listing tools...")        
            response = await self.session.list_tools()
            tools = response.tools if hasattr(response, 'tools') else []        
            print("\nConnected to server with tools:", [tool.name for tool in tools])

            self.available_tools = []
            for tool in tools:
                openai_tool = {
                    "type": "function",
                    "function": {
                        "name": tool.name,
                        "description": tool.description,
                        "parameters": tool.inputSchema if hasattr(tool, 'inputSchema') else {
                            "type": "object",
                            "properties": {},
                            "required": []
                        }
                    }
                }
                self.available_tools.append(openai_tool)
                
    async def InitializePrompt(self, systemPrompt: str):
        self.chatHistory = []
        self.chatHistory.append(Chat.SystemChatMessage(systemPrompt))
        
    async def CallChatCompletion(self):
                
        completion = self.client.chat.completions.create(
            model=self.OPENAI_MODEL,            
            temperature=self.OPENAI_Temperature,
            top_p=self.OPENAI_TopP,
            max_completion_tokens=self.OPENAI_MaxOutputTokenCount,
            frequency_penalty=self.OPENAI_FrequencyPenalty,
            presence_penalty=self.OPENAI_PresencePenalty,
            messages=self.chatHistory,
            tools=self.available_tools
        )

        self.chatHistory.append(Chat.AssistantChatMessage(completion))

        for content in completion.choices:    
            match content.finish_reason:
                case 'stop':
                    print(content.message.content)

                case 'tool_calls':                    
                    for tool_call in content.message.tool_calls:
                  
                        tool_name = tool_call.function.name                        
                        tool_args = json.loads(tool_call.function.arguments)
                        
                        # Execute tool call
                        print(f"Tool call detected, calling MCP server...")
                        print(f"[{tool_name}: {tool_args}]")
                        result = await self.session.call_tool(tool_name, tool_args)

                        print(f"Tool call result {result.content[0].text}")
                        self.chatHistory.append(Chat.ToolChatMessage(
                            tool_call.id,
                            tool_name,
                            result.content
                        ))
                        await self.CallChatCompletion()                        

                case _:
                    raise ValueError("Not valid")

    async def MessageLoop(self):        
        await self.connect_to_mcp_server()
        systemPrompt = self.defaultSystemPrompt;
        await self.InitializePrompt(systemPrompt)
        
        print("Your prompt:")
        while True:
            userPrompt = input()
            if userPrompt.lower() == "exit" or userPrompt.lower() == "quit" or userPrompt.lower() == "q" or userPrompt == "":

                break
            if userPrompt.lower() == "clear":
                print("Clearing chat history...")
                await self.InitializePrompt(self.defaultSystemPrompt)
                continue
            if userPrompt.lower() == "show prompt":
                print("Prompt:")
                print(systemPrompt)
                continue

            if userPrompt.lower() == "change":
                print("Changing prompt:")
                systemPrompt = input()
                if systemPrompt == "":
                    print("Prompt change cancelled.")
                    systemPrompt = self.defaultSystemPrompt
                await self.InitializePrompt(systemPrompt)
                print("Prompt:")
                print(systemPrompt)

                continue

            self.chatHistory.append(Chat.UserChatMessage(userPrompt))
            await self.CallChatCompletion()
            
    async def cleanup(self):
        """Properly clean up the session and streams"""
        if self._session_context:
            await self._session_context.__aexit__(None, None, None)
        if self._streams_context:  
            await self._streams_context.__aexit__(None, None, None)          