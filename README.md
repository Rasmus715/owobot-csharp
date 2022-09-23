# owobot-csharp
![example branch parameter](https://github.com/Rasmus715/owobot-csharp/actions/workflows/docker-publish.yml/badge.svg?branch=develop)
[![CodeFactor](https://www.codefactor.io/repository/github/rasmus715/owobot-csharp/badge/develop)](https://www.codefactor.io/repository/github/rasmus715/owobot-csharp/overview/develop) <br>
An anime pics bot for Telegram, written on C# using .NET 6, taking data from Booru sites and reddit.   
Бот для Telegram, присылающий аниме девочек, написанный на C# с использованием .NET 6, берущий данные с Booru-сайтов и Reddit.

### Features
* ~~Multithreading~~ Thanks to DI, now I don't need to worry about threads!
* Preventing timeout errors
* User related NSFW settings
* User related language settings (English and Russian)
* Can answer on `owo` and `uwu`

### How to use:
1. Go to [@owopics_junior_bot](https://t.me/owopics_junior_bot) and just use the bot!

If for some reason you want to run it by yourself:

## Parameters description
 - BOT_VERSION - sets up bot version
 - TELEGRAM_TOKEN - bot token retrieved by BotFather
 - REDDIT_APP_ID (Optional), REDDIT_SECRET (Optional), REDDIT_REFRESH_TOKEN (Optional) - data provided by Reddit API. Be aware that if you want to use reddit functions, all three fields should be present.
 - PROXY (Optional) - supports 2 values: ```HTTP```, ```SOCKS5``` depending of the protocol you about to use. If PROXY field is filled, the ones below should be present too.
 - PROXY_ADDRESS - If ```PROXY``` field is equal to```HTTP``` then the address should accord to this format: ```https://example.org```. If ```PROXY``` equals ```SOCKS5```, address should accord to the next format: ```socks5://127.0.0.1```
 - PROXY_PORT - port of your proxy server
 - PROXY_USERNAME, PROXY_PASSWORD (Optional) - credentials of your proxy server 
### Native execution
1. Clone code somewhere
2. Install .NET 6 SDK and type the next command into shell, filling parameters with your own values

```shell
BOT_VERSION= TELEGRAM_TOKEN= REDDIT_APP_ID= REDDIT_SECRET= REDDIT_REFRESH_TOKEN= PROXY= PROXY_ADDRESS= PROXY_USERNAME= PROXY_PASSWORD= dotnet run
```

### docker-compose way
```yaml
   owobot:
      image: etozherasmus/owobot-csharp:latest
      volumes:
        - .Esseintials/:/app/Essentials
      environment:
        - BOT_VERSION=v0.1
        - TELEGRAM_TOKEN=
        - REDDIT_APP_ID= //Optional
        - REDDIT_SECRET= //Optional
        - REDDIT_REFRESH_TOKEN= //Optional
        - PROXY= //Optional
        - PROXY_ADDRESS= //Optional
        - PROXY_USERNAME= //Optional
        - PROXY_PASSWORD= //Optional
```

### docker CLI way
```shell
docker run -d \
  -e BOT_VERSION=v0.1 \
  -e TELEGRAM_TOKEN= \
  -e REDDIT_APP_ID= \
  -e REDDIT_SECRET= \
  -e REDDIT_REFRESH_TOKEN= \
  -e PROXY= \
  -e PROXY_ADDRESS= \
  -e PROXY_USERNAME= \
  -e PROXY_PASSWORD= \
  -v "Essentials:/app/Essentials" \
  etozherasmus/owobot-csharp
```

### Reason of choosing some questionable solutions

First of all, I'm a C# newbie.<br />
The whole idea of this project was to learn how to work with Telegram, Reddit client libraries aswell how to set up EF Core in console application and use local database.


### TODO: 
1. ~~Add external, user-friendly configuration.~~ Done
2. ~~Add chats compatibility.~~ Done. Needs additional testing
3. ~~Push docker image to dockerhub.~~ Done.
4. ~~Add more sources (such as yande.re, konachan)~~

P.S. The reddit API is a slow mess and using `/random` command will always give you pics from booru boards. <br>
To use reddit exclusively, proceed with `/random_reddit` 
