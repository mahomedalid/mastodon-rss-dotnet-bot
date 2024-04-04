FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY src/RssBot/* ./
RUN dotnet publish ./RssBot.csproj -c Release -o "rssbot" -p:PublishSingleFile=false --self-contained true

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS runtime

WORKDIR /opt/rssbot

RUN apt-get update && apt-get -y install cron

COPY --from=build /app/rssbot/* ./
COPY scripts/fetcher_env.sh ./
COPY scripts/post.sh ./

ENV RSSBOT_DB=/data/rssbot.db

COPY scripts/cronjobs /etc/crontabs/root
RUN cat /etc/crontabs/root | crontab - 

VOLUME /data

CMD ["cron", "-f"]