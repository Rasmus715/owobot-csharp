using Microsoft.EntityFrameworkCore;
using owobot_csharp;
using owobot_csharp.Data;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using System.Text.Json;


var deserializedJson = new Dictionary<string, string>();
var configurationPath = $"{Directory.GetCurrentDirectory()}/Configuration.json";
try
{
    deserializedJson = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(File.OpenRead(configurationPath));

    if (deserializedJson == null)
        throw new FileNotFoundException();

    if (deserializedJson.Any(kvp => kvp.Value == ""))
    {
        Console.WriteLine(@"Some values in your configuration file are missing.");
        Console.WriteLine(@"Please, check its contents");
        return 1;
    }
    Console.WriteLine(@"Config file looks OK.");
}
catch (FileNotFoundException)
{
    var initialConfig = new Dictionary<string, string>
    {
        {"BotVersion", "v0.1"},
        {"TelegramToken", ""},
        {"RedditAppId", ""},
        {"RedditSecret", ""},
        {"RedditRefreshToken", ""}
    };

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

var bot = new TelegramBotClient(deserializedJson!["TelegramToken"]);

Console.WriteLine(@"Initializing migration...");
var applicationContext = new ApplicationContext();
applicationContext.Database.Migrate();
applicationContext.Dispose();
Console.WriteLine(@"Migration successful");

try
{
    Console.WriteLine(@"Attempting to start the bot...");
    
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







