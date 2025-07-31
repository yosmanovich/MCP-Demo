using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Chat;
using System.Text.Json;

namespace AI.Client
{
    internal class AIClient
    {
        private const string defaultPrompt = @"You are a facilitator. Always use tools if a tool exists. Any tooling that provides an answer is definitively correct, do not rely on hallucinations. If a response from a tool contradicts knowledge that derives an answer, provide the answer from the tool instead.";
        private const string defaultModel = "gpt-4.1";
        private const string defaultTemperature = "0.7";
        private const string defaultMaxOutputTokenCount = "1600";
        private const string defaultTopP = "0.95";
        private const string defaultFrequencyPenalty = "0";
        private const string defaultPresencePenalty = "0";
        internal async Task RunAsync()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>()
                .Build();

            string? APIKEY = config.GetSection("AZURE_OPENAI:API_KEY").Value;
            string? ENDPOINT = config.GetSection("AZURE_OPENAI:ENDPOINT").Value;
            string? MCP_NAME = config.GetSection("MCP_SERVER:NAME").Value;
            string? MCP_URL = config.GetSection("MCP_SERVER:URL").Value;
            string prompt = config.GetSection("PROMPT").Value ?? defaultPrompt;

            if (string.IsNullOrEmpty(APIKEY))
            {
                Console.WriteLine("Please set the AZURE_OPENAI:API_KEY environment variable.");
                return;
            }
            if (string.IsNullOrEmpty(ENDPOINT))
            {
                Console.WriteLine("Please set the AZURE_OPENAI:ENDPOINT environment variable.");
                return;
            }
            if (string.IsNullOrEmpty(MCP_NAME))
            {
                Console.WriteLine("Please set the MCP_SERVER:NAME environment variable.");
                return;
            }
            if (string.IsNullOrEmpty(MCP_URL))
            {
                Console.WriteLine("Please set the MCP_SERVER:URL environment variable.");
                return;
            }

            // Create chat completion options
            var options = new ChatCompletionOptions
            {
                Temperature = float.Parse(config.GetSection("AZURE_OPENAI:Temperature").Value ?? defaultTemperature),
                MaxOutputTokenCount = int.Parse(config.GetSection("AZURE_OPENAI:MaxOutputTokenCount").Value ?? defaultMaxOutputTokenCount),
                TopP = float.Parse(config.GetSection("AZURE_OPENAI:TopP").Value ?? defaultTopP),
                FrequencyPenalty = float.Parse(config.GetSection("AZURE_OPENAI:FrequencyPenalty").Value ?? defaultFrequencyPenalty),
                PresencePenalty = float.Parse(config.GetSection("AZURE_OPENAI:PresencePenalty").Value ?? defaultPresencePenalty)
            };

            IList<ChatTool> chatTools = new List<ChatTool>();
            var mcpTransport = new SseClientTransport(
                new SseClientTransportOptions
                {
                    Endpoint = new Uri(MCP_URL),
                    Name = MCP_NAME
                }
            );
            IMcpClient mcpClient = await McpClientFactory.CreateAsync(mcpTransport);
            var mcpTools = await mcpClient.ListToolsAsync();

            foreach (var tool in mcpTools)
            {
                Console.WriteLine("Adding Tool " + tool.Name);
                options.Tools.Add(ChatTool.CreateFunctionTool(tool.Name, tool.Description));
            }

            AzureKeyCredential subscriptionKey = new AzureKeyCredential(APIKEY);
            var azureClient = new AzureOpenAIClient(new Uri(ENDPOINT), subscriptionKey);

