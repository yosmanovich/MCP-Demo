using MCP.Tools;

namespace MCP.Tools.Tests
{
    public class EchoToolTests
    {
        [Theory]
        [InlineData("hello world")]
        public void EchoTest2(string str)
        {            
            Assert.Equal(EchoTool.Echo(str), $"hello {str}");
        }
        [Theory]
        [InlineData("hello world", "dlrow olleh")]
        public void FlipTest(string? str, string? fstr)
        {
            Assert.Equal(EchoTool.Flip(str), fstr);
        }
        [Theory]
        [InlineData("hello world")]
        public void MLengthTest(string str)
        {            
            Assert.Equal(EchoTool.MLength(str), str.Length);
        }
    }
}
