using System.Net;
using Microsoft.EntityFrameworkCore;
using owobot_csharp;
using owobot_csharp.Data;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using System.Text.Json;
using Newtonsoft.Json;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;


var deserializedJson = new Configuration();
var configurationPath = $"{Directory.GetCurrentDirectory()}/Configuration.json";
try
{
    var parseSuccessful = true;
    var file = await File.ReadAllTextAsync(configurationPath);
    deserializedJson = JsonConvert.DeserializeObject<Configuration>(file);

    if (deserializedJson == null)
        throw new FileNotFoundException();

    if (deserializedJson.ProxyHttp.Address != "" && deserializedJson.ProxySocks5.Address != "")
    {
        Console.WriteLine(@"Both ProxyHTTP and ProxySOCKS5 fields are filled.");
        Console.WriteLine(@"Please, remove one of the proxy server.");
        return 1;
    }
    
    if (deserializedJson.TelegramToken == "")
    {
        Console.WriteLine(@"Telegram Token field in configuration file is not filled.");
        parseSuccessful = false;
    }
    
    if (deserializedJson.RedditAppId == "")
    {
        Console.WriteLine(@"Reddit App Id field in configuration file is not filled.");
        parseSuccessful = false;
    }
    
    if (deserializedJson.RedditSecret == "")
    {
        Console.WriteLine(@"Reddit Secret Id field in configuration file is not filled.");
        parseSuccessful = false;
    }
    
    if (deserializedJson.RedditRefreshToken == "")
    {
        Console.WriteLine(@"Reddit Refresh Token Id field in configuration file is not filled.");
        parseSuccessful = false;
    }
    
    if (deserializedJson.ProxyHttp.Address != "")
    {
        if (deserializedJson.ProxyHttp.Username != "")
        {
             Console.WriteLine(@"Proxy field is filled but no username was provided.");
             parseSuccessful = false;
        }

        if (deserializedJson.ProxyHttp.Password != "")
        {
            Console.WriteLine(@"Proxy field is filled but no password was provided.");
            parseSuccessful = false;
        }
        
        if (deserializedJson.ProxyHttp.Port != "")
        {
            Console.WriteLine(@"Proxy field is filled but no port was provided.");
            parseSuccessful = false;
        }
    }
    
    if (deserializedJson.ProxySocks5?.Address != "")
    {
        if (deserializedJson.ProxySocks5.Username != "")
        {
            Console.WriteLine(@"Proxy field is filled but no username was provided.");
            parseSuccessful = false;
        }

        if (deserializedJson.ProxySocks5.Password != "")
        {
            Console.WriteLine(@"Proxy field is filled but no password was provided.");
            parseSuccessful = false;
        }
        
        if (deserializedJson.ProxySocks5.Port != "")
        {
            Console.WriteLine(@"Proxy field is filled but no port was provided.");
            parseSuccessful = false;
        }
    }
    
    if (parseSuccessful)
        Console.WriteLine(@"Config file looks OK.");
    else
    {
        Console.WriteLine(@"Please, fix the errors listed above and try again");
        return 1;
    }
}
catch (FileNotFoundException)
{

    var initialConfig = new Configuration();

    var json = JsonSerializer.Serialize(initialConfig, new JsonSerializerOptions
    {
        WriteIndented = true
    });

    await File.WriteAllTextAsync($"{configurationPath}", json);

    Console.WriteLine(@"Successfully created configuration file.");
    Console.WriteLine(@"Please, fill it with your credentials");
    return 1;
}
catch (JsonException)
{
    Console.WriteLine(@"There was an error while parsing your configuration file.");
    Console.WriteLine(@"Please, check its contents");
    return 1;
}

var usingProxyHttp = false;
var usingProxySocks5 = false;

if (deserializedJson.ProxyHttp.Address != "")
{
    Console.WriteLine(@"Using proxies, huh? Cool");
    Console.WriteLine(@"I was too lazy to test their functionality so expect this function to work incorrectly.");
    
    usingProxyHttp = true;
}
else if (deserializedJson.ProxySocks5.Address != "")
{
    Console.WriteLine(@"Using proxies, huh? Cool");
    Console.WriteLine(@"I was too lazy to test their functionality so expect this function to work incorrectly.");

    usingProxySocks5 = true;
}
    //var bot = new TelegramBotClient(deserializedJson!["TelegramToken"]);

Console.WriteLine(@"Initializing migration...");
var applicationContext = new ApplicationContext();
applicationContext.Database.Migrate();
applicationContext.Dispose();
Console.WriteLine(@"Migration successful");

if (usingProxyHttp)
{
    try
    {
        var webProxy = new WebProxy(deserializedJson.ProxyHttp.Address, int.Parse(deserializedJson.ProxyHttp.Port)) {
                // Credentials if needed:
                Credentials = new NetworkCredential(deserializedJson.ProxyHttp.Username, deserializedJson.ProxyHttp.Password)
            };
            var httpClient = new HttpClient(
                new HttpClientHandler { Proxy = webProxy, UseProxy = true, }
            );
        
            Console.WriteLine(@"Attempting to start the bot...");
            
            var botClient = new TelegramBotClient(deserializedJson.TelegramToken, httpClient);
            
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
         var proxy = new WebProxy($"{deserializedJson.ProxySocks5.Address}:{deserializedJson.ProxySocks5.Port}")
            {
                Credentials = new NetworkCredential(deserializedJson.ProxySocks5.Username, deserializedJson.ProxySocks5.Password)
            };
            var httpClient = new HttpClient(
                new SocketsHttpHandler { Proxy = proxy, UseProxy = true, }
            );
            
            Console.WriteLine(@"Attempting to start the bot...");
            
            var botClient = new TelegramBotClient(deserializedJson.TelegramToken, httpClient);
            
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
    var bot = new TelegramBotClient(deserializedJson.TelegramToken);
    
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








