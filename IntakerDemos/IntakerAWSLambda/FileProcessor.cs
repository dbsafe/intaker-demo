using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using DataProcessor;
using DataProcessor.Contracts;
using DataProcessor.DataSource.InStream;
using DataProcessor.InputDefinitionFile;
using DataProcessor.InputDefinitionFile.Models;
using DataProcessor.Models;
using DataProcessor.ProcessorDefinition;
using DataProcessor.ProcessorDefinition.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static Amazon.S3.Util.S3EventNotification;

namespace IntakerAWSLambda
{
    public class FileProcessorConfiguration
    {
        public string BucketIntakerOut { get; set; }
        public string BucketIntakerFileSpecs { get; set; }
    }

    public class FileProcessor
    {
        private readonly FileProcessorConfiguration _config;
        private readonly IFileProcessorLogger _logger;

        public FileProcessor(IConfiguration configuration, IFileProcessorLogger logger)
        {
            _logger = logger;
            _config = configuration.GetSection(AppConstants.CONFIG_SECTION_FILE_PROCESSOR).Get<FileProcessorConfiguration>();
            LogObject("Configuration - FileProcessor:", _config);
        }

        public async Task ProcessFileAsync(S3EventNotificationRecord record)
        {
            _logger.Log($"Processing file '{record.S3.Object.Key}' from bucket '{record.S3.Bucket.Name}'");

            var intakerFileContent = await LoadIntakerFileAsync(record);
            _logger.Log($"Intaker file content:{Environment.NewLine}{intakerFileContent}");

            var intakerVersion = GetFrameworkVersionFromIntakerFileContent(intakerFileContent);

            switch (intakerVersion)
            {
                case "1.0":
                    await ProcessFileWithVersion10Async(intakerFileContent, record);
                    break;
                default:
                    throw new Exception($"Intaker FrameworkVersion '{intakerVersion}' not supported");
            }

            await ExecuteAsync(() => DeleteFileFromBucketAsync(record));
        }

        private async Task ProcessFileWithVersion10Async(string intakerFileContent, S3EventNotificationRecord record)
        {
            InputDefinitionFile10 inputDefinitionFile = null;
            FileProcessorDefinition10 fileProcessorDefinition = null;

            Execute(() =>
            {
                inputDefinitionFile = FileLoader.LoadFromXml<InputDefinitionFile10>(intakerFileContent);
                fileProcessorDefinition = FileProcessorDefinitionBuilder.CreateFileProcessorDefinition(inputDefinitionFile);
            });

            ParsedData10 parsed = null;

            await ExecuteAsync(async () =>
            {
                var regionEndpoint = RegionEndpoint.GetBySystemName(record.AwsRegion);
                using var client = new AmazonS3Client(regionEndpoint);
                var request = new GetObjectRequest
                {
                    BucketName = record.S3.Bucket.Name,
                    Key = record.S3.Object.Key
                };

                using var response = await client.GetObjectAsync(request);
                var dataSource = BuildDataSource(response.ResponseStream, inputDefinitionFile);

                var processor = new ParsedDataProcessor10(dataSource, fileProcessorDefinition);

                parsed = processor.Process();

                if (record.S3.Object.Size < 5000)
                {
                    LogObject("Parsed Data:", parsed);
                }
                else
                {
                    _logger.Log($"File is too large to log the parsed data");
                }
            });

            var outputFilename = $"{Path.GetFileNameWithoutExtension(record.S3.Object.Key)}-{DateTime.Now:yyyyMMdd-HHmmss}.json";

            await ExecuteAsync(() => CreateOutputFileAsync(record, outputFilename, parsed.DataRows));
        }

        private async Task CreateOutputFileAsync(S3EventNotificationRecord sourceRecord, string outputFilename, IList<Row> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[");

            foreach(var row in rows)
            {
                sb.AppendLine(row.Json);
            }

            sb.AppendLine("]");

            var contentBody = sb.ToString();
            var request = new PutObjectRequest
            {
                BucketName = _config.BucketIntakerOut,
                Key = outputFilename,
                ContentBody = contentBody                
            };

            _logger.Log($"Creating file '{request.Key}' in bucket '{request.BucketName}'");
            var regionEndpoint = RegionEndpoint.GetBySystemName(sourceRecord.AwsRegion);
            using var client = new AmazonS3Client(regionEndpoint);
            await client.PutObjectAsync(request);
        }

        private static IDataSource<ParserContext10> BuildDataSource(Stream stream, InputDefinitionFile10 inputDefinition)
        {
            var streamDataSourceConfig = new StreamDataSourceConfig
            {
                Delimiter = inputDefinition.Delimiter,
                HasFieldsEnclosedInQuotes = inputDefinition.HasFieldsEnclosedInQuotes,
                CommentedOutIndicator = inputDefinition.CommentedOutIndicator
            };

            return new StreamDataSource<ParserContext10>(streamDataSourceConfig, stream);
        }

        private string GetFrameworkVersionFromIntakerFileContent(string intakerFileContent)
        {
            var inputDefinitionFrameworkVersion = FileLoader.LoadFromXml<InputDefinitionFrameworkVersion>(intakerFileContent);
            return inputDefinitionFrameworkVersion.FrameworkVersion;
        }

        private async Task<string> LoadIntakerFileAsync(S3EventNotificationRecord record)
        {
            var intakerFile = BuildFileSpecsName(record.S3.Object.Key);
            var regionEndpoint = RegionEndpoint.GetBySystemName(record.AwsRegion);
            using var client = new AmazonS3Client(regionEndpoint);

            var request = new GetObjectRequest { BucketName = _config.BucketIntakerFileSpecs, Key = intakerFile };
            _logger.Log($"Getting intaker file '{request.Key}' from bucket '{request.BucketName}'");

            using var response = await client.GetObjectAsync(request);
            using var reader = new StreamReader(response.ResponseStream);

            return reader.ReadToEnd();
        }

        private string BuildFileSpecsName(string filename)
        {
            var temp = Path.GetFileNameWithoutExtension(filename);
            var ext = Path.GetExtension(temp);

            if (string.IsNullOrWhiteSpace(ext))
            {
                throw new Exception($"Invalid file name format '{filename}'");
            }

            return $"intaker{ext}.xml";
        }

        private async Task DeleteFileFromBucketAsync(S3EventNotificationRecord record)
        {
            RegionEndpoint regionEndpoint = RegionEndpoint.GetBySystemName(record.AwsRegion);
            using var client = new AmazonS3Client(regionEndpoint);
            var request = new DeleteObjectRequest
            {
                BucketName = record.S3.Bucket.Name,
                Key = record.S3.Object.Key
            };

            _logger.Log($"Deleting file '{request.Key}' from bucket '{request.BucketName}'");
            await client.DeleteObjectAsync(request);
        }

        private Task ExecuteAsync(Func<Task> func, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            try
            {
                return func.Invoke();
            }
            catch (AmazonS3Exception ex)
            {
                _logger.Log($"{caller} - Error encountered on server. {ex}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Log($"{caller} - Unknown encountered on server. {ex}");
                throw;
            }
        }

        private void Execute(Action action, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            try
            {
                action.Invoke();
            }
            catch (AmazonS3Exception ex)
            {
                _logger.Log($"{caller} - Error encountered on server. {ex}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Log($"{caller} - Unknown encountered on server. {ex}");
                throw;
            }
        }

        private void LogObject(string message, object obj)
        {
            _logger.Log($"{message}{Environment.NewLine}{JsonConvert.SerializeObject(obj)}");
        }
    }
}
