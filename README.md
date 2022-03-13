# Simple XP Bot

Official bot: @simplexpbot in Telegram

This bot helps you to collect "*experience*"

## Install:

```bash
git clone https://github.com/xbt573/simplexpbot
cd simplexpbot/
dotnet run
```

## Usage:

### Help
Bot sends you a help message on /help command

### Language change
At this moment, bot supports only two languages: Russian and English. To change launguage send bot a /lang \[ru | en] command. Note that in groups only administrators can change language!

### Get XP and level
To get XP and level just send bot a /xp command

## Used libraries

### Serilog
Logging library

### Telegram.Bot
Library for working with Telegram Bot API

### System.Data.SQLite
Library for working with SQLite3 databases