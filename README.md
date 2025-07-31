# Model Context Protocol (MCP) Server and AI Client - .NET Implementation

This project contains two .NET app implementations of a Model Context Protocol (MCP) server; one runs in standard IO mode and another runs through a web application. The web application may be deployed to Azure App Service.

The MCP server provides an API that follows the Model Context Protocol specification, allowing AI models to request additional context during inference.

The MCP tools are in a separate project. The tools are not complex; they show the basic struture of how a tool is developed and to provide a starting point for building additional tools. There is also a Tests project that demonstrates how to unit test each tool. 

A simple AI Client exists that may be used to make Chat calls to your Azure AI service and to an MCP Server. This client shows the basic flow between the system, user, assistant, and tools. 

## Key Features

- Azure App Service integration
- Custom tools support

## Project Structure
- `.vscode/` - VS Code workspace files
  - `mcp.json` - The 
- `src/` - Contains the main C# project files
  - `AI.Client/` - The AI Client files
    - `Program.cs` - The entry point for the AI Client
    - `AIClient.cs` - The logic for the AI Client
  - `MCP.Local/` - The MCP Local Server that runs via stdio
    - `Program.cs` - The entry point for the MCP Local Server
  - `MCP.Server/` - The MCP Server that runs via HTTP/HTTPS
    - `Program.cs` - The entry point for the MCP Server
  - `MCP.Tools/` - Contains custom tools that can be used by models via the MCP protocol
    - `EchoTools.cs` - Tool for simple string echo tests
    - `MultiplicationTool.cs` - Example tool that performs multiplication operations
    - `TemperatureConverterTool.cs` - Tool for converting between Celsius and Fahrenheit
  - `MCP.Tools.Tests/` - The unit tests for the MCP Tools
    - `EchoToolsTests.cs` - Unit Tests for the Echo Tools
    - `MultiplicationToolTests.cs` - Unit Tests for the Multiplication Tool
    - `TemperatureConverterToolTests.cs` - Unit Tests for the Temperature Converter Tool


## Prerequisites

- [Azure Developer CLI](https://aka.ms/azd)
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- For local development with VS Code:
  - [Visual Studio Code](https://code.visualstudio.com/)
- For local development with docker:
  - [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- An Azure Open AI Instance with a chat completion model deployed

- MCP C# SDK:
  ```bash
  dotnet add package ModelContextProtocol --prerelease
  ```

## Local Development

### Run the Server Locally

1. Clone this repository
2. Navigate to the project directory
   ```bash
   cd src
   ```
3. Install required packages
   ```bash
   dotnet restore
   ```
4. Run the project:
   ```bash
   dotnet run
   ```
4. The MCP server will be available at `http://localhost:5000`
5. When you're done, press Ctrl+C in the terminal to stop the app

### Creating a docker image
1. Open terminal
2. Navigate to MCP.Local;
```bash
   cd .\src\MCP.Local\
```   
3. Build the docker container:
```bash
   dotnet publish /t:PublishContainer
```
### Testing the Available Tools

The server provides these tools:
- **Multiplication**: `Multiply` - Multiplies two numbers
- **Temperature Conversion**: 
  - `CelsiusToFahrenheit` - Converts temperature from Celsius to Fahrenheit
  - `FahrenheitToCelsius` - Converts temperature from Fahrenheit to Celsius

### Connect to the Local MCP Server

#### Using VS Code - Copilot Agent Mode

1. **Add MCP Server** from command palette and add the URL to your running server's HTTP endpoint:
   ```
   http://localhost:5000
   ```
2. **List MCP Servers** from command palette and start the server
3. In Copilot chat agent mode, enter a prompt to trigger the tool:
   ```
   Multiply 3423 and 5465
   ```
4. When prompted to run the tool, consent by clicking **Continue**

You can ask things like:
- Convert 25 degrees Celsius to Fahrenheit

#### Using MCP Inspector

1. In a **new terminal window**, install and run MCP Inspector:
   ```bash
   npx @modelcontextprotocol/inspector
   ```
2. CTRL+click the URL displayed by the app (e.g. http://localhost:5173/#resources)
3. Set the transport type to `HTTP`
4. Set the URL to your running server's HTTP endpoint and **Connect**:
   ```
   http://localhost:5000
   ```
5. **List Tools**, click on a tool, and **Run Tool**

## Deploy to Azure

1. Login to Azure:
   ```bash
   azd auth login
   ```

2. Initialize your environment:
   ```bash
   azd env new
   ```

3. Deploy the application:
   ```bash
   azd up
   ```

   This will:
   - Build the .NET application
   - Provision Azure resources defined in the Bicep templates
   - Deploy the application to Azure App Service

### Connect to Remote MCP Server

#### Using MCP Inspector
Use the web app's URL:
```
https://<webappname>.azurewebsites.net
```

#### Using VS Code - GitHub Copilot
Follow the same process as with the local app, but use your App Service URL:
```
https://<webappname>.azurewebsites.net
```
### Run the AI Client

#### Modify the appsettings.json
1. Set the AZURE_OPENAI ENDPOINT to your Azure Open AI endpoint
2. Set the AZURE_OPENAI MODEL to a model you have deployed in Azure Open AI
3. Set the MCP_SERVER URL to the MCP server that you are running

#### Modify the local secrets
1. Set the AZURE_OPENAI API_KEY to your Azure Open AI API Key

#### Run the AI Client
Start the AI.Client project
```
dotnet run --project .\src\AI.Client\AI.Client.csproj
```
The client will connect to your MCP server and add all tools, notifying you in the console.

You can ask the LLM to provide information; if you provide a message that can be handled by one of the tools it will call the tool to get information from the tool and report that to you.

The application will run until you either type exit and hit enter or if you press enter with no other input.

You may clear the chat history by typing 
```
clear
```

You can review the sytem prompt by typing 
```
show prompt
```

You can change the sytem prompt by typing 
```
set prompt
``` or 
```
change prompt
```
If you enter an empty prompt, the tool will reset the prompt back to the default prompt / what was entered in appsettings.json.

Observe that the if the LLM has knowledge that conflicts with the tools, it will notify the user of that. This may be corrected via changes to the prompt, for example: 
- "You are a math expert..."
- "You do not understand math..."
- "You are a facilitator..."

Additionally, if you modify the system prompt with calls to the tools; the AI Client should handle those tool calls as well as part of the conversation, for example: 
- "Always convert the answer from the tool's response from fahrenheit to celsius at the end."

## Clean up resources

When you're done working with your app and related resources, you can use this command to delete the function app and its related resources from Azure and avoid incurring any further costs:

```shell
azd down
```

## Custom Tools

The project includes several sample tools in the `MCP.Tools` project:
- `EchoTool.cs` - Performs basic string echo operations
- `MultiplicationTool.cs` - Performs multiplication operations
- `TemperatureConverterTool.cs` - Converts between Celsius and Fahrenheit

To add new tools:
1. Create a new class in the `Tools` directory
2. Implement the MCP tool interface
3. Register the tool in `Program.cs`

## References
This project is an amalgamation of several projects found on the web. Each did one thing right and this is meant to be an all-in-one, easy to consume project that discusses getting started with MCP all the way through deploying something to Azure and how it ties into both Copilot and a custom AI Client.

- Complete implementation of the MCP protocol in C#/.NET using [MCP csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)
- Model Context Protocol (MCP) Server - .NET Implementation [remote-mcp-webapp-dotnet](https://github.com/Azure-Samples/remote-mcp-webapp-dotnet)
