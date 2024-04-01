# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the remaining source code and build the application
COPY src/RssBot/* ./
RUN dotnet publish ./RssBot.csproj -c Release -o "rssbot" -p:PublishSingleFile=false --self-contained true

# Build the runtime image
FROM mcr.microsoft.com/dotnet/runtime:5.0 AS runtime
WORKDIR /opt/rssbot

RUN apt-get update && apt-get -y install cron

# Copy the compiled executable and scripts
COPY --from=build /app/rssbot/* ./
COPY scripts/fetcher_env.sh ./
COPY scripts/post.sh ./

# Set environment variable for Mastodon access token
ENV RSSBOT_DB=/data/rssbot.db

# Set execute permissions on scripts
COPY scripts/cronjobs /etc/crontabs/root
RUN cat /etc/crontabs/root | crontab - 

# Create a volume for shared data
VOLUME /data

# Start the cron service in the foreground
CMD ["cron", "-f"]