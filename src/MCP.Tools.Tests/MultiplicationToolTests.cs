using MCP.Tools;

namespace MCP.Tools.Tests
{
    public class MultiplicationToolTests
    {
        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(3, 2, 36)]
        [InlineData(5, 4, 400)]
        public void MultiplyTest(double a, double b, double c)
        {            
            Assert.Equal(MultiplicationTool.Multiply(a, b), c);
        }
    }
}
