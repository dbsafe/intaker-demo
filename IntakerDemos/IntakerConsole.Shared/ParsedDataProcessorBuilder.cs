using DataProcessor;
using DataProcessor.Contracts;
using DataProcessor.DataSource.File;
using DataProcessor.InputDefinitionFile;
using DataProcessor.Models;
using DataProcessor.ProcessorDefinition;

namespace IntakerConsole.Shared
{
    public static class ParsedDataProcessorBuilder
    {
        public static ParsedDataProcessor10 BuildParsedDataProcessor(string specsPath, string inputPath)
        {
            var inputDefinitionFile = FileLoader.Load<InputDefinitionFile10>(specsPath);
            var fileDataSourceValidFile = BuildFileDataSource(inputPath, inputDefinitionFile);

            var fileProcessorDefinition = FileProcessorDefinitionBuilder.CreateFileProcessorDefinition(inputDefinitionFile);
            return new ParsedDataProcessor10(fileDataSourceValidFile, fileProcessorDefinition);
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
    }
}
