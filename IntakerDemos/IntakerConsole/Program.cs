namespace IntakerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (ArgHelper.LoadConfigFromArgs(args, out IntakerConsoleAppConfig config))
            {
                IntakerConsoleApp.Run(config);
            }
        }
    }
}
