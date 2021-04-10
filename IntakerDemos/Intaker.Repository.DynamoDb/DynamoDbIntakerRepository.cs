using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Intaker.Repository.DynamoDb
{

    public class DynamoDbIntakerRepositoryConfig
    {
        public string TableName { get; set; }
    }

    public class DynamoDbIntakerRepository : IIntakerRepository
    {
        private const string TABLE_ATTRIBUTE_FILE_ID = "FileId";
        private const string TABLE_ATTRIBUTE_ROW_NUMBER = "RowNumber";

        private const string TABLE_ATTRIBUTE_CREATED_ON = "CreatedOn";
        private const string TABLE_ATTRIBUTE_VALIDATION_RESULT = "ValidationResult";
        private const string TABLE_ATTRIBUTE_VALIDATION_DETAIL = "ValidationDetail";
        private const string TABLE_ATTRIBUTE_FILE_NAME = "FileName";
        private const string TABLE_ATTRIBUTE_FILE_PATH = "FilePath";

        private const string TABLE_ATTRIBUTE_ROW_RAW = "Raw";
        private const string TABLE_ATTRIBUTE_ROW_RECORD_TYPE = "RecordType";

        private readonly AmazonDynamoDBClient _dbClient;
        private readonly DynamoDbIntakerRepositoryConfig _config;

        public DynamoDbIntakerRepository(AmazonDynamoDBClient dbClient, DynamoDbIntakerRepositoryConfig config)
        {
            _dbClient = dbClient;
            _config = config;
        }

        public async Task<InsertFileItemResponse> InsertFileItemAsync(InsertFileItemRequest request)
        {
            var fileItem = BuildFileItem(request);

            var table = Table.LoadTable(_dbClient, _config.TableName);
            var item = new Document
            {
                [TABLE_ATTRIBUTE_FILE_ID] = fileItem.Id,
                [TABLE_ATTRIBUTE_ROW_NUMBER] = -1,

                [TABLE_ATTRIBUTE_CREATED_ON] = fileItem.CreatedOn.ToString(CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern),
                [TABLE_ATTRIBUTE_VALIDATION_RESULT] = fileItem.ValidationResult,
                [TABLE_ATTRIBUTE_VALIDATION_DETAIL] = fileItem.ValidationDetail,
                [TABLE_ATTRIBUTE_FILE_NAME] = Path.GetFileName(fileItem.Path),
                [TABLE_ATTRIBUTE_FILE_PATH] = Path.GetDirectoryName(fileItem.Path),
            };

            await table.PutItemAsync(item);

            return new InsertFileItemResponse { FileItem = fileItem };
        }

        private static FileItem BuildFileItem(InsertFileItemRequest request)
        {
            return new FileItem
            {
                CreatedOn = DateTime.UtcNow,
                FileType = request.FileType,
                Id = $"{request.FileType}:{Guid.NewGuid()}",
                Path = request.Path,
                SizeBytes = request.SizeBytes,
                ValidationResult = request.ValidationResult.ToString(),
                ValidationDetail = request.ValidationDetail
            };
        }

        public async Task InsertRowItemsAsync(InsertRowItemsRequest request)
        {
            var batchCount = 1;
            var batch = new List<RowRecord>(25);
            foreach (var RowsRecord in request.RowsRecords)
            {
                batch.Add(RowsRecord);
                if (batch.Count == 25)
                {
                    await WriteBatchAsync(batch, request.FileId, batchCount);
                    batchCount++;
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                await WriteBatchAsync(batch, request.FileId, batchCount);
        }

        private async Task WriteBatchAsync(List<RowRecord> batch, string fileId, int batchCount)
        {
            Console.WriteLine($"Writing batch number {batchCount}");

            var writeRequests = new List<WriteRequest>();
            foreach(var item in batch)
            {
                var doc = BuildRowDocument(item, fileId);

                var writeRequest = new WriteRequest
                {
                    PutRequest = new PutRequest
                    {
                        Item = doc.ToAttributeMap()
                    }
                };

                writeRequests.Add(writeRequest);
            }

            var request = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [_config.TableName] = writeRequests
                }
            };

            await _dbClient.BatchWriteItemAsync(request);
        }

        private Document BuildRowDocument(RowRecord rowRecord, string fileId)
        {
            var createdOn = DateTime.UtcNow;

            var doc = Document.FromJson(rowRecord.Row.Json);

            doc[TABLE_ATTRIBUTE_FILE_ID] = fileId;
            doc[TABLE_ATTRIBUTE_ROW_NUMBER] = rowRecord.Row.Index;

            doc[TABLE_ATTRIBUTE_CREATED_ON] = createdOn.ToString(CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern);
            doc[TABLE_ATTRIBUTE_VALIDATION_RESULT] = rowRecord.Row.ValidationResult.ToString();

            doc[TABLE_ATTRIBUTE_VALIDATION_DETAIL] = rowRecord.ValidationDetail;
            doc[TABLE_ATTRIBUTE_ROW_RAW] = rowRecord.Row.Raw;
            doc[TABLE_ATTRIBUTE_ROW_RECORD_TYPE] = rowRecord.RecordType;

            return doc;
        }
    }
}
