using DataProcessor;
using DataProcessor.Contracts;
using DataProcessor.DataSource.File;
using DataProcessor.InputDefinitionFile;
using DataProcessor.Models;
using DataProcessor.ProcessorDefinition;
using System;
using System.Collections.Generic;
using System.IO;

namespace IntakerConsole
{
    public static class IntakerConsoleApp
    {
        public static void Run(IntakerConsoleAppConfig config)
        {
            if (!ValidatePaths(config))
            {
                return;
            }

            PrintInputs(config);

            var inputDefinitionFile = FileLoader.Load<InputDefinitionFile10>(config.SpecsPath);
            var fileDataSourceValidFile = BuildFileDataSource(config.InputPath, inputDefinitionFile);

            var fileProcessorDefinition = FileProcessorDefinitionBuilder.CreateFileProcessorDefinition(inputDefinitionFile);
            var processor = new ParsedDataProcessor10(fileDataSourceValidFile, fileProcessorDefinition);

            var memoryBeforeLoading = GC.GetTotalMemory(true) / 1024;
            var parsed = processor.Process();
            var memoryAfterLoading = GC.GetTotalMemory(true) / 1024;
            var increase = memoryAfterLoading - memoryBeforeLoading;

            Console.WriteLine($"Memory after loading: {memoryBeforeLoading:n0} Kb");
            Console.WriteLine($"Memory after loading: {memoryAfterLoading:n0} Kb");
            Console.WriteLine($"Memory after loading: {increase:n0} Kb");

            PrintSummary(parsed);
            WriteDataRowsToFile(config.OutputPath, parsed.DataRows);
            PrintInvalidDataRows(parsed.InvalidDataRows);
        }

        private static void PrintSummary(ParsedData10 parsed)
        {
            Console.WriteLine("Summary:");
            Console.WriteLine($"Validation result: {parsed.ValidationResult}");
            Console.WriteLine($"Data row count: {parsed.DataRows.Count}");
            Console.WriteLine($"Invalid data row count: {parsed.InvalidDataRows.Count}");
            Console.WriteLine();

            PrintRowWithName("Header", parsed.Header);
            Console.WriteLine();

            PrintRowWithName("Trailer", parsed.Trailer);
            Console.WriteLine();
        }

        private static void PrintRowWithName(string name, Row row)
        {
            if (row == null)
            {
                Console.WriteLine($"{name}: none");
            }
            else
            {
                Console.WriteLine(name);
                PrintRow(row);
            }
        }

        private static void PrintInvalidDataRows(IList<Row> rows)
        {
            if (rows.Count == 0)
            {
                Console.WriteLine("Invalid Rows: none");
            }

            Console.WriteLine("Invalid Rows:");
            foreach(var row in rows)
            {
                PrintRow(row);
            }
        }

        private static void PrintRow(Row row)
        {
            Console.WriteLine($"Index: {row.Index}, Raw: {row.Raw}");
            Console.WriteLine($"Json: {row.Json}");
            PrintList("Errors", row.Errors);
            PrintList("Warnings", row.Warnings);
        }

        private static void PrintList(string name, IList<string> list)
        {
            if (list.Count > 0)
            {
                Console.WriteLine($"{name}:");
                foreach (var item in list)
                {
                    Console.WriteLine($"\t{item}");
                }
            }
        }

        private static void PrintInputs(IntakerConsoleAppConfig config)
        {
            Console.WriteLine($"specs: {config.SpecsPath}");
            Console.WriteLine($"input: {config.InputPath}");
            Console.WriteLine($"output: {config.OutputPath}");
            Console.WriteLine();
        }

        private static void WriteDataRowsToFile(string outputPath, IList<Row> rows)
        {
            if (rows.Count == 0)
            {
                File.WriteAllText(outputPath, "[]");
                return;
            }

            using var jsonFileDataWriter = new JsonFileDataWriter(outputPath);
            foreach(var row in rows)
            {
                jsonFileDataWriter.Write(row.Json);
            }
        }

        private static IDataSource<ParserContext10> BuildFileDataSource(string inputPath, InputDefinitionFile10 inputDefinition)
        {
            var fileDataSourceConfig = new FileDataSourceConfig 
            { 
                Delimiter = inputDefinition.Delimiter, 
                HasFieldsEnclosedInQuotes = inputDefinition.HasFieldsEnclosedInQuotes, 
                Path = inputPath,
                CommentedOutIndicator = inputDefinition.CommentedOutIndicator
            };

            return new FileDataSource<ParserContext10>(fileDataSourceConfig);
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
