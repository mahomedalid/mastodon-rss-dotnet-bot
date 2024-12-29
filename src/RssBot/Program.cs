using System.IO;
using System.CommandLine.Parsing;
using System.CommandLine;
using Microsoft.SemanticKernel;
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
                var createThread = Environment.GetEnvironmentVariable("RSSBOT_CREATE_THREAD")?.Equals("1") ?? false;

                if (createThread) {
                    logger?.LogInformation($"Generating thread ...");
                    var service = serviceProvider.GetRequiredService<GenerateThreadService>();
                    var thread = await RssUtils.GetContentThread(post, service);
            
                    await mastodonService.PostThread(thread!.Select(p => p.ToString()).ToList());
                } else {
                    var postBody = RssUtils.GetContent(post);
                    await mastodonService.Post(postBody, string.Empty);
                }
                
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

            Console.WriteLine("USING DB FILE: " + filePath);
            
            return new Db(filePath);
        }).AddSingleton((sp) => {
            var builder = Kernel.CreateBuilder();

            builder.AddAzureOpenAIChatCompletion(
                      Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL")!,
                      Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
                      Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")!);

            return builder.Build();
        }).AddSingleton((sp) => {
            var kernel = sp.GetRequiredService<Kernel>();

            var kernelFunctions = kernel.CreatePluginFromPromptDirectory("_prompts");

            return new GenerateThreadService(kernel, kernelFunctions["generateThread"]);
        });
}