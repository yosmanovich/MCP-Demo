namespace AI.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            AIClient client = new AIClient();
            await client.RunAsync();
        }
    }
}
