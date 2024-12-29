using System.Text.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;
using System.CodeDom;

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

        public async Task<HttpResponseMessage> Post(string postBody, string? inReplyTo)
        {
            Logger?.LogInformation($"Posting {postBody.Substring(0, 25)} ... to {host} ({inReplyTo})");
            
            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            // Posting to mastodon
            var postUrl = GetApiUrl($"/api/v1/statuses");

            dynamic bodyObject;

            if (string.IsNullOrEmpty(inReplyTo))
            {
                bodyObject = new
                {
                    status = postBody
                };
                
            } else
            {
                bodyObject = new
                {
                    status = postBody,
                    in_reply_to_id = inReplyTo
                };
            }

            var body = new StringContent(JsonSerializer.Serialize(bodyObject), System.Text.Encoding.UTF8, "application/json");

            Logger?.LogInformation($"POST {postUrl} [{postBody}]");

            var postResponse = await httpClient.PostAsync(postUrl, body);

            postResponse.EnsureSuccessStatusCode();

            return postResponse;
        }

        public async Task PostThread(IList<string?> posts)
        {
            var inReplyTo = string.Empty;

            foreach (var post in posts)
            {
                Console.WriteLine(post);
                // get the body of HttpResponseMessage
                var response = await Post(post!, inReplyTo);
                response.EnsureSuccessStatusCode();

                var bodyPlain = await response.Content.ReadAsStringAsync();

                Console.WriteLine(bodyPlain);

                // deserialize
                var body = JsonSerializer.Deserialize<dynamic>(bodyPlain);
                // fix Unhandled exception: Microsoft.CSharp.RuntimeBinder.RuntimeBinderException: 'System.Text.Json.JsonElement' does not contain a definition for 'string'
            
                inReplyTo = body!.GetProperty("id").GetString();

                Console.WriteLine(inReplyTo);
            }
        }
    }
}