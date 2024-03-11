#!/bin/bash

LIST_FILE="list.txt"

while read LINE; do
    URL=$(echo $LINE | awk '{print $1}')
    RssBot fetch-rss --rssUrl "$URL"
done < $LIST_FILE
