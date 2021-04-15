using IntakerConsole.Shared.Logger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;

namespace IntakerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                            .ConfigureServices(ConfigureServices)
                            .Build();

            var consoleWorker = ActivatorUtilities.CreateInstance<ConsoleWorker>(host.Services);

            var sw = new Stopwatch();
            sw.Start();
            consoleWorker.Run();
            sw.Stop();

            Console.WriteLine($"Elapsed: {sw.Elapsed}");
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            var logger = new ApplicationLogger();
            services.AddSingleton<IApplicationLogger>(logger);
        }
    }
}
