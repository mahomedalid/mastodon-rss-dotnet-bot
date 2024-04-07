using System;
using System.Text;
using System.Numerics;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml.Linq;
using System.CommandLine.Parsing;
using System.CommandLine;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace RssBot
{
    public static class RssUtils
    {
        // TODO: dehardcode this and take it from a configuration file
        public static IDictionary<string, string> Tags = new Dictionary<string, string>() {
            { ".net", "dotnet" },
            { "dotnet", "dotnet" },
            { "maui", "dotnetmaui" },
            { "c#", "dotnet" },
            { "visualstudio", "visualstudio"},
            { "visual studio", "visualstudio"},
            { "azure", "azure"},
            { "cosmosdb", "cosmosdb"},
            { "sql", "sql"},
            { "sqlserver", "sqlserver"},
            { "sql server", "sqlserver"},
            { "asp.net", "aspnet"},
            { "aspnet", "aspnet"},
            { "aspnetcore", "aspnetcore"},
            { "xamarin", "xamarin"},
            { "xamarinforms", "xamarinforms"},
            { "copilot", "copilot"},
            { "github", "github" },
            { "polly", "polly" },
            { "typescript", "typescript" },
            { "javascript", "javascript" },
            { "nodejs", "nodejs" },
            { "react", "react" },
            { "golang", "golang" },
            { "grafana", "grafana" },
            { "semantic kernel", "semantic-kernel" },
            { "blazor", "blazor" },
            { "python", "python" },
            { "ise", "ise" },
            { "identity", "identity" },
            { "entraid", "entraid" },
            { "openai", "openai" },
            { "chatgpt", "chatgpt" },
            { "databricks", "databricks" },
            { "accessibility", "accessibility" }, 
            { "nosql", "nosql" },
            { "swagger", "swagger" },
            { "openapi", "openapi" },
            { "azure devops", "azuredevops" },
            { "devops", "devops" }
        };

        public static string GetContent(Post post)
        {
            //TODO: Dehardcode this and make it a configuration
            var contentTemplate = "{0}\n\n{1}\n\n{2}\n\nBy: {3}\n\n{4}";

            var tagsList = new List<string>() {
                "microsoft"
            };
            
            //Clean html tags from post.Description
            var postDescription = System.Text.RegularExpressions.Regex.Replace(post.Description, "<.*?>", string.Empty);

            // Remove "The .* appeared first on .*"
            postDescription = System.Text.RegularExpressions.Regex.Replace(postDescription, "The .* appeared first on .*", string.Empty);

            // Replace \n+ for a single \n
            postDescription = System.Text.RegularExpressions.Regex.Replace(postDescription, @"\n+", "\n");

            // Fix &#8217;
            postDescription = postDescription.Replace("&#8217;", "'");

            foreach(var tag in RssUtils.Tags) {
                if (postDescription.ToLowerInvariant().Contains(tag.Key) || post.Uri.Contains(tag.Key) || post.Title.Contains(tag.Key)) {
                    tagsList.Add(tag.Value);
                }
            }

            var tagsContent = string.Join(" ", tagsList.Distinct().Select(tag => $"#{tag}"));

            //TODO: Make author an actual reference when it matches a list of author and their accounts

            var content = string.Format(contentTemplate,
                post.Title,
                postDescription,
                post.Uri,
                post.Author,
                tagsContent);

            if (content.Length > 500)
            {
                content = string.Format(contentTemplate,
                    post.Title,
                    // TODO: Make this fancier to not cut words
                    postDescription.Substring(0, 150) + "...",
                    post.Uri,
                    post.Author,
                    tagsContent);
            }

            return content;
        }

        
        public static string GetMention(string name, string link)
        {
            return $"<a href=\"{link}\" class=\"u-url mention\">@<span>{name}</span></a>";
        }

        public static string GetLinkUniqueHash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to a hexadecimal string representation.
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2")); // "x2" means hexadecimal with two digits.
                }

                return sb.ToString();
            }
        }

        public static string? ParsePubDate(string? pubDate)
        {
            if (DateTimeOffset.TryParse(pubDate, out DateTimeOffset parsedDate))
            {
                return parsedDate.ToString("yyyy-MM-ddTHH:mm:sszzz");
            }

            // If parsing fails, return the original string
            return pubDate;
        }

        public static IList<Post> ParseRss(string url, ILogger? logger = null)
        {
            logger?.LogInformation($"Downloading RSS from {url}");

            // Download RSS XML
            string rssXmlString;
            using (HttpClient client = new HttpClient())
            {
                rssXmlString = client.GetStringAsync(url).Result;
            }

            logger?.LogInformation($"Downloaded RSS from {url}");

            // Parse RSS XML
            XDocument rssXml = XDocument.Parse(rssXmlString);

            XNamespace dc = "http://purl.org/dc/elements/1.1/";
            // Extract items from RSS XML
            var items = rssXml.Descendants("item")
                .Select(item => new
                {
                    Title = item.Element("title")?.Value,
                    //TODO: Replace the domain if it is different
                    Link = item.Element("link")?.Value,
                    Description = item.Element("description")?.Value,
                    PubDate = item.Element("pubDate")?.Value,
                    Author = item.Element(dc + "creator")?.Value
                });
                
            // Get summary from the rss
            var summary = rssXml.Descendants("channel")
                .Select(channel => channel.Element("description")?.Value)
                .FirstOrDefault();

            var title = rssXml.Descendants("channel")
                .Select(channel => channel.Element("title")?.Value)
                .FirstOrDefault();

            var posts = new List<Post>();

            foreach (var item in items)
            {
                var post = new Post()
                {
                    Uri = item.Link!,
                    RssUri = url,
                    Title = item.Title!,
                    RssTitle = title!,
                    Description = item.Description!,
                    PublishedAt = item.PubDate!,
                    Author = item.Author!
                };

                posts.Add(post);
            }

            return posts;
        }
    }
}