using System.Net;
using Microsoft.EntityFrameworkCore;
using owobot_csharp;
using owobot_csharp.Data;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

    var parseSuccessful = true;

    if (!configuration.GetSection("TELEGRAM_TOKEN").Exists() || configuration.GetSection("TELEGRAM_TOKEN").Value.Equals(""))
    {
        Console.WriteLine(@"Telegram Token field in configuration file is not filled.");
        parseSuccessful = false;
    }
    
    if (!configuration.GetSection("REDDIT_APP_ID").Exists() || configuration.GetSection("REDDIT_APP_ID").Value.Equals(""))
    {
        Console.WriteLine(@"Reddit App Id field in configuration file is not filled.");
        parseSuccessful = false;
    }
    
    if (!configuration.GetSection("REDDIT_SECRET").Exists() || configuration.GetSection("REDDIT_SECRET").Value.Equals(""))
    {
        Console.WriteLine(@"Reddit Secret Id field in configuration file is not filled.");
        parseSuccessful = false;
    }
    
    if (!configuration.GetSection("REDDIT_REFRESH_TOKEN").Exists() || configuration.GetSection("REDDIT_REFRESH_TOKEN").Value.Equals(""))
    {
        Console.WriteLine(@"Reddit Refresh Token Id field in configuration file is not filled.");
        parseSuccessful = false;
    }

    if (configuration.GetSection("PROXY").Exists() && configuration.GetSection("PROXY").Value.Equals("HTTP") && configuration.GetSection("PROXY").Value.Equals("SOCKS5"))
    {
        
        if (!configuration.GetSection("PROXY_ADDRESS").Exists() || configuration.GetSection("PROXY_ADDRESS").Value.Equals(""))
        {
             Console.WriteLine(@"Proxy field is filled but no address was provided.");
             parseSuccessful = false;
        }
        
        if (!configuration.GetSection("PROXY_PORT").Exists() || configuration.GetSection("PROXY_PORT").Value.Equals(""))
        {
            Console.WriteLine(@"Proxy field is filled but no port was provided.");
            parseSuccessful = false;
        }

        // if (configuration.GetSection("PROXY_USERNAME").Value.Equals(""))
        // {
        //     Console.WriteLine(@"Proxy field is filled but no username was provided.");
        //     parseSuccessful = false;
        // }
        //
        // if (configuration.GetSection("PROXY_PASSWORD").Value.Equals(""))
        // {
        //     Console.WriteLine(@"Proxy field is filled but no password was provided.");
        //     parseSuccessful = false;
        // }
    }
    
    if (parseSuccessful)
        Console.WriteLine(@"Configuration looks OK.");
    else
    {
        Console.WriteLine(@"Please, fix the errors listed above and try again");
        return 1;
    }

    var usingProxyHttp = false;
var usingProxySocks5 = false;

if (configuration.GetSection("PROXY").Exists())
{

    if (configuration.GetSection("PROXY").Value.Equals("HTTP"))
    {
        Console.WriteLine(@"Using proxies, huh? Cool...");
        Console.WriteLine(
            @"I was too lazy to test their functionality so expect this function to work incorrectly or don't work at all.");

        usingProxyHttp = true;
    }

    else if (configuration.GetSection("PROXY").Value.Equals("SOCKS5"))
    {
        Console.WriteLine(@"Using proxies, huh? Cool...");
        Console.WriteLine(
            @"I was too lazy to test their functionality so expect this function to work incorrectly or don't work at all.");

        usingProxySocks5 = true;
    }
}
//var bot = new TelegramBotClient(deserializedJson!["TelegramToken"]);

    Console.WriteLine(@"Initializing migration...");

if (!Directory.Exists("Essentials"))
{
    Console.WriteLine(@"Creating ""Essentials"" Directory");
    Directory.CreateDirectory("Essentials");
}
        


var applicationContext = new ApplicationContext();
applicationContext.Database.Migrate();
applicationContext.Dispose();
Console.WriteLine(@"Migration successful");

if (usingProxyHttp)
{
    try
    {
        var webProxy = new WebProxy(configuration.GetSection("PROXY_ADDRESS").Value,
            int.Parse(configuration.GetSection("PROXY_PORT").Value))
        {
            // Credentials if needed:
            Credentials = new NetworkCredential(configuration.GetSection("PROXY_USERNAME").Value,
                configuration.GetSection("PROXY_PASSWORD").Value)
        };

        var httpClient = new HttpClient(
            new HttpClientHandler
            {
                Proxy = webProxy, UseProxy = true
            });

        Console.WriteLine(@"Attempting to start the bot...");

        var botClient = new TelegramBotClient(configuration.GetSection("TELEGRAM_TOKEN").Value, httpClient);

        var me = await botClient.GetMeAsync();
        Console.WriteLine($@"Start listening for @{me.Username}");

        await botClient.ReceiveAsync(Handlers.HandleUpdateAsync,
            Handlers.HandleErrorAsync,
            new ReceiverOptions
            {
                ThrowPendingUpdates = true
            });

        return 0;
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException ex)
    {
        Console.WriteLine(@$"Unable to start the bot. Reason: {ex.Message}");
        return 1;
    }

}

if (usingProxySocks5)
{
    try
    {
        var proxy = new WebProxy(
            $"{configuration.GetSection("PROXY_ADDRESS").Value}:{configuration.GetSection("PROXY_PORT").Value}")
        {
            Credentials = new NetworkCredential(configuration.GetSection("PROXY_USERNAME").Value,
                configuration.GetSection("PROXY_PASSWORD").Value)
        };
        var httpClient = new HttpClient(
            new SocketsHttpHandler {Proxy = proxy, UseProxy = true,}
        );

        Console.WriteLine(@"Attempting to start the bot...");

        var botClient = new TelegramBotClient(configuration.GetSection("TELEGRAM_TOKEN").Value, httpClient);

        var me = await botClient.GetMeAsync();
        Console.WriteLine($@"Start listening for @{me.Username}");

        await botClient.ReceiveAsync(Handlers.HandleUpdateAsync,
            Handlers.HandleErrorAsync,
            new ReceiverOptions
            {
                ThrowPendingUpdates = true
            });

        return 0;
    }

    catch (Telegram.Bot.Exceptions.ApiRequestException ex)
    {
        Console.WriteLine(@$"Unable to start the bot. Reason: {ex.Message}");
        return 1;
    }

}


try
{
    Console.WriteLine(@"Attempting to start the bot...");
    var bot = new TelegramBotClient(configuration.GetSection("TELEGRAM_TOKEN").Value);
    
    var me = await bot.GetMeAsync(); 
    
    Console.WriteLine($@"Start listening for @{me.Username}");
    
    await bot.ReceiveAsync(Handlers.HandleUpdateAsync,
        Handlers.HandleErrorAsync,
        new ReceiverOptions
        {
            ThrowPendingUpdates = true
        });

    return 0;
}
catch (Telegram.Bot.Exceptions.ApiRequestException ex)
{
    Console.WriteLine(@$"Unable to start the bot. Reason: {ex.Message}");
    return 1;
}








