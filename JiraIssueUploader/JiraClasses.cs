namespace JiraIssueUploader
{
    class IssueEntry
    {
        public string? ProjectKey { get; set; }
        public string? IssueSummary { get; set; }
        public string? IssueType { get; set; }
        public string? Description { get; set; }
        public string? Assignee { get; set; }
    }
}
