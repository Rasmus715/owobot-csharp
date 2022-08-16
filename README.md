# owobot-csharp

An anime pics bot for Telegram, written on C# using .NET 6, taking data from reddit.   
Бот для Telegram, присылающий аниме девочек, написанный на C# с использованием .NET 6, берущий данные с Reddit.

Bot link:  [@owopics_junior_bot](https://t.me/owopics_junior_bot)

### Features
* Multithreading
* Preventing timeout errors
* User related NSFW settings
* User related language settings (english and russian)
* Can answer on `owo` and `uwu`

### How to use:
1. Go to [@owopics_junior_bot](https://t.me/owopics_junior_bot) and just use the bot!

If for some reason you want to run it by yourself:

## Parameters description
 - BOT_VERSION - sets up bot version
 - TELEGRAM_TOKEN - token retrieved by BotFather
 - REDDIT_APP_ID, REDDIT_SECRET, REDDIT_REFRESH_TOKEN - data provided by Reddit API
 - PROXY (Optional) - supports 2 values: ```HTTP```, ```SOCKS5``` depending of the protocol you about to use. If PROXY field is filled, the ones below should be present too.
 - PROXY_ADDRESS - If ```PROXY``` field is equal to```HTTP``` then the address should accord to this format: ```https://example.org```. If ```PROXY``` equals ```SOCKS5```, address should accord to the next format: ```socks5://127.0.0.1```
 - PROXY_PORT - port of your proxy server
 - PROXY_USERNAME, PROXY_PASSWORD (Optional) - credentials of your proxy server 
### Native execution
1. Clone code somewhere
2. Install .NET 6 SDK and type

```shell
TELEGRAM_TOKEN= REDDIT_APP_ID= REDDIT_SECRET= REDDIT_REFRESH_TOKEN= dotnet run
```
filling parameters with your own values

### Docker-Compose method
```yaml
   owobot:
      image: etozherasmus/owobot-csharp:latest
      volumes:
        - .Esseintials/:/app/Essentials
      environment:
        - BOT_VERSION=v0.1
        - TELEGRAM_TOKEN=
        - REDDIT_APP_ID=
        - REDDIT_SECRET=
        - REDDIT_REFRESH_TOKEN=
        - PROXY= //Optional
        - PROXY_ADDRESS= //Optional
        - PROXY_USERNAME= //Optional
        - PROXY_PASSWORD= //Optional
```

### Docker CLI method
```shell
docker run -d \
  -e TELEGRAM_TOKEN=5378235767:AAGNBiePIq5UqZVUKx4-qdeaF7QOZeVI5FM \
  -e REDDIT_APP_ID=uU59EoywSUFJPc6t1DFX-w \
  -e REDDIT_SECRET=XdTT8-a2GON4U9NBQGb0pCs7TAhTQA \
  -e REDDIT_REFRESH_TOKEN=1793999091867-jyFmJj1SLY7JPRBNdFHiH8FOvB4NuQ \
  -v "Essentials:/app/Essentials" \
  etozherasmus/owobot-csharp
```

### Reason of choosing some questionable solutions

I'm a C# newbie

### TODO: 
1. ~~Add external, user-friendly configuration file~~ Done
2. ~~Add chats compatibility~~ Done. Needs additional testing
3. ~~Push docker image to dockerhub~~ Done.
4. Add more sources (such as yande.re, konachan)

