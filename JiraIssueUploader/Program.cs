using Figgle;
using JiraIssueUploader;

Colorful.Console.WriteLine(FiggleFonts.Slant.Render("Jira Issue Uploader !"));

await JiraIssue.UploadIssue();
