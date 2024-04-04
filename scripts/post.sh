#!/bin/bash
# Get the current hour in 24-hour format
current_hour=$(date +%H)

# Check if the current hour is between 14 (2pm) and 23 (11pm)
#if [ "$current_hour" -ge 14 ] && [ "$current_hour" -le 23 ]; then
./RssBot post --accessToken "$RSSBOT_ACCESSTOKEN" --host "$RSSBOT_INSTANCEHOST"
#fi