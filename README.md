# Mastodon RSS Dotnet Bot

## Setup your own fediverse bot from multiple RSS feeds
### A very Simple Command-Line based Mastodon RSS Dotnet Bot

**Sometimes the best things are just the simple things.**

This repository contains a simple RSS bot for Mastodon, implemented in C# using .NET, works in linux/windows/macos. The bot is designed to fetch RSS feeds and post articles to a Mastodon instance.

All the interface is a very simple command-line, and the database is just a local sqlite file. This is the code behind of [@msftdevblogs](https://dotnet.social/@msftdevblogs), and it is scheduled in a very small random linux box with a crontab.

## Features

- Fetch and process RSS feeds from specified URLs.
- Post articles to a Mastodon instance.
- Configurable options for access token, Mastodon host, RSS URL, and limit on posts to publish.

## Getting Started

### Pre-compiled binaries

Download a precompiled binaries from the releases page, decompress it, and optionally add the pat to your system variables.

### Build 

Clone the repository to your local machine:

```bash
git clone https://github.com/mahomedalid/mastodon-rss-dotnet-bot.git
```
Navigate to the repository directory and build the project:

```bash
cd mastodon-rss-dotnet-bot
dotnet build
```

### Docker

Pull the image

```bash
docker pull ghcr.io/mahomedalid/fediverse/mastodon-rss-bot:latest
```

Start the container specifiying the env variables for feeds (comma separated), instance of the bot and access token, ex.

```bash
docker run \
  -e "RSSBOT_FEEDS='https://www.youtube.com/feeds/videos.xml?channel_id=UCVvpATOqqanu2jD5-ttRYLQ'" \
  -e "RSSBOT_INSTANCEHOST=dotnet.social" \
  -e "RSSBOT_ACCESSTOKEN=<myaccesstoken>"
  -d rssbot
```

## Usage

Run the bot using the CLI interface. Here's an example command to fetch and process an RSS endpoint:

```bash
dotnet run fetch-rss --rssUrl <RSS_URL>
```

Replace `<RSS_URL>` with the URL of the RSS feed you want to fetch.
I have added a util script to fetch from multiple sources in a simple txt file, see [scripts/fetcher.sh](scripts/fetcher.sh).

To post articles to Mastodon, use the following command:

```bash
dotnet run post --accessToken <ACCESS_TOKEN> --host <MASTODON_HOST> --top <NUMBER_OF_POSTS>
```

Replace `<ACCESS_TOKEN>` with your Mastodon access token, `<MASTODON_HOST>` with the Mastodon instance host, and `<NUMBER_OF_POSTS>` with the number of posts to publish.

## ROADMAP

Bunch of TODOS to fix, look into the code, including templating, configurable hashtags, skip old posts, tag authors, etc.

## Contributing

Contributions are welcome! If you encounter any bugs or have suggestions for improvements, please feel free to open an issue or submit a pull request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
