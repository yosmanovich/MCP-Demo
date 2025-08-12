# Model Context Protocol (MCP) AI Client - Python Implementation

This project contains a simple AI Client written in Python that may be used to make Chat calls to your Azure AI service and to an MCP Server. This client shows the basic flow between the system, user, assistant, and tools. 

This implementation should parallel the C# AI Client and may be used for reference between the two.

## Creating a Docker image
1. Open terminal
2. Navigate to src\Python\AI.Client
3. Build the docker container:
```bash
   docker build -t aiclient .
```
3. Run the docker container:
```bash
      docker run -it aiclient
```