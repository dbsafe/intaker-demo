using System;
using System.Collections.Generic;
using System.IO;

namespace IntakerConsole
{
    public static class ArgHelper
    {
        public static bool LoadConfigFromArgs(string[] args, out IntakerConsoleAppConfig config)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Invalid number of arguments.");
                PrintHelp();
                config = default;
                return false;
            }

            if (!DecodeArgs(args, out Dictionary<string, string> dictionary))
            {
                PrintHelp();
                config = default;
                return false;
            }

            if (!LoadConfig(dictionary, out config))
            {
                PrintHelp();
                config = default;
                return false;
            }

            return true;
        }

        private static bool LoadConfig(Dictionary<string, string> dictionary, out IntakerConsoleAppConfig config)
        {
            config = new IntakerConsoleAppConfig();

            if (!LoadConfigValue(dictionary, ApplicationConstants.ARG_SPECS, out string configValue))
            {
                return false;
            }

            config.SpecsPath = GetFullPath(configValue);


            if (!LoadConfigValue(dictionary, ApplicationConstants.ARG_INPUT, out configValue))
            {
                return false;
            }

            config.InputPath = GetFullPath(configValue);

            if (!LoadConfigValue(dictionary, ApplicationConstants.ARG_OUTPUT, out configValue))
            {
                return false;
            }

            config.OutputPath = GetFullPath(configValue);

            return true;
        }

        private static bool LoadConfigValue(Dictionary<string, string> dictionary, string key, out string value)
        {
            if (!dictionary.ContainsKey(key))
            {
                Console.WriteLine($"Argument '{key}' not found");
                value = default;
                return false;
            }

            value = dictionary[key];
            return true;
        }

        private static bool DecodeArgs(string[] args, out Dictionary<string, string> dictionary)
        {
            dictionary = new Dictionary<string, string>();
            foreach (var arg in args)
            {
                if (!DecodeArg(arg, out KeyValuePair<string, string> decodedArg))
                {
                    Console.WriteLine($"Invalid argument '{arg}'");
                    return false;
                }

                dictionary.Add(decodedArg.Key, decodedArg.Value);
            }

            return true;
        }

        private static bool DecodeArg(string arg, out KeyValuePair<string, string> decodedArg)
        {
            var parts = arg.Split(':');
            if (parts.Length != 2)
            {
                decodedArg = default;
                return false;
            }

            decodedArg = new KeyValuePair<string, string>(parts[0], parts[1]);
            return true;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("IntakerConsole specs:file-specification-path input:input-file-path output:output-file-path");
        }

        private static string GetFullPath(string path)
        {
            return Path.IsPathFullyQualified(path) ? path : Path.GetFullPath(path);
        }
    }
}
