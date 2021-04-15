namespace IntakerConsole.Shared.Logger
{
    public class ApplicationLogger : IApplicationLogger
    {
        public void Log(string message) => System.Console.WriteLine($"{nameof(ApplicationLogger)} - {message}");
    }
}
