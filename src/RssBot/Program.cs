using System.IO;
using System.CommandLine.Parsing;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RssBot;

var serviceCollection = new ServiceCollection();

ConfigureServices(serviceCollection, args);

using var serviceProvider = serviceCollection.BuildServiceProvider();

var db = serviceProvider.GetRequiredService<Db>();

var rootCommand = new RootCommand();

var loggerFactory = serviceProvider.GetService<ILoggerFactory>()!;

var accessTokenOption = new Option<string>("--accessToken", "Access token for Mastodon")
{
    IsRequired = true,
};

var hostOption = new Option<string>("--host", "Mastodon host")
{
    IsRequired = true,
};

var rssUrlOption = new Option<string>("--rssUrl", "RSS Url to fetch")
{
    IsRequired = true,
};

var limitOption = new Option<int>("--top", () => 1, "Number of posts to publish");

var fetchCmd = new Command("fetch-rss", "Fetch and process an RSS endpoint");

fetchCmd.AddOption(rssUrlOption);

fetchCmd.SetHandler((rssUrl) =>
{
    var logger = loggerFactory.CreateLogger<Program>();

    var posts = RssUtils.ParseRss(rssUrl, logger);

    foreach (var post in posts)
    {
        db.InsertOrIgnorePost(post);
    }
}, rssUrlOption);

var postCmd = new Command("post", "Post an article to Mastodon");

postCmd.AddOption(accessTokenOption);
postCmd.AddOption(hostOption);
postCmd.AddOption(limitOption);

postCmd.SetHandler(async (accessToken, host, limit) =>
{
    var logger = loggerFactory.CreateLogger<Program>();

    var mastodonService = new MastodonService(host, accessToken)
    {
        Logger = logger
    };

    try
    {
        logger?.LogInformation($"Posting in {host}");

        logger?.LogInformation($"Getting {limit} posts that has not been posted");

        var sql = "SELECT * FROM posts WHERE status IS NULL ORDER BY published_at ASC LIMIT @limit";
        
        var parameters = new Dictionary<string, object>
        {
            { "@limit", limit }
        };
        
        var posts = db.GetPosts(sql, parameters);

        logger?.LogInformation($"{posts.Count()} retrieved");

        foreach (var post in posts)
        {
            try {
                var postBody = RssUtils.GetContent(post);
                await mastodonService.Post(postBody);
                
                db.MarkPostAsPosted(post);
            } catch (Exception ex)
            {
                logger?.LogCritical(ex.ToString());
                db.MarkPostAsFail(post);
            }
        }
    } catch (Exception ex)
    {
        logger?.LogCritical(ex.ToString());
    }
   
}, accessTokenOption, hostOption, limitOption);

rootCommand.AddCommand(fetchCmd);
rootCommand.AddCommand(postCmd);

var result = await rootCommand.InvokeAsync(args);

return result;

static void ConfigureServices(ServiceCollection serviceCollection, string[] args)
{
    serviceCollection
        .AddLogging(configure =>
        {
            configure.AddSimpleConsole(options => options.TimestampFormat = "hh:mm:ss ");

            if (args.Any("--debug".Contains))
            {
                configure.SetMinimumLevel(LogLevel.Debug);
            }
        })
        .AddSingleton((sp) => 
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            var dbFilePath = Environment.GetEnvironmentVariable("RSSBOT_DB") ?? "rssbot.db";
            string filePath = Path.Combine(currentDirectory, dbFilePath);

            return new Db(filePath);
        });
}