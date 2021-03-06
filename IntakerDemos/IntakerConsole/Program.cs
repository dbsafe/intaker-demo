using System;
using System.Diagnostics;

namespace IntakerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (ArgHelper.LoadConfigFromArgs(args, out IntakerConsoleAppConfig config))
            {
                //Console.WriteLine(GC.GetTotalMemory(true));
                var sw = new Stopwatch();
                sw.Start();
                IntakerConsoleApp.Run(config);
                sw.Stop();

                Console.WriteLine($"Elapsed: {sw.Elapsed}");
                //Console.WriteLine(GC.GetTotalMemory(true));
            }
        }
    }
}
