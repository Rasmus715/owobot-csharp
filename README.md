# owobot-csharp

An anime pics bot for Telegram, written on C# using .NET6, taking data from reddit.   
Бот для Telegram, присылающий аниме девочек, написанный на C# с использованием .NET6, берущий данные с Reddit.

Bot link:  [@owopics_junior_bot](https://t.me/owopics_junior_bot)

### Features
* Multithreading
* Preventing timeout errors
* User related NSFW settings
* User related language settings (english and russian)
* Can answer on `owo` and `uwu`

### How to use
1. Go to [@owopics_junior_bot](https://t.me/owopics_junior_bot) and just use the bot!

If for some reason you want to run it by yourself:

1. Clone code somewhere
2. Fill `Configuration.cs` with your credentials
3. Install .NET 6 SDK and type `dotnet run` or just use Docker
6. Run again and enjoy

Or if you want to use Docker:

```shell
docker-compose up -d
```

### Reason of choosing some questionable solutions

I'm a C# newbie

### TODO
1. Add configuration file
2. Add chats compatibility
3. Add more sources (such as yande.re, konachan)

