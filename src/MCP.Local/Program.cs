using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MCP.Tools;

/// <summary>
/// This is a simple console application that runs an MCP server with various tools.
/// It uses the Stdio transport for communication.
/// It includes tools for echoing messages, multiplying numbers, and converting temperatures.
/// </summary>  
var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<EchoTool>()    
    .WithTools<MultiplicationTool>()
    .WithTools<TemperatureConverterTool>()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();