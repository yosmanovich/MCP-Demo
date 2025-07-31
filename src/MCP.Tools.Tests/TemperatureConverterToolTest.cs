using MCP.Tools;

namespace MCP.Tools.Tests
{
    public class TemperatureConverterToolTests
    {
        [Theory]
        [InlineData(0,32)]
        [InlineData(21, 70)]
        public void CelsiusToFahrenheitTest(double celsius, double fahrenheit)
        {
            Assert.Equal(TemperatureConverterTool.CelsiusToFahrenheit(celsius), fahrenheit);
        }
        [Theory]
        [InlineData(32,0)]
        [InlineData(70, 21)]
        public void FahrenheitToCelsiusTest(double fahrenheit, double celsius)
        {
            Assert.Equal(TemperatureConverterTool.FahrenheitToCelsius(fahrenheit), celsius);
        }
    }
}
