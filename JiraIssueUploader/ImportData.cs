using CsvHelper;
using System.Globalization;

namespace JiraIssueUploader
{
    internal class ImportData
    {
        public static List<IssueEntry> ReadFromCSV(string? filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    throw new IOException($"Error in Finding file {Environment.NewLine} Path : {filePath}");

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                return new List<IssueEntry>(csv.GetRecords<IssueEntry>());
            }
            catch (IOException ex)
            {
                throw new IOException($"Error in Reading file {Environment.NewLine} Path : {filePath} {Environment.NewLine}  {ex.Message}", ex);
            }
        }
    }
}