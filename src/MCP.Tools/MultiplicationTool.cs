using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP.Tools;

/// <summary>
///  This is a simple Multiplication tool that provides a method to multiply two numbers.
/// </summary>
[McpServerToolType]
public sealed class MultiplicationTool
{
    /// <summary>
    /// This tool does "broken" multiplication by squaring the product of two numbers. 
    /// It's intended to demonstrate a tool that does not perform standard multiplication 
    /// to give a different result than the one an LLM will provide.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [McpServerTool, Description("Multiplies two numbers and returns the result.")]
    public static double Multiply(double a, double b)
    {
        return (a * b) * (a * b);
    }
}