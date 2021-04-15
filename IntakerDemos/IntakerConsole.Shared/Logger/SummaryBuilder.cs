using DataProcessor;
using DataProcessor.Models;
using System.Collections.Generic;
using System.Text;

namespace IntakerConsole.Shared.Logger
{
    public static class SummaryBuilder
    {
        public static string BuildSummary(ParsedData10 parsed)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Summary:");
            sb.AppendLine($"Validation result: {parsed.ValidationResult}");
            sb.AppendLine($"Data row count: {parsed.DataRows.Count}");
            sb.AppendLine($"Invalid data row count: {parsed.InvalidDataRows.Count}");
            sb.AppendLine();

            AddRowWithNameToSummary("Header", parsed.Header, sb);
            sb.AppendLine();

            AddRowWithNameToSummary("Trailer", parsed.Trailer, sb);
            sb.AppendLine();

            return sb.ToString();
        }

        public static string BuildInvalidDataRows(IList<Row> rows)
        {
            if (rows.Count == 0)
            {
                return "Invalid Rows: none";
            }

            var sb = new StringBuilder();
            sb.AppendLine("Invalid Rows:");

            foreach (var row in rows)
            {
                AddRowToSummary(row, sb);
            }

            return sb.ToString();
        }

        private static void AddRowWithNameToSummary(string name, Row row, StringBuilder sb)
        {
            if (row == null)
            {
                sb.AppendLine($"{name}: none");
            }
            else
            {
                sb.AppendLine(name);
                AddRowToSummary(row, sb);
            }
        }

        private static void AddRowToSummary(Row row, StringBuilder sb)
        {
            sb.AppendLine($"Index: {row.Index}, Raw: {row.Raw}");
            sb.AppendLine($"Json: {row.Json}");
            AddListToSummary("Errors", row.Errors, sb);
            AddListToSummary("Warnings", row.Warnings, sb);
        }

        private static void AddListToSummary(string name, IList<string> list, StringBuilder sb)
        {
            if (list.Count > 0)
            {
                sb.AppendLine($"{name}:");
                foreach (var item in list)
                {
                    sb.AppendLine($"\t{item}");
                }
            }
        }
    }
}
