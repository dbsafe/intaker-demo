using DataProcessor.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Intaker.Repository.DynamoDb
{
    public interface IIntakerRepository
    {
        Task<InsertFileItemResponse> InsertFileItemAsync(InsertFileItemRequest request);
        Task InsertRowItemsAsync(InsertRowItemsRequest request);
    }

    public class InsertRowItemsRequest
    {
        public string FileId { get; set; }
        public string FileType { get; set; }
        public IEnumerable<RowRecord> RowsRecords { get; set; }
    }

    public class RowRecord
    {
        public Row Row { get; set; }
        public string RecordType { get; set; }
        public string ValidationDetail { get; set; }
    }

    public class InsertFileItemRequest
    {
        public string Path { get; set; }
        public long SizeBytes { get; set; }
        public ValidationResultType ValidationResult { get; set; }
        public string ValidationDetail { get; set; }
        public string FileType { get; set; }
    }

    public class InsertFileItemResponse
    {
        public FileItem FileItem { get; set; }
    }

    public class FileItem
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public long SizeBytes { get; set; }
        public string ValidationResult { get; set; }
        public string ValidationDetail { get; set; }
        public string FileType { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
