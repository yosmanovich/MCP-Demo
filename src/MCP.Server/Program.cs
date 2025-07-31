using MCP.Tools;

/// <summary>
/// This is a simple web application that runs an MCP server with various tools.
/// It uses HTTP transport for communication.
/// It includes tools for echoing messages, multiplying numbers, and converting temperatures.
/// </summary>  

var builder = WebApplication.CreateBuilder(args);

// Add MCP server services with HTTP transport
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<EchoTool>()
    .WithTools<MultiplicationTool>()
    .WithTools<TemperatureConverterTool>();

// Add CORS for HTTP transport support in browsers
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Enable CORS
app.UseCors();

// Map MCP endpoints
app.MapMcp();

app.UseHttpsRedirection();

// Add a simple home page
app.MapGet("/status", () => "MCP Server on Azure App Service - Ready for use with HTTP transport");

app.Run();
