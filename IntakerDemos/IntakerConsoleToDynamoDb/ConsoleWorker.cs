using DataProcessor;
using DataProcessor.Models;
using Intaker.Repository.DynamoDb;
using IntakerConsole.Shared;
using IntakerConsole.Shared.Logger;
using IntakerConsoleToDynamoDb.Configuration;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IntakerConsoleToDynamoDb
{
    public class ConsoleWorker
    {
        private readonly IntakerConsoleAppConfig _intakerConsoleAppConfig;
        private readonly AwsDymanoDbClient _awsDymanoDbClientConfig;
        private readonly IApplicationLogger _logger;
        private readonly IIntakerRepository _intakerRepository;

        public ConsoleWorker(IConfiguration configuration, IApplicationLogger logger, IIntakerRepository intakerRepository)
        {
            _logger = logger;
            _intakerRepository = intakerRepository;

            _intakerConsoleAppConfig = configuration.GetSection(ApplicationConstants.CONFIG_SECTION_INTAKER_CONSOLE_APP).Get<IntakerConsoleAppConfig>();
            _awsDymanoDbClientConfig = configuration.GetSection(ApplicationConstants.CONFIG_SECTION_AWS_DYNAMODB_CLIENT).Get<AwsDymanoDbClient>();

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
                
                var task = Task.Run(async () =>
                {
                    await WriteFileToTableAsync(parsed);
                });

                task.Wait();

            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString());
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
            _logger.Log($"{ApplicationConstants.CONFIG_SECTION_AWS_DYNAMODB_CLIENT}: {JsonConvert.SerializeObject(_awsDymanoDbClientConfig)}");
        }

        private static void ValidatePaths(IntakerConsoleAppConfig config)
        {
            if (!File.Exists(config.SpecsPath))
                throw new FileNotFoundException($"File not found '{config.SpecsPath}'");

            if (!File.Exists(config.InputPath))
                throw new FileNotFoundException($"File not found '{config.InputPath}'");
        }

        private static long GetFileSize(string path)
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.Length;
        }

        private async Task<InsertFileItemResponse> InsertFileItemAsync(ParsedData10 parsedData)
        {
            var sizeBytes = GetFileSize(_intakerConsoleAppConfig.InputPath);
            var initializeFileRequest = new InsertFileItemRequest
            {
                FileType = _intakerConsoleAppConfig.DataType,
                Path = _intakerConsoleAppConfig.InputPath,
                SizeBytes = sizeBytes,
                ValidationResult = parsedData.ValidationResult
            };

            var insertFileItemResponse = await _intakerRepository.InsertFileItemAsync(initializeFileRequest);
            _logger.Log($"FileId: {insertFileItemResponse.FileItem.Id}");

            return insertFileItemResponse;
        }

        private static string BuildRowValidationDetail(Row row, JsonSerializerSettings jsonSerializerSettings)
        {
            var rowValidationDetail = new RowValidationDetail();

            if (row.Errors?.Count > 0)
                rowValidationDetail.Errors = row.Errors.ToArray();

            if (row.Warnings?.Count > 0)
                rowValidationDetail.Warnings = row.Warnings.ToArray();

            return JsonConvert.SerializeObject(rowValidationDetail, jsonSerializerSettings);
        }

        private static void AddHeader(ParsedData10 parsedData, List<RowRecord> rowRecords, JsonSerializerSettings jsonSerializerSettings)
        {
            if (parsedData.Header != null)
            {
                var haderRecord = new RowRecord
                {
                    Row = parsedData.Header,
                    RecordType = "header",
                    ValidationDetail = BuildRowValidationDetail(parsedData.Header, jsonSerializerSettings)
                };

                rowRecords.Add(haderRecord);
            }
        }

        private static void AddTrailer(ParsedData10 parsedData, List<RowRecord> rowRecords, JsonSerializerSettings jsonSerializerSettings)
        {
            if (parsedData.Trailer != null)
            {
                var trailerRecord = new RowRecord
                {
                    Row = parsedData.Trailer,
                    RecordType = "trailer",
                    ValidationDetail = BuildRowValidationDetail(parsedData.Trailer, jsonSerializerSettings)
                };

                rowRecords.Add(trailerRecord);
            }
        }

        private static void AddData(ParsedData10 parsedData, List<RowRecord> rowRecords, JsonSerializerSettings jsonSerializerSettings, string dataType)
        {
            foreach (var dataRow in parsedData.DataRows)
            {
                var record = new RowRecord
                {
                    Row = dataRow,
                    RecordType = dataType,
                    ValidationDetail = BuildRowValidationDetail(dataRow, jsonSerializerSettings)
                };

                rowRecords.Add(record);
            }
        }

        private async Task WriteFileToTableAsync(ParsedData10 parsedData)
        {
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var insertFileItemResponse = await InsertFileItemAsync(parsedData);

            var rowRecords = new List<RowRecord>();
            AddHeader(parsedData, rowRecords, jsonSerializerSettings);
            AddData(parsedData, rowRecords, jsonSerializerSettings, _intakerConsoleAppConfig.DataType);
            AddTrailer(parsedData, rowRecords, jsonSerializerSettings);

            var insertRowItemsRequest = new InsertRowItemsRequest
            {
                FileId = insertFileItemResponse.FileItem.Id,
                FileType = insertFileItemResponse.FileItem.FileType,
                RowsRecords = rowRecords
            };

            await _intakerRepository.InsertRowItemsAsync(insertRowItemsRequest);
        }

        private class RowValidationDetail
        {
            public string[] Errors { get; set; }
            public string[] Warnings { get; set; }
        }
    }
}
