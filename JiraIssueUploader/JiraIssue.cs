using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JiraIssueUploader
{
    internal class JiraIssue
    {
        private static string? JIRA_DOMAIN;
        private static string? username;
        private static string? apiToken;

        private static string? csvFilePath;

        // Configuration object to load appsettings.json
        private static IConfiguration? Configuration;

        static void JiraIssueConfig()
        {
            // Build configuration from appsettings.json
            Configuration = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                //.AddEnvironmentVariables();
                .Build();

            // Load the Jira settings from appsettings.json
            JIRA_DOMAIN = Configuration["Jira:Domain"] ?? "";
            username = Configuration["Jira:Username"];
            apiToken = Configuration["Jira:ApiToken"];

            csvFilePath = Configuration["Source:CSVFilePath"];
        }

        internal static async Task UploadIssue()
        {
            JiraIssueConfig();

            using (HttpClient client = new HttpClient())
            {
                string url = $"{JIRA_DOMAIN}/rest/api/3/issue";

                Stopwatch stopwatch = Stopwatch.StartNew();
                client.BaseAddress = new Uri(string.IsNullOrEmpty(JIRA_DOMAIN) ? "" : JIRA_DOMAIN);
                var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{apiToken}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                // Reading Items from CSV file
                Colorful.Console.WriteLine($"{Environment.NewLine}Reading items from CSV fie...{Environment.NewLine}", Color.Cyan);
                var issues = ImportData.ReadFromCSV(csvFilePath);

                if (issues.Any())
                {
                    Colorful.Console.WriteLine($"{Environment.NewLine} {issues.Count} items was found ...{Environment.NewLine}", Color.AliceBlue);
                    int counter = 1;

                    var successFailedItems = new Dictionary<string, List<string>>
                    {
                        { "SuccessItems", new List<string>() },
                        { "FailedItems", new List<string>() }
                    };

                    foreach (var issue in issues)
                    {
                        string issueTitle = $" {counter} :  '{issue.IssueSummary}";

                        bool success = await CreateJiraIssueAsync(client, issue, counter);
                        if (success)
                            successFailedItems["SuccessItems"].Add(issueTitle);
                        else
                            successFailedItems["FailedItems"].Add(issueTitle);

                        ShowProgressBar(counter, issues.Count, issueTitle); // Showing progress bar

                        counter++;
                    }

                    Colorful.Console.WriteLine($"{Environment.NewLine} Successful Items...{Environment.NewLine}", Color.Blue);

                    foreach (var item in successFailedItems["SuccessItems"])
                    {
                        Colorful.Console.WriteLine(item);
                    }

                    Colorful.Console.WriteLine($"{Environment.NewLine}Failed Items...{Environment.NewLine}", Color.DarkRed);

                    foreach (var item in successFailedItems["FailedItems"])
                    {
                        Colorful.Console.WriteLine(item);
                    }

                }
                else
                {
                    Colorful.Console.WriteLine($"{Environment.NewLine}Nothing Found! Check JQL Or Configs of you JIRA!", Color.Red);
                }

                stopwatch.Stop();
                Colorful.Console.WriteLine($"{Environment.NewLine}Inserting issues was finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds.");

            }
        }

        /// <summary>
        /// Displays a progress bar in the console.
        /// </summary>
        /// <param name="progress">The current progress.</param>
        /// <param name="total">The total amount of work to be done.</param>
        /// <param name="text">Text to display alongside the progress bar.</param>
        static void ShowProgressBar(int progress, int total, string text)
        {
            double percent = (progress / (double)total) * 100;
            int barLength = 50; // Length of progress bar (based on your idea)
            int filledLength = (int)(barLength * progress / total);

            Console.Write("\r["); // Start progress bar
            Console.Write(new string('█', filledLength)); // Filled portion of the progress
            Console.Write(new string('-', barLength - filledLength)); // Empty portion of the progress
            Console.Write($"] {percent:0.0}% - {text}"); // Displaying percentage
        }

        /// <summary>
        /// inserting jira issue by calling api
        /// </summary>
        /// <param name="client"></param>
        /// <param name="issue"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        static async Task<bool> CreateJiraIssueAsync(HttpClient client, IssueEntry issue, int counter)
        {
            var issueData = new
            {
                fields = new
                {
                    project = new { key = issue.ProjectKey },
                    summary = issue.IssueSummary,
                    description = issue.Description,
                    issuetype = new { name = issue.IssueType },
                    assignee = new { name = issue.Assignee },
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(issueData), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("/rest/api/2/issue", jsonContent);

            return response.IsSuccessStatusCode;
        }
    }
}