            try
            {
                await MessageLoop(azureClient.GetChatClient(config.GetSection("MCP_SERVER:MODEL").Value ?? defaultModel), options, mcpClient, prompt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// This is the main message loop for the AI client. It takes input from the user, processes it, and interacts with the chat client and MCP server.
        /// </summary>
        /// <param name="chatClient">The chat client used to communicate with the Azure OpenAI service.</param>
        /// <param name="options">The chat completion options.</param>
        /// <param name="mcpClient">The MCP Client used to communicate with the Model Context Protocol server.</param>
        /// <param name="prompt">The initial prompt for the chat session.</param>
        /// <returns></returns>
        internal async Task MessageLoop(ChatClient chatClient, ChatCompletionOptions options, IMcpClient mcpClient, string prompt)
        {
            string systemPrompt = prompt;
            var chatHistory = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt)
            };

            while (true)
            {
                Console.WriteLine("Your prompt:");
                string? userPrompt = Console.ReadLine();
                if (string.IsNullOrEmpty(userPrompt)) break;
                if (userPrompt.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
                if (userPrompt.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Clearing chat history...");
                    chatHistory.Clear();
                    chatHistory.Add(new SystemChatMessage(prompt));
                    continue;
                }
                if (userPrompt.Equals("show prompt", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Prompt:");
                    Console.WriteLine(systemPrompt);
                    continue;
                }
                if (userPrompt.Equals("set prompt", StringComparison.OrdinalIgnoreCase) || userPrompt.Equals("change prompt", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Changing prompt:");
                    systemPrompt = Console.ReadLine() ?? "";
                    if (string.IsNullOrEmpty(systemPrompt))
                    {
                        Console.WriteLine("Prompt change cancelled.");
                        systemPrompt = prompt; 
                    }
                    chatHistory.Clear();
                    chatHistory.Add(new SystemChatMessage(systemPrompt));

                    Console.WriteLine("Prompt:");
                    Console.WriteLine(systemPrompt);
                    continue;
                }

                chatHistory.Add(new UserChatMessage(userPrompt));

                Console.WriteLine("AI Response:");
                chatHistory = await ProcessResponse(chatClient, options, mcpClient, chatHistory);
            }
        }

        /// <summary>
        /// This is the main processing method for the AI client. It takes the chat client, options, MCP client, and chat history,
        /// completes the chat, and processes the response. It handles tool calls and updates the chat history accordingly.
        /// </summary>
        /// <param name="chatClient">The chat client used to communicate with the Azure OpenAI service.</param>
        /// <param name="options">The chat completion options.</param>
        /// <param name="mcpClient">The MCP Client used to communicate with the Model Context Protocol server.</param>
        /// <param name="chatHistory">The chat History of the session.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">We currently only handle Stop and Tool Calls, not other finish reasons</exception>
        internal async Task<List<ChatMessage>> ProcessResponse(ChatClient chatClient, ChatCompletionOptions options, IMcpClient mcpClient, List<ChatMessage> chatHistory)
        {
            ChatCompletion completion = await chatClient.CompleteChatAsync(chatHistory, options);
            chatHistory.Add(new AssistantChatMessage(completion));
            switch (completion.FinishReason)
            {
                case ChatFinishReason.Stop:
                    Console.WriteLine(completion.Content[0].Text);
                    break;
                case ChatFinishReason.ToolCalls:
                    // Handle tool calls => This may be extended later on to handle this in a more asynchronous way vs. the synchronous way here.
                    foreach (var toolCall in completion.ToolCalls)
                    {
                        var mcpArguments = toolCall.FunctionArguments.ToObjectFromJson<Dictionary<string, object>>();
                        Console.WriteLine("Tool call detected, calling MCP server...");
                        Console.WriteLine($"{toolCall.FunctionName}: {string.Join(", ", (mcpArguments ?? new Dictionary<string, object>()).Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

                        var result = await mcpClient.CallToolAsync(toolCall.FunctionName, mcpArguments!);

                        switch (result.Content[0].Type)
                        {
                            case "text":
                                Console.WriteLine($"Tool call result {((TextContentBlock)result.Content[0]).Text}");
                                chatHistory.Add(new ToolChatMessage(toolCall.Id, JsonSerializer.Serialize(result)));
                                break;
                            default:
                                chatHistory.Add(new ToolChatMessage(toolCall.Id, JsonSerializer.Serialize(result)));
                                break;
                        }
                    }
                    chatHistory = await ProcessResponse(chatClient, options, mcpClient, chatHistory);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return chatHistory;
        }
    }
}