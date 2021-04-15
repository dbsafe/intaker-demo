using DataProcessor.Models;
using IntakerConsole.Configuration;
using IntakerConsole.Shared;
using IntakerConsole.Shared.Logger;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace IntakerConsole
{
    public class ConsoleWorker
    {
        private readonly IntakerConsoleAppConfig _intakerConsoleAppConfig;
        private readonly IApplicationLogger _logger;

        public ConsoleWorker(IConfiguration configuration, IApplicationLogger logger)
        {
            _logger = logger;
            _intakerConsoleAppConfig = configuration.GetSection(ApplicationConstants.CONFIG_SECTION_INTAKER_CONSOLE_APP).Get<IntakerConsoleAppConfig>();
            LogConfiguration();
        }

        public void Run()
        {
            try
            {
                ValidatePaths(_intakerConsoleAppConfig);

                var processor = ParsedDataProcessorBuilder.BuildParsedDataProcessor(_intakerConsoleAppConfig.SpecsPath, _intakerConsoleAppConfig.InputPath);

                var memoryBeforeLoading = GC.GetTotalMemory(true) / 1024;
                var parsed = processor.Process();
                var memoryAfterLoading = GC.GetTotalMemory(true) / 1024;
                LogMemoryUsage(memoryBeforeLoading, memoryAfterLoading);

                _logger.Log(SummaryBuilder.BuildSummary(parsed));

                WriteDataRowsToFile(_intakerConsoleAppConfig.OutputPath, parsed.DataRows);

                _logger.Log(SummaryBuilder.BuildInvalidDataRows(parsed.InvalidDataRows));
            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString());
            }
        }

        private static void WriteDataRowsToFile(string outputPath, IList<Row> rows)
        {
            if (rows.Count == 0)
            {
                File.WriteAllText(outputPath, "[]");
                return;
            }

            using var jsonFileDataWriter = new JsonFileDataWriter(outputPath);
            foreach (var row in rows)
            {
                jsonFileDataWriter.Write(row.Json);
            }
        }

        private void LogMemoryUsage(long memoryBeforeLoading, long memoryAfterLoading)
        {
            var increase = memoryAfterLoading - memoryBeforeLoading;

            _logger.Log($"Memory after loading: {memoryBeforeLoading:n0} Kb");
            _logger.Log($"Memory after loading: {memoryAfterLoading:n0} Kb");
            _logger.Log($"Memory after loading: {increase:n0} Kb");
        }

        private void LogConfiguration()
        {
            _logger.Log($"{ApplicationConstants.CONFIG_SECTION_INTAKER_CONSOLE_APP}: {JsonConvert.SerializeObject(_intakerConsoleAppConfig)}");
        }

        private static bool ValidatePaths(IntakerConsoleAppConfig config)
        {
            if (!File.Exists(config.SpecsPath))
            {
                Console.WriteLine($"File not found '{config.SpecsPath}'");
                return false;
            }

            if (!File.Exists(config.InputPath))
            {
                Console.WriteLine($"File not found '{config.InputPath}'");
                return false;
            }

            return true;
        }
    }
}