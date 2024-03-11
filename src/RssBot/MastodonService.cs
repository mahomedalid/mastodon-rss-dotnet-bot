using System.Text.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;

namespace RssBot
{
    public class MastodonService
    {
        private readonly string? accessToken;

        private readonly string host;

        public ILogger? Logger { get; set; }

        public MastodonService(string host)
        {
            this.host = host;
        }

        public MastodonService(string host, string accessToken)
        {
            this.host = host;
            this.accessToken = accessToken;
        }

        private string GetApiUrl(string endpoint)
        {
            return $"https://{host}/{endpoint}";
        }

        public async Task Post(string postBody)
        {
            Logger?.LogDebug($"Posting {postBody.Substring(0, 25)} ... to {host}");
            
            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            // Posting to mastodon
            var postUrl = GetApiUrl($"/api/v1/statuses");
            
            var body = new StringContent(JsonSerializer.Serialize(new
            {
                status = postBody
            }), System.Text.Encoding.UTF8, "application/json");
            
            Logger?.LogInformation($"POST {postUrl} [{postBody}]");

            var postResponse = await httpClient.PostAsync(postUrl, body);

            postResponse.EnsureSuccessStatusCode();
        }
    }
}