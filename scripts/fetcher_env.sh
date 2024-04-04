#!/bin/bash

# Use ";" as the delimiter to separate URLs
IFS=';' read -ra URL_ARRAY <<< "$RSSBOT_FEEDS"

for URL in "${URL_ARRAY[@]}"; do
    ./RssBot fetch-rss --rssUrl "$URL"
done
