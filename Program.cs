using System;
using System.IO;
using System.Net;
using System.Text;
using YouTrackSharp.Infrastructure;
using YouTrackSharp.Issues;
using YouTrackSharp.Projects;

namespace YoutrackExport
{
    internal static class Program
    {
        private static void Main()
        {
            const String username = "your email";
            const String password = "your password";
            const String site = "URL to your YT instance without HTTP";
            const String dest = @"C:\Temp\YtExport\";
            const String sessionId = "sessionId";

            var connection = new Connection(site, 80, false, "youtrack");
            connection.Authenticate(username, password);

            var projectManager = new ProjectManagement(connection);
            var projects = projectManager.GetProjects();
            foreach (var project in projects)
            {
                var issueManager = new IssueManagement(connection);
                var issues = issueManager.GetAllIssuesForProject(project.ShortName);

                foreach (dynamic issue in issues)
                {
                    Console.WriteLine($"Exporting issue: {issue.Id}");

                    var exportIssue = new StringBuilder();

                    exportIssue.AppendLine($"Id: {issue.Id}");
                    foreach (var prop in issue.ToExpandoObject())
                    {
                        if (prop.Value != null && prop.Value.ToString() == "System.String[]")
                        {
                            var propArray = (String[]) prop.Value;
                            var valueArray = String.Join(",", propArray);
                            exportIssue.AppendLine($"{prop.Key}: {valueArray}");
                        }
                        else if (prop.Value != null && prop.Value.ToString() == "System.Object[]")
                        {
                            var propArray = (Object[]) prop.Value;
                            var valueArray = String.Join(",", propArray);
                            exportIssue.AppendLine($"{prop.Key}: {valueArray}");
                        }
                        else
                        {
                            exportIssue.AppendLine($"{prop.Key}: {prop.Value}");
                        }
                    }

                    exportIssue.AppendLine();
                    exportIssue.AppendLine("-- Comments --");

                    var comments = issueManager.GetCommentsForIssue(issue.Id);
                    foreach (var comment in comments)
                    {
                        exportIssue.AppendLine($"{comment.Author} {comment.Created}");
                        exportIssue.AppendLine($"{comment.Text}");
                        exportIssue.AppendLine();
                    }

                    exportIssue.AppendLine("-- Attachments --");

                    foreach (var attachment in issue.Attachments)
                    {
                        String name = attachment.name;
                        String url = attachment.url;
                        String author = attachment.authorLogin;
                        String id = attachment.id;
                        String group = attachment.group;
                        Int64 created = attachment.created;

                        exportIssue.AppendLine($"{author} {created} {id} {group}");
                        exportIssue.AppendLine($"{name}");
                        exportIssue.AppendLine($"{url}");
                        exportIssue.AppendLine();

                        var downloadPath = String.Format("{0}/{1}_{2}_{3}", dest, issue.Id, id, name);

                        using (var webClient = new CookieAwareWebClient())
                        {
                            webClient.CookieContainer.SetCookies(new Uri("http://" + site), "JSESSIONID=" + sessionId + ";");

                            webClient.DownloadFile(url, downloadPath);
                        }
                    }

                    var path = String.Format("{0}/{1}.txt", dest, issue.Id);
                    File.WriteAllText(path, exportIssue.ToString());
                }
            }
        }
    }

    public class CookieAwareWebClient : WebClient
    {
        public CookieAwareWebClient()
        {
            CookieContainer = new CookieContainer();
        }

        public CookieContainer CookieContainer { get; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest) base.GetWebRequest(address);
            request.CookieContainer = CookieContainer;
            return request;
        }
    }
}